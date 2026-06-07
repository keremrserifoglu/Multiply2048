using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ComboBannerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeSeconds = 0.12f;

    [Header("Pulse")]
    [SerializeField, Min(1f)] private float pulseScale = 1.14f;
    [SerializeField, Min(0.01f)] private float pulseSeconds = 0.18f;

    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0.95f, 0.45f, 1f);
    [SerializeField, Min(0.01f)] private float flashSeconds = 0.18f;

    [Header("Board-Like Shake")]
    [SerializeField, Min(0f)] private float shakePixels = 10f;
    [SerializeField, Min(0.01f)] private float shakeSeconds = 0.10f;
    [SerializeField, Min(1f)] private float shakeFrequency = 38f;
    [SerializeField, Min(0f)] private float rotationalStrength = 0.9f;
    [SerializeField] private AnimationCurve shakeFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine animCo;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPosition;
    private Quaternion baseLocalRotation;
    private Color baseColor = Color.white;
    private RectTransform rectTransform;
    private int lastComboCount = -1;
    private bool lastHad2048Plus;

    private void Awake()
    {
        if (!label)
            label = GetComponentInChildren<TMP_Text>(true);

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        rectTransform = transform as RectTransform;
        baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        baseLocalRotation = transform.localRotation;

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        if (label != null)
            baseColor = label.color;

        HideImmediate();
    }

    public void ShowCombo(int comboCount, int scoreMultiplier, bool has2048Plus)
    {
        if (!label)
            return;

        if (comboCount <= 0)
        {
            Hide();
            return;
        }

        label.text = BuildComboText(comboCount, scoreMultiplier, has2048Plus);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        gameObject.SetActive(true);

        bool shouldEmphasize = comboCount != lastComboCount || has2048Plus != lastHad2048Plus;
        lastComboCount = comboCount;
        lastHad2048Plus = has2048Plus;

        if (shouldEmphasize)
            PlayEmphasis(comboCount);
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (animCo != null)
            StopCoroutine(animCo);

        animCo = StartCoroutine(CoFadeOut());
    }

    public void HideImmediate()
    {
        if (animCo != null)
            StopCoroutine(animCo);

        RestoreBaseTransformAndColor();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        lastComboCount = -1;
        lastHad2048Plus = false;

        gameObject.SetActive(false);
    }

    private void PlayEmphasis(int comboCount)
    {
        if (animCo != null)
            StopCoroutine(animCo);

        animCo = StartCoroutine(CoEmphasis(comboCount));
    }

    private string BuildComboText(int comboCount, int scoreMultiplier, bool has2048Plus)
    {
        if (comboCount >= 3)
            return has2048Plus
                ? $"Super Great Combo x{comboCount}"
                : $"Super Combo x{comboCount}";

        if (has2048Plus)
            return $"Great Combo x{comboCount}";

        return $"Combo x{comboCount}";
    }

    private IEnumerator CoEmphasis(int comboCount)
    {
        float elapsed = 0f;
        float totalSeconds = Mathf.Max(pulseSeconds, flashSeconds, shakeSeconds);
        float comboStrength = Mathf.Clamp01(comboCount / 8f);
        float effectiveShakePixels = shakePixels * Mathf.Lerp(1f, 1.45f, comboStrength);

        while (elapsed < totalSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            float pulseT = Mathf.Clamp01(elapsed / pulseSeconds);
            float flashT = Mathf.Clamp01(elapsed / flashSeconds);
            float shakeT = Mathf.Clamp01(elapsed / shakeSeconds);

            float pulseCurve = Mathf.Sin(pulseT * Mathf.PI);
            transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, pulseCurve);

            if (label != null)
            {
                float flashCurve = 1f - flashT;
                label.color = Color.Lerp(baseColor, flashColor, flashCurve);
            }

            if (rectTransform != null)
            {
                float fade = shakeFalloff != null ? shakeFalloff.Evaluate(shakeT) : 1f - shakeT;
                float noiseX = (Mathf.PerlinNoise(13.1f, elapsed * shakeFrequency) - 0.5f) * 2f;
                float noiseY = (Mathf.PerlinNoise(29.7f, elapsed * shakeFrequency) - 0.5f) * 2f;
                float noiseR = (Mathf.PerlinNoise(47.3f, elapsed * shakeFrequency) - 0.5f) * 2f;

                Vector2 offset = new Vector2(noiseX, noiseY) * (effectiveShakePixels * fade);
                float angle = noiseR * rotationalStrength * effectiveShakePixels * 0.30f * fade;

                rectTransform.anchoredPosition = baseAnchoredPosition + offset;
                transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        RestoreBaseTransformAndColor();
        animCo = null;
    }

    private IEnumerator CoFadeOut()
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeSeconds);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        RestoreBaseTransformAndColor();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        lastComboCount = -1;
        lastHad2048Plus = false;

        gameObject.SetActive(false);
        animCo = null;
    }

    private void RestoreBaseTransformAndColor()
    {
        transform.localScale = baseScale;
        transform.localRotation = baseLocalRotation;

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;

        if (label != null)
            label.color = baseColor;
    }
}
