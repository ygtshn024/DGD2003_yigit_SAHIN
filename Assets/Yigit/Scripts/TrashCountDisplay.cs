using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
/// <summary>
/// Canvas üzerindeki Text (TMP) objesine bu scripti ekle.
/// Player referansı vermene gerek yok; FirstPersonController ilerlemeyi buradan günceller.
/// </summary>
[DisallowMultipleComponent]
public class TrashCountDisplay : MonoBehaviour
{
    public static TrashCountDisplay Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI label;

    [Tooltip("İlerleme: {0} = sahnede kalan toplanabilir (topladıkça azalır).")]
    public string displayFormat = "Kalan: {0}";

    [Tooltip("Tüm toplanabilirler bitince bu metin gösterilir.")]
    public string completeMessage = "Tebrikler! Tum toplanabilir objeleri topladin!";

    [Header("Renkler")]
    [FormerlySerializedAs("colorBelowThreshold")]
    [Tooltip("Fırlatmaya hazır envanter (throw eşiği) altındayken ilerleme metni.")]
    [SerializeField] private Color colorBelowThrowThreshold = Color.white;
    [FormerlySerializedAs("colorAtOrAboveThreshold")]
    [Tooltip("Envanter fırlatma eşiğine ulaştığında ilerleme metni.")]
    [SerializeField] private Color colorAtOrAboveThrowThreshold = Color.red;
    [Tooltip("Tüm objeler toplanınca tebrik metni.")]
    [SerializeField] private Color colorAllCollected = new Color(0.35f, 0.95f, 0.45f);

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TrashCountDisplay: Sahne içinde birden fazla TrashCountDisplay var. Son etkin olan kullanılacak: " + name);
        }

        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static bool SetProgress(
        int gatheredInScene,
        int totalInScene,
        bool allSceneCollectiblesDone,
        string progressFormatOverride,
        string completeMessageOverride,
        int inventoryForThrow,
        int throwReadyThreshold,
        string inventoryFullHintLine = null,
        bool trashBinGoalMet = false,
        string trashBinWinMessage = null,
        string trashBinProgressLine = null)
    {
        if (Instance == null || Instance.label == null)
        {
            return false;
        }

        Instance.ApplyProgress(
            gatheredInScene,
            totalInScene,
            allSceneCollectiblesDone,
            progressFormatOverride,
            completeMessageOverride,
            inventoryForThrow,
            throwReadyThreshold,
            inventoryFullHintLine,
            trashBinGoalMet,
            trashBinWinMessage,
            trashBinProgressLine);
        return true;
    }

    private void ApplyProgress(
        int gathered,
        int total,
        bool allDone,
        string progressFormatOverride,
        string completeMessageOverride,
        int inventoryForThrow,
        int throwReadyThreshold,
        string inventoryFullHintLine,
        bool trashBinGoalMet,
        string trashBinWinMessage,
        string trashBinProgressLine)
    {
        string progressFmt = string.IsNullOrEmpty(progressFormatOverride) ? displayFormat : progressFormatOverride;
        string winMsg = string.IsNullOrEmpty(completeMessageOverride) ? completeMessage : completeMessageOverride;

        if (trashBinGoalMet)
        {
            string binMsg = string.IsNullOrEmpty(trashBinWinMessage) ? winMsg : trashBinWinMessage;
            label.text = binMsg;
            label.color = colorAllCollected;
        }
        else if (allDone && total > 0)
        {
            label.text = winMsg;
            label.color = colorAllCollected;
        }
        else
        {
            int remaining = Mathf.Max(0, total - gathered);
            string body = string.Format(progressFmt, remaining);
            if (!string.IsNullOrEmpty(inventoryFullHintLine))
            {
                body += "\n" + inventoryFullHintLine;
            }

            if (!string.IsNullOrEmpty(trashBinProgressLine))
            {
                body += "\n" + trashBinProgressLine;
            }

            label.text = body;
            label.color = inventoryForThrow >= throwReadyThreshold ? colorAtOrAboveThrowThreshold : colorBelowThrowThreshold;
        }

        label.enabled = true;
        label.gameObject.SetActive(true);

        Canvas canvas = label.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
        }

        label.ForceMeshUpdate(true);
        Canvas.ForceUpdateCanvases();
    }
}
