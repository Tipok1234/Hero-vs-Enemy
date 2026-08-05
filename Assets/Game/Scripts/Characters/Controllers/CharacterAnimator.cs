using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float movementDampTime = 0.1f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackVariantHash = Animator.StringToHash("AttackVariant");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int SpawnHash = Animator.StringToHash("Spawn");
    private static readonly int SpawnStateHash = Animator.StringToHash("Base Layer.Spawn");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private readonly HashSet<int> parameters = new HashSet<int>();
    private bool isDead;

    public bool IsReady => animator != null && animator.runtimeAnimatorController != null;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        CacheParameters();
    }

    public void Configure(float dampTime)
    {
        movementDampTime = dampTime;
    }

    public void SetMovement(float speed, Vector2 directionalInput)
    {
        if (!IsReady || isDead) return;

        speed = Mathf.Clamp01(speed);
        SetFloat(SpeedHash, speed);
        SetFloat(MoveXHash, directionalInput.x);
        SetFloat(MoveYHash, directionalInput.y);
        SetBool(IsMovingHash, speed > 0.001f);
    }

    public bool PlayAttack(int variant = 0)
    {
        if (isDead) return false;
        SetInteger(AttackVariantHash, variant);
        return SetTrigger(AttackHash);
    }
    public void PlayHit()
    {
        if (!isDead) SetTrigger(HitHash);
    }

    public bool PlayJump()
    {
        return !isDead && SetTrigger(JumpHash);
    }

    public bool PlaySpawn()
    {
        if (isDead || !IsReady || !animator.HasState(0, SpawnStateHash)) return false;

        if (parameters.Contains(SpawnHash)) animator.ResetTrigger(SpawnHash);
        animator.Play(SpawnStateHash, 0, 0f);
        animator.Update(0f);
        return true;
    }

    public void PlayDeath()
    {
        if (isDead) return;
        isDead = true;
        SetBool(IsDeadHash, true);
        SetTrigger(DieHash);
    }

    public void ResetState()
    {
        if (animator == null) return;
        isDead = false;
        animator.Rebind();
        animator.Update(0f);
        SetBool(IsDeadHash, false);
        SetBool(IsMovingHash, false);
    }

    private void CacheParameters()
    {
        parameters.Clear();
        if (!IsReady) return;

        foreach (var parameter in animator.parameters)
            parameters.Add(parameter.nameHash);
    }

    private void SetFloat(int parameterHash, float value)
    {
        if (parameters.Contains(parameterHash))
            animator.SetFloat(parameterHash, value, movementDampTime, Time.deltaTime);
    }

    private void SetBool(int parameterHash, bool value)
    {
        if (IsReady && parameters.Contains(parameterHash))
            animator.SetBool(parameterHash, value);
    }

    private void SetInteger(int parameterHash, int value)
    {
        if (IsReady && parameters.Contains(parameterHash))
            animator.SetInteger(parameterHash, value);
    }

    private bool SetTrigger(int parameterHash)
    {
        if (!IsReady || !parameters.Contains(parameterHash)) return false;
        animator.ResetTrigger(parameterHash);
        animator.SetTrigger(parameterHash);
        return true;
    }
}
