using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WindPushZone : MonoBehaviour
{
    public Vector2 direction = Vector2.left;

    private static readonly HashSet<WindPushZone> activeZones = new HashSet<WindPushZone>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<PushBox> boxes = new HashSet<PushBox>();
    private BoxCollider2D area;

    public bool IsWindActive =>
        isActiveAndEnabled && area != null && area.enabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetZones()
    {
        activeZones.Clear();
    }

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
    }

    private void OnEnable()
    {
        area = GetComponent<BoxCollider2D>();
        activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
        boxes.Clear();
        overlaps.Clear();
    }

    public bool Affects(PushBox box)
    {
        if (!IsWindActive || box == null || !box.isActiveAndEnabled)
            return false;

        Collider2D boxCollider = box.GetComponentInChildren<Collider2D>();
        if (boxCollider == null)
            return area.OverlapPoint(box.AnchorPosition);

        return area.bounds.Intersects(boxCollider.bounds);
    }

    public static bool IsBoxAffected(PushBox box)
    {
        foreach (WindPushZone zone in activeZones)
        {
            if (zone != null && zone.Affects(box))
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackBox(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrackBox(other);
    }

    private void TrackBox(Collider2D other)
    {
        PushBox box = other.GetComponentInParent<PushBox>();
        if (Affects(box))
        {
            boxes.Add(box);
        }
    }

    private void FixedUpdate()
    {
        if (area == null || !area.enabled)
        {
            boxes.Clear();
            return;
        }

        boxes.Clear();
        PushBox[] sceneBoxes =
            FindObjectsByType<PushBox>(FindObjectsInactive.Exclude);

        foreach (PushBox box in sceneBoxes)
        {
            if (Affects(box))
            {
                boxes.Add(box);
            }
        }

        foreach (PushBox box in boxes)
        {
            box.TryWindPush(direction, this);
        }
    }
}
