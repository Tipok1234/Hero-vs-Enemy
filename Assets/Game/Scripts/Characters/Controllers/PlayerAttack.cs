using System.Collections;
using UnityEngine;

public sealed class PlayerAttack : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private CharacterAnimator characterAnimator;
    [SerializeField] private Transform weapon;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private ArrowsPool arrowsPool;
    [SerializeField] private bool autoAttack = true;
    [SerializeField, Min(0.05f)] private float targetRefreshInterval = 0.15f;
    [SerializeField, Min(0f)] private float targetHeightOffset = 0.9f;
    [SerializeField, Min(0f)] private float aimRotationSpeed = 12f;

    private float nextAttackTime;
    private float nextTargetRefreshTime;
    private Coroutine weaponPoseRoutine;
    private EnemyHealth currentTarget;
    private CharacterMotor characterMotor;
    private EnemyHealth pendingShotTarget;
    private float pendingShotTime;
    private bool hasPendingShot;

    public EnemyHealth CurrentTarget => currentTarget;

    private void Awake()
    {
        if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
        characterMotor = GetComponent<CharacterMotor>();
        if (weapon == null) weapon = FindDeepChild(transform, "Crossbow_1H");
        if (projectileSpawnPoint == null && weapon != null)
            projectileSpawnPoint = FindDeepChild(weapon, "ProjectileSpawnPoint");
        if (arrowsPool == null) arrowsPool = FindObjectOfType<ArrowsPool>();

        if (config == null)
        {
            Debug.LogError("PlayerAttack requires a PlayerConfig.", this);
            enabled = false;
            return;
        }

        SetWeaponRotationX(config.WeaponBaseRotationX);
    }

    private void Update()
    {
        FirePendingShotIfReady();

        if (autoAttack)
        {
            RefreshTargetIfNeeded();
            if (IsMoving())
            {
                if (characterMotor != null) characterMotor.ClearFacingDirection();
            }
            else
            {
                AimAtCurrentTarget();
                if (IsTargetAvailable(currentTarget)) TryAttack(currentTarget);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            RefreshTarget(true);
            TryAttack(currentTarget);
        }
    }

    public bool TryAttack()
    {
        RefreshTargetIfNeeded();
        return TryAttack(currentTarget);
    }

    public bool TryAttack(EnemyHealth target)
    {
        if (IsMoving()) return false;
        if (!IsTargetAvailable(target)) return false;
        if (Time.time < nextAttackTime) return false;
        nextAttackTime = Time.time + config.AttackInterval;
        if (characterAnimator != null)
            characterAnimator.PlayAttack();

        pendingShotTarget = target;
        pendingShotTime = Time.time + config.ProjectileSpawnDelay;
        hasPendingShot = true;
        Debug.Log($"[ArrowDebug] Shot scheduled. target={target.name}, delay={config.ProjectileSpawnDelay:F2}, player={transform.position}", this);

        if (weaponPoseRoutine != null) StopCoroutine(weaponPoseRoutine);
        weaponPoseRoutine = StartCoroutine(AnimateWeaponPose());
        return true;
    }

    private bool IsMoving()
    {
        return characterMotor != null &&
               (characterMotor.IsMoving || !characterMotor.IsGrounded);
    }

    private void FireProjectile(EnemyHealth target)
    {
        if (arrowsPool == null)
            arrowsPool = FindObjectOfType<ArrowsPool>();

        if (arrowsPool == null || projectileSpawnPoint == null)
        {
            Debug.LogWarning("PlayerAttack cannot fire: arrows pool or spawn point is missing.", this);
            return;
        }

        if (!IsTargetAvailable(target))
        {
            RefreshTarget(true);
            target = currentTarget;
            if (!IsTargetAvailable(target)) return;
        }

        var targetPosition = target.transform.position + Vector3.up * targetHeightOffset;
        var shotDirection = targetPosition - projectileSpawnPoint.position;
        if (shotDirection.sqrMagnitude < 0.0001f) return;

        var arrow = arrowsPool.Get();
        Debug.Log($"[ArrowDebug] Launch requested. arrow={arrow.GetInstanceID()}, spawn={projectileSpawnPoint.position}, target={targetPosition}, speed={config.ProjectileSpeed:F2}", this);
        arrow.Launch(
            projectileSpawnPoint.position,
            shotDirection.normalized,
            config.ProjectileSpeed,
            config.AttackRange,
            config.AttackDamage,
            transform);
    }

    private void FirePendingShotIfReady()
    {
        if (!hasPendingShot || Time.time < pendingShotTime) return;

        hasPendingShot = false;
        var target = pendingShotTarget;
        pendingShotTarget = null;
        Debug.Log($"[ArrowDebug] Pending shot ready. target={(target != null ? target.name : "NULL")}", this);
        FireProjectile(target);
    }

    private void RefreshTargetIfNeeded()
    {
        if (Time.time < nextTargetRefreshTime) return;
        RefreshTarget(false);
    }

    private void RefreshTarget(bool force)
    {
        if (!force && Time.time < nextTargetRefreshTime && IsTargetAvailable(currentTarget)) return;
        nextTargetRefreshTime = Time.time + targetRefreshInterval;

        currentTarget = null;
        var closestDistanceSqr = config.AttackRange * config.AttackRange;
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (!IsTargetAvailable(enemy)) continue;

            var distanceSqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > closestDistanceSqr) continue;

            closestDistanceSqr = distanceSqr;
            currentTarget = enemy;
        }
    }

    private bool IsTargetAvailable(EnemyHealth target)
    {
        if (target == null ||
            !target.gameObject.activeInHierarchy ||
            target.IsDead ||
            !target.IsCombatActive) return false;
        return (target.transform.position - transform.position).sqrMagnitude <=
               config.AttackRange * config.AttackRange;
    }

    private void AimAtCurrentTarget()
    {
        if (!IsTargetAvailable(currentTarget))
        {
            if (characterMotor != null) characterMotor.ClearFacingDirection();
            return;
        }

        var direction = currentTarget.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        if (characterMotor != null)
        {
            characterMotor.SetFacingDirection(direction);
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            aimRotationSpeed * Time.deltaTime);
    }

    private IEnumerator AnimateWeaponPose()
    {
        if (weapon == null) yield break;

        yield return RotateWeaponTo(config.WeaponAttackRotationX, config.WeaponPoseBlendDuration);

        if (config.WeaponAttackPoseDuration > 0f)
            yield return new WaitForSeconds(config.WeaponAttackPoseDuration);

        yield return RotateWeaponTo(config.WeaponBaseRotationX, config.WeaponPoseBlendDuration);
        weaponPoseRoutine = null;
    }

    private IEnumerator RotateWeaponTo(float targetX, float duration)
    {
        var startRotation = weapon.localRotation;
        var euler = weapon.localEulerAngles;
        var targetRotation = Quaternion.Euler(targetX, euler.y, euler.z);

        if (duration <= 0f)
        {
            weapon.localRotation = targetRotation;
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            weapon.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        weapon.localRotation = targetRotation;
    }

    private void SetWeaponRotationX(float rotationX)
    {
        if (weapon == null) return;
        var euler = weapon.localEulerAngles;
        weapon.localRotation = Quaternion.Euler(rotationX, euler.y, euler.z);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        weaponPoseRoutine = null;
        currentTarget = null;
        hasPendingShot = false;
        pendingShotTarget = null;
        if (characterMotor != null) characterMotor.ClearFacingDirection();
        if (config != null) SetWeaponRotationX(config.WeaponBaseRotationX);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var found = FindDeepChild(child, childName);
            if (found != null) return found;
        }

        return null;
    }
}
