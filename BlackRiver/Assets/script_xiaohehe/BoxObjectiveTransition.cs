using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class BoxObjectiveTransition : MonoBehaviour
{
    public Collider2D[] markerPoints;
    public next transition;
    [Min(0f)] public float delayAfterComplete;
    public bool unlockOnly;
    public UnityEvent onComplete = new UnityEvent();
    public bool IsComplete { get; private set; }

    private void OnEnable()
    {
        StopAllCoroutines();
        IsComplete = false;
        if (transition != null && unlockOnly)
            transition.locked = true;
    }

    private void Start()
    {
        if (markerPoints == null || markerPoints.Length == 0)
            markerPoints = GetComponentsInChildren<Collider2D>(true)
                .Where(c => c.isTrigger && c.name.Contains("供奉箱标记点")).ToArray();
        Physics2D.SyncTransforms();
        Refresh();
    }

    private void Update()
    {
        if (!IsComplete)
            Refresh();
    }

    private void Refresh()
    {
        if (markerPoints == null || markerPoints.Length == 0 ||
            !markerPoints.All(IsMarkerFilled))
            return;
        IsComplete = true;
        if (transition != null)
        {
            if (unlockOnly)
                transition.Unlock();
            else if (delayAfterComplete > 0f)
                StartCoroutine(DelayedTransition());
            else
                transition.TryTransition();
        }
        onComplete.Invoke();
    }

    private bool IsMarkerFilled(Collider2D marker)
    {
        if (marker == null || !marker.enabled || !marker.gameObject.activeInHierarchy ||
            marker.gameObject.scene != gameObject.scene)
            return false;
        foreach (Collider2D hit in Physics2D.OverlapPointAll(marker.bounds.center))
        {
            // Marker names also contain "供奉箱": names alone self-complete every level.
            if (hit == marker || hit.isTrigger || hit.gameObject.scene != gameObject.scene)
                continue;
            PushBox box = hit.GetComponentInParent<PushBox>();
            if (box != null && box.isActiveAndEnabled &&
                box.gameObject.scene == gameObject.scene &&
                (box.isSpecialBox || box.name.StartsWith("供奉箱_")) &&
                !box.IsMoving)
                return true;
        }
        return false;
    }

    private IEnumerator DelayedTransition()
    {
        yield return new WaitForSeconds(delayAfterComplete);
        transition.TryTransition();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsComplete = false;
    }
}
