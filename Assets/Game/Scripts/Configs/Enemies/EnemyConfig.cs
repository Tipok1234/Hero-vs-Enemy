using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Hero vs Enemy/Enemy Config")]
public sealed class EnemyConfig : CharacterConfig
{
    [Header("Presentation")]
    [SerializeField] private GameObject modelPrefab;

    [Header("AI")]
    [SerializeField, Min(0f)] private float detectionRange = 10f;
    [SerializeField, Min(0f)] private float attackRange = 1.5f;
    [SerializeField, Min(0f)] private float stoppingDistance = 1.2f;
    [SerializeField, Min(0f)] private float attackHitDelay = 0.35f;

    public GameObject ModelPrefab => modelPrefab;
    public float DetectionRange => detectionRange;
    public float AttackRange => attackRange;
    public float StoppingDistance => stoppingDistance;
    public float AttackHitDelay => attackHitDelay;
}
