using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Attach this to a TextMeshProUGUI text object that already contains the label "Reload Time".
/// The script updates that text to show a countdown (updated in 0.1s intervals).
/// 
/// Usage:
/// - Assign the TextMeshProUGUI component (or let Reset() auto-assign).
/// - Optionally assign the PlayerMovmentAndShooting instance in `player`.
///   * If `autoDetectFromGunObject` is true and player.gunObject is assigned,
///     the script will start the countdown automatically when the gunObject becomes active.
///   * Or call `StartReload(duration)` from your player script when a shot is fired.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ReloadTimerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PlayerMovmentAndShooting component (optional). Used automatically if set.")]
    public PlayerMovmentAndShooting player;

    [Tooltip("TextMeshProUGUI component to update (auto-filled if left empty).")]
    public TextMeshProUGUI textComponent;

    [Header("Display")]
    [Tooltip("Label text shown before the numeric countdown (keeps whatever you already have but used for formatting).")]
    public string label = "Reload Time: ";

    [Tooltip("Text shown when reload is finished.")]
    public string readyText = "Ready";

    [Header("Behavior")]
    [Tooltip("If true and `player` is assigned and player.gunObject is assigned, the countdown will start automatically when the gunObject becomes active.")]
    public bool autoDetectFromGunObject = true;

    [Tooltip("Interval in seconds for updating the display (0.1s as requested).")]
    [Range(0.01f, 1f)]
    public float updateInterval = 0.1f;

    // runtime
    float remaining = 0f;
    Coroutine countdownCoroutine;
    bool lastGunActive = false;

    void Reset()
    {
        // Try to auto-assign the TMP component on the same GameObject
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        // Initialize display (assume ready)
        UpdateText(0f);
    }

    void Update()
    {
        if (player != null && autoDetectFromGunObject && player.gunObject != null)
        {
            bool isActive = player.gunObject.activeSelf;
            // gunObject becomes active on shoot in PlayerMovmentAndShooting.DoShootRoutine
            if (isActive && !lastGunActive)
            {
                // start countdown using player's cooldown
                StartReload(player.shootCooldown);
            }

            lastGunActive = isActive;
        }
    }

    /// <summary>
    /// Starts the reload countdown for the given duration (seconds).
    /// Call this from your player script if you prefer explicit notification instead of auto-detection.
    /// </summary>
    public void StartReload(float duration)
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(RunCountdown(duration));
    }

    /// <summary>
    /// Stops any active countdown and sets display to ready state.
    /// </summary>
    public void CancelReload()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        remaining = 0f;
        UpdateText(0f);
    }

    IEnumerator RunCountdown(float duration)
    {
        // Guard
        if (duration <= 0f)
        {
            UpdateText(0f);
            yield break;
        }

        remaining = duration;
        while (remaining > 0f)
        {
            UpdateText(remaining);
            yield return new WaitForSeconds(updateInterval);
            remaining -= updateInterval;
            if (remaining < 0f) remaining = 0f;
        }

        UpdateText(0f);
        countdownCoroutine = null;
    }

    void UpdateText(float value)
    {
        if (textComponent == null) return;

        if (value <= 0f)
        {
            textComponent.text = $"{label}{readyText}";
        }
        else
        {
            // show with one decimal place as requested (0.1s steps)
            textComponent.text = $"{label}{value:F1}s";
        }
    }
}