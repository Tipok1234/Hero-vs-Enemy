using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Hero vs Enemy/Player Config")]
public sealed class PlayerConfig : CharacterConfig
{
    [Header("Player Movement")]
    [SerializeField, Range(0f, 1f)] private float inputDeadZone = 0.1f;

    [Header("Ranged Attack")]
    [SerializeField, Min(0f)] private float attackRange = 12f;
    [SerializeField, Min(0f)] private float projectileSpeed = 18f;
    [SerializeField, Min(0f)] private float projectileSpawnDelay = 0.2f;
    [SerializeField, Min(1)] private int projectilePoolSize = 12;

    [Header("Weapon Pose")]
    [SerializeField] private float weaponBaseRotationX = -90f;
    [SerializeField] private float weaponAttackRotationX = -180f;
    [SerializeField, Min(0f)] private float weaponPoseBlendDuration = 0.08f;
    [SerializeField, Min(0f)] private float weaponAttackPoseDuration = 0.55f;

    public float InputDeadZone => inputDeadZone;
    public float AttackRange => attackRange;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileSpawnDelay => projectileSpawnDelay;
    public int ProjectilePoolSize => projectilePoolSize;
    public float WeaponBaseRotationX => weaponBaseRotationX;
    public float WeaponAttackRotationX => weaponAttackRotationX;
    public float WeaponPoseBlendDuration => weaponPoseBlendDuration;
    public float WeaponAttackPoseDuration => weaponAttackPoseDuration;
}
