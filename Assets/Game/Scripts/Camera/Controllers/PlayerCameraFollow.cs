using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 followOffset = new(0f, 14f, -10f);
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.18f;
    [SerializeField, Min(0f)] private float maximumFollowSpeed = 50f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private Transform ignoredCollisionRoot;
    [SerializeField, Min(0.05f)] private float collisionRadius = 0.45f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.2f;
    [SerializeField, Min(0.1f)] private float minimumCameraDistance = 2f;

    [Header("Focus")]
    [SerializeField, Min(0f)] private float focusHeight = 1.2f;
    [SerializeField, Min(0f)] private float lookAheadDistance = 1f;
    [SerializeField, Min(0.01f)] private float lookAheadSmoothTime = 0.2f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;

    private CharacterMotor targetMotor;
    private Vector3 positionVelocity;
    private Vector3 smoothedLookAhead;
    private Vector3 lookAheadVelocity;
    private readonly RaycastHit[] collisionHits = new RaycastHit[32];

    private void Awake()
    {
        if (ignoredCollisionRoot == null)
        {
            var environment = GameObject.Find("Environment");
            if (environment != null)
                ignoredCollisionRoot = environment.transform;
        }

        if (target == null)
            FindPlayer();
    }

    private void Start()
    {
        if (target != null)
            SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindPlayer();
            if (target == null) return;
        }

        var moveDirection = targetMotor != null ? targetMotor.MoveDirection : Vector3.zero;
        var desiredLookAhead = moveDirection * lookAheadDistance;
        smoothedLookAhead = Vector3.SmoothDamp(
            smoothedLookAhead,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime);

        var focusPoint = target.position + Vector3.up * focusHeight + smoothedLookAhead;
        var desiredPosition = focusPoint + followOffset;

        var collisionSafePosition = ResolveCameraCollision(focusPoint, desiredPosition);
        transform.position = Vector3.SmoothDamp(
            transform.position,
            collisionSafePosition,
            ref positionVelocity,
            positionSmoothTime,
            maximumFollowSpeed);

        var desiredRotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void FindPlayer()
    {
        var playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement == null) return;

        target = playerMovement.transform;
        targetMotor = target.GetComponent<CharacterMotor>();
    }

    private void SnapToTarget()
    {
        targetMotor = target.GetComponent<CharacterMotor>();
        smoothedLookAhead = Vector3.zero;

        var focusPoint = target.position + Vector3.up * focusHeight;
        transform.position = ResolveCameraCollision(focusPoint, focusPoint + followOffset);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private Vector3 ResolveCameraCollision(Vector3 focusPoint, Vector3 cameraPosition)
    {
        var offset = cameraPosition - focusPoint;
        var desiredDistance = offset.magnitude;
        if (desiredDistance <= 0.0001f) return cameraPosition;

        var direction = offset / desiredDistance;
        var hitCount = Physics.SphereCastNonAlloc(
            focusPoint,
            collisionRadius,
            direction,
            collisionHits,
            desiredDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        var nearestDistance = desiredDistance;
        for (var i = 0; i < hitCount; i++)
        {
            var hitCollider = collisionHits[i].collider;
            if (hitCollider == null) continue;
            if (target != null && hitCollider.transform.root == target.root) continue;
            if (ignoredCollisionRoot != null &&
                hitCollider.transform.IsChildOf(ignoredCollisionRoot)) continue;
            if (hitCollider.GetComponentInParent<CharacterHealth>() != null) continue;

            nearestDistance = Mathf.Min(nearestDistance, collisionHits[i].distance);
        }

        if (nearestDistance >= desiredDistance) return cameraPosition;

        var safeMinimumDistance = nearestDistance >= minimumCameraDistance + collisionPadding
            ? minimumCameraDistance
            : 0.1f;
        var safeDistance = Mathf.Clamp(
            nearestDistance - collisionPadding,
            safeMinimumDistance,
            desiredDistance);
        return focusPoint + direction * safeDistance;
    }
}
