using UnityEngine;

/// <summary>
/// Simple camera follow script. Drag the player (or any Transform) into the Target field in the inspector.
/// Uses SmoothDamp for smooth following. Works in 2D and 3D; default offset keeps camera behind in 2D (-10 z).
/// </summary>
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Target the camera will follow. Drag the player GameObject's Transform here.")]
    public Transform target;

    [Tooltip("Offset applied to the target position (use Z = -10 for 2D orthographic camera).")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("Smoothing time for following. Smaller = tighter follow, larger = smoother.")]
    [Min(0f)]
    public float smoothTime = 0.15f;

    [Tooltip("Follow target's X position.")]
    public bool followX = true;

    [Tooltip("Follow target's Y position.")]
    public bool followY = true;

    [Tooltip("Optional world bounds to clamp the camera position. Enable to use.")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-Mathf.Infinity, -Mathf.Infinity);
    public Vector2 maxBounds = new Vector2(Mathf.Infinity, Mathf.Infinity);

    Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        // Preserve axes the user doesn't want to follow
        Vector3 current = transform.position;
        if (!followX) desired.x = current.x;
        if (!followY) desired.y = current.y;

        // Smoothly move camera
        Vector3 newPos = Vector3.SmoothDamp(current, desired, ref velocity, Mathf.Max(0.0001f, smoothTime));

        // Apply bounds if requested
        if (useBounds)
        {
            newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
            newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
        }

        transform.position = new Vector3(newPos.x, newPos.y, newPos.z);
    }

    /// <summary>
    /// Set the follow target at runtime.
    /// </summary>
    public void SetTarget(Transform t)
    {
        target = t;
    }
}