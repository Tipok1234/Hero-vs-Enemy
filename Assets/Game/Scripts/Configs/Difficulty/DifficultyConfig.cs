using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Hero vs Enemy/Difficulty Config")]
public sealed class DifficultyConfig : ScriptableObject
{
    [SerializeField, Min(1f)] private float waveDuration = 30f;
    [SerializeField, Min(1)] private int baseEnemyCount = 5;
    [SerializeField, Min(0)] private int enemiesPerWave = 1;
    [SerializeField, Min(1)] private int maximumEnemyCount = 12;
    [SerializeField, Min(0f)] private float healthIncreasePerWave = 0.1f;
    [SerializeField, Min(0f)] private float speedIncreasePerWave = 0.05f;
    [SerializeField, Min(0f)] private float damageIncreasePerWave = 0.1f;
    [SerializeField, Min(1f)] private float maximumHealthMultiplier = 2.5f;
    [SerializeField, Min(1f)] private float maximumSpeedMultiplier = 1.6f;
    [SerializeField, Min(1f)] private float maximumDamageMultiplier = 2.5f;

    public float WaveDuration => Mathf.Max(1f, waveDuration);

    public DifficultySnapshot GetWave(int wave)
    {
        wave = Mathf.Max(1, wave);
        var completedWaves = wave - 1;
        return new DifficultySnapshot(
            wave,
            Mathf.Min(maximumEnemyCount, baseEnemyCount + completedWaves * enemiesPerWave),
            Mathf.Min(maximumHealthMultiplier, 1f + completedWaves * healthIncreasePerWave),
            Mathf.Min(maximumSpeedMultiplier, 1f + completedWaves * speedIncreasePerWave),
            Mathf.Min(maximumDamageMultiplier, 1f + completedWaves * damageIncreasePerWave));
    }
}

public readonly struct DifficultySnapshot
{
    public DifficultySnapshot(int wave, int enemyCount, float health, float speed, float damage)
    {
        Wave = wave;
        EnemyCount = enemyCount;
        HealthMultiplier = health;
        SpeedMultiplier = speed;
        DamageMultiplier = damage;
    }

    public int Wave { get; }
    public int EnemyCount { get; }
    public float HealthMultiplier { get; }
    public float SpeedMultiplier { get; }
    public float DamageMultiplier { get; }
}
