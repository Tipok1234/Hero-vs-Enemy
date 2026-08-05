using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class VfxPool : MonoBehaviour
{
    [Serializable]
    private sealed class Entry
    {
        public string id;
        public GameObject prefab;
        [Min(1)] public int initialSize = 4;
        [Min(0.01f)] public float scale = 1f;
        [Min(0f)] public float duration;
    }

    private sealed class RuntimePool
    {
        public readonly GameObject Prefab;
        public readonly float Scale;
        public readonly float Duration;
        public readonly Queue<GameObject> Available = new Queue<GameObject>();

        public RuntimePool(GameObject prefab, float scale, float duration)
        {
            Prefab = prefab;
            Scale = scale;
            Duration = duration;
        }
    }

    [SerializeField] private List<Entry> effects = new List<Entry>();

    private readonly Dictionary<string, RuntimePool> pools =
        new Dictionary<string, RuntimePool>(StringComparer.Ordinal);

    private void Awake()
    {
        foreach (var entry in effects)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.prefab == null)
                continue;

            if (pools.ContainsKey(entry.id))
            {
                Debug.LogWarning($"VFX id '{entry.id}' is configured more than once.", this);
                continue;
            }

            var runtimePool = new RuntimePool(entry.prefab, entry.scale, entry.duration);
            pools.Add(entry.id, runtimePool);

            for (var i = 0; i < entry.initialSize; i++)
                runtimePool.Available.Enqueue(CreateInstance(runtimePool));
        }
    }

    public GameObject Play(
        string id,
        Vector3 position,
        Quaternion rotation,
        Action onFinished = null)
    {
        if (!pools.TryGetValue(id, out var runtimePool))
        {
            Debug.LogWarning($"VFX id '{id}' is not configured.", this);
            onFinished?.Invoke();
            return null;
        }

        var instance = runtimePool.Available.Count > 0
            ? runtimePool.Available.Dequeue()
            : CreateInstance(runtimePool);

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        StartCoroutine(ReturnWhenFinished(runtimePool, instance, particles, onFinished));
        return instance;
    }

    private GameObject CreateInstance(RuntimePool runtimePool)
    {
        var instance = Instantiate(runtimePool.Prefab, transform);
        instance.transform.localScale = runtimePool.Prefab.transform.localScale * runtimePool.Scale;
        instance.SetActive(false);
        return instance;
    }

    private IEnumerator ReturnWhenFinished(
        RuntimePool runtimePool,
        GameObject instance,
        ParticleSystem[] particles,
        Action onFinished)
    {
        yield return null;

        if (runtimePool.Duration > 0f)
        {
            yield return new WaitForSeconds(runtimePool.Duration);
            foreach (var particle in particles)
            {
                if (particle != null)
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        while (instance != null && IsAnyParticleAlive(particles))
            yield return null;

        if (instance == null) yield break;

        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        runtimePool.Available.Enqueue(instance);
        onFinished?.Invoke();
    }

    private static bool IsAnyParticleAlive(ParticleSystem[] particles)
    {
        foreach (var particle in particles)
        {
            if (particle != null && particle.IsAlive(true)) return true;
        }

        return false;
    }
}
