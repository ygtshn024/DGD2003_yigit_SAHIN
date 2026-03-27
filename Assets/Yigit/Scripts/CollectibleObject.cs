using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    [Header("Highlight")]
    public GameObject highlightVisual;
    public Light highlightLight;

    public bool IsCollected { get; private set; }

    private void Awake()
    {
        SetHighlighted(false);
    }

    public void Collect()
    {
        if (IsCollected)
        {
            return;
        }

        IsCollected = true;
        SetHighlighted(false);
        gameObject.SetActive(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(highlighted);
        }

        if (highlightLight != null)
        {
            highlightLight.enabled = highlighted;
        }
    }
}
