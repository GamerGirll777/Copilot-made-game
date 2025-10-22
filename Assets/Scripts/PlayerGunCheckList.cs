using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach this to your Player GameObject. This component does nothing at runtime.
/// It only provides a checklist (visible in the Inspector) to guide you through
/// setting up the gun, gun point and bullet prefab for the PlayerMovmentAndShooting script.
/// All functionality is editor-only; runtime behavior is intentionally empty.
/// </summary>
[DisallowMultipleComponent]
public class GunSetupChecklist : MonoBehaviour
{
    [Header("Gun Setup Checklist (tick items as you complete them)")]
    [Tooltip("Create a child GameObject named 'Gun' (SpriteRenderer with a black gun sprite).")]
    public bool createdGunChild = false;

    [Tooltip("Set the Gun GameObject inactive by default (uncheck the checkbox in the Hierarchy).")]
    public bool gunSetInactive = false;

    [Tooltip("Assign the Gun GameObject to the PlayerMovmentAndShooting 'gunObject' field.")]
    public bool gunAssignedToScript = false;

    [Tooltip("Create a child Transform under Gun named 'GunPoint' at the muzzle position.")]
    public bool createdGunPoint = false;

    [Tooltip("Assign the GunPoint Transform to the PlayerMovmentAndShooting 'gunPoint' field.")]
    public bool gunPointAssignedToScript = false;

    [Space]
    [Tooltip("Create a Bullet prefab (small circle sprite).")]
    public bool createdBulletPrefab = false;

    [Tooltip("Bullet prefab has a CircleCollider2D with Is Trigger = true.")]
    public bool bulletHasCollider = false;

    [Tooltip("Bullet prefab has a Rigidbody2D (Gravity Scale = 0, Freeze Rotation enabled).")]
    public bool bulletHasRigidbody = false;

    [Tooltip("Drag the Bullet prefab into the PlayerMovmentAndShooting 'bulletPrefab' field.")]
    public bool bulletAssignedToScript = false;

    [Space]
    [Tooltip("Create a child Transform 'GroundCheck' at the player's feet and assign to PlayerMovmentAndShooting.")]
    public bool groundCheckCreatedAndAssigned = false;

    [Tooltip("Set 'groundLayer' on PlayerMovmentAndShooting to include ground colliders.")]
    public bool groundLayerSet = false;

    [Space]
    [Tooltip("Adjust movement/jump values (moveSpeed, jumpForce, fall/lowJump multipliers) to taste.")]
    public bool tunedMovementAndJump = false;

    [Tooltip("Adjust bulletSpeed, bulletLifetime, bulletMaxHits, shootCooldown, gunShowDuration.")]
    public bool tunedBulletAndShooting = false;

    [Space]
    [Tooltip("(Optional) Mark destructible targets with tag 'Destructible' or assign them to a layer.")]
    public bool destructibleTagOrLayerSet = false;

    [Space]
    [Tooltip("Final test: Play and verify movement, jump, gun appear, bullet spawns and destroys targets.")]
    public bool testedInPlayMode = false;

    // Intentionally no runtime logic.
}

#if UNITY_EDITOR
[CustomEditor(typeof(GunSetupChecklist))]
internal class GunSetupChecklistEditor : Editor
{
    SerializedProperty createdGunChild;
    SerializedProperty gunSetInactive;
    SerializedProperty gunAssignedToScript;
    SerializedProperty createdGunPoint;
    SerializedProperty gunPointAssignedToScript;
    SerializedProperty createdBulletPrefab;
    SerializedProperty bulletHasCollider;
    SerializedProperty bulletHasRigidbody;
    SerializedProperty bulletAssignedToScript;
    SerializedProperty groundCheckCreatedAndAssigned;
    SerializedProperty groundLayerSet;
    SerializedProperty tunedMovementAndJump;
    SerializedProperty tunedBulletAndShooting;
    SerializedProperty destructibleTagOrLayerSet;
    SerializedProperty testedInPlayMode;

