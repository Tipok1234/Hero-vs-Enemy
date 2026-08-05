using UnityEngine;
using UnityEngine.Serialization;

public abstract class CharacterConfig : ScriptableObject
{
    [Header("Survivability")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float rotationSpeed = 14f;
    [SerializeField] private float gravity = -20f;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float animationDampTime = 0.1f;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackDamage = 20f;
    [FormerlySerializedAs("shotsPerSecond")]
    [SerializeField, Min(0.01f)] private float attacksPerSecond = 1.25f;

    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float Gravity => gravity;
    public float AnimationDampTime => animationDampTime;
    public float AttackDamage => attackDamage;
    public float AttacksPerSecond => attacksPerSecond;
    public float AttackInterval => 1f / attacksPerSecond;
}
