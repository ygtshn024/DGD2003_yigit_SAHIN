using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas üzerindeki Text (TMP) objesine bu scripti ekle.
/// Player referansı vermene gerek yok; FirstPersonController sayıyı buradan günceller.
/// </summary>
[DisallowMultipleComponent]
public class TrashCountDisplay : MonoBehaviour
{
    public static TrashCountDisplay Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI label;

    [Tooltip("Örnek: Toplanan Çöp: {0}")]
    public string displayFormat = "Toplanan Çöp: {0}";

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

    public static bool SetCount(int count, string formatOverride = null)
    {
        if (Instance == null || Instance.label == null)
        {
            return false;
        }

        Instance.Apply(count, formatOverride);
        return true;
    }

    private void Apply(int count, string formatOverride)
    {
        string format = string.IsNullOrEmpty(formatOverride) ? displayFormat : formatOverride;
        label.text = string.Format(format, count);
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
