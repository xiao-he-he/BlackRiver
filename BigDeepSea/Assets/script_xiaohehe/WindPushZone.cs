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
        return isActiveAndEnabled && area != null && area.enabled &&
            box != null && box.isActiveAndEnabled && area.OverlapPoint(box.AnchorPosition);
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
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        area.Overlap(filter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            TrackBox(other);
        }

        foreach (PushBox box in boxes)
        {
            box.TryWindPush(direction, this);
        }
    }
}
