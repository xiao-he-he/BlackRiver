using UnityEngine;

[DisallowMultipleComponent]
public sealed class StaffGrantTrigger : MonoBehaviour
{
    public Collider2D grantTrigger;

    private void OnTriggerEnter2D(Collider2D other) => Grant(other);
    private void OnTriggerStay2D(Collider2D other) => Grant(other);

    private void Grant(Collider2D other)
    {
        if (grantTrigger == null || !grantTrigger.enabled || !grantTrigger.isTrigger ||
            other == null || other.isTrigger || !grantTrigger.IsTouching(other))
            return;
        StaffAbility ability = other.GetComponentInParent<StaffAbility>();
        if (ability != null)
            ability.AcquireStaff();
    }
}