    void OnEnable()
    {
        createdGunChild = serializedObject.FindProperty(nameof(GunSetupChecklist.createdGunChild));
        gunSetInactive = serializedObject.FindProperty(nameof(GunSetupChecklist.gunSetInactive));
        gunAssignedToScript = serializedObject.FindProperty(nameof(GunSetupChecklist.gunAssignedToScript));
        createdGunPoint = serializedObject.FindProperty(nameof(GunSetupChecklist.createdGunPoint));
        gunPointAssignedToScript = serializedObject.FindProperty(nameof(GunSetupChecklist.gunPointAssignedToScript));
        createdBulletPrefab = serializedObject.FindProperty(nameof(GunSetupChecklist.createdBulletPrefab));
        bulletHasCollider = serializedObject.FindProperty(nameof(GunSetupChecklist.bulletHasCollider));
        bulletHasRigidbody = serializedObject.FindProperty(nameof(GunSetupChecklist.bulletHasRigidbody));
        bulletAssignedToScript = serializedObject.FindProperty(nameof(GunSetupChecklist.bulletAssignedToScript));
        groundCheckCreatedAndAssigned = serializedObject.FindProperty(nameof(GunSetupChecklist.groundCheckCreatedAndAssigned));
        groundLayerSet = serializedObject.FindProperty(nameof(GunSetupChecklist.groundLayerSet));
        tunedMovementAndJump = serializedObject.FindProperty(nameof(GunSetupChecklist.tunedMovementAndJump));
        tunedBulletAndShooting = serializedObject.FindProperty(nameof(GunSetupChecklist.tunedBulletAndShooting));
        destructibleTagOrLayerSet = serializedObject.FindProperty(nameof(GunSetupChecklist.destructibleTagOrLayerSet));
        testedInPlayMode = serializedObject.FindProperty(nameof(GunSetupChecklist.testedInPlayMode));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Gun Setup Checklist — tick items as you complete them. This component does nothing at runtime.", MessageType.Info);
        EditorGUILayout.Space();

        DrawGroup("Gun Visual", () =>
        {
            EditorGUILayout.PropertyField(createdGunChild);
            if (!createdGunChild.boolValue)
                EditorGUILayout.HelpBox("Right-click Player -> Create Empty -> name it 'Gun'. Add SpriteRenderer and set gun sprite.", MessageType.None);

            EditorGUILayout.PropertyField(gunSetInactive);
            if (!gunSetInactive.boolValue)
                EditorGUILayout.HelpBox("Uncheck the Gun GameObject in the Hierarchy so it is hidden by default.", MessageType.None);

            EditorGUILayout.PropertyField(gunAssignedToScript);
            if (!gunAssignedToScript.boolValue)
                EditorGUILayout.HelpBox("Select Player, drag 'Gun' into PlayerMovmentAndShooting's 'gunObject' field.", MessageType.None);
        });

        DrawGroup("Muzzle / Spawn Point", () =>
        {
            EditorGUILayout.PropertyField(createdGunPoint);
            if (!createdGunPoint.boolValue)
                EditorGUILayout.HelpBox("Create a child Transform under 'Gun' named 'GunPoint' and position it at the muzzle.", MessageType.None);

            EditorGUILayout.PropertyField(gunPointAssignedToScript);
            if (!gunPointAssignedToScript.boolValue)
                EditorGUILayout.HelpBox("Drag the 'GunPoint' Transform into PlayerMovmentAndShooting's 'gunPoint' field.", MessageType.None);
        });

        DrawGroup("Bullet Prefab", () =>
        {
            EditorGUILayout.PropertyField(createdBulletPrefab);
            if (!createdBulletPrefab.boolValue)
                EditorGUILayout.HelpBox("Create a Bullet sprite, add CircleCollider2D (Is Trigger), Rigidbody2D (Gravity=0), then make prefab.", MessageType.None);

            EditorGUILayout.PropertyField(bulletHasCollider);
            EditorGUILayout.PropertyField(bulletHasRigidbody);
            EditorGUILayout.PropertyField(bulletAssignedToScript);
            if (!bulletAssignedToScript.boolValue)
                EditorGUILayout.HelpBox("Drag the Bullet prefab into PlayerMovmentAndShooting's 'bulletPrefab' field.", MessageType.None);
        });

        DrawGroup("Ground & Movement", () =>
        {
            EditorGUILayout.PropertyField(groundCheckCreatedAndAssigned);
            if (!groundCheckCreatedAndAssigned.boolValue)
                EditorGUILayout.HelpBox("Create a child Transform at the player's feet named 'GroundCheck' and assign it to the script.", MessageType.None);

            EditorGUILayout.PropertyField(groundLayerSet);
            if (!groundLayerSet.boolValue)
                EditorGUILayout.HelpBox("Create a 'Ground' layer and set the ground objects to that layer; assign it to the script's groundLayer.", MessageType.None);

            EditorGUILayout.PropertyField(tunedMovementAndJump);
            EditorGUILayout.PropertyField(tunedBulletAndShooting);
        });

        DrawGroup("Optional & Final", () =>
        {
            EditorGUILayout.PropertyField(destructibleTagOrLayerSet);
            if (!destructibleTagOrLayerSet.boolValue)
                EditorGUILayout.HelpBox("If you want bullets to only destroy certain objects, tag them 'Destructible' or set a layer and modify the script accordingly.", MessageType.None);

            EditorGUILayout.PropertyField(testedInPlayMode);
            if (!testedInPlayMode.boolValue)
                EditorGUILayout.HelpBox("Play the scene and verify movement, jump, gun behavior, and bullets.", MessageType.None);
        });

        EditorGUILayout.Space();

        // Convenience buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Check All")) SetAll(true);
        if (GUILayout.Button("Uncheck All")) SetAll(false);
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawGroup(string title, System.Action drawContents)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        drawContents();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    void SetAll(bool value)
    {
        createdGunChild.boolValue = value;
        gunSetInactive.boolValue = value;
        gunAssignedToScript.boolValue = value;
        createdGunPoint.boolValue = value;
        gunPointAssignedToScript.boolValue = value;
        createdBulletPrefab.boolValue = value;
        bulletHasCollider.boolValue = value;
        bulletHasRigidbody.boolValue = value;
        bulletAssignedToScript.boolValue = value;
        groundCheckCreatedAndAssigned.boolValue = value;
        groundLayerSet.boolValue = value;
        tunedMovementAndJump.boolValue = value;
        tunedBulletAndShooting.boolValue = value;
        destructibleTagOrLayerSet.boolValue = value;
        testedInPlayMode.boolValue = value;
    }
}
#endif