using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    [Header("Highlight")]
    public GameObject highlightVisual;
    public Light highlightLight;

    [Header("Outline (ekran ortasına bakınca)")]
    [Tooltip("Sadece outline materyalli child MeshRenderer'ları buraya sürükle; bakınca açılır. Shader: Yigit/CollectibleOutlineURP")]
    public Renderer[] outlineRenderers;

    [Header("Toplanınca efekt")]
    [Tooltip("Tüm çöplerde aynı prefab'ı kullanabilirsin. Boşsa sadece obje kaybolur.")]
    public GameObject collectEffectPrefab;
    [Tooltip("Efekt sahneyi kirletmesin diye kaç saniye sonra silinsin. 0 = partikül süresinden tahmin et.")]
    public float collectEffectDestroyAfterSeconds = 0f;
    public AudioClip collectSound;
    [Range(0f, 1f)]
    public float collectSoundVolume = 1f;

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
        PlayCollectEffect();
        gameObject.SetActive(false);
    }

    private void PlayCollectEffect()
    {
        Vector3 pos = transform.position;

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, pos, collectSoundVolume);
        }

        if (collectEffectPrefab == null)
        {
            return;
        }

        GameObject fx = Instantiate(collectEffectPrefab, pos, Quaternion.identity);
        float life = collectEffectDestroyAfterSeconds;
        if (life <= 0f)
        {
            ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                float startLife = main.startLifetime.constantMax;
                life = main.duration + startLife + 0.25f;
            }
            else
            {
                life = 2f;
            }
        }

        Destroy(fx, life);
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

        if (outlineRenderers != null)
        {
            for (int i = 0; i < outlineRenderers.Length; i++)
            {
                if (outlineRenderers[i] != null)
                {
                    outlineRenderers[i].enabled = highlighted;
                }
            }
        }
    }
}
