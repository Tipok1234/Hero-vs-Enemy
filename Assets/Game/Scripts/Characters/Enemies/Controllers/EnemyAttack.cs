using System.Collections;
using UnityEngine;

public sealed class EnemyAttack : MonoBehaviour
{
    [SerializeField] private EnemyConfig config;
    [SerializeField] private CharacterHealth target;

    private CharacterAnimator characterAnimator;
    private float nextAttackTime;
    private Coroutine attackRoutine;
    private float damageMultiplier = 1f;

    public bool IsAttacking => attackRoutine != null;

    private void Awake()
    {
        characterAnimator = GetComponent<CharacterAnimator>();
    }

    private void Update()
    {
        if (config == null || target == null || target.IsDead || IsAttacking) return;
        if (Time.time < nextAttackTime) return;

        var offset = target.transform.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > config.AttackRange * config.AttackRange) return;

        if (offset.sqrMagnitude > 0.0001f)
        {
            var targetRotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.RotationSpeed * Time.deltaTime);
        }

        TryAttack();
    }

    public void Configure(EnemyConfig enemyConfig, CharacterHealth attackTarget = null)
    {
        config = enemyConfig;
        damageMultiplier = 1f;
        if (attackTarget != null) target = attackTarget;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetTarget(CharacterHealth attackTarget)
    {
        target = attackTarget;
    }

    private void TryAttack()
    {
        var attackVariant = Random.Range(0, 3);
        if (characterAnimator != null)
            characterAnimator.PlayAttack(attackVariant);

        nextAttackTime = Time.time + config.AttackInterval;
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (config.AttackHitDelay > 0f)
            yield return new WaitForSeconds(config.AttackHitDelay);

        if (target != null && !target.IsDead)
        {
            var offset = target.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= config.AttackRange * config.AttackRange)
                target.TakeDamage(config.AttackDamage * damageMultiplier);
        }

        attackRoutine = null;
    }

    private void OnDisable()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
    }
}
