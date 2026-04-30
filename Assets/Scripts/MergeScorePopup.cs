using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class MergeScorePopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float lifetime = 0.9f;
    [SerializeField, Min(0f)] private float travelPixels = 46f;
    [SerializeField, Min(0f)] private float startScaleMultiplier = 1.05f;
    [SerializeField, Min(0f)] private float endScaleMultiplier = 0.92f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool faceCamera = true;

    private Camera targetCamera;
    private Coroutine playCo;

    private void Awake()
    {
        if (!scoreText)
            scoreText = GetComponent<TMP_Text>();
    }

    public void Play(long amount, Camera cameraForPixels, Color color, int sortingOrder)
    {
        if (!scoreText)
            scoreText = GetComponent<TMP_Text>();

        targetCamera = cameraForPixels != null ? cameraForPixels : Camera.main;

        scoreText.text = "+" + amount;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.textWrappingMode = TextWrappingModes.NoWrap;
        scoreText.overflowMode = TextOverflowModes.Overflow;

        color.a = 1f;
        scoreText.color = color;

        Renderer textRenderer = scoreText.GetComponent<Renderer>();
        if (textRenderer != null)
            textRenderer.sortingOrder = sortingOrder;

        if (playCo != null)
            StopCoroutine(playCo);

        playCo = StartCoroutine(CoPlay(color));
    }

    private IEnumerator CoPlay(Color baseColor)
    {
        Vector3 startPosition = transform.position;
        Vector3 moveDirection = targetCamera != null ? targetCamera.transform.up : Vector3.up;
        Vector3 endPosition = startPosition + moveDirection * PixelsToWorldDistance(travelPixels);

        Vector3 baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        Vector3 startScale = baseScale * startScaleMultiplier;
        Vector3 endScale = baseScale * endScaleMultiplier;

        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += delta;

            float t = Mathf.Clamp01(elapsed / lifetime);
            float moveT = 1f - Mathf.Pow(1f - t, 3f);
            float alphaT = t * t;

            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, moveT);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, t);

            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, alphaT);
            scoreText.color = c;

            FaceCameraIfNeeded();

            yield return null;
        }

        Destroy(gameObject);
    }

    private float PixelsToWorldDistance(float pixels)
    {
        if (targetCamera == null)
            return pixels * 0.01f;

        if (targetCamera.orthographic)
        {
            float screenHeight = Mathf.Max(1f, Screen.height);
            return pixels * ((targetCamera.orthographicSize * 2f) / screenHeight);
        }

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(transform.position);
        if (screenPosition.z <= 0f)
            return pixels * 0.01f;

        Vector3 worldA = targetCamera.ScreenToWorldPoint(screenPosition);
        Vector3 worldB = targetCamera.ScreenToWorldPoint(screenPosition + new Vector3(0f, pixels, 0f));
        return Vector3.Distance(worldA, worldB);
    }

    private void FaceCameraIfNeeded()
    {
        if (!faceCamera || targetCamera == null)
            return;

        transform.rotation = targetCamera.transform.rotation;
    }
}