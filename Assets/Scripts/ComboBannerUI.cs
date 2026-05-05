using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ComboBannerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float fadeSeconds = 0.12f;
    [SerializeField, Min(1f)] private float pulseScale = 1.08f;
    [SerializeField, Min(0.01f)] private float pulseSeconds = 0.16f;

    private Coroutine pulseCo;
    private Vector3 baseScale;

    private void Awake()
    {
        if (!label)
            label = GetComponentInChildren<TMP_Text>(true);

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
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

        if (pulseCo != null)
            StopCoroutine(pulseCo);

        pulseCo = StartCoroutine(CoPulse());
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (pulseCo != null)
            StopCoroutine(pulseCo);

        pulseCo = StartCoroutine(CoFadeOut());
    }

    public void HideImmediate()
    {
        if (pulseCo != null)
            StopCoroutine(pulseCo);

        transform.localScale = baseScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    private string BuildComboText(int comboCount, int multiplier, bool has2048Plus)
    {
        if (comboCount >= 2)
            return has2048Plus ? $"Super Great Combo  x{multiplier}" : $"Super Combo  x{multiplier}";

        if (has2048Plus)
            return $"Great Combo  x{multiplier}";

        return $"Combo  x{multiplier}";
    }

    private IEnumerator CoPulse()
    {
        transform.localScale = baseScale * pulseScale;

        float elapsed = 0f;
        while (elapsed < pulseSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pulseSeconds);
            transform.localScale = Vector3.Lerp(baseScale * pulseScale, baseScale, t);
            yield return null;
        }

        transform.localScale = baseScale;
        pulseCo = null;
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

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
        pulseCo = null;
    }
}