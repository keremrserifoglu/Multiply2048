using UnityEngine;

public class MergeGhost : MonoBehaviour
{
    [Header("Fade (visual only)")]
    [SerializeField] private float fadeDuration = 1.2f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.45f;
    [SerializeField] private float endScale = 0.1f;

    [Header("Physics")]
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float minUpVelocity = 1.8f;
    [SerializeField] private float maxUpVelocity = 3.0f;
    [SerializeField] private float minSideVelocity = -1.1f;
    [SerializeField] private float maxSideVelocity = 1.1f;
    [SerializeField] private float minAngularVelocity = -220f;
    [SerializeField] private float maxAngularVelocity = 220f;

    [Header("Size by Value")]
    [SerializeField] private float scaleMul3Digits = 1.15f;   // 100-999
    [SerializeField] private float scaleMul2048Plus = 1.45f;  // 2048+

    [Header("Glow")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowScaleMul = 1.22f;
    [SerializeField] private float glowAlpha = 0.22f;
    [SerializeField, Range(0f, 1f)] private float glowWhiten = 0.5f;

    [Header("2048+ Glow Boost")]
    [SerializeField] private float glowScaleMul2048Plus = 1.38f;
    [SerializeField] private float glowAlpha2048Plus = 0.40f;
    [SerializeField, Range(0f, 1f)] private float glowWhiten2048Plus = 0.75f;

    [Header("Destroy Condition")]
    [SerializeField] private float killPadding = 0.6f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private SpriteRenderer glowSr;

    private Camera mainCam;

    private float t;
    private float scaleMul = 1f;
    private Color baseColor;

    private float glowAlphaUsed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    private void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Sprite sprite, Color color, int value)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (mainCam == null) mainCam = Camera.main;

        sr.sprite = sprite;

        baseColor = color;
        baseColor.a = 0.6f;
        sr.color = baseColor;

        scaleMul = 1f;
        if (value >= 2048) scaleMul = scaleMul2048Plus;
        else if (value >= 100 && value <= 999) scaleMul = scaleMul3Digits;

        transform.localScale = Vector3.one * (startScale * scaleMul);

        if (rb != null)
        {
            rb.gravityScale = gravityScale;

            rb.linearVelocity = new Vector2(
                UnityEngine.Random.Range(minSideVelocity, maxSideVelocity),
                UnityEngine.Random.Range(minUpVelocity, maxUpVelocity)
            );

            rb.angularVelocity = UnityEngine.Random.Range(minAngularVelocity, maxAngularVelocity);
        }

        if (enableGlow)
        {
            bool is2048Plus = value >= 2048;
            SetupGlow(sprite, color, is2048Plus);
        }
    }

    private void SetupGlow(Sprite sprite, Color color, bool is2048Plus)
    {
        if (glowSr != null) return;

        GameObject glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = Vector3.zero;
        glowGo.transform.localRotation = Quaternion.identity;

        float gs = is2048Plus ? glowScaleMul2048Plus : glowScaleMul;
        glowGo.transform.localScale = Vector3.one * gs;

        glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = sprite;

        glowSr.sortingLayerID = sr.sortingLayerID;
        glowSr.sortingOrder = sr.sortingOrder - 1;

        float whiten = is2048Plus ? glowWhiten2048Plus : glowWhiten;
        glowAlphaUsed = is2048Plus ? glowAlpha2048Plus : glowAlpha;

        Color g = Color.Lerp(color, Color.white, whiten);
        g.a = glowAlphaUsed;
        glowSr.color = g;
    }

    private void Update()
    {
        // Visual fade/scale (does not control destroy)
        t += Time.deltaTime;
        float n = t / Mathf.Max(0.0001f, fadeDuration);
        float n01 = Mathf.Clamp01(n);

        float s = Mathf.Lerp(startScale, endScale, n01) * scaleMul;
        transform.localScale = Vector3.one * s;

        Color c = baseColor;
        c.a = Mathf.Lerp(0.6f, 0f, n01);
        sr.color = c;

        if (glowSr != null)
        {
            Color g = glowSr.color;
            g.a = Mathf.Lerp(glowAlphaUsed, 0f, n01);
            glowSr.color = g;
        }

        // Destroy only when leaving screen bottom
        if (mainCam != null)
        {
            float bottomY = mainCam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).y;
            if (transform.position.y < bottomY - killPadding)
                Destroy(gameObject);
        }
        else
        {
            // Fallback if no camera exists
            if (n >= 2f)
                Destroy(gameObject);
        }
    }
}