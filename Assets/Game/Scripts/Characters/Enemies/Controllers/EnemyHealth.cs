using System.Collections;
using System;
using UnityEngine;

public sealed class EnemyHealth : CharacterHealth
{
    public static event Action<EnemyHealth> EnemyDied;

    [SerializeField, Min(0f)] private float disableDelayAfterDeath = 1.2f;
    [SerializeField] private GameObject healthCanvasPrefab;
    [SerializeField] private string deathVfxId = "EnemyDeath";
    [SerializeField, Min(0f)] private float deathVfxHeight = 0.8f;

    private GameObject healthCanvasInstance;
    private VfxPool vfxPool;
    private bool isCombatActive = true;

    public bool IsCombatActive => isCombatActive;

    protected override void Awake()
    {
        base.Awake();
        vfxPool = FindObjectOfType<VfxPool>();
        EnsureHealthCanvas();
    }

    protected override void OnDied()
    {
        isCombatActive = false;
        base.OnDied();
        EnemyDied?.Invoke(this);
        if (vfxPool != null)
        {
            var effectPosition = transform.position + Vector3.up * deathVfxHeight;
            vfxPool.Play(deathVfxId, effectPosition, Quaternion.identity);
        }

        var movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = false;
        var attack = GetComponent<EnemyAttack>();
        if (attack != null) attack.enabled = false;
        StartCoroutine(DisableAfterDeath());
    }

    public void PrepareForSpawn(
        EnemyConfig config,
        bool activateCombat = true,
        DifficultySnapshot difficulty = default)
    {
        StopAllCoroutines();
        isCombatActive = activateCombat;
        Configure(config);
        EnsureHealthCanvas();
        if (healthCanvasInstance != null) healthCanvasInstance.SetActive(true);

        var movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.Configure(config);
            movement.enabled = activateCombat;
        }

        var attack = GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.Configure(config);
            attack.enabled = activateCombat;
        }

        var characterAnimator = GetComponent<CharacterAnimator>();
        if (characterAnimator != null) characterAnimator.ResetState();
        ApplyDifficulty(difficulty.Wave > 0
            ? difficulty
            : new DifficultySnapshot(1, 5, 1f, 1f, 1f), false);
    }

    public void ApplyDifficulty(DifficultySnapshot difficulty, bool preserveHealthRatio = true)
    {
        SetMaxHealthMultiplier(difficulty.HealthMultiplier, preserveHealthRatio);

        var motor = GetComponent<CharacterMotor>();
        if (motor != null) motor.SetSpeedMultiplier(difficulty.SpeedMultiplier);

        var attack = GetComponent<EnemyAttack>();
        if (attack != null) attack.SetDamageMultiplier(difficulty.DamageMultiplier);
    }

    public void ActivateCombat()
    {
        if (!gameObject.activeInHierarchy || IsDead) return;

        isCombatActive = true;
        var movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = true;

        var attack = GetComponent<EnemyAttack>();
        if (attack != null) attack.enabled = true;
    }

    private void EnsureHealthCanvas()
    {
        var existingHealthView = GetComponentInChildren<Views.HealthView>(true);
        if (existingHealthView != null)
        {
            healthCanvasInstance = existingHealthView.transform.root == transform
                ? existingHealthView.gameObject
                : existingHealthView.transform.parent != null
                    ? existingHealthView.transform.parent.gameObject
                    : existingHealthView.gameObject;
            return;
        }

        if (healthCanvasPrefab == null)
            healthCanvasPrefab = Resources.Load<GameObject>("EnemyHealthCanvas");

        if (healthCanvasPrefab == null)
        {
            Debug.LogError("EnemyHealthCanvas was not found in Resources.", this);
            return;
        }

        healthCanvasInstance = Instantiate(healthCanvasPrefab, transform, false);
        healthCanvasInstance.name = healthCanvasPrefab.name;
    }

    private IEnumerator DisableAfterDeath()
    {
        if (disableDelayAfterDeath > 0f)
            yield return new WaitForSeconds(disableDelayAfterDeath);

        gameObject.SetActive(false);
    }
}
