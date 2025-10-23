using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class EffectAreas : MonoBehaviour
{
    [Header("Area Type (choose one)")]
    [Tooltip("If checked this area will change the player's reload time while the player is inside.")]
    public bool isReloadArea = false;
    [Tooltip("If checked this area will change the player's bullet max hits while the player is inside.")]
    public bool isMaxHitsArea = false;
    [Tooltip("If checked this area will change the bullet lifetime settings while the player is inside.")]
    public bool isLifetimeArea = false;

    [Header("Reload Area (shown when Reload Area checked)")]
    [Tooltip("New reload time (seconds) applied to the player while inside this area.")]
    public float reloadTimeChange = 1f;

    [Header("Max Hits Area (shown when Max Hits Area checked)")]
    [Tooltip("New bullet max hits applied to the player while inside this area (0 = unlimited).")]
    public int maxHitsChange = 3;

    [Header("Lifetime Area (shown when Life Time Area checked)")]
    [Tooltip("How long bullets live in the air (seconds) while player is inside this area.")]
    public float bulletLifeFlying = 5f;
    [Tooltip("How long bullets live after their last hit (seconds) while player is inside this area).")]
    public float bulletLifeBouncing = 2f;

    [Header("Gizmo")]
    [Tooltip("Gizmo color used to show this area in the editor (only visible in Scene view).")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.25f);

    // Tracks original player values so we can restore them when the player exits the area.
    class OriginalValues
    {
        public float shootCooldown;
        public int bulletMaxHits;
        public float bulletLifetime;
        public float bulletFallLifetime;
    }

    // Store original values per player instance (handles multiple players if present).
    readonly Dictionary<PlayerMovmentAndShooting, OriginalValues> _originals = new Dictionary<PlayerMovmentAndShooting, OriginalValues>();

    void Reset()
    {
        // Ensure the Collider2D exists and is a trigger so OnTriggerEnter2D/Exit works.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnValidate()
    {
        // Prefer single selection in the inspector: if user turns one on, turn the others off.
        // This logic mirrors the requested "check 1 of 3 boxes" behavior.
        int checkedCount = 0;
        if (isReloadArea) checkedCount++;
        if (isMaxHitsArea) checkedCount++;
        if (isLifetimeArea) checkedCount++;

        // If multiple selected keep the most recently toggled one (best-effort).
        // Unity's serialization makes it hard to detect which changed; we enforce single selection here by clearing others when >1.
        if (checkedCount > 1)
        {
            // If more than one is true, reset all to false so the user can choose clearly.
            isReloadArea = false;
            isMaxHitsArea = false;
            isLifetimeArea = false;
        }

        // Make sure collider is trigger for detection
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        var player = other.GetComponent<PlayerMovmentAndShooting>() ?? other.GetComponentInParent<PlayerMovmentAndShooting>();
        if (player == null) return;

        // If we already applied this area to the player, do nothing.
        if (_originals.ContainsKey(player)) return;

        // Capture originals and apply overrides
        var orig = new OriginalValues
        {
            shootCooldown = player.shootCooldown,
            bulletMaxHits = player.bulletMaxHits,
            bulletLifetime = player.bulletLifetime,
            bulletFallLifetime = player.bulletFallLifetime
        };

        // Save originals
        _originals[player] = orig;

        // Apply requested changes (only the fields for the selected area)
        if (isReloadArea)
        {
            player.shootCooldown = reloadTimeChange;
        }

        if (isMaxHitsArea)
        {
            player.bulletMaxHits = maxHitsChange;
        }

        if (isLifetimeArea)
        {
            player.bulletLifetime = bulletLifeFlying;
            player.bulletFallLifetime = bulletLifeBouncing;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;

        var player = other.GetComponent<PlayerMovmentAndShooting>() ?? other.GetComponentInParent<PlayerMovmentAndShooting>();
        if (player == null) return;

        // If we did not store originals for this player, nothing to restore.
        if (!_originals.TryGetValue(player, out var orig)) return;

        // Restore originals
        player.shootCooldown = orig.shootCooldown;
        player.bulletMaxHits = orig.bulletMaxHits;
        player.bulletLifetime = orig.bulletLifetime;
        player.bulletFallLifetime = orig.bulletFallLifetime;

        _originals.Remove(player);
    }

    void OnDisable()
    {
        // If the area is disabled while players are inside, restore saved values.
        RestoreAll();
    }

    void OnDestroy()
    {
        RestoreAll();
    }

    void RestoreAll()
    {
        foreach (var kvp in _originals)
        {
            var player = kvp.Key;
            var orig = kvp.Value;
            if (player != null)
            {
                player.shootCooldown = orig.shootCooldown;
                player.bulletMaxHits = orig.bulletMaxHits;
                player.bulletLifetime = orig.bulletLifetime;
                player.bulletFallLifetime = orig.bulletFallLifetime;
            }
        }
        _originals.Clear();
    }

    // Optional: visually show the area in the Scene view even if the object has no renderer.
    void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = gizmoColor;

        // Draw different gizmo depending on collider type
        if (col is BoxCollider2D box)
        {
            var pos = (Vector3)box.offset + transform.position;
            var size = new Vector3(box.size.x * transform.lossyScale.x, box.size.y * transform.lossyScale.y, 1f);
            Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
        }
        else if (col is CircleCollider2D circle)
        {
            var pos = (Vector3)circle.offset + transform.position;
            Gizmos.DrawSphere(pos, circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
        }
        else
        {
            // Fallback: draw collider bounds
            Bounds b = col.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EffectAreas))]
internal class EffectAreasEditor : Editor
{
    SerializedProperty isReloadArea;
    SerializedProperty isMaxHitsArea;
    SerializedProperty isLifetimeArea;

    SerializedProperty reloadTimeChange;
    SerializedProperty maxHitsChange;
    SerializedProperty bulletLifeFlying;
    SerializedProperty bulletLifeBouncing;

    SerializedProperty gizmoColor;

    void OnEnable()
    {
        isReloadArea = serializedObject.FindProperty("isReloadArea");
        isMaxHitsArea = serializedObject.FindProperty("isMaxHitsArea");
        isLifetimeArea = serializedObject.FindProperty("isLifetimeArea");

        reloadTimeChange = serializedObject.FindProperty("reloadTimeChange");
        maxHitsChange = serializedObject.FindProperty("maxHitsChange");
        bulletLifeFlying = serializedObject.FindProperty("bulletLifeFlying");
        bulletLifeBouncing = serializedObject.FindProperty("bulletLifeBouncing");

        gizmoColor = serializedObject.FindProperty("gizmoColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Choose exactly one area type. While the player remains inside this (trigger) collider the selected effect will be applied. When the player exits values are restored.", MessageType.Info);
        EditorGUILayout.Space();

        // Show the three toggles, but enforce single selection here.
        bool prevReload = isReloadArea.boolValue;
        bool prevMax = isMaxHitsArea.boolValue;
        bool prevLife = isLifetimeArea.boolValue;

        bool newReload = EditorGUILayout.ToggleLeft("Reload Area", prevReload);
        bool newMax = EditorGUILayout.ToggleLeft("Max Hits Area", prevMax);
        bool newLife = EditorGUILayout.ToggleLeft("Life Time Area", prevLife);

        // If the user turned one on, turn the others off.
        if (newReload && !prevReload)
        {
            newMax = false;
            newLife = false;
        }
        if (newMax && !prevMax)
        {
            newReload = false;
            newLife = false;
        }
        if (newLife && !prevLife)
        {
            newReload = false;
            newMax = false;
        }

        isReloadArea.boolValue = newReload;
        isMaxHitsArea.boolValue = newMax;
        isLifetimeArea.boolValue = newLife;

        EditorGUILayout.Space();

        // Show the appropriate fields for the selected type
        if (isReloadArea.boolValue)
        {
            EditorGUILayout.LabelField("Reload Area Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(reloadTimeChange, new GUIContent("Reload Time (seconds)"));
            EditorGUILayout.Space();
        }
        else if (isMaxHitsArea.boolValue)
        {
            EditorGUILayout.LabelField("Max Hits Area Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxHitsChange, new GUIContent("Bullet Max Hits (0 = unlimited)"));
            EditorGUILayout.Space();
        }
        else if (isLifetimeArea.boolValue)
        {
            EditorGUILayout.LabelField("Lifetime Area Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bulletLifeFlying, new GUIContent("Bullet Life (flying) seconds"));
            EditorGUILayout.PropertyField(bulletLifeBouncing, new GUIContent("Bullet Life (after last hit) seconds"));
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField("Gizmo", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(gizmoColor, new GUIContent("Gizmo Color"));

        // Draw defaults for the rest of properties (none in this class beyond what we explicitly show)
        serializedObject.ApplyModifiedProperties();

        // Editor-time help: ensure there is a Collider2D and isTrigger is set
        var t = (EffectAreas)target;
        var col = t.GetComponent<Collider2D>();
        if (col == null)
        {
            EditorGUILayout.HelpBox("No Collider2D found on this GameObject. EffectAreas requires a Collider2D (set to isTrigger) to detect the player. Click the button below to add a BoxCollider2D as a trigger.", MessageType.Warning);
            if (GUILayout.Button("Add BoxCollider2D (isTrigger)"))
            {
                var added = Undo.AddComponent<BoxCollider2D>(t.gameObject);
                added.isTrigger = true;
                EditorUtility.SetDirty(t);
            }
        }
        else if (!col.isTrigger)
        {
            EditorGUILayout.HelpBox("Collider2D is not marked as 'isTrigger'. The area will not detect the player unless the collider is a trigger.", MessageType.Warning);
            if (GUILayout.Button("Set isTrigger = true"))
            {
                Undo.RecordObject(col, "Set isTrigger");
                col.isTrigger = true;
                EditorUtility.SetDirty(col);
            }
        }
    }
}
#endif