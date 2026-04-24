using UnityEngine;

public class MergeSparkle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody2D rb;

    [Header("Timing")]
    [SerializeField] private float lifeTime = 0.17f;
    public float LifeTime => lifeTime;
    [SerializeField] private float fadeExponent = 2.2f;

    private float usedLifeTime;

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
    private float glowAlphaUsed;
    private Color baseColor;
    private SpriteRenderer glowSr;

    private void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Color color, bool is2048Plus, int waveIndex, float waveDelay, int sortingLayerId, int sortingOrder, float customLifeTime = -1f)
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

        sr.sortingLayerID = sortingLayerId;
        sr.sortingOrder = sortingOrder;

        baseColor = Color.Lerp(color, Color.white, whiteBlend);
        baseColor.a = startAlpha;

        scaleMul = is2048Plus ? scaleMul2048Plus : 1f;
        usedLifeTime = customLifeTime > 0f ? customLifeTime : lifeTime;
        startDelay = Mathf.Max(0, waveIndex) * Mathf.Max(0f, waveDelay);

        transform.localScale = Vector3.one * (startScale * scaleMul);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        if (enableGlow)
            SetupGlow(is2048Plus);
    }

    private void SetupGlow(bool is2048Plus)
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
        glowGo.transform.localScale = Vector3.one * (is2048Plus ? glowScaleMul2048Plus : glowScaleMul);

        glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = sr.sprite;
        glowSr.sortingLayerID = sr.sortingLayerID;
        glowSr.sortingOrder = sr.sortingOrder - 1;

        glowAlphaUsed = is2048Plus ? glowAlpha2048Plus : glowAlpha;
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
        float scaleEase = 1f - Mathf.Pow(1f - n, 3f);
        float alphaEase = 1f - Mathf.Pow(n, fadeExponent);

        float scale = Mathf.Lerp(startScale, endScale, scaleEase) * scaleMul;
        transform.localScale = Vector3.one * scale;

        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, startAlpha * alphaEase);

        if (glowSr != null)
            glowSr.color = new Color(baseColor.r, baseColor.g, baseColor.b, glowAlphaUsed * alphaEase);

        if (n >= 1f)
            Destroy(gameObject);
    }
}