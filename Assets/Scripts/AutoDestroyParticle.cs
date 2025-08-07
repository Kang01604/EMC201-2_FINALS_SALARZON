using UnityEngine;

public class AutoDestroyParticle : MonoBehaviour
{
    void Start()
    {
        // Destroy the GameObject after the particle system's main duration has elapsed
        // Make sure your Particle System's "Duration" is set correctly in the Inspector
        Destroy(gameObject, GetComponent<ParticleSystem>().main.duration);
    }
}