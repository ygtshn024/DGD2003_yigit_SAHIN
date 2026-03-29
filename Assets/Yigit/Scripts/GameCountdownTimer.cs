using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Counts down MM:SS. Can freeze on win. On time up: optional fail message, wait, 3-2-1, reload scene.
/// </summary>
public class GameCountdownTimer : MonoBehaviour
{
    [Tooltip("Total countdown length in minutes (1.3 = 78 seconds).")]
    [SerializeField]
    private float durationMinutes = 1.3f;

    [Tooltip("Optional: drag your HUD Text (TMP). If null, only events / public API update.")]
    [SerializeField]
    private TextMeshProUGUI displayText;

    [Tooltip("If true, keeps counting when game is paused (Time.timeScale = 0).")]
    [SerializeField]
    private bool useUnscaledTime;

    [SerializeField]
    private bool startOnEnable = true;

    [Header("Warning (blink)")]
    [SerializeField]
    private float warningThresholdSeconds = 30f;

    [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField]
    private Color warningColor = Color.red;

    [SerializeField]
    private float blinkPulseHz = 2f;

    [Header("Time up")]
    [SerializeField]
    private UnityEvent onTimerFinished;

    [Tooltip("If true and round not won, show fail text then reload active scene.")]
    [SerializeField]
    private bool restartSceneOnTimeUp = true;

    [Tooltip("Shown when time hits 0 (if round not won).")]
    [SerializeField]
    private TextMeshProUGUI roundStatusText;

    [SerializeField]
    private string timeFailMessage = "Basarisiz oldun";

    [Tooltip("Seconds to show fail message before 3-2-1.")]
    [SerializeField]
    private float failMessageHoldSeconds = 3f;

    [Tooltip("Seconds per countdown number (3, 2, 1).")]
    [SerializeField]
    private float countdownTickSeconds = 1f;

    [SerializeField]
    private FirstPersonController playerController;

    private float remainingSeconds;
    private bool isRunning;
    private bool hasFinished;
    private int lastShownTotalSeconds = int.MinValue;
    private bool timeFrozen;
    private bool failureFlowStarted;

    public float RemainingSeconds => Mathf.Max(0f, remainingSeconds);
    public bool HasFinished => hasFinished;
    public bool IsRunning => isRunning;
    public bool IsFrozen => timeFrozen;

    private void OnEnable()
    {
        if (startOnEnable)
        {
            BeginCountdown();
        }
    }

    /// <summary>Resets and starts the timer from full duration.</summary>
    public void BeginCountdown()
    {
        timeFrozen = false;
        failureFlowStarted = false;
        if (roundStatusText != null)
        {
            roundStatusText.text = string.Empty;
            roundStatusText.gameObject.SetActive(false);
        }

        remainingSeconds = Mathf.Max(0f, durationMinutes * 60f);
        isRunning = remainingSeconds > 0f;
        hasFinished = false;
        lastShownTotalSeconds = int.MinValue;
        RefreshDisplay();
        ApplyLabelColorImmediate(normalColor);
    }

    /// <summary>Stops countdown; display stays at current value (win freeze).</summary>
    public void FreezeCountdown()
    {
        timeFrozen = true;
        isRunning = false;
    }

    /// <summary>Stops without freeze flag (legacy).</summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (timeFrozen)
        {
            UpdateLabelVisuals();
            return;
        }

        if (isRunning && !hasFinished)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            remainingSeconds -= dt;

            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                isRunning = false;
                hasFinished = true;
                lastShownTotalSeconds = int.MinValue;
                RefreshDisplay();
                onTimerFinished?.Invoke();
                TryBeginFailureFlow();
            }
            else
            {
                RefreshDisplay();
            }
        }

        UpdateLabelVisuals();
    }

    private void TryBeginFailureFlow()
    {
        if (FirstPersonController.RoundWon)
        {
            return;
        }

        if (!restartSceneOnTimeUp)
        {
            return;
        }

        if (failureFlowStarted)
        {
            return;
        }

        failureFlowStarted = true;
        StartCoroutine(FailureAndRestartRoutine());
    }

    private IEnumerator FailureAndRestartRoutine()
    {
        if (FirstPersonController.RoundWon)
        {
            yield break;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }

        playerController?.BeginTimeExpiredSequence();

        if (roundStatusText != null)
        {
            roundStatusText.gameObject.SetActive(true);
            roundStatusText.text = timeFailMessage;
        }

        yield return new WaitForSecondsRealtime(failMessageHoldSeconds);

        if (FirstPersonController.RoundWon)
        {
            yield break;
        }

        if (roundStatusText != null)
        {
            for (int n = 3; n >= 1; n--)
            {
                if (FirstPersonController.RoundWon)
                {
                    yield break;
                }

                roundStatusText.text = n.ToString();
                yield return new WaitForSecondsRealtime(countdownTickSeconds);
            }

            roundStatusText.text = string.Empty;
        }
        else
        {
            yield return new WaitForSecondsRealtime(countdownTickSeconds * 3f);
        }

        if (FirstPersonController.RoundWon)
        {
            yield break;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RefreshDisplay()
    {
        if (displayText == null)
        {
            return;
        }

        int total = Mathf.Max(0, Mathf.FloorToInt(remainingSeconds));
        if (total == lastShownTotalSeconds && remainingSeconds > 0f)
        {
            return;
        }

        lastShownTotalSeconds = total;
        displayText.text = FormatDigitalClock(remainingSeconds);
    }

    private void UpdateLabelVisuals()
    {
        if (displayText == null)
        {
            return;
        }

        if (hasFinished || remainingSeconds <= 0f)
        {
            ApplyLabelColorImmediate(normalColor);
            return;
        }

        if (remainingSeconds < warningThresholdSeconds)
        {
            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            float pulse = Mathf.PingPong(t * blinkPulseHz * 2f, 1f);
            Color c = warningColor;
            c.a = Mathf.Lerp(0.35f, 1f, pulse);
            displayText.color = c;
        }
        else
        {
            ApplyLabelColorImmediate(normalColor);
        }
    }

    private void ApplyLabelColorImmediate(Color c)
    {
        if (displayText == null)
        {
            return;
        }

        c.a = 1f;
        displayText.color = c;
    }

    public static string FormatDigitalClock(float secondsLeft)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(secondsLeft));
        int minutes = total / 60;
        int secs = total % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    public string GetFormattedTime()
    {
        return FormatDigitalClock(remainingSeconds);
    }
}
