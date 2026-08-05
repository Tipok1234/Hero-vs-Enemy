using UnityEngine;

public sealed class PlayerHealth : CharacterHealth
{
    private CharacterAnimator characterAnimator;
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;
    private CharacterMotor characterMotor;

    protected override void Awake()
    {
        base.Awake();
        characterAnimator = GetComponent<CharacterAnimator>();
        if (characterAnimator == null) characterAnimator = gameObject.AddComponent<CharacterAnimator>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        characterMotor = GetComponent<CharacterMotor>();
    }

    protected override void OnDied()
    {
        base.OnDied();
        if (playerMovement != null) playerMovement.SetInputEnabled(false);
        if (playerAttack != null) playerAttack.enabled = false;
    }

    public void PrepareForRespawn(Vector3 position, Quaternion rotation)
    {
        if (playerMovement != null) playerMovement.BindGameScreenControls();

        var characterController = GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (characterController != null) characterController.enabled = true;

        if (characterMotor != null) characterMotor.ResetMotion();
        RestoreFullHealth();
        if (characterAnimator != null) characterAnimator.ResetState();
        if (playerMovement != null) playerMovement.SetInputEnabled(false);
        if (playerAttack != null) playerAttack.enabled = false;
    }

    public void ActivateAfterSpawn()
    {
        if (IsDead) return;
        if (playerMovement != null) playerMovement.SetInputEnabled(true);
        if (playerAttack != null) playerAttack.enabled = true;
    }

    public bool PlaySpawnAnimation()
    {
        return characterAnimator != null && characterAnimator.PlaySpawn();
    }

    private void Update()
    {
        if (characterAnimator == null) return;

        if (Input.GetKeyDown(KeyCode.B))
            characterAnimator.PlayHit();

        if (Input.GetKeyDown(KeyCode.N))
            characterAnimator.PlayDeath();
    }
}
