using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to any scene object you want special behavior for when hit by the bullet
/// from `PlayerMovmentAndShooting`.
///
/// Behavior:
/// - When a bullet hits this object, call `OnHitByBullet(bullet, incomingVelocity)` (this script
///   provides that public method). The object will gain a Rigidbody2D (if configured) and receive
///   an impulse (recoil) in the incoming bullet direction. When the object becomes "still"
///   for a configured duration it will be destroyed.
/// - If the bullet should not immediately disappear (bullet reached its object-destruction limit),
///   call `MakeBulletFallAndDisappear(bullet)` to convert the bullet into a physics object (enable
///   gravity / collisions) and schedule the bullet to be destroyed after `bulletFallLifetime`.
///
/// NOTE: For reliable behavior you should replace the immediate-destroy calls in
/// `PlayerMovmentAndShooting.OnTriggerEnter2D` so that the bullet hands the hit to this script
/// when present (instructions below). This avoids racing between the bullet destroying the object
/// and this component running its logic.
/// </summary>
[DisallowMultipleComponent]
public class DestructibleOnBulletHit : MonoBehaviour
{
    [Header("Target (this object) settings")]
    [Tooltip("Impulse applied to the target when hit (based on bullet incoming direction).")]
    public float recoilForce = 6f;

    [Tooltip("If true and the hit object lacks a Rigidbody2D, one will be added so it can be flung.")]
    public bool addRigidbodyIfMissing = true;

    [Tooltip("Gravity scale applied to the object's Rigidbody2D when added.")]
    public float objectGravityScale = 1f;

    [Tooltip("Velocity magnitude under which the object is considered 'still'.")]
    public float stillVelocityThreshold = 0.05f;

    [Tooltip("How long the object must remain below the still threshold before being destroyed (seconds).")]
    public float stillTimeToDestroy = 0.6f;

    [Tooltip("Optional extra delay after object is still before destroying it (seconds).")]
    public float additionalDelayAfterStill = 0.1f;

    [Header("Bullet fall settings (when bullet should 'fall' instead of instantly disappear)")]
    [Tooltip("Gravity scale applied to the bullet when it becomes a physical falling object.")]
    public float bulletGravityScale = 1f;

    [Tooltip("How long the fallen bullet lives (seconds) before being destroyed.")]
    public float bulletFallLifetime = 2f;

    // runtime
    Coroutine monitorCoroutine;

    /// <summary>
    /// Call this when this object is hit by a bullet.
    /// bulletGameObject: the bullet instance that hit this object.
    /// incomingVelocity: bullet velocity (use bullet Rigidbody2D.velocity if available).
    /// </summary>
    public void OnHitByBullet(GameObject bulletGameObject, Vector2 incomingVelocity)
    {
        // Apply recoil to this object
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null && addRigidbodyIfMissing)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = objectGravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = false;
        }

        if (rb != null)
        {
            Vector2 dir;
            if (incomingVelocity.sqrMagnitude > 0.0001f)
                dir = incomingVelocity.normalized;
            else
                dir = ((Vector2)(transform.position - bulletGameObject.transform.position)).normalized;

            // Apply impulse away from the incoming direction (object is pushed by bullet)
            rb.AddForce(dir * recoilForce, ForceMode2D.Impulse);

            // Start monitoring object to destroy when still
            if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
            monitorCoroutine = StartCoroutine(MonitorAndDestroyWhenStill(rb));
        }
        else
        {
            // fallback: if we can't give it physics, just destroy immediately
            Destroy(gameObject);
        }
    }

    IEnumerator MonitorAndDestroyWhenStill(Rigidbody2D rb)
    {
        float elapsedStill = 0f;

        // Wait until the object is relatively still for the configured duration
        while (true)
        {
            if (rb == null) yield break;

            if (rb.linearVelocity.sqrMagnitude <= stillVelocityThreshold * stillVelocityThreshold)
            {
                elapsedStill += Time.deltaTime;
                if (elapsedStill >= stillTimeToDestroy)
                    break;
            }
            else
            {
                elapsedStill = 0f;
            }

            yield return null;
        }

        if (additionalDelayAfterStill > 0f)
            yield return new WaitForSeconds(additionalDelayAfterStill);

        Destroy(gameObject);
    }

    /// <summary>
    /// Convert the bullet into a falling physics object and schedule its destruction.
    /// Call this when the bullet should not be instantly removed after reaching its object-hit limit.
    /// </summary>
    public void MakeBulletFallAndDisappear(GameObject bullet)
    {
        if (bullet == null) return;

        // Ensure bullet has Rigidbody2D
        Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();
        if (brb == null) brb = bullet.AddComponent<Rigidbody2D>();

        // Enable physics/gravity
        brb.gravityScale = bulletGravityScale;
        brb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        brb.freezeRotation = false;

        // If bullet collider is trigger, switch to non-trigger so it falls with collisions
        Collider2D col = bullet.GetComponent<Collider2D>();
        if (col != null && col.isTrigger) col.isTrigger = false;

        // Destroy bullet after fall lifetime
        Destroy(bullet, bulletFallLifetime);
    }
}