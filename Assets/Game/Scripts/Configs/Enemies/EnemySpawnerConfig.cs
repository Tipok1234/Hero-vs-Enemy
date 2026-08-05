using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnerConfig", menuName = "Hero vs Enemy/Enemy Spawner Config")]
public sealed class EnemySpawnerConfig : ScriptableObject
{
    [Serializable]
    public sealed class EnemyEntry
    {
        [SerializeField] private EnemyHealth prefab;
        [SerializeField] private EnemyConfig config;
        [SerializeField, Min(1)] private int poolSize = 5;

        public EnemyHealth Prefab => prefab;
        public EnemyConfig Config => config;
        public int PoolSize => Mathf.Max(1, poolSize);
    }

    [SerializeField] private List<EnemyEntry> enemies = new();
    [SerializeField, Min(1)] private int minimumActiveEnemies = 5;
    [SerializeField, Min(0.05f)] private float populationCheckInterval = 0.25f;
    [SerializeField, Min(0f)] private float minimumSpawnDistance = 8f;
    [SerializeField, Min(0f)] private float maximumSpawnDistance = 9.5f;
    [SerializeField, Min(1)] private int spawnPositionAttempts = 12;
    [SerializeField, Min(0f)] private float spawnClearanceRadius = 0.6f;

    public IReadOnlyList<EnemyEntry> Enemies => enemies;
    public int MinimumActiveEnemies => Mathf.Max(1, minimumActiveEnemies);
    public float PopulationCheckInterval => Mathf.Max(0.05f, populationCheckInterval);
    public float MinimumSpawnDistance => Mathf.Max(0f, minimumSpawnDistance);
    public float MaximumSpawnDistance => Mathf.Max(MinimumSpawnDistance, maximumSpawnDistance);
    public int SpawnPositionAttempts => Mathf.Max(1, spawnPositionAttempts);
    public float SpawnClearanceRadius => Mathf.Max(0f, spawnClearanceRadius);
}
