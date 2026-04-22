using System.Collections;
using TMPro;
using UnityEngine;

public class CandyTile : MonoBehaviour
{
    [HideInInspector] public int x;
    [HideInInspector] public int y;

    [Header("Refs")]
    public TMP_Text valueText;
    public SpriteRenderer spriteRenderer;

    [Header("Number Sizing")]
    public float baseFontSize = 10f;
    public float fontSizeFor3Digits = 9.4f;
    public float fontSizeFor4Digits = 8.8f;

    public Vector3 oneDigitScale = new Vector3(0.65f, 0.65f, 0.65f);
    public Vector3 twoDigitScale = new Vector3(0.55f, 0.55f, 0.55f);
    public Vector3 threeDigitScale = new Vector3(0.48f, 0.48f, 0.48f);
    public Vector3 fourDigitScale = new Vector3(0.40f, 0.40f, 0.40f);

    [Header("Merge Pop")]
    [SerializeField, Range(1f, 1.5f)] private float popScaleSmall = 1.12f;
    [SerializeField, Range(1f, 1.5f)] private float popScaleMedium = 1.18f;
    [SerializeField, Range(1f, 1.6f)] private float popScaleLarge = 1.24f;
    [SerializeField, Range(1f, 1.5f)] private float textPopScaleMul = 1.10f;
    [SerializeField, Min(0.01f)] private float popSettleTime = 0.11f;
    [SerializeField, Range(0f, 0.20f)] private float popBounce = 0.05f;
    [SerializeField, Range(0f, 1f)] private float mergeFlashStrength = 0.72f;
    [SerializeField, Min(0.01f)] private float mergeFlashFadeTime = 0.10f;

    public int Value { get; private set; }
    public Color CurrentColor => spriteRenderer ? spriteRenderer.color : Color.white;

    [HideInInspector] public BoardController board;

    private Coroutine moveCo;
    private Coroutine popCo;
    private Coroutine flashCo;
    private Coroutine idleHintCo;

    private Vector3 idleHintBaseScale;
    private Quaternion idleHintBaseRotation;
    private bool idleHintBaselineCached;

    private void Awake()
    {
        CacheIdleHintBaseline();
    }

    private void OnDisable()
    {
        ClearIdleHint();

        if (popCo != null)
        {
            StopCoroutine(popCo);
            popCo = null;
        }

        if (flashCo != null)
        {
            StopCoroutine(flashCo);
            flashCo = null;
        }
    }

    public void Init(BoardController b, int gx, int gy, int value)
    {
        board = b;
        x = gx;
        y = gy;

        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (!valueText)
            valueText = GetComponentInChildren<TMP_Text>(true);

        if (valueText)
        {
            valueText.enableAutoSizing = false;
            valueText.overflowMode = TextOverflowModes.Truncate;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        CacheIdleHintBaseline();
        SetValue(value);
    }

    public void SetValue(int v)
    {
        Value = v;

        if (valueText)
        {
            valueText.text = v.ToString();
            ApplyNumberSizing(v);
        }

        RefreshColor();
    }

    private void ApplyNumberSizing(int v)
    {
        if (!valueText)
            return;

        int digits = v.ToString().Length;
        valueText.fontSize = baseFontSize;

        if (digits <= 1)
        {
            valueText.transform.localScale = oneDigitScale;
        }
        else if (digits == 2)
        {
            valueText.transform.localScale = twoDigitScale;
        }
        else if (digits == 3)
        {
            valueText.transform.localScale = threeDigitScale;
            valueText.fontSize = fontSizeFor3Digits;
        }
        else
        {
            valueText.transform.localScale = fourDigitScale;
            valueText.fontSize = fontSizeFor4Digits;
        }
    }

    public void RefreshColor()
    {
        if (!spriteRenderer)
            return;

        var tm = ThemeManager.I;
        if (tm == null)
            return;

        Color tileC = tm.GetTileColor(Value);
        tileC.a = 1f;
        spriteRenderer.color = tileC;

        if (valueText != null)
        {
            Color txt = tm.GetTextColorForTile(tileC);
            txt.a = 1f;
            valueText.color = txt;
        }
    }

    public void PlayPop(float scale, float time)
    {
        CacheIdleHintBaseline();

        if (popCo != null)
        {
            StopCoroutine(popCo);
            popCo = null;
        }

        float startTileMul = Mathf.Max(1f, scale);
        float duration = Mathf.Max(0.01f, time);

        Vector3 baseTileScale = GetSafeIdleHintBaseScale();
        Vector3 baseTextScale = valueText != null ? valueText.transform.localScale : Vector3.one;

        transform.localScale = baseTileScale * startTileMul;

        if (valueText != null)
            valueText.transform.localScale = baseTextScale * Mathf.Max(1f, textPopScaleMul);

        popCo = StartCoroutine(CoPop(baseTileScale, baseTextScale, startTileMul, duration));
    }

    public void PlayPopByValue(int v)
    {
        if (v >= 2048)
            PlayPop(popScaleLarge, popSettleTime * 1.10f);
        else if (v >= 256)
            PlayPop(popScaleMedium, popSettleTime);
        else
            PlayPop(popScaleSmall, popSettleTime * 0.92f);
    }

    public void PlayMergeFlash()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (!spriteRenderer)
            return;

        if (flashCo != null)
        {
            StopCoroutine(flashCo);
            flashCo = null;
        }

        RefreshColor();

        Color baseC = spriteRenderer.color;
        Color flashC = Color.Lerp(baseC, Color.white, mergeFlashStrength);
        flashC.a = 1f;
        spriteRenderer.color = flashC;

        flashCo = StartCoroutine(CoMergeFlash());
    }

