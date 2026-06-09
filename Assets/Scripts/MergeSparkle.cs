using UnityEngine;

public class MergeSparkle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody2D rb;

    [Header("Timing")]
    [SerializeField] private float lifeTime = 0.17f;
    [SerializeField] private float fadeExponent = 2.2f;
    public float LifeTime => lifeTime;

    [Header("Wave Scale")]
    [SerializeField] private float startScale = 0.14f;
    [SerializeField] private float endScale = 1.45f;
    [SerializeField] private float scaleMul2048Plus = 1.28f;

    [Header("Wave Glow")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowScaleMul = 1.18f;
    [SerializeField] private float glowAlpha = 0.30f;
    [SerializeField] private float glowAlpha2048Plus = 0.48f;
    [SerializeField] private float glowScaleMul2048Plus = 1.34f;

    [Header("Wave Alpha")]
    [SerializeField] private float startAlpha = 0.82f;

    [Header("Color")]
    [SerializeField, Range(0f, 1f)] private float whiteBlend = 0.72f;

    private float elapsed;
    private float startDelay;
    private float scaleMul = 1f;
    private float glowScaleMulUsed = 1f;
    private float glowAlphaUsed;
    private float startAlphaUsed;
    private float fadeExponentUsed;
    private float usedLifeTime;
    private bool useLinearScale;
    private Color baseColor;
    private SpriteRenderer glowSr;

    private void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(
        Color color,
        bool is2048Plus,
        int waveIndex,
        float waveDelay,
        int sortingLayerId,
        int sortingOrder,
        float customLifeTime = -1f,
        float customScaleMul = -1f,
        float customStartAlpha = -1f,
        float customGlowAlpha = -1f,
        float customWhiteBlend = -1f,
        float customFadeExponent = -1f)
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.simulated = false;
        }

        if (sr == null)
        {
            Destroy(gameObject);
            return;
        }

        sr.sortingLayerID = sortingLayerId;
        sr.sortingOrder = sortingOrder;

        elapsed = 0f;
        startDelay = Mathf.Max(0, waveIndex) * Mathf.Max(0f, waveDelay);
        usedLifeTime = customLifeTime > 0f ? customLifeTime : lifeTime;

        float blend = customWhiteBlend >= 0f ? Mathf.Clamp01(customWhiteBlend) : whiteBlend;
        baseColor = Color.Lerp(color, Color.white, blend);

        scaleMul = customScaleMul > 0f ? customScaleMul : (is2048Plus ? scaleMul2048Plus : 1f);
        startAlphaUsed = customStartAlpha >= 0f ? Mathf.Clamp01(customStartAlpha) : startAlpha;
        glowAlphaUsed = customGlowAlpha >= 0f ? Mathf.Clamp01(customGlowAlpha) : (is2048Plus ? glowAlpha2048Plus : glowAlpha);
        glowScaleMulUsed = is2048Plus ? glowScaleMul2048Plus : glowScaleMul;
        fadeExponentUsed = customFadeExponent > 0f ? customFadeExponent : fadeExponent;
        useLinearScale = is2048Plus;

        transform.localScale = Vector3.one * (startScale * scaleMul);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        if (enableGlow)
            SetupGlow();
    }

    private void SetupGlow()
    {
        if (glowSr != null)
        {
            Destroy(glowSr.gameObject);
            glowSr = null;
        }

        GameObject glowGo = new GameObject("WaveGlow");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = Vector3.zero;
        glowGo.transform.localRotation = Quaternion.identity;
        glowGo.transform.localScale = Vector3.one * glowScaleMulUsed;

        glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = sr.sprite;
        glowSr.sortingLayerID = sr.sortingLayerID;
        glowSr.sortingOrder = sr.sortingOrder - 1;
        glowSr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }

    private void Update()
    {
        if (startDelay > 0f)
        {
            startDelay -= Time.deltaTime;
            return;
        }

        elapsed += Time.deltaTime;

        float activeLifeTime = usedLifeTime > 0f ? usedLifeTime : lifeTime;
        float n = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, activeLifeTime));
        float scaleEase = useLinearScale ? n : 1f - Mathf.Pow(1f - n, 3f);
        float alphaEase = 1f - Mathf.Pow(n, fadeExponentUsed);
        float scale = Mathf.Lerp(startScale, endScale, scaleEase) * scaleMul;

        transform.localScale = Vector3.one * scale;
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, startAlphaUsed * alphaEase);

        if (glowSr != null)
            glowSr.color = new Color(baseColor.r, baseColor.g, baseColor.b, glowAlphaUsed * alphaEase);

        if (n >= 1f)
            Destroy(gameObject);
    }
}
