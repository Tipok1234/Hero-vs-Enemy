using UnityEngine;

public sealed class PlayerHealth : CharacterHealth
{
    private CharacterAnimator playerAnimator;
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;
    private CharacterMotor characterMotor;

    protected override void Awake()
    {
        base.Awake();
        playerAnimator = GetComponent<CharacterAnimator>();
        if (playerAnimator == null) playerAnimator = gameObject.AddComponent<CharacterAnimator>();
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
        if (playerAnimator != null) playerAnimator.ResetState();
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
        return playerAnimator != null && playerAnimator.PlaySpawn();
    }

    private void Update()
    {
        if (playerAnimator == null) return;

        if (Input.GetKeyDown(KeyCode.B))
            playerAnimator.PlayHit();

        if (Input.GetKeyDown(KeyCode.N))
            playerAnimator.PlayDeath();
    }
}
