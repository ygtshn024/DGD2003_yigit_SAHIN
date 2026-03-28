using UnityEngine;

/// <summary>
/// Fırlatılan objeyi yalnızca belirli bir Tag'e sahip objeye çarpınca yok eder.
/// Hedefe <see cref="destroyOnlyTag"/> ile aynı Tag'i ver (Edit > Project Settings > Tags).
/// </summary>
public class ThrownObjectDestroyOnImpact : MonoBehaviour
{
    [Tooltip("Sadece bu Tag'e sahip objeye çarpınca yok olur. Collider child'ta olsa bile parent'larda Tag aranır.")]
    [SerializeField] private string destroyOnlyTag = "ThrowTarget";

    private void OnCollisionEnter(Collision collision)
    {
        if (string.IsNullOrEmpty(destroyOnlyTag))
        {
            return;
        }

        if (ColliderMatchesTagInHierarchy(collision.collider, destroyOnlyTag))
        {
            Destroy(gameObject);
        }
    }

    private static bool ColliderMatchesTagInHierarchy(Collider col, string tag)
    {
        Transform t = col.transform;
        while (t != null)
        {
            if (t.CompareTag(tag))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }
}
