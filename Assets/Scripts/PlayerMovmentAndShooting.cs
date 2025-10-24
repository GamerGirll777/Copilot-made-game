using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Single script that can be used either on the Player or on a Bullet prefab/instance.
/// Use the inspector checkboxes "I am PLAYER" or "I am BULLET" to switch mode.
/// When "I am PLAYER" is checked the player-related editable fields are shown.
/// When "I am BULLET" is checked the bullet-related editable fields are shown (the other checkbox is auto-cleared).
/// Both checkboxes may be unchecked (no mode) — in that state nothing mode-specific is exposed.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovmentAndShooting : MonoBehaviour
{
    public enum ControllerInput
    {
        None,
        LeftStickHorizontal,
        RightTrigger,
        Button0, Button1, Button2, Button3,
        Button4, Button5, Button6, Button7,
        Button8, Button9, Button10, Button11,
        Button12, Button13, Button14, Button15,
        Button16, Button17, Button18, Button19
    }

    [Header("Mode")]
    [Tooltip("Check if this instance is the Player.")]
    public bool isPlayer = true;

    [Tooltip("Check if this instance is a Bullet. If checked, bullet-only options appear and player options hide.")]
    public bool isBullet = false;

    // -----------------------
    // Input customization (player)
    // -----------------------
    [Header("Input - Keyboard")]
    [Tooltip("Key to move right")]
    public KeyCode moveRightKey = KeyCode.D;
    [Tooltip("Allow controller input for Move Right")]
    public bool moveRightAllowController = true;
    [Tooltip("Controller input to use for Move Right (shown only when Allow Controller is checked)")]
    public ControllerInput moveRightController = ControllerInput.LeftStickHorizontal;

    [Tooltip("Key to move left")]
    public KeyCode moveLeftKey = KeyCode.A;
    [Tooltip("Allow controller input for Move Left")]
    public bool moveLeftAllowController = true;
    [Tooltip("Controller input to use for Move Left (shown only when Allow Controller is checked)")]
    public ControllerInput moveLeftController = ControllerInput.LeftStickHorizontal;

    [Tooltip("Key to jump")]
    public KeyCode jumpKey = KeyCode.Space;
    [Tooltip("Allow controller input for Jump")]
    public bool jumpAllowController = true;
    [Tooltip("Controller input to use for Jump (shown only when Allow Controller is checked)")]
    public ControllerInput jumpController = ControllerInput.Button0; // A button

    [Tooltip("Key to shoot")]
    public KeyCode shootKey = KeyCode.LeftControl;
    [Tooltip("Allow controller input for Shoot")]
    public bool shootAllowController = true;
    [Tooltip("Controller input to use for Shoot (shown only when Allow Controller is checked)")]
    public ControllerInput shootController = ControllerInput.RightTrigger;

    [Header("Movement (Player)")]
    [Tooltip("Horizontal move speed")]
    public float moveSpeed = 6f;

    [Header("Jump (Player)")]
    [Tooltip("Initial jump velocity (positive up)")]
    public float jumpForce = 12f;
    [Tooltip("Multiplier applied when player is falling (makes fall faster)")]
    public float fallMultiplier = 2.5f;
    [Tooltip("Multiplier applied when player releases jump early (makes low jump)")]
    public float lowJumpMultiplier = 2f;

    [Header("Ground Check (Player)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Shooting (Player)")]
    [Tooltip("Prefab for bullet. Assign on player only.")]
    public GameObject bulletPrefab;
    [Tooltip("Where bullets spawn from")]
    public Transform gunPoint;
    [Tooltip("Optional GameObject (child) that visually represents the gun; will be shown briefly when shooting.")]
    public GameObject gunObject;
    [Tooltip("Bullet travel speed")]
    public float bulletSpeed = 16f;
    [Tooltip("How many objects the bullet must destroy before disappearing (0 = unlimited until lifetime)")]
    public int bulletMaxHits = 3;
    [Tooltip("How long a bullet lasts if it doesn't reach bulletMaxHits (seconds)")]
    public float bulletLifetime = 5f;
    [Tooltip("Cooldown time after shooting (seconds)")]
    public float shootCooldown = 5f;
    [Tooltip("How long the gun is shown when firing (seconds)")]
    public float gunShowDuration = 0.12f;

    // -----------------------
    // Bullet physics-on-last-hit settings (editable on bullet instances)
    // -----------------------
    [Header("Bullet - last hit physics")]
    [Tooltip("If bullet hits the last allowed object, apply this gravity scale to the bullet so it falls.")]
    public float bulletFallGravity = 1f;
    [Tooltip("How long the fallen bullet lasts before disappearing (seconds).")]
    public float bulletFallLifetime = 2f;
    [Tooltip("If the hit object doesn't have a DestructibleOnBulletHit component, apply this impulse to the object.")]
    public float objectRecoilForce = 6f;
    [Tooltip("Gravity scale to add to the hit object if we add a Rigidbody2D to it.")]
    public float objectGravityScaleOnHit = 1f;
    [Tooltip("Velocity magnitude under which the object is considered still.")]
    public float objectStillVelocityThreshold = 0.05f;
    [Tooltip("How long the object must remain below still threshold before being destroyed (seconds).")]
    public float objectStillTimeToDestroy = 0.6f;
    [Tooltip("Additional delay after object still before destroying it (seconds).")]
    public float objectAdditionalDelayAfterStill = 0.1f;

    // -----------------------
    // Bullet recoil on last hit (bullet-only options)
    // -----------------------
    [Header("Bullet - recoil (bullet-only)")]
    [Tooltip("When this instance is a bullet enable recoil applied to the bullet itself on its final hit.")]
    public bool bulletEnableRecoilOnLastHit = false;
    [Tooltip("Recoil force (impulse) applied to the bullet opposite its incoming direction when it hits its last allowed object.")]
    public float bulletRecoilForce = 4f;
    [Tooltip("Upward bias added to the recoil direction (0 = purely opposite, 1 = equal up component).")]
    public float bulletRecoilUpwardBias = 0.6f;

    // -----------------------
    // Bullet - objects that should NOT be destroyed when hit (bullet-only)
    // -----------------------
    [Header("Bullet - do-not-destroy list (bullet-only)")]
    [Tooltip("List of GameObjects that should NOT be destroyed when hit by this bullet. If hit, the bullet will convert to physics but the target will NOT be given physics nor destroyed.")]
    public List<GameObject> doNotDestroyList = new List<GameObject>(50);

    // -----------------------
    // Bullet appearance
    // -----------------------
    [Header("Bullet (internal when isBullet == true)")]
    [Tooltip("Color applied to bullet SpriteRenderer (if present)")]
    public Color bulletColor = Color.gray;

    // runtime
    Rigidbody2D rb;

    // player state
    bool canShoot = true;
    bool isGrounded;
    float horizontalInput;
    bool facingRight = true;

    // bullet state
    int hitCount = 0;
    float lifeTimer = 0f;
    GameObject owner; // who shot this bullet (to avoid self-destroy)
    bool lastHitProcessed = false; // ensure only the first last-hit processes object-physics

    const float axisThreshold = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // If this is a bullet instance, ensure bullet state and flags reflect that
        if (isBullet)
        {
            isPlayer = false;
            lifeTimer = 0f;
            hitCount = 0;
            lastHitProcessed = false;
        }
        else
        {
            // if it's player mode, ensure bullet flag off
            isBullet = false;
        }
    }

    void Update()
    {
        if (isPlayer)
        {
            // compute horizontal input from keys / controller
            bool right = (moveRightKey != KeyCode.None && Input.GetKey(moveRightKey))
                         || (moveRightAllowController && CheckControllerPressed(moveRightController, false, true));
            bool left = (moveLeftKey != KeyCode.None && Input.GetKey(moveLeftKey))
                        || (moveLeftAllowController && CheckControllerPressed(moveLeftController, false, false));

            if (right && !left) horizontalInput = 1f;
            else if (left && !right) horizontalInput = -1f;
            else horizontalInput = 0f;

            if (IsJumpPressed() && IsGrounded()) Jump();
            if (IsShootPressed()) TryShoot();
        }
        else
        {
            // bullet lifetime handling
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= bulletLifetime && bulletLifetime > 0f)
                SafeDestroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (isPlayer)
        {
            Move();
            ApplyBetterJumpPhysics();
        }
    }

    // -----------------------
    // Movement & jump (player)
    // -----------------------
    void Move()
    {
        Vector2 v = rb != null ? rb.linearVelocity : Vector2.zero;
        v.x = horizontalInput * moveSpeed;
        if (rb != null) rb.linearVelocity = v;

        if (horizontalInput > 0.01f && !facingRight) Flip();
        else if (horizontalInput < -0.01f && facingRight) Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    void Jump()
    {
        if (rb == null) return;
        Vector2 v = rb.linearVelocity;
        v.y = jumpForce;
        rb.linearVelocity = v;
    }

    void ApplyBetterJumpPhysics()
    {
        if (rb == null) return;

        if (rb.linearVelocity.y < 0)
        {
            Vector2 vel = rb.linearVelocity;
            vel += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = vel;
        }
        else if (rb.linearVelocity.y > 0 && !IsJumpHeld())
        {
            Vector2 vel = rb.linearVelocity;
            vel += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = vel;
        }
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = hit != null;
        return isGrounded;
    }

    bool IsJumpPressed()
    {
        bool key = (jumpKey != KeyCode.None && Input.GetKeyDown(jumpKey));
        bool ctrl = jumpAllowController && CheckControllerPressed(jumpController, true, false);
        return key || ctrl;
    }

    bool IsJumpHeld()
    {
        bool key = (jumpKey != KeyCode.None && Input.GetKey(jumpKey));
        bool ctrl = jumpAllowController && CheckControllerPressed(jumpController, false, false);
        return key || ctrl;
    }

    bool IsShootPressed()
    {
        bool key = (shootKey != KeyCode.None && Input.GetKeyDown(shootKey));
        bool ctrl = shootAllowController && CheckControllerPressed(shootController, true, false);
        return key || ctrl;
    }

    bool CheckControllerPressed(ControllerInput mapping, bool downOnly, bool positiveForAxis)
    {
        if (mapping == ControllerInput.None) return false;

        switch (mapping)
        {
            case ControllerInput.LeftStickHorizontal:
                float h = Input.GetAxisRaw("Horizontal");
                return positiveForAxis ? h > axisThreshold : h < -axisThreshold;

            case ControllerInput.RightTrigger:
                float rt = 0f;
                if (InputManagerHasAxis("RightTrigger")) rt = Input.GetAxis("RightTrigger");
                else if (InputManagerHasAxis("Triggers")) rt = Input.GetAxis("Triggers");
                else
                {
                    KeyCode kb = KeyCode.JoystickButton7;
                    return downOnly ? Input.GetKeyDown(kb) : Input.GetKey(kb);
                }
                return rt > axisThreshold;

            default:
                int idx = GetButtonIndex(mapping);
                if (idx >= 0)
                {
                    KeyCode kc = KeyCode.JoystickButton0 + idx;
                    return downOnly ? Input.GetKeyDown(kc) : Input.GetKey(kc);
                }
                break;
        }
        return false;
    }

    static int GetButtonIndex(ControllerInput mapping)
    {
        if (mapping >= ControllerInput.Button0 && mapping <= ControllerInput.Button19)
            return (int)mapping - (int)ControllerInput.Button0;
        return -1;
    }

    bool InputManagerHasAxis(string name)
    {
#if UNITY_EDITOR
        try { foreach (var a in Input.GetJoystickNames()) { } } catch { }
#endif
        return false; // no reliable runtime enumeration; kept for editor fallback logic
    }

    // -----------------------
    // Shooting (player) / bullet creation
    // -----------------------
    void TryShoot()
    {
        if (!canShoot) return;
        if (bulletPrefab == null || gunPoint == null) return;
        StartCoroutine(DoShootRoutine());
    }

    IEnumerator DoShootRoutine()
    {
        canShoot = false;

        if (gunObject != null) gunObject.SetActive(true);

        GameObject bullet = Instantiate(bulletPrefab, gunPoint.position, Quaternion.identity);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null) bulletRb = bullet.AddComponent<Rigidbody2D>();
        bulletRb.gravityScale = 0f;

        Collider2D bulletCol = bullet.GetComponent<Collider2D>();
        if (bulletCol == null)
        {
            CircleCollider2D c = bullet.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
        }
        else bulletCol.isTrigger = true;

        PlayerMovmentAndShooting bulletScript = bullet.GetComponent<PlayerMovmentAndShooting>();
        if (bulletScript == null) bulletScript = bullet.AddComponent<PlayerMovmentAndShooting>();

        // initialize bullet as bullet-mode
        bulletScript.InitializeAsBullet(bulletSpeed, bulletLifetime, bulletMaxHits, bulletColor, this.gameObject);

        // copy the shooter's do-not-destroy list into the bullet instance so runtime hits use the same exclusions.
        if (doNotDestroyList != null && doNotDestroyList.Count > 0)
        {
            bulletScript.doNotDestroyList = new List<GameObject>(doNotDestroyList);
        }

        float dir = facingRight ? 1f : -1f;
        bullet.transform.localScale = new Vector3(Mathf.Abs(bullet.transform.localScale.x) * dir, bullet.transform.localScale.y, bullet.transform.localScale.z);

        bulletRb.linearVelocity = new Vector2(dir * bulletSpeed, 0f);

        SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = bulletColor;

        yield return new WaitForSeconds(gunShowDuration);

        if (gunObject != null) gunObject.SetActive(false);

        yield return new WaitForSeconds(shootCooldown);

        canShoot = true;
    }

    public void InitializeAsBullet(float speed, float lifetime, int maxHits, Color color, GameObject bulletOwner)
    {
        // set to bullet mode
        isPlayer = false;
        isBullet = true;

        bulletSpeed = speed;
        bulletLifetime = lifetime;
        bulletMaxHits = maxHits;
        bulletColor = color;
        owner = bulletOwner;

        lifeTimer = 0f;
        hitCount = 0;
        lastHitProcessed = false;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;
    }

    // -----------------------
    // Bullet collision logic
    // -----------------------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayer) return;

        // Skip collisions with the shooter (owner) or any child colliders of the owner
        if (owner != null)
        {
            if (other.gameObject == owner || other.transform.IsChildOf(owner.transform) || other.transform.root == owner.transform.root)
                return;
        }

        // Use parent-aware lookup so collisions with child colliders of the player are recognized as the player
        PlayerMovmentAndShooting otherScript = other.GetComponentInParent<PlayerMovmentAndShooting>();
        // ignore bullets and players entirely
        if (otherScript != null)
        {
            if (otherScript.isBullet) return;
            if (otherScript.isPlayer) return; // bullets should not affect the player
        }

        // determine the canonical target to check against exclusion list:
        GameObject hitObject = other.gameObject;
        GameObject hitRoot = other.transform.root != null ? other.transform.root.gameObject : hitObject;

        // determine owner player script for area-aware exclusion checks
        PlayerMovmentAndShooting ownerPlayerScript = null;
        if (owner != null) ownerPlayerScript = owner.GetComponent<PlayerMovmentAndShooting>();

        // check exclusion against collider, its parents, attached rigidbody object and root
        bool isExcluded = IsInDoNotDestroyList(hitObject, ownerPlayerScript)
                          || IsInDoNotDestroyList(hitRoot, ownerPlayerScript)
                          || (other.attachedRigidbody != null && IsInDoNotDestroyList(other.attachedRigidbody.gameObject, ownerPlayerScript));