    private IEnumerator CoPop(Vector3 baseTileScale, Vector3 baseTextScale, float startTileMul, float duration)
    {
        float startTextMul = Mathf.Max(1f, textPopScaleMul);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, duration);
            float n = Mathf.Clamp01(t);
            float e = 1f - Mathf.Pow(1f - n, 3f);
            float bounce = Mathf.Sin(n * Mathf.PI) * popBounce * (1f - n);

            float tileMul = Mathf.Lerp(startTileMul, 1f, e) - bounce;
            float textMul = Mathf.Lerp(startTextMul, 1f, e) + (bounce * 0.35f);

            transform.localScale = baseTileScale * tileMul;

            if (valueText != null)
                valueText.transform.localScale = baseTextScale * textMul;

            yield return null;
        }

        transform.localScale = baseTileScale;

        if (valueText != null)
            valueText.transform.localScale = baseTextScale;

        popCo = null;
    }

    private IEnumerator CoMergeFlash()
    {
        if (!spriteRenderer)
            yield break;

        Color flashStart = spriteRenderer.color;

        RefreshColor();
        Color baseC = spriteRenderer.color;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, mergeFlashFadeTime);
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            spriteRenderer.color = Color.Lerp(flashStart, baseC, e);
            yield return null;
        }

        spriteRenderer.color = baseC;
        flashCo = null;
    }

    public void ShowIdleHint(float highlightStrength, float pulseScale, float pulseDuration, int pulseCount)
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (!valueText)
            valueText = GetComponentInChildren<TMP_Text>(true);

        CacheIdleHintBaseline();

        if (idleHintCo != null)
        {
            StopCoroutine(idleHintCo);
            idleHintCo = null;
        }

        RestoreIdleHintBaseline();
        idleHintCo = StartCoroutine(CoIdleHint(highlightStrength, pulseScale, pulseDuration, pulseCount));
    }

    public void ClearIdleHint()
    {
        if (idleHintCo != null)
        {
            StopCoroutine(idleHintCo);
            idleHintCo = null;
        }

        RestoreIdleHintBaseline();
        RefreshColor();
    }

    private void CacheIdleHintBaseline()
    {
        if (idleHintBaselineCached || transform == null)
            return;

        idleHintBaseScale = transform.localScale;
        idleHintBaseRotation = transform.localRotation;
        idleHintBaselineCached = true;
    }

    private Vector3 GetSafeIdleHintBaseScale()
    {
        if (!idleHintBaselineCached)
            CacheIdleHintBaseline();

        return idleHintBaseScale == Vector3.zero ? Vector3.one : idleHintBaseScale;
    }

    private Quaternion GetSafeIdleHintBaseRotation()
    {
        if (!idleHintBaselineCached)
            CacheIdleHintBaseline();

        return idleHintBaseRotation;
    }

    private void RestoreIdleHintBaseline()
    {
        if (transform == null)
            return;

        transform.localScale = GetSafeIdleHintBaseScale();
        transform.localRotation = GetSafeIdleHintBaseRotation();
    }

    private IEnumerator CoIdleHint(float highlightStrength, float pulseScale, float pulseDuration, int pulseCount)
    {
        if (!spriteRenderer)
        {
            idleHintCo = null;
            yield break;
        }

        RefreshColor();

        Color baseSpriteColor = spriteRenderer.color;
        Color baseTextColor = valueText != null ? valueText.color : Color.white;

        float clampedStrength = Mathf.Clamp01(highlightStrength);
        float clampedPulseScale = Mathf.Max(1f, pulseScale);
        float clampedPulseDuration = Mathf.Max(0.10f, pulseDuration);
        int clampedPulseCount = Mathf.Max(1, pulseCount);

        Color targetSpriteColor = Color.Lerp(baseSpriteColor, Color.white, clampedStrength);
        Color targetTextColor = baseTextColor;

        for (int i = 0; i < clampedPulseCount; i++)
        {
            yield return PulseHintPhase(
                baseSpriteColor,
                baseTextColor,
                targetSpriteColor,
                targetTextColor,
                0f,
                1f,
                1f,
                clampedPulseScale,
                clampedPulseDuration * 0.5f
            );

            yield return PulseHintPhase(
                baseSpriteColor,
                baseTextColor,
                targetSpriteColor,
                targetTextColor,
                1f,
                0f,
                clampedPulseScale,
                1f,
                clampedPulseDuration * 0.5f
            );
        }

        float restingStrength = Mathf.Max(0.08f, clampedStrength * 0.55f);
        float restingScale = Mathf.Lerp(1f, clampedPulseScale, 0.18f);
        float loopTime = 0f;

        while (true)
        {
            loopTime += Time.unscaledDeltaTime;

            float shimmer01 = 0.5f + 0.5f * Mathf.Sin(loopTime * 3.6f);
            float wobble = Mathf.Sin(loopTime * 11f) * 3.5f;
            float strength = Mathf.Lerp(restingStrength * 0.70f, restingStrength, shimmer01);
            float scale = Mathf.Lerp(1f, restingScale, 0.55f + 0.45f * Mathf.Sin(loopTime * 7.5f));

            ApplyIdleHintVisual(
                baseSpriteColor,
                baseTextColor,
                targetSpriteColor,
                targetTextColor,
                strength,
                scale,
                wobble
            );

            yield return null;
        }
    }

    private IEnumerator PulseHintPhase(
        Color baseSpriteColor,
        Color baseTextColor,
        Color targetSpriteColor,
        Color targetTextColor,
        float fromStrength,
        float toStrength,
        float fromScale,
        float toScale,
        float duration)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, duration);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float strength = Mathf.Lerp(fromStrength, toStrength, eased);
            float scale = Mathf.Lerp(fromScale, toScale, eased);
            float wobble = Mathf.Sin(eased * Mathf.PI * 4f) * 5f * strength;

            ApplyIdleHintVisual(
                baseSpriteColor,
                baseTextColor,
                targetSpriteColor,
                targetTextColor,
                strength,
                scale,
                wobble
            );

            yield return null;
        }
    }

    private void ApplyIdleHintVisual(
        Color baseSpriteColor,
        Color baseTextColor,
        Color targetSpriteColor,
        Color targetTextColor,
        float strength,
        float scaleMultiplier,
        float rotationZDegrees)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(baseSpriteColor, targetSpriteColor, Mathf.Clamp01(strength));

        if (valueText != null)
            valueText.color = Color.Lerp(baseTextColor, targetTextColor, Mathf.Clamp01(strength));

        if (transform != null)
        {
            transform.localScale = GetSafeIdleHintBaseScale() * scaleMultiplier;
            transform.localRotation = GetSafeIdleHintBaseRotation() * Quaternion.Euler(0f, 0f, rotationZDegrees);
        }
    }

    public void SetWorldPosInstant(Vector3 worldPos)
    {
        if (moveCo != null)
        {
            StopCoroutine(moveCo);
            moveCo = null;
        }

        transform.position = worldPos;
    }

    public void MoveToWorld(Vector3 worldPos, float duration)
    {
        if (moveCo != null)
        {
            StopCoroutine(moveCo);
            moveCo = null;
        }

        moveCo = StartCoroutine(MoveRoutine(worldPos, duration));
    }

    private IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, duration);
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        moveCo = null;
    }

    public void SetLabelRotation(Quaternion worldRotation)
    {
        if (valueText == null)
            return;

        RectTransform rt = valueText.rectTransform;
        if (rt != null)
            rt.rotation = worldRotation;
        else
            valueText.transform.rotation = worldRotation;
    }
}