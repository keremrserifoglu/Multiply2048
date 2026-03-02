using UnityEngine;

public class MergeFirework : MonoBehaviour
{
    [SerializeField] private SpriteRenderer headSr;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private TrailRenderer trail;

    [Header("Life")]
    [SerializeField] private float lifeTime = 1.05f;

    [Header("Head Scale")]
    [SerializeField] private float startScale = 0.48f;
    [SerializeField] private float endScale = 0.07f;

    [Header("Head Glow")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowScaleMul = 2.10f;
    [SerializeField] private float glowAlpha = 0.72f;

    [Header("Trail")]
    [SerializeField] private float trailTime = 0.42f;
    [SerializeField] private float trailStartWidth = 0.26f;
    [SerializeField] private float trailEndWidth = 0.00f;
    [SerializeField] private float trailAlphaStart = 0.75f;

    [Header("Finish Burst")]
    [SerializeField] private bool enableFinishBurst = true;
    [SerializeField] private int finishBurstCount = 7;
    [SerializeField] private float finishBurstMinSpeed = 2.2f;
    [SerializeField] private float finishBurstMaxSpeed = 4.2f;
    [SerializeField] private float finishBurstLife = 0.35f;
    [SerializeField] private float finishBurstStartScale = 0.20f;
    [SerializeField] private float finishBurstEndScale = 0.02f;

    private SpriteRenderer glowSr;
    private float t;
    private Color baseColor;
    private float glowAlphaUsed;
    private bool finished;

    private void Reset()
    {
        headSr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
    }

    // Main init: direction and speed are provided by spawner (for perfect ring spread)
    public void Init(Sprite headSprite, Color color, Vector2 dir, float speed)
    {
        if (headSr == null) headSr = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (trail == null) trail = GetComponent<TrailRenderer>();

        headSr.sprite = headSprite;

        baseColor = Color.Lerp(color, Color.white, 0.86f);
        baseColor.a = 1.0f;
        headSr.color = baseColor;

        transform.localScale = Vector3.one * startScale;

        if (trail != null)
        {
            trail.time = trailTime;
            trail.startWidth = trailStartWidth;
            trail.endWidth = trailEndWidth;

            Gradient g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(baseColor, 0f),
                    new GradientColorKey(baseColor, 1f)
                },
                new[] {
                    new GradientAlphaKey(trailAlphaStart, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = g;
        }

        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir = dir.normalized;

        rb.linearVelocity = dir * speed;
        rb.angularVelocity = UnityEngine.Random.Range(-720f, 720f);

        if (enableGlow)
            SetupGlow(headSprite);
    }

    private void SetupGlow(Sprite headSprite)
    {
        if (glowSr != null) return;

        GameObject glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = Vector3.zero;
        glowGo.transform.localRotation = Quaternion.identity;
        glowGo.transform.localScale = Vector3.one * glowScaleMul;

        glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = headSprite;

        glowSr.sortingLayerID = headSr.sortingLayerID;
        glowSr.sortingOrder = headSr.sortingOrder - 1;

        glowAlphaUsed = glowAlpha;
        Color g = Color.white;
        g.a = glowAlphaUsed;
        glowSr.color = g;
    }

    private void Update()
    {
        t += Time.deltaTime;
        float n = t / Mathf.Max(0.0001f, lifeTime);
        float n01 = Mathf.Clamp01(n);

        float s = Mathf.Lerp(startScale, endScale, n01);
        transform.localScale = Vector3.one * s;

        Color c = baseColor;
        c.a = Mathf.Lerp(0.98f, 0f, n01);
        headSr.color = c;

        if (glowSr != null)
        {
            Color g = glowSr.color;
            g.a = Mathf.Lerp(glowAlphaUsed, 0f, n01);
            glowSr.color = g;
        }

        if (!finished && n >= 1f)
        {
            finished = true;

            if (enableFinishBurst)
                DoFinishBurst();

            Destroy(gameObject);
        }
    }

    private void DoFinishBurst()
    {
        // 1.5x count (with cap for performance)
        int count = Mathf.Clamp(Mathf.CeilToInt(finishBurstCount * 1.5f), 0, 24);
        if (count <= 0) return;

        Sprite sprite = headSr != null ? headSr.sprite : null;
        Vector3 pos = transform.position;

        float angleOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("FW_Pop");
            p.transform.position = pos;

            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = sprite;
            psr.sortingLayerID = headSr.sortingLayerID;
            psr.sortingOrder = headSr.sortingOrder;

            Color pc = Color.Lerp(baseColor, Color.white, 0.65f);
            pc.a = 0.85f;
            psr.color = pc;

            var prb = p.AddComponent<Rigidbody2D>();
            prb.gravityScale = 0.05f;
            prb.linearDamping = 2.6f;
            prb.angularDamping = 2.0f;

            float ang = angleOffset + (i * (Mathf.PI * 2f / count));
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            float spd = UnityEngine.Random.Range(finishBurstMinSpeed, finishBurstMaxSpeed);
            prb.linearVelocity = dir * spd;
            prb.angularVelocity = UnityEngine.Random.Range(-720f, 720f);

            p.transform.localScale = Vector3.one * finishBurstStartScale;

            var pop = p.AddComponent<FireworkPop>();
            pop.Init(psr, finishBurstLife, finishBurstStartScale, finishBurstEndScale);
        }
    }

    private class FireworkPop : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float life;
        private float t;
        private float s0;
        private float s1;
        private Color baseC;

        public void Init(SpriteRenderer spriteRenderer, float lifeTime, float startScaleValue, float endScaleValue)
        {
            sr = spriteRenderer;
            life = Mathf.Max(0.0001f, lifeTime);
            s0 = startScaleValue;
            s1 = endScaleValue;
            baseC = sr.color;
        }

        private void Update()
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / life);

            float s = Mathf.Lerp(s0, s1, n);
            transform.localScale = Vector3.one * s;

            Color c = baseC;
            c.a = Mathf.Lerp(baseC.a, 0f, n);
            sr.color = c;

            if (n >= 1f)
                Destroy(gameObject);
        }
    }
}