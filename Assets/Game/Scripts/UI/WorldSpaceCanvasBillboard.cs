using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldSpaceCanvasBillboard : MonoBehaviour
{
    private Camera targetCamera;

    private void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        var directionToCamera = targetCamera.transform.position - transform.position;
        directionToCamera.y = 0f;
        if (directionToCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
    }
}
