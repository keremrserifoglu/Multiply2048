using UnityEngine;

public class MergeSparkle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody2D rb;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 0.45f;

    [Header("Motion")]
    [SerializeField] private float minSpeed = 1.2f;
    [SerializeField] private float maxSpeed = 3.2f;
    [SerializeField] private float minUpBias = 0.2f;   // Slight upward tendency
    [SerializeField] private float maxUpBias = 0.9f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.22f;
    [SerializeField] private float endScale = 0.02f;

    [Header("Glow")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowScaleMul = 1.35f;
    [SerializeField] private float glowAlpha = 0.25f;

    [Header("2048+ Boost")]
    [SerializeField] private float scaleMul2048Plus = 1.35f;
    [SerializeField] private float glowAlpha2048Plus = 0.45f;
    [SerializeField] private float glowScaleMul2048Plus = 1.55f;

    private float t;
    private Color baseColor;
    private float scaleMul = 1f;
    private float glowAlphaUsed;

    private SpriteRenderer glowSr;

    private void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Color color, bool is2048Plus)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        baseColor = Color.Lerp(color, Color.white, 0.55f);
        baseColor.a = 0.85f;

        scaleMul = is2048Plus ? scaleMul2048Plus : 1f;

        sr.color = baseColor;
        transform.localScale = Vector3.one * (startScale * scaleMul);

        // Random outward velocity with slight upward bias
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        float up = UnityEngine.Random.Range(minUpBias, maxUpBias);

        Vector2 vel = new Vector2(dir.x, Mathf.Abs(dir.y) + up).normalized * speed;
        rb.linearVelocity = vel;
        rb.angularVelocity = UnityEngine.Random.Range(-360f, 360f);

        if (enableGlow)
            SetupGlow(is2048Plus);
    }

    private void SetupGlow(bool is2048Plus)
    {
        if (glowSr != null) return;

        GameObject glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = Vector3.zero;
        glowGo.transform.localRotation = Quaternion.identity;

        float gs = is2048Plus ? glowScaleMul2048Plus : glowScaleMul;
        glowGo.transform.localScale = Vector3.one * gs;

        glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = sr.sprite;

        glowSr.sortingLayerID = sr.sortingLayerID;
        glowSr.sortingOrder = sr.sortingOrder - 1;

        glowAlphaUsed = is2048Plus ? glowAlpha2048Plus : glowAlpha;

        Color g = Color.Lerp(baseColor, Color.white, 0.75f);
        g.a = glowAlphaUsed;
        glowSr.color = g;
    }

    private void Update()
    {
        t += Time.deltaTime;
        float n = t / Mathf.Max(0.0001f, lifeTime);
        float n01 = Mathf.Clamp01(n);

        float s = Mathf.Lerp(startScale, endScale, n01) * scaleMul;
        transform.localScale = Vector3.one * s;

        Color c = baseColor;
        c.a = Mathf.Lerp(0.85f, 0f, n01);
        sr.color = c;

        if (glowSr != null)
        {
            Color g = glowSr.color;
            g.a = Mathf.Lerp(glowAlphaUsed, 0f, n01);
            glowSr.color = g;
        }

        if (n >= 1f)
            Destroy(gameObject);
    }
}