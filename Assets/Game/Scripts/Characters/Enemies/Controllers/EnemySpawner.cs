using System.Collections.Generic;
using Managers;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemySpawnerConfig config;
    [SerializeField] private Transform player;
    [SerializeField] private string spawnVfxId = "EnemySpawn";
    [SerializeField, Min(0f)] private float spawnVfxHeight = 0.1f;
    [SerializeField, Min(1f)] private float groundProbeHeight = 20f;
    [SerializeField, Min(0.1f)] private float spawnVisibilityHeight = 1f;

    private readonly List<EnemyHealth> pooledEnemies = new();
    private readonly List<EnemySpawnerConfig.EnemyEntry> validEntries = new();
    private readonly Dictionary<EnemyHealth, EnemySpawnerConfig.EnemyEntry> enemyEntries = new();
    private readonly RaycastHit[] spawnHits = new RaycastHit[32];
    private VfxPool vfxPool;
    private Transform environmentRoot;
    private float nextPopulationCheckTime;
    private DifficultyController difficultyController;

    private void Awake()
    {
        vfxPool = FindObjectOfType<VfxPool>();
        difficultyController = FindObjectOfType<DifficultyController>();
        if (difficultyController != null)
            difficultyController.WaveChanged += OnWaveChanged;
        var environment = GameObject.Find("Environment");
        if (environment != null) environmentRoot = environment.transform;

        if (config == null)
        {
            Debug.LogError("EnemySpawner requires an EnemySpawnerConfig.", this);
            enabled = false;
            return;
        }

        CacheValidEntries();
        if (validEntries.Count == 0)
        {
            Debug.LogError("EnemySpawnerConfig does not contain any valid enemy prefabs.", this);
            enabled = false;
            return;
        }

        PrewarmPools();
    }

    private void Start()
    {
        if (player == null)
        {
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null) player = playerHealth.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("EnemySpawner is waiting for the player.", this);
            enabled = false;
            return;
        }

        EnsureMinimumPopulation();
    }

    private void OnDestroy()
    {
        if (difficultyController != null)
            difficultyController.WaveChanged -= OnWaveChanged;
    }

    private void Update()
    {
        if (Time.time < nextPopulationCheckTime) return;
        nextPopulationCheckTime = Time.time + config.PopulationCheckInterval;
        EnsureMinimumPopulation();
    }

    private void CacheValidEntries()
    {
        validEntries.Clear();
        foreach (var entry in config.Enemies)
        {
            if (entry == null || entry.Prefab == null || entry.Config == null) continue;
            validEntries.Add(entry);
        }
    }

    private void PrewarmPools()
    {
        foreach (var entry in validEntries)
        {
            for (var i = 0; i < entry.PoolSize; i++)
                CreateEnemy(entry);
        }
    }

    private EnemyHealth CreateEnemy(EnemySpawnerConfig.EnemyEntry entry)
    {
        var enemy = Instantiate(entry.Prefab, transform);
        enemy.name = entry.Prefab.name;
        enemy.gameObject.SetActive(false);
        pooledEnemies.Add(enemy);
        enemyEntries[enemy] = entry;
        return enemy;
    }

    private void EnsureMinimumPopulation()
    {
        var activeEnemies = 0;
        foreach (var enemy in pooledEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                activeEnemies++;
        }

        var desiredEnemyCount = difficultyController != null
            ? difficultyController.Current.EnemyCount
            : config.MinimumActiveEnemies;
        var missingEnemies = desiredEnemyCount - activeEnemies;
        for (var i = 0; i < missingEnemies; i++)
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (!TryFindSpawnPosition(out var spawnPosition)) return;

        var enemy = FindInactiveEnemy();
        if (enemy == null)
        {
            var entry = validEntries[Random.Range(0, validEntries.Count)];
            enemy = CreateEnemy(entry);
        }

        var directionToPlayer = player.position - spawnPosition;
        directionToPlayer.y = 0f;
        var rotation = directionToPlayer.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up)
            : Quaternion.identity;

        enemy.transform.SetPositionAndRotation(spawnPosition, rotation);
        enemy.gameObject.SetActive(true);
        var difficulty = difficultyController != null
            ? difficultyController.Current
            : new DifficultySnapshot(1, config.MinimumActiveEnemies, 1f, 1f, 1f);
        enemy.PrepareForSpawn(enemyEntries[enemy].Config, false, difficulty);

        if (vfxPool == null)
        {
            enemy.ActivateCombat();
            return;
        }

        var effectPosition = spawnPosition + Vector3.up * spawnVfxHeight;
        vfxPool.Play(spawnVfxId, effectPosition, Quaternion.identity, enemy.ActivateCombat);
    }

    private EnemyHealth FindInactiveEnemy()
    {
        if (pooledEnemies.Count == 0) return null;

        var startIndex = Random.Range(0, pooledEnemies.Count);
        for (var i = 0; i < pooledEnemies.Count; i++)
        {
            var enemy = pooledEnemies[(startIndex + i) % pooledEnemies.Count];
            if (enemy != null && !enemy.gameObject.activeSelf) return enemy;
        }

        return null;
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;
        for (var i = 0; i < config.SpawnPositionAttempts; i++)
        {
            var direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;

            var distance = Random.Range(config.MinimumSpawnDistance, config.MaximumSpawnDistance);
            var candidate = player.position + new Vector3(direction.x, 0f, direction.y) * distance;
            if (!TryProjectToGround(candidate, out candidate)) continue;
            if (!HasLineOfSightTo(candidate)) continue;

            var clearanceCenter = candidate + Vector3.up;
            if (config.SpawnClearanceRadius <= 0f ||
                !Physics.CheckSphere(clearanceCenter, config.SpawnClearanceRadius,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryProjectToGround(Vector3 candidate, out Vector3 groundedPosition)
    {
        groundedPosition = default;
        var origin = new Vector3(
            candidate.x,
            Mathf.Max(player.position.y, candidate.y) + groundProbeHeight,
            candidate.z);
        var hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            spawnHits,
            groundProbeHeight * 2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        var nearestDistance = float.PositiveInfinity;
        for (var i = 0; i < hitCount; i++)
        {
            var hitCollider = spawnHits[i].collider;
            if (ShouldIgnoreSpawnHit(hitCollider)) continue;
            if (spawnHits[i].distance >= nearestDistance) continue;

            nearestDistance = spawnHits[i].distance;
            groundedPosition = spawnHits[i].point;
        }

        return nearestDistance < float.PositiveInfinity;
    }

    private bool HasLineOfSightTo(Vector3 candidate)
    {
        var origin = player.position + Vector3.up * spawnVisibilityHeight;
        var targetPoint = candidate + Vector3.up * spawnVisibilityHeight;
        var offset = targetPoint - origin;
        var distance = offset.magnitude;
        if (distance <= 0.001f) return true;

        var hitCount = Physics.RaycastNonAlloc(
            origin,
            offset / distance,
            spawnHits,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < hitCount; i++)
        {
            if (!ShouldIgnoreSpawnHit(spawnHits[i].collider)) return false;
        }

        return true;
    }

    private bool ShouldIgnoreSpawnHit(Collider hitCollider)
    {
        if (hitCollider == null) return true;
        if (hitCollider.GetComponentInParent<CharacterHealth>() != null) return true;
        return environmentRoot != null && hitCollider.transform.IsChildOf(environmentRoot);
    }

    public void BeginSpawning()
    {
        enabled = true;
        nextPopulationCheckTime = 0f;
        EnsureMinimumPopulation();
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    private void OnWaveChanged(DifficultySnapshot difficulty)
    {
        foreach (var enemy in pooledEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead) continue;
            enemy.ApplyDifficulty(difficulty);
        }

        if (enabled) EnsureMinimumPopulation();
    }

    public void ResetAllEnemies()
    {
        enabled = false;
        foreach (var enemy in pooledEnemies)
        {
            if (enemy == null) continue;
            enemy.StopAllCoroutines();
            enemy.gameObject.SetActive(false);
        }
    }
}
