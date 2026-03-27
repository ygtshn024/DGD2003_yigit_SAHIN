using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    public bool IsCollected { get; private set; }

    public void Collect()
    {
        if (IsCollected)
        {
            return;
        }

        IsCollected = true;
        gameObject.SetActive(false);
    }
}
