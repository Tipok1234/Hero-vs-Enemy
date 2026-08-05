using System;
using UnityEngine;

public class CharacterHealth : MonoBehaviour, IDamageable, IHealth
{
    [SerializeField] private CharacterConfig config;

    private float currentHealth;
    private float maxHealthMultiplier = 1f;
    private CharacterAnimator characterAnimator;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => config != null ? config.MaxHealth * maxHealthMultiplier : 0f;
    public bool IsDead => currentHealth <= 0f;

    protected virtual void Awake()
    {
        characterAnimator = GetComponent<CharacterAnimator>();

        if (config == null)
        {
            Debug.LogError($"{GetType().Name} requires a CharacterConfig.", this);
            enabled = false;
            return;
        }

        currentHealth = MaxHealth;
    }

    public void RestoreFullHealth()
    {
        if (config == null) return;

        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void Configure(CharacterConfig characterConfig, bool restoreHealth = true)
    {
        config = characterConfig;
        maxHealthMultiplier = 1f;
        if (restoreHealth) RestoreFullHealth();
    }

    public void SetMaxHealthMultiplier(float multiplier, bool preserveHealthRatio)
    {
        var previousMaxHealth = MaxHealth;
        var healthRatio = previousMaxHealth > 0f ? currentHealth / previousMaxHealth : 1f;
        maxHealthMultiplier = Mathf.Max(1f, multiplier);
        currentHealth = preserveHealthRatio ? MaxHealth * healthRatio : MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (!enabled || damage <= 0f || IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        if (IsDead)
        {
            OnDied();
            Died?.Invoke();
        }
        else if (characterAnimator != null) characterAnimator.PlayHit();
    }

    protected virtual void OnDied()
    {
        if (characterAnimator != null) characterAnimator.PlayDeath();
        Debug.Log($"{name} died.", this);
    }
}
