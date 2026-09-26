using UnityEngine;

// Kept under the original class name so existing prefab references remain valid.
// The old expanding water-wave visual is now a small-box scatter effect.
public class MergeSparkle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody2D rb;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float lifeTime = 0.28f;
    [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.35f;
    public float LifeTime => lifeTime;

    [Header("Box Size (relative to merged tile)")]
    [SerializeField, Range(0.01f, 0.25f)] private float minSizeFraction = 0.12f;
    [SerializeField, Range(0.01f, 0.25f)] private float maxSizeFraction = 0.25f;
    [SerializeField, Range(0.1f, 1f)] private float endSizeMultiplier = 0.55f;

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float alpha = 0.55f;
    [SerializeField, Min(0f)] private float maxRotationDegrees = 220f;

    private Vector3 startWorldPosition;
    private Vector3 endWorldPosition;
    private Vector3 startLocalScale;
    private Vector3 endLocalScale;
    private float startRotation;
    private float endRotation;
    private float elapsed;
    private float startDelay;
    private float usedLifeTime;
    private float usedAlpha;
    private float usedFadeStart;
    private Color boxColor;

    private void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(
        Sprite tileSprite,
        Color tileColor,
        Vector2 direction,
        float travelDistance,
        Vector3 sourceLocalScale,
        int pieceIndex,
        float pieceDelay,
        int sortingLayerId,
        int sortingOrder,
        float customLifeTime = -1f,
        float customMinSizeFraction = -1f,
        float customMaxSizeFraction = -1f,
        float customAlpha = -1f)
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

        if (sr == null || tileSprite == null)
        {
            Destroy(gameObject);
            return;
        }

        sr.sprite = tileSprite;
        sr.sortingLayerID = sortingLayerId;
        sr.sortingOrder = sortingOrder;

        float minFraction = customMinSizeFraction > 0f
            ? Mathf.Clamp(customMinSizeFraction, 0.01f, 0.25f)
            : minSizeFraction;
        float maxFraction = customMaxSizeFraction > 0f
            ? Mathf.Clamp(customMaxSizeFraction, 0.01f, 0.25f)
            : maxSizeFraction;

        if (maxFraction < minFraction)
            maxFraction = minFraction;

        float sizeFraction = Random.Range(minFraction, maxFraction);
        startLocalScale = Vector3.Scale(sourceLocalScale, Vector3.one * sizeFraction);
        endLocalScale = startLocalScale * endSizeMultiplier;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        direction.Normalize();
        startWorldPosition = transform.position;
        endWorldPosition = startWorldPosition + (Vector3)(direction * Mathf.Max(0f, travelDistance));

        startRotation = Random.Range(-25f, 25f);
        endRotation = startRotation + Random.Range(-maxRotationDegrees, maxRotationDegrees);
        transform.rotation = Quaternion.Euler(0f, 0f, startRotation);
        transform.localScale = startLocalScale;

        usedLifeTime = customLifeTime > 0f ? customLifeTime : lifeTime;
        usedAlpha = customAlpha >= 0f ? Mathf.Clamp01(customAlpha) : alpha;
        usedFadeStart = Mathf.Clamp01(fadeStartNormalized);
        startDelay = Mathf.Max(0, pieceIndex) * Mathf.Max(0f, pieceDelay);
        elapsed = 0f;

        boxColor = tileColor;
        boxColor.a = usedAlpha;
        sr.color = new Color(boxColor.r, boxColor.g, boxColor.b, 0f);
    }

    private void Update()
    {
        if (startDelay > 0f)
        {
            startDelay -= Time.deltaTime;
            return;
        }

        elapsed += Time.deltaTime;
        float n = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, usedLifeTime));
        float moveEase = 1f - Mathf.Pow(1f - n, 3f);

        transform.position = Vector3.LerpUnclamped(startWorldPosition, endWorldPosition, moveEase);
        transform.localScale = Vector3.Lerp(startLocalScale, endLocalScale, n);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRotation, endRotation, moveEase));

        float fadeT = Mathf.InverseLerp(usedFadeStart, 1f, n);
        float visibleAlpha = n < 0.08f
            ? Mathf.Lerp(0f, usedAlpha, n / 0.08f)
            : Mathf.Lerp(usedAlpha, 0f, fadeT);

        sr.color = new Color(boxColor.r, boxColor.g, boxColor.b, visibleAlpha);

        if (n >= 1f)
            Destroy(gameObject);
    }
}
