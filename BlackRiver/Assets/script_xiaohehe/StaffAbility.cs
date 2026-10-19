using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Opt-in player ability. Future scenes only need this component and their own art references.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(move))]
public sealed class StaffAbility : MonoBehaviour
{
    // Session state survives scene loads/restarts, but deliberately does not modify a save file.
    public static bool UnlockedThisSession { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => UnlockedThisSession = false;

    public KeyCode castKey = KeyCode.V;
    [Min(0.01f)] public float castDuration = 1f;
    public GameObject downIdle, upIdle, leftIdle, rightIdle;
    public GameObject downWalk, upWalk, leftWalk, rightWalk;
    public GameObject downCast, upCast, leftCast, rightCast;
    [Tooltip("Full textures are used because some source frames have been sliced into fragments.")]
    public Texture2D[] laserFrames;
    public Material laserMaterial;
    public LayerMask raycastLayers = ~0;
    public Vector2 originOffset = new Vector2(0f, -0.3f);
    [Min(0.01f)] public float laserWidth = 0.22f;
    [Min(1f)] public float laserFramesPerSecond = 12f;
    public UnityEvent onStaffAcquired = new UnityEvent();

    public bool HasStaff => UnlockedThisSession;
    public bool IsCasting { get; private set; }
    public GameObject CastState { get; private set; }
    public Vector2 CastDirection { get; private set; }
    public Vector2 BeamOrigin => (Vector2)transform.position + originOffset;
    public Vector2 BeamEnd { get; private set; }
    public Collider2D BlockingCollider { get; private set; }
    public int WheelsActivatedThisCast => activatedWheels.Count;
    public float CastElapsed { get; private set; }
    private move movement;
    private SpriteRenderer beam;
    private Sprite[] frames;
    private readonly HashSet<PrayerWheelInteraction> activatedWheels =
        new HashSet<PrayerWheelInteraction>();
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private GameObject[] states;

    private void Awake()
    {
        movement = GetComponent<move>();
        states = new[] {downIdle, upIdle, leftIdle, rightIdle, downWalk, upWalk,
            leftWalk, rightWalk, downCast, upCast, leftCast, rightCast};
        HideStatesExcept(null);
    }

    public bool AcquireStaff()
    {
        if (HasStaff)
            return false;
        UnlockedThisSession = true;
        onStaffAcquired.Invoke();
        return true;
    }

    public bool TryGetLocomotionState(Vector2 direction, bool walking, out GameObject state)
    {
        state = !HasStaff ? null : Select(direction,
            walking ? downWalk : downIdle, walking ? upWalk : upIdle,
            walking ? leftWalk : leftIdle, walking ? rightWalk : rightIdle);
        return state != null;
    }

    public void HideStatesExcept(GameObject target)
    {
        if (states == null)
            states = new[] {downIdle, upIdle, leftIdle, rightIdle, downWalk, upWalk,
                leftWalk, rightWalk, downCast, upCast, leftCast, rightCast};
        foreach (GameObject state in states)
            if (state != null && state.activeSelf != (state == target))
                state.SetActive(state == target);
    }

    public bool TryCast()
    {
        if (!isActiveAndEnabled || !HasStaff || IsCasting || Time.timeScale <= 0f ||
            movement == null || !movement.isActiveAndEnabled || movement.IsJumping)
            return false;
        CastDirection = movement.FacingDirection;
        CastState = Select(CastDirection, downCast, upCast, leftCast, rightCast);
        if (CastState == null || laserFrames == null || laserFrames.Length == 0)
            return false;
        EnsureBeam();
        CastElapsed = 0f;
        activatedWheels.Clear();
        IsCasting = true;
        HideStatesExcept(CastState);
        Animator animator = CastState.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();
            animator.Update(0f);
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            animator.speed = clips.Length > 0 ? clips[0].length / castDuration : 1f;
        }
        UpdateBeam();
        return true;
    }

    // Called by move before choosing a visual state, so movement and casting cannot fight.
    public void TickCasting()
    {
        if (!IsCasting || Time.deltaTime <= 0f)
            return;
        CastElapsed += Time.deltaTime;
        if (CastElapsed >= castDuration)
        {
            CancelCast();
            return;
        }
        UpdateBeam();
    }

