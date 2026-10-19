using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class BarrageEmitter : MonoBehaviour
{
    [Tooltip("Existing scene asset; kept inactive, used only as a visual template.")]
    public SpriteRenderer projectileTemplate;
    [Header("弹幕发射调节")]
    [InspectorName("发射间隔（秒/发）")]
    [Tooltip("数值越小，发射越频繁。0.75 表示每 0.75 秒发射一次。")]
    [Min(0.05f)] public float fireInterval = 1.5f;
    [Min(1)] public int projectilesPerShot = 1;
    [Range(0f, 180f)] public float spreadAngle = 30f;
    [InspectorName("弹幕飞行速度（单位/秒）")]
    [Tooltip("调整新发射弹幕的飞行速度，不影响发射频率。")]
    [Min(0.1f)] public float projectileSpeed = 4f;
    [Min(0.1f)] public float projectileLifetime = 8f;
    [Min(0.01f)] public float hitRadius = 0.14f;
    [Tooltip("Local muzzle position. Does not rotate the original turret or Tilemap artwork.")]
    public Vector3 muzzleOffset;
    [Min(0f)] public float muzzleAdvance = 0.5f;

    public int ShotsFired { get; private set; }
    public Vector2 LastAimDirection { get; private set; }
    public Vector3 MuzzlePosition => transform.TransformPoint(muzzleOffset);
    private BarrageEncounter encounter;
    private float nextShot;

    internal void Begin(BarrageEncounter owner)
    {
        encounter = owner;
        ShotsFired = 0;
        nextShot = Mathf.Max(0.05f, fireInterval);
        if (projectileTemplate != null)
            projectileTemplate.gameObject.SetActive(false);
    }

    internal void Tick(float elapsed, float deadline)
    {
        if (encounter == null || encounter.player == null || projectileTemplate == null)
            return;
        while (nextShot <= elapsed && nextShot < deadline)
        {
            Fire();
            nextShot += Mathf.Max(0.05f, fireInterval);
        }
    }

    private void Fire()
    {
        Vector2 direction = encounter.player.HitCenter - (Vector2)MuzzlePosition;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.down;
        LastAimDirection = direction.normalized;
        int count = Mathf.Max(1, projectilesPerShot);
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f,
                i / (float)(count - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * LastAimDirection;
            GameObject bullet = new GameObject(name + "_弹幕");
            SceneManager.MoveGameObjectToScene(bullet, gameObject.scene);
            bullet.transform.position = MuzzlePosition + (Vector3)(shotDirection * muzzleAdvance);
            bullet.transform.localScale = projectileTemplate.transform.lossyScale;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg);
            SpriteRenderer visual = bullet.AddComponent<SpriteRenderer>();
            visual.sprite = projectileTemplate.sprite;
            visual.sharedMaterial = projectileTemplate.sharedMaterial;
            visual.color = projectileTemplate.color;
            visual.flipX = projectileTemplate.flipX;
            visual.flipY = projectileTemplate.flipY;
            visual.sortingLayerID = projectileTemplate.sortingLayerID;
            visual.sortingOrder = Mathf.Max(5, projectileTemplate.sortingOrder);
            bullet.AddComponent<BarrageProjectile>().Initialize(encounter, shotDirection,
                projectileSpeed, projectileLifetime, hitRadius);
        }
        ShotsFired++;
        encounter.RecordShot();
    }
}