#if UNITY_EDITOR
        Debug.Log($"Bullet '{name}' hit '{hitObject.name}' (root '{hitRoot.name}'). excluded={isExcluded}. hitCount={hitCount}. willBeLast={(bulletMaxHits > 0 && (hitCount + 1 >= bulletMaxHits))}");
#endif

        bool willBeLastHit = (bulletMaxHits > 0) && (hitCount + 1 >= bulletMaxHits);

        Vector2 incomingVel = Vector2.zero;
        var bulletRb = GetComponent<Rigidbody2D>();
        if (bulletRb != null) incomingVel = bulletRb.linearVelocity;

        if (willBeLastHit)
        {
            // Ensure only the first last-hit processes object-physics / destruction rules
            if (lastHitProcessed)
            {
                // Once the bullet already processed its final-hit conversion, do NOT add physics to other objects.
                if (!isExcluded)
                    SafeDestroy(other.gameObject);
                return;
            }

            // Mark this bullet as converted so no further objects receive physics
            lastHitProcessed = true;

            var destructible = other.GetComponent<DestructibleOnBulletHit>();
            if (destructible != null)
            {
                if (isExcluded)
                {
                    // EXCLUDED: do NOT affect the target at all. Only convert bullet to physics and optionally apply recoil to bullet.
                    if (isBullet && bulletEnableRecoilOnLastHit && bulletRb != null)
                        ApplyBulletRecoil(bulletRb, incomingVel);

                    // Convert bullet to falling physics (bullet only). Do NOT call destructible.OnHitByBullet.
                    MakeThisBulletFallAndDisappear();

#if UNITY_EDITOR
                    Debug.Log($"Excluded target '{hitObject.name}' — bullet converted but target untouched.");
#endif
                    // Do NOT increment hitCount — excluded hits do not consume player max-hits.
                    return;
                }
                else
                {
                    // NORMAL: affect the target via its DestructibleOnBulletHit logic, and convert bullet to falling physics.
                    if (isBullet && bulletEnableRecoilOnLastHit && bulletRb != null)
                        ApplyBulletRecoil(bulletRb, incomingVel);

                    destructible.OnHitByBullet(this.gameObject, incomingVel);
                    destructible.MakeBulletFallAndDisappear(this.gameObject);

                    hitCount++;
                    return;
                }
            }
            else
            {
                // other does not have DestructibleOnBulletHit
                if (isExcluded)
                {
                    // EXCLUDED: do NOT add Rigidbody2D to other and do NOT destroy it.
                    if (isBullet && bulletEnableRecoilOnLastHit && bulletRb != null)
                        ApplyBulletRecoil(bulletRb, incomingVel);

                    // Convert bullet to falling physics (bullet only).
                    MakeThisBulletFallAndDisappear();

#if UNITY_EDITOR
                    Debug.Log($"Excluded non-destructible '{hitObject.name}' — bullet converted but target untouched.");
#endif
                    // Do NOT increment hitCount — excluded hits do not consume player max-hits.
                    return;
                }
                else
                {
                    // DEFAULT: give other a Rigidbody2D and fling it, then make bullet fall
                    Rigidbody2D otherRb = other.GetComponent<Rigidbody2D>();
                    if (otherRb == null)
                    {
                        otherRb = other.gameObject.AddComponent<Rigidbody2D>();
                        otherRb.gravityScale = objectGravityScaleOnHit;
                        otherRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                        otherRb.freezeRotation = false;
                    }

                    Vector2 dir;
                    if (incomingVel.sqrMagnitude > 0.0001f) dir = incomingVel.normalized;
                    else dir = ((Vector2)(other.transform.position - transform.position)).normalized;

                    otherRb.AddForce(dir * objectRecoilForce, ForceMode2D.Impulse);

                    if (isBullet && bulletEnableRecoilOnLastHit && bulletRb != null)
                        ApplyBulletRecoil(bulletRb, incomingVel);

                    StartCoroutine(MonitorAndDestroyWhenStill(otherRb, other.gameObject));
                    MakeThisBulletFallAndDisappear();

                    hitCount++;
                    return;
                }
            }
        }

        // NOT last hit:
        // If the hit object is excluded, do NOT destroy it and do NOT count the hit toward maxHits.
        if (isExcluded)
        {
#if UNITY_EDITOR
            Debug.Log($"Hit excluded object '{hitObject.name}' (not last hit) — ignoring target and not counting the hit.");
#endif
            // Do nothing to the target; do not increment hitCount.
            return;
        }

        // Normal non-excluded behavior: destroy the target and count as a hit.
        SafeDestroy(other.gameObject);
        hitCount++;

        if (bulletMaxHits > 0 && hitCount >= bulletMaxHits) MakeThisBulletFallAndDisappear();
    }

    // Helper to check exclusion list (null-safe). Tries multiple heuristics and supports EffectAreas-aware exclusion:
    // - exact reference match (scene instance)
    // - child/parent relationships
    // - root match
    // - normalized name match (removes "(Clone)")
    // - tag fallback (if entry has a non-Untagged tag)
    // - if an entry is an EffectAreas GameObject, it counts as an exclusion ONLY WHEN ownerPlayer != null AND the owner player is currently inside that EffectAreas instance.
    bool IsInDoNotDestroyList(GameObject go, PlayerMovmentAndShooting ownerPlayer = null)
    {
        if (doNotDestroyList == null || go == null) return false;

        // Never treat the player as excluded
        var ps = go.GetComponent<PlayerMovmentAndShooting>();
        if (ps != null && ps.isPlayer) return false;

        string goNorm = NormalizeName(go.name);

        for (int i = 0; i < doNotDestroyList.Count; i++)
        {
            var entry = doNotDestroyList[i];
            if (entry == null) continue;

            // If entry is an EffectAreas instance, treat specially:
            var area = entry.GetComponent<EffectAreas>();
            if (area != null)
            {
                // Check if the hit object is the area or a child/root of the area.
                bool hitsArea = (entry == go)
                                || go.transform.IsChildOf(entry.transform)
                                || (entry == go.transform.root.gameObject)
                                || (entry.transform.root == go.transform.root);

                if (hitsArea)
                {
                    // Only treat as excluded if the owner who shot the bullet is currently inside that EffectArea.
                    if (ownerPlayer != null && area.IsPlayerInside(ownerPlayer))
                        return true;

                    // Owner is not inside area or owner unknown -> do NOT treat as excluded by this entry.
                    continue;
                }
            }

            // 1) exact reference (scene instance)
            if (entry == go) return true;

            // 2) child/parent relationship
            if (go.transform.IsChildOf(entry.transform) || entry.transform.IsChildOf(go.transform)) return true;

            // 3) root match
            if (entry == go.transform.root.gameObject || entry.transform.root == go.transform.root) return true;

            // 4) normalized name match (handles "(Clone)" and case differences)
            if (!string.IsNullOrEmpty(entry.name) && NormalizeName(entry.name) == goNorm) return true;

            // 5) tag fallback
            if (!string.IsNullOrEmpty(entry.tag) && entry.tag != "Untagged" && entry.tag == go.tag) return true;
        }

        return false;
    }

    static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return name.Replace("(Clone)", "").Trim();
    }

    // Apply immediate recoil to bullet: fling opposite its incoming direction using bulletRecoilForce.
    void ApplyBulletRecoil(Rigidbody2D bulletRb, Vector2 incomingVel)
    {
        if (bulletRb == null) return;

        Vector2 incomingDir;
        if (incomingVel.sqrMagnitude > 0.0001f) incomingDir = incomingVel.normalized;
        else incomingDir = new Vector2(Mathf.Sign(transform.localScale.x), 0f);

        // Base recoil direction is opposite the incoming direction.
        Vector2 baseDir = -incomingDir;

        // Add upward bias so bullet is flung backwards+upwards.
        Vector2 recoilDir = (baseDir + Vector2.up * bulletRecoilUpwardBias).normalized;

        // Ensure the bullet behaves as a dynamic physics body so the fling is visible.
        bulletRb.bodyType = RigidbodyType2D.Dynamic;

        // Set immediate velocity so the bullet is flung; magnitude controlled by bulletRecoilForce.
        bulletRb.linearVelocity = recoilDir * bulletRecoilForce;
    }

    void MakeThisBulletFallAndDisappear()
    {
        Rigidbody2D myRb = GetComponent<Rigidbody2D>();
        if (myRb == null) myRb = gameObject.AddComponent<Rigidbody2D>();
        myRb.gravityScale = bulletFallGravity;
        myRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        myRb.freezeRotation = false;

        var col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger) col.isTrigger = false;

        Destroy(gameObject, bulletFallLifetime);
    }

    IEnumerator MonitorAndDestroyWhenStill(Rigidbody2D rbToWatch, GameObject target)
    {
        float elapsedStill = 0f;

        while (true)
        {
            if (rbToWatch == null) yield break;

            if (rbToWatch.linearVelocity.sqrMagnitude <= objectStillVelocityThreshold * objectStillVelocityThreshold)
            {
                elapsedStill += Time.deltaTime;
                if (elapsedStill >= objectStillTimeToDestroy)
                    break;
            }
            else elapsedStill = 0f;

            yield return null;
        }

        if (objectAdditionalDelayAfterStill > 0f) yield return new WaitForSeconds(objectAdditionalDelayAfterStill);

        // Before destroying, ensure target is not in exclusion list
        if (target != null && IsInDoNotDestroyList(target))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Monitor aborted destroy: '{target.name}' is in do-not-destroy list.");
#endif
            yield break;
        }

        Destroy(target);
    }

    void SafeDestroy(GameObject obj)
    {
        // Double-check exclusion before destroying (defensive)
        if (obj != null && IsInDoNotDestroyList(obj))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Prevented destruction of excluded object '{obj.name}'.");
#endif
            return;
        }

        Destroy(obj);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (gunPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(gunPoint.position, 0.05f);
        }
    }

    // -----------------------
    // Editor & validation
    // -----------------------
    void OnValidate()
    {
        // If both are checked, prefer player mode by clearing bullet.
        if (isPlayer && isBullet)
        {
            isBullet = false;
        }

        // Allow both false: editor will show both checkboxes unchecked.
        EnforceUniqueKey(ref moveRightKey, "MoveRight");
        EnforceUniqueKey(ref moveLeftKey, "MoveLeft");
        EnforceUniqueKey(ref jumpKey, "Jump");
        EnforceUniqueKey(ref shootKey, "Shoot");
    }

    void EnforceUniqueKey(ref KeyCode keyToCheck, string fieldName)
    {
        if (keyToCheck == KeyCode.None) return;

        int occurrences = 0;
        if (moveRightKey == keyToCheck) occurrences++;
        if (moveLeftKey == keyToCheck) occurrences++;
        if (jumpKey == keyToCheck) occurrences++;
        if (shootKey == keyToCheck) occurrences++;

        if (occurrences > 1)
        {
            Debug.LogWarning($"Key {keyToCheck} assigned to multiple actions. Clearing duplicate assignment on '{fieldName}'.");
            keyToCheck = KeyCode.None;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerMovmentAndShooting))]
