using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(move))]
public sealed class BarragePlayerHealth : MonoBehaviour
{
    [Min(1)] public int maxHits = 2;
    [Min(0f)] public float invulnerabilityDuration = 0.65f;
    [Min(0.01f)] public float redFlashDuration = 0.22f;
    [Min(0f)] public float knockbackSpeed = 1.8f;
    [Min(0f)] public float shakeDuration = 0.15f;
    [Min(0f)] public float shakeMagnitude = 0.07f;
    public UnityEvent onHit = new UnityEvent();

    public int HitsTaken { get; private set; }
    public int HitsRemaining => Mathf.Max(0, maxHits - HitsTaken);
    public bool IsDead { get; private set; }
    public Collider2D BodyCollider { get; private set; }
    public Vector2 HitCenter => BodyCollider != null ?
        (Vector2)BodyCollider.bounds.center : (Vector2)transform.position;
    private BarrageEncounter encounter;
    private move movement;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private float invulnerableUntil;
    private float flashUntil;
    private bool flashing;

    private void Awake()
    {
        movement = GetComponent<move>();
        BodyCollider = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;
    }

    internal void Bind(BarrageEncounter owner) => encounter = owner;

    public bool TryTakeHit(Vector2 travelDirection)
    {
        if (encounter == null || !encounter.IsRunning || IsDead ||
            Time.time < invulnerableUntil)
            return false;
        HitsTaken++;
        invulnerableUntil = Time.time + invulnerabilityDuration;
        flashUntil = Time.time + redFlashDuration;
        flashing = true;
        ApplyFlash();
        CameraShake2D.ShakeMainCamera(shakeDuration, shakeMagnitude);
        movement.ApplyKnockback(travelDirection.normalized * knockbackSpeed);
        if (HitsTaken >= maxHits)
        {
            IsDead = true;
            encounter.PlayerDied();
            movement.enabled = false;
            StartCoroutine(RestartAfterFeedback());
        }
        onHit.Invoke();
        return true;
    }

    private void LateUpdate()
    {
        if (!flashing)
            return;
        if (Time.time < flashUntil)
            ApplyFlash();
        else
            RestoreColors();
    }

    private void ApplyFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null && renderers[i] != movement.jumpShadow)
                renderers[i].color = new Color(1f, 0.15f, 0.15f, originalColors[i].a);
    }

    private void RestoreColors()
    {
        flashing = false;
        if (renderers == null)
            return;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null && renderers[i] != movement.jumpShadow)
                renderers[i].color = originalColors[i];
    }

    private IEnumerator RestartAfterFeedback()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, redFlashDuration));
        movement.RestartJumpParkScene();
    }

    private void OnDisable() => RestoreColors();
}
