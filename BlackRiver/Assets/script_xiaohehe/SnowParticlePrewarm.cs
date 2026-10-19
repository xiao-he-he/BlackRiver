using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SnowParticlePrewarm : MonoBehaviour
{
    [Min(0f)] public float prewarmSeconds = 10f;

    private void Start()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Simulate(prewarmSeconds, true, true, false);
        particles.Play(true);
    }
}
