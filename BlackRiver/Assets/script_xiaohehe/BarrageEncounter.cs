using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Scene-local survival encounter. Scene routing belongs to its consumer.</summary>
[DisallowMultipleComponent]
public sealed class BarrageEncounter : MonoBehaviour
{
    [Min(0.1f)] public float survivalDuration = 20f;
    [Tooltip("No survival deadline. Used only by the endless survival scene.")]
    public bool endlessMode;
    public BarragePlayerHealth player;
    public BarrageEmitter[] emitters;
    [Tooltip("Connect the future unified scene manager here. No scene is loaded automatically.")]
    public UnityEvent onSurvived = new UnityEvent();
    public UnityEvent onPlayerDied = new UnityEvent();

    public event Action<BarrageEncounter> Survived;
    public float Elapsed { get; private set; }
    public float Remaining => endlessMode ? float.PositiveInfinity :
        Mathf.Max(0f, survivalDuration - Elapsed);
    public bool IsRunning { get; private set; }
    public bool HasSurvived { get; private set; }
    public int TotalShotsFired { get; private set; }
    public int TotalProjectilesSpawned { get; private set; }
    private readonly List<BarrageProjectile> projectiles = new List<BarrageProjectile>();

    private void Start()
    {
        if (player == null || emitters == null || emitters.Length == 0)
        {
            Debug.LogError("Barrage encounter needs a player and at least one emitter.", this);
            enabled = false;
            return;
        }
        player.Bind(this);
        foreach (BarrageEmitter emitter in emitters)
            if (emitter != null)
                emitter.Begin(this);
        IsRunning = true;
    }

    private void Update()
    {
        if (!IsRunning || Time.deltaTime <= 0f)
            return;
        Elapsed = endlessMode ? Elapsed + Time.deltaTime :
            Mathf.Min(survivalDuration, Elapsed + Time.deltaTime);
        // Dispatch only scheduled shots BEFORE the deadline, even on a long frame.
        foreach (BarrageEmitter emitter in emitters)
            if (emitter != null && emitter.isActiveAndEnabled)
                emitter.Tick(Elapsed, endlessMode ? float.PositiveInfinity : survivalDuration);
        if (!endlessMode && Elapsed >= survivalDuration && !player.IsDead)
        {
            IsRunning = false;
            HasSurvived = true;
            ClearProjectiles();
            Debug.Log("Barrage survived: " + gameObject.scene.name, this);
            onSurvived.Invoke();
            Survived?.Invoke(this);
        }
    }

    public void PlayerDied()
    {
        if (!IsRunning)
            return;
        IsRunning = false;
        ClearProjectiles();
        onPlayerDied.Invoke();
    }

    internal void RecordShot() => TotalShotsFired++;

    internal void Register(BarrageProjectile projectile)
    {
        projectiles.Add(projectile);
        TotalProjectilesSpawned++;
    }

    internal void Unregister(BarrageProjectile projectile) => projectiles.Remove(projectile);

    private void ClearProjectiles()
    {
        foreach (BarrageProjectile projectile in projectiles.ToArray())
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }
        projectiles.Clear();
    }

    private void OnDisable()
    {
        IsRunning = false;
        ClearProjectiles();
    }
}
