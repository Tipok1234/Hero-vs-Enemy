using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
public sealed class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyConfig config;
    [SerializeField] private Transform target;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0.1f)] private float obstacleCheckDistance = 2f;
    [SerializeField, Min(0.05f)] private float obstacleCheckRadius = 0.35f;
    [SerializeField, Min(0f)] private float obstacleCheckHeight = 0.8f;
    [SerializeField, Range(5f, 30f)] private float avoidanceAngleStep = 30f;

    private CharacterMotor motor;
    private CharacterAnimator characterAnimator;
    private EnemyAttack enemyAttack;
    private readonly RaycastHit[] obstacleHits = new RaycastHit[16];
    private float avoidanceSide;

    private void Awake()
    {
        avoidanceSide = GetInstanceID() % 2 == 0 ? 1f : -1f;
        motor = GetComponent<CharacterMotor>();
        characterAnimator = GetComponent<CharacterAnimator>();
        if (characterAnimator == null)
            characterAnimator = gameObject.AddComponent<CharacterAnimator>();
        enemyAttack = GetComponent<EnemyAttack>();
        if (enemyAttack == null) enemyAttack = gameObject.AddComponent<EnemyAttack>();

        if (config == null)
        {
            Debug.LogError("EnemyMovement requires an EnemyConfig.", this);
            enabled = false;
            return;
        }

        Configure(config);
    }

    public void Configure(EnemyConfig enemyConfig)
    {
        if (enemyConfig == null) return;

        config = enemyConfig;
        if (motor != null) motor.Configure(config);
        if (characterAnimator != null) characterAnimator.Configure(config.AnimationDampTime);
        if (enemyAttack != null) enemyAttack.Configure(config);
    }

    private void Start()
    {
        if (target == null)
        {
            var player = FindObjectOfType<PlayerHealth>();
            if (player != null)
            {
                target = player.transform;
                enemyAttack.SetTarget(player);
            }
        }
        else
        {
            var healthTarget = target.GetComponent<CharacterHealth>();
            if (healthTarget != null) enemyAttack.SetTarget(healthTarget);
        }
    }

    private void Update()
    {
        if (target == null)
        {
            motor.Move(Vector3.zero);
            return;
        }

        var offset = target.position - transform.position;
        offset.y = 0f;
        var distance = offset.magnitude;

        var stoppingDistance = Mathf.Max(config.StoppingDistance, config.AttackRange);
        var shouldMove = !enemyAttack.IsAttacking &&
                         distance <= config.DetectionRange &&
                         distance > stoppingDistance;
        var moveDirection = shouldMove
            ? FindMovementDirection(offset.normalized)
            : Vector3.zero;
        motor.Move(moveDirection);
        if (characterAnimator != null)
            characterAnimator.SetMovement(moveDirection.sqrMagnitude > 0.0001f ? 1f : 0f, Vector2.up);
    }

    private Vector3 FindMovementDirection(Vector3 targetDirection)
    {
        if (!HasObstacle(targetDirection)) return targetDirection;

        for (var step = 1; step <= 3; step++)
        {
            var angle = avoidanceAngleStep * step;
            var preferredDirection = RotateDirection(targetDirection, angle * avoidanceSide);
            if (!HasObstacle(preferredDirection)) return preferredDirection;

            var oppositeDirection = RotateDirection(targetDirection, -angle * avoidanceSide);
            if (!HasObstacle(oppositeDirection))
            {
                avoidanceSide *= -1f;
                return oppositeDirection;
            }
        }

        return Vector3.zero;
    }

    private bool HasObstacle(Vector3 direction)
    {
        var origin = transform.position + Vector3.up * obstacleCheckHeight;
        var hitCount = Physics.SphereCastNonAlloc(
            origin,
            obstacleCheckRadius,
            direction,
            obstacleHits,
            obstacleCheckDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < hitCount; i++)
        {
            var hitCollider = obstacleHits[i].collider;
            if (hitCollider == null || hitCollider.transform.root == transform.root) continue;
            if (hitCollider.GetComponentInParent<CharacterHealth>() != null) continue;
            return true;
        }

        return false;
    }

    private static Vector3 RotateDirection(Vector3 direction, float angle)
    {
        return Quaternion.AngleAxis(angle, Vector3.up) * direction;
    }
}
