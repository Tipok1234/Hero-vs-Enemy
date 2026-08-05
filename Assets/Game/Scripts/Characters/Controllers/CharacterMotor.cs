using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class CharacterMotor : MonoBehaviour
{
    [SerializeField] private CharacterConfig config;

    private CharacterController characterController;
    private float verticalVelocity;
    private Vector3 facingDirection;
    private bool hasFacingOverride;
    private float speedMultiplier = 1f;

    public Vector3 MoveDirection { get; private set; }
    public bool IsMoving => MoveDirection.sqrMagnitude > 0.0001f;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool HasFacingOverride => hasFacingOverride;
    public Vector3 FacingDirection => hasFacingOverride ? facingDirection : transform.forward;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Configure(CharacterConfig characterConfig)
    {
        config = characterConfig;
        speedMultiplier = 1f;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void Move(Vector3 worldDirection)
    {
        if (config == null)
        {
            Debug.LogError("CharacterMotor requires a CharacterConfig.", this);
            return;
        }

        worldDirection.y = 0f;
        MoveDirection = Vector3.ClampMagnitude(worldDirection, 1f);

        var desiredFacingDirection = hasFacingOverride ? facingDirection : MoveDirection;
        if (desiredFacingDirection.sqrMagnitude > 0.0001f)
        {
            var targetRotation = Quaternion.LookRotation(desiredFacingDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.RotationSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += config.Gravity * Time.deltaTime;

        var velocity = MoveDirection * config.MoveSpeed * speedMultiplier;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void SetFacingDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            ClearFacingDirection();
            return;
        }

        facingDirection = worldDirection.normalized;
        hasFacingOverride = true;
    }

    public void ClearFacingDirection()
    {
        hasFacingOverride = false;
    }

    public bool TryJump(float jumpHeight)
    {
        if (config == null || !IsGrounded || jumpHeight <= 0f || config.Gravity >= 0f)
            return false;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * config.Gravity);
        return true;
    }

    public void ResetMotion()
    {
        verticalVelocity = 0f;
        MoveDirection = Vector3.zero;
        ClearFacingDirection();
    }
}
