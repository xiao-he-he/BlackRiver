using System.Collections.Generic;
using UnityEngine;

/// <summary>Sweeps the full travelled segment; only the player's body counts, not GroundCheck.</summary>
public sealed class BarrageProjectile : MonoBehaviour
{
    private BarrageEncounter encounter;
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private float radius;
    private bool consumed;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    public void Initialize(BarrageEncounter owner, Vector2 travelDirection,
        float travelSpeed, float maxLifetime, float hitRadius)
    {
        encounter = owner;
        direction = travelDirection.normalized;
        speed = travelSpeed;
        lifetime = maxLifetime;
        radius = hitRadius;
        owner.Register(this);
    }

    private void FixedUpdate()
    {
        if (consumed || encounter == null || !encounter.IsRunning)
        {
            Destroy(gameObject);
            return;
        }
        float distance = speed * Time.fixedDeltaTime;
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        hits.Clear();
        Physics2D.CircleCast(transform.position, radius, direction, filter, hits, distance);
        foreach (RaycastHit2D hit in hits)
            if (hit.collider == encounter.player.BodyCollider)
            {
                consumed = true;
                encounter.player.TryTakeHit(direction);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
        transform.position += (Vector3)(direction * distance);
        lifetime -= Time.fixedDeltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (encounter != null)
            encounter.Unregister(this);
    }
}