    private void UpdateBeam()
    {
        Physics2D.SyncTransforms();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(raycastLayers);
        filter.useTriggers = true;
        hits.Clear();
        // Gameplay ray has no range limit. Visual length is only clipped to the visible world.
        Physics2D.CircleCast(BeamOrigin, laserWidth * 0.5f, CastDirection,
            filter, hits, Mathf.Infinity);
        hits.Sort((a, b) => a.distance.CompareTo(b.distance));
        BlockingCollider = null;
        float stopDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D collider = hit.collider;
            if (IgnoreCollider(collider))
                continue;
            if (IsLaserBlocker(collider))
            {
                stopDistance = hit.distance;
                BlockingCollider = collider;
                break;
            }
        }
        foreach (RaycastHit2D hit in hits)
        {
            // The blocking wheel itself must still receive the spell.
            if (hit.distance > stopDistance)
                break;
            if (IgnoreCollider(hit.collider))
                continue;
            PrayerWheelInteraction wheel = ResolveWheel(hit.collider);
            if (wheel != null && !activatedWheels.Contains(wheel) && wheel.TryActivateBySpell())
            {
                activatedWheels.Add(wheel);
                // Rotation can change the sail's physical silhouette immediately.
                // Re-query before considering any target farther along the beam.
                UpdateBeam();
                return;
            }
        }
        float length = float.IsPositiveInfinity(stopDistance) ? VisibleBeamLength() : stopDistance;
        BeamEnd = BeamOrigin + CastDirection * length;
        float visualLength = Mathf.Min(length, VisibleBeamLength());
        Vector2 visualEnd = BeamOrigin + CastDirection * visualLength;
        int frame = (int)(CastElapsed * laserFramesPerSecond) % frames.Length;
        beam.sprite = frames[frame];
        beam.transform.position = (BeamOrigin + visualEnd) * 0.5f;
        beam.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(CastDirection.y, CastDirection.x) * Mathf.Rad2Deg - 90f);
        beam.size = new Vector2(laserWidth, Mathf.Max(0.001f, visualLength));
        beam.enabled = visualLength > 0.001f;
    }

    private bool IgnoreCollider(Collider2D collider) =>
        collider == null || collider.gameObject.scene != gameObject.scene ||
        collider.transform.IsChildOf(transform);

    public static bool IsLaserBlocker(Collider2D collider)
    {
        if (collider == null)
            return false;
        // Wheel bodies are authored as triggers, but still stop and receive the beam.
        if (ResolveWheel(collider) != null)
            return true;

        // Wind zones, grid cells and interaction volumes are not physical obstacles.
        if (collider.isTrigger || collider.GetComponentInParent<PushWinTrigger>() != null)
            return false;
        if (collider.GetComponentInParent<PushBox>() != null ||
            collider.GetComponentInParent<WindSailController>() != null)
            return true;

        // Static props have no gameplay component. Check their own/parent prop names,
        // never generic BoxCollider2D types or the wall container named "碰撞箱".
        for (Transform current = collider.transform; current != null; current = current.parent)
        {
            string propName = current.name;
            if (propName.Contains("标记") || propName.Contains("目标"))
                return false;
            if (propName.StartsWith("普通箱子") || propName.StartsWith("供奉箱") ||
                propName.StartsWith("转经筒") || propName.StartsWith("风帆") ||
                propName.StartsWith("石头") || propName.StartsWith("木桩"))
                return true;
        }
        return false;
    }

    private static PrayerWheelInteraction ResolveWheel(Collider2D collider)
    {
        PrayerWheelSpellReceiver receiver =
            collider.GetComponentInParent<PrayerWheelSpellReceiver>();
        return receiver != null ? receiver.wheel :
            collider.GetComponentInParent<PrayerWheelInteraction>();
    }

    private float VisibleBeamLength()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return 64f;
        float halfHeight = camera.orthographic ? camera.orthographicSize : 32f;
        return Mathf.Max(64f, Vector2.Distance(BeamOrigin, camera.transform.position) +
            halfHeight * Mathf.Sqrt(1f + camera.aspect * camera.aspect) * 2f);
    }

    private void EnsureBeam()
    {
        if (beam != null)
            return;
        frames = new Sprite[laserFrames.Length];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(laserFrames[i],
                new Rect(0, 0, laserFrames[i].width, laserFrames[i].height),
                Vector2.one * 0.5f, 50f, 0, SpriteMeshType.FullRect);
        GameObject visual = new GameObject("StaffLaser");
        visual.transform.SetParent(transform, false);
        beam = visual.AddComponent<SpriteRenderer>();
        beam.sharedMaterial = laserMaterial;
        beam.drawMode = SpriteDrawMode.Tiled;
        beam.tileMode = SpriteTileMode.Continuous;
        beam.sortingOrder = 10;
        beam.enabled = false;
    }

    private static GameObject Select(Vector2 direction, GameObject down, GameObject up,
        GameObject left, GameObject right) =>
        direction.y > 0f ? up : direction.y < 0f ? down : direction.x < 0f ? left : right;

    private void CancelCast()
    {
        IsCasting = false;
        if (beam != null)
            beam.enabled = false;
        HideStatesExcept(null);
        CastState = null;
    }

    private void OnDisable() => CancelCast();

    private void OnDestroy()
    {
        if (frames != null)
            foreach (Sprite frame in frames)
                if (frame != null)
                    Destroy(frame);
    }
}
