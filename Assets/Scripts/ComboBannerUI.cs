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

    [Header("Shake")]
    [SerializeField, Min(0f)] private float shakePixels = 8f;
    [SerializeField, Min(0.01f)] private float shakeSeconds = 0.18f;
    [SerializeField, Min(1f)] private float shakeFrequency = 42f;

    private Coroutine animCo;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPosition;
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

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;

        if (label != null)
            baseColor = label.color;

        HideImmediate();
    }

    public void ShowCombo(int comboCount, int multiplier, bool has2048Plus)
    {
        if (!label)
            return;

        if (comboCount <= 0)
        {
            Hide();
            return;
        }

        label.text = BuildComboText(comboCount, multiplier, has2048Plus);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        gameObject.SetActive(true);

        bool shouldEmphasize = comboCount != lastComboCount || has2048Plus != lastHad2048Plus;
        lastComboCount = comboCount;
        lastHad2048Plus = has2048Plus;

        if (shouldEmphasize)
            PlayEmphasis();
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

        transform.localScale = baseScale;

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;

        if (label != null)
            label.color = baseColor;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        lastComboCount = -1;
        lastHad2048Plus = false;

        gameObject.SetActive(false);
    }

    private void PlayEmphasis()
    {
        if (animCo != null)
            StopCoroutine(animCo);

        animCo = StartCoroutine(CoEmphasis());
    }

    private string BuildComboText(int comboCount, int multiplier, bool has2048Plus)
    {
        if (comboCount >= 2)
            return has2048Plus ? $"Super Great Combo x{multiplier}" : $"Super Combo x{multiplier}";

        if (has2048Plus)
            return $"Great Combo x{multiplier}";

        return $"Combo x{multiplier}";
    }

    private IEnumerator CoEmphasis()
    {
        float elapsed = 0f;
        float totalSeconds = Mathf.Max(pulseSeconds, flashSeconds, shakeSeconds);

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
                float shakeFade = 1f - shakeT;
                float shakeX = Mathf.Sin(elapsed * shakeFrequency) * shakePixels * shakeFade;
                float shakeY = Mathf.Cos(elapsed * shakeFrequency * 0.73f) * shakePixels * 0.35f * shakeFade;
                rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(shakeX, shakeY);
            }

            yield return null;
        }

        transform.localScale = baseScale;

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;

        if (label != null)
            label.color = baseColor;

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

        transform.localScale = baseScale;

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;

        if (label != null)
            label.color = baseColor;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        lastComboCount = -1;
        lastHad2048Plus = false;

        gameObject.SetActive(false);
        animCo = null;
    }
}