internal class PlayerMovmentAndShootingEditor : Editor
{
    SerializedProperty isPlayerProp;
    SerializedProperty isBulletProp;

    SerializedProperty moveRightKey;
    SerializedProperty moveRightAllowController;
    SerializedProperty moveRightController;

    SerializedProperty moveLeftKey;
    SerializedProperty moveLeftAllowController;
    SerializedProperty moveLeftController;

    SerializedProperty jumpKey;
    SerializedProperty jumpAllowController;
    SerializedProperty jumpController;

    SerializedProperty shootKey;
    SerializedProperty shootAllowController;
    SerializedProperty shootController;

    SerializedProperty bulletEnableRecoilOnLastHit;
    SerializedProperty bulletRecoilForce;
    SerializedProperty bulletRecoilUpwardBias;

    SerializedProperty doNotDestroyListProp;

    SerializedProperty iteratorProp;

    void OnEnable()
    {
        isPlayerProp = serializedObject.FindProperty("isPlayer");
        isBulletProp = serializedObject.FindProperty("isBullet");

        moveRightKey = serializedObject.FindProperty("moveRightKey");
        moveRightAllowController = serializedObject.FindProperty("moveRightAllowController");
        moveRightController = serializedObject.FindProperty("moveRightController");

        moveLeftKey = serializedObject.FindProperty("moveLeftKey");
        moveLeftAllowController = serializedObject.FindProperty("moveLeftAllowController");
        moveLeftController = serializedObject.FindProperty("moveLeftController");

        jumpKey = serializedObject.FindProperty("jumpKey");
        jumpAllowController = serializedObject.FindProperty("jumpAllowController");
        jumpController = serializedObject.FindProperty("jumpController");

        shootKey = serializedObject.FindProperty("shootKey");
        shootAllowController = serializedObject.FindProperty("shootAllowController");
        shootController = serializedObject.FindProperty("shootController");

        bulletEnableRecoilOnLastHit = serializedObject.FindProperty("bulletEnableRecoilOnLastHit");
        bulletRecoilForce = serializedObject.FindProperty("bulletRecoilForce");
        bulletRecoilUpwardBias = serializedObject.FindProperty("bulletRecoilUpwardBias");

        doNotDestroyListProp = serializedObject.FindProperty("doNotDestroyList");

        iteratorProp = serializedObject.GetIterator();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Mode selection: choose either PLAYER or BULLET. When one is selected the other is auto-cleared. You may uncheck to return to showing both choices.", MessageType.Info);
        EditorGUILayout.Space();

        // Always show both toggles so user can pick either; editor logic enforces exclusivity when toggling.
        bool prevPlayer = isPlayerProp.boolValue;
        bool prevBullet = isBulletProp.boolValue;

        bool newPlayer = EditorGUILayout.ToggleLeft("I am PLAYER", prevPlayer);
        bool newBullet = EditorGUILayout.ToggleLeft("I am BULLET", prevBullet);

        // Enforce exclusivity: if user turned one on, turn the other off.
        if (newPlayer != prevPlayer)
        {
            if (newPlayer) newBullet = false;
        }
        if (newBullet != prevBullet)
        {
            if (newBullet) newPlayer = false;
        }

        isPlayerProp.boolValue = newPlayer;
        isBulletProp.boolValue = newBullet;

        EditorGUILayout.Space();

        // Draw player input & settings only when PLAYER mode selected
        if (isPlayerProp.boolValue)
        {
            EditorGUILayout.LabelField("Player - Input", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(moveRightKey, new GUIContent("Move Right Key"));
            EditorGUILayout.PropertyField(moveRightAllowController, new GUIContent("Allow Controller (Move Right)"));
            if (moveRightAllowController.boolValue) EditorGUILayout.PropertyField(moveRightController, new GUIContent("Controller Mapping (Move Right)"));

            EditorGUILayout.PropertyField(moveLeftKey, new GUIContent("Move Left Key"));
            EditorGUILayout.PropertyField(moveLeftAllowController, new GUIContent("Allow Controller (Move Left)"));
            if (moveLeftAllowController.boolValue) EditorGUILayout.PropertyField(moveLeftController, new GUIContent("Controller Mapping (Move Left)"));

            EditorGUILayout.PropertyField(jumpKey, new GUIContent("Jump Key"));
            EditorGUILayout.PropertyField(jumpAllowController, new GUIContent("Allow Controller (Jump)"));
            if (jumpAllowController.boolValue) EditorGUILayout.PropertyField(jumpController, new GUIContent("Controller Mapping (Jump)"));

            EditorGUILayout.PropertyField(shootKey, new GUIContent("Shoot Key"));
            EditorGUILayout.PropertyField(shootAllowController, new GUIContent("Allow Controller (Shoot)"));
            if (shootAllowController.boolValue) EditorGUILayout.PropertyField(shootController, new GUIContent("Controller Mapping (Shoot)"));

            EditorGUILayout.Space();
        }

        // Draw bullet-only options only when BULLET mode selected
        if (isBulletProp.boolValue)
        {
            EditorGUILayout.LabelField("Bullet - recoil (bullet-only)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bulletEnableRecoilOnLastHit, new GUIContent("Enable Recoil On Last Hit"));
            if (bulletEnableRecoilOnLastHit.boolValue)
            {
                EditorGUILayout.PropertyField(bulletRecoilForce, new GUIContent("Bullet Recoil Force"));
                EditorGUILayout.PropertyField(bulletRecoilUpwardBias, new GUIContent("Bullet Recoil Upward Bias"));
            }
            EditorGUILayout.Space();

            // Fixed-size "do not destroy" list: exactly 50 slots, no dynamic expansion.
            EditorGUILayout.LabelField("Bullet - Do Not Destroy List (50 slots)", EditorStyles.boldLabel);
            if (doNotDestroyListProp.arraySize != 50) doNotDestroyListProp.arraySize = 50;
            for (int i = 0; i < 50; i++)
            {
                var el = doNotDestroyListProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(el, new GUIContent($"Slot {i + 1}"));
            }

            EditorGUILayout.Space();
        }

        // Draw remaining properties (movement, shooting, bullet physics, etc.) but avoid duplicating fields above.
        EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
        iteratorProp = serializedObject.GetIterator();
        bool enter = true;
        while (iteratorProp.NextVisible(enter))
        {
            enter = false;
            if (iteratorProp.name == "m_Script") continue;

            // Skip the properties we've already drawn explicitly
            if (iteratorProp.name == isPlayerProp.name ||
                iteratorProp.name == isBulletProp.name ||
                iteratorProp.name == moveRightKey.name ||
                iteratorProp.name == moveRightAllowController.name ||
                iteratorProp.name == moveRightController.name ||
                iteratorProp.name == moveLeftKey.name ||
                iteratorProp.name == moveLeftAllowController.name ||
                iteratorProp.name == moveLeftController.name ||
                iteratorProp.name == jumpKey.name ||
                iteratorProp.name == jumpAllowController.name ||
                iteratorProp.name == jumpController.name ||
                iteratorProp.name == shootKey.name ||
                iteratorProp.name == shootAllowController.name ||
                iteratorProp.name == shootController.name ||
                iteratorProp.name == bulletEnableRecoilOnLastHit.name ||
                iteratorProp.name == bulletRecoilForce.name ||
                iteratorProp.name == bulletRecoilUpwardBias.name ||
                iteratorProp.name == doNotDestroyListProp.name)
            {
                continue;
            }

            EditorGUILayout.PropertyField(iteratorProp, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif