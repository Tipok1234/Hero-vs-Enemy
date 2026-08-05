using UnityEngine;
using UnityEngine.UI;
using Screens;

[RequireComponent(typeof(CharacterMotor))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfig config;

    [Header("Input")]
    [SerializeField] private Joystick joystick;
    [SerializeField] private Button jumpButton;
    [SerializeField] private bool cameraRelativeMovement = true;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;

    private CharacterMotor motor;
    private CharacterAnimator characterAnimator;
    private Camera mainCamera;
    private bool inputEnabled = true;

    public Vector2 MoveInput { get; private set; }
    public bool IsMoving => MoveInput.sqrMagnitude > 0f;
    public PlayerConfig Config => config;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        if (motor == null) motor = gameObject.AddComponent<CharacterMotor>();
        motor.Configure(config);
        characterAnimator = GetComponent<CharacterAnimator>();
        if (characterAnimator == null) characterAnimator = gameObject.AddComponent<CharacterAnimator>();
        characterAnimator.Configure(config != null ? config.AnimationDampTime : 0.1f);

        if (config == null)
        {
            Debug.LogError("PlayerMovement requires a PlayerConfig.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        BindGameScreenControls();
        mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (jumpButton != null) jumpButton.onClick.RemoveListener(Jump);
    }

    public void BindGameScreenControls()
    {
        joystick = FindJoystickInGameScreen();

        var foundJumpButton = FindJumpButtonInGameScreen();
        if (foundJumpButton == jumpButton) return;

        if (jumpButton != null) jumpButton.onClick.RemoveListener(Jump);
        jumpButton = foundJumpButton;
        if (jumpButton != null) jumpButton.onClick.AddListener(Jump);
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            MoveInput = Vector2.zero;
            motor.Move(Vector3.zero);
            UpdateAnimator();
            return;
        }

        ReadInput();
        Move();
        UpdateAnimator();
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;
        if (joystick != null)
        {
            if (!value) joystick.OnPointerUp(null);
            joystick.enabled = value;
        }
        if (jumpButton != null) jumpButton.interactable = value;
        if (value) return;

        MoveInput = Vector2.zero;
        if (motor != null)
        {
            motor.ClearFacingDirection();
            motor.Move(Vector3.zero);
        }

        if (characterAnimator != null)
            characterAnimator.SetMovement(0f, Vector2.zero);
    }

    private void ReadInput()
    {
        if (joystick == null)
        {
            MoveInput = Vector2.zero;
            return;
        }

        var input = new Vector2(joystick.Horizontal, joystick.Vertical);
        MoveInput = input.sqrMagnitude < config.InputDeadZone * config.InputDeadZone
            ? Vector2.zero
            : Vector2.ClampMagnitude(input, 1f);
    }

    private void Move()
    {
        var direction = GetWorldDirection(MoveInput);

        motor.Move(direction);
    }

    private void Jump()
    {
        if (!inputEnabled || motor == null) return;
        if (motor.TryJump(jumpHeight) && characterAnimator != null)
            characterAnimator.PlayJump();
    }

    private Vector3 GetWorldDirection(Vector2 input)
    {
        if (!cameraRelativeMovement || mainCamera == null)
            return new Vector3(input.x, 0f, input.y);

        var forward = mainCamera.transform.forward;
        var right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
    }

    private void UpdateAnimator()
    {
        characterAnimator.SetMovement(MoveInput.magnitude, MoveInput);
    }

    private static Joystick FindJoystickInGameScreen()
    {
        var gameScreen = FindObjectOfType<GameScreen>();
        return gameScreen == null ? null : gameScreen.GetComponentInChildren<Joystick>(true);
    }

    private static Button FindJumpButtonInGameScreen()
    {
        var gameScreen = FindObjectOfType<GameScreen>();
        if (gameScreen == null) return null;

        var buttons = gameScreen.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button.name == "JumpBtn") return button;
        }

        return null;
    }
}
