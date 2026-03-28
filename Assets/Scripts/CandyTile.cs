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

    // 4 digits slightly larger
    public float fontSizeFor4Digits = 9.1f;
    public Vector3 oneDigitScale = new Vector3(0.65f, 0.65f, 0.65f);
    public Vector3 defaultScale = new Vector3(0.55f, 0.55f, 0.55f);
    public Vector3 fourDigitScale = new Vector3(0.56f, 0.56f, 0.56f);

    public int Value { get; private set; }
    public Color CurrentColor => spriteRenderer ? spriteRenderer.color : Color.white;

    [HideInInspector] public BoardController board;

    private Coroutine moveCo;
    private Coroutine flashCo;
    private Coroutine idleHintCo;
    private Vector3 idleHintBaseScale;
    private Quaternion idleHintBaseRotation;

    public void Init(BoardController b, int gx, int gy, int value)
    {
        board = b;
        x = gx;
        y = gy;

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!valueText) valueText = GetComponentInChildren<TMP_Text>(true);

        if (valueText)
        {
            valueText.enableAutoSizing = false;
            valueText.overflowMode = TextOverflowModes.Truncate;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
        }

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
        if (!valueText) return;

        int digits = v.ToString().Length;
        valueText.fontSize = baseFontSize;

        if (digits == 1)
        {
            valueText.transform.localScale = oneDigitScale;
        }
        else if (digits == 4)
        {
            valueText.transform.localScale = fourDigitScale;
            valueText.fontSize = fontSizeFor4Digits;
        }
        else
        {
            valueText.transform.localScale = defaultScale;
        }
    }

    public void RefreshColor()
    {
        if (!spriteRenderer) return;

        var tm = ThemeManager.I;
        if (tm == null) return;

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

    // --------------------------
    // Merge effects (NO SCALE CHANGE)
    // --------------------------
    public void PlayPop(float scale, float time)
    {
        // Disabled: scaling breaks board layout
    }

    public void PlayPopByValue(int v)
    {
        // Disabled
    }

    public void PlayMergeFlash()
    {
        // Disabled: keep visuals clean (only fireworks should remain)
        if (flashCo != null)
        {
            StopCoroutine(flashCo);
            flashCo = null;
        }

        // Ensure the tile color is restored if a flash was mid-play
        RefreshColor();
    }

    private IEnumerator CoMergeFlash()
    {
        if (!spriteRenderer) yield break;

        Color baseC = spriteRenderer.color;
        Color flashC = Color.Lerp(baseC, Color.white, 0.65f);
        flashC.a = 1f;

        float up = 0.05f;
        float down = 0.08f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, up);
            float e = Mathf.Sin(t * Mathf.PI * 0.5f);
            spriteRenderer.color = Color.Lerp(baseC, flashC, e);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, down);
            float e = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            spriteRenderer.color = Color.Lerp(flashC, baseC, e);
            yield return null;
        }

        spriteRenderer.color = baseC;
        flashCo = null;
    }

    public void ShowIdleHint(float highlightStrength, float pulseScale, float pulseDuration, int pulseCount)
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!valueText) valueText = GetComponentInChildren<TMP_Text>(true);

        if (idleHintCo != null)
        {
            StopCoroutine(idleHintCo);
            idleHintCo = null;
        }

        idleHintBaseScale = transform.localScale;
        idleHintBaseRotation = transform.localRotation;
        idleHintCo = StartCoroutine(CoIdleHint(highlightStrength, pulseScale, pulseDuration, pulseCount));
    }

    public void ClearIdleHint()
    {
        if (idleHintCo != null)
        {
            StopCoroutine(idleHintCo);
            idleHintCo = null;
        }

        if (transform != null)
        {
            Vector3 resetScale = idleHintBaseScale == Vector3.zero ? Vector3.one : idleHintBaseScale;
            transform.localScale = resetScale;
            transform.localRotation = idleHintBaseRotation;
        }

        RefreshColor();
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
        Color targetTextColor = Color.Lerp(baseTextColor, Color.white, Mathf.Clamp01(clampedStrength + 0.10f));

        for (int i = 0; i < clampedPulseCount; i++)
        {
            yield return PulseHintPhase(baseSpriteColor, baseTextColor, targetSpriteColor, targetTextColor, 0f, 1f, 1f, clampedPulseScale, clampedPulseDuration * 0.5f);
            yield return PulseHintPhase(baseSpriteColor, baseTextColor, targetSpriteColor, targetTextColor, 1f, 0f, clampedPulseScale, 1f, clampedPulseDuration * 0.5f);
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

            ApplyIdleHintVisual(baseSpriteColor, baseTextColor, targetSpriteColor, targetTextColor, strength, scale, wobble);
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
            ApplyIdleHintVisual(baseSpriteColor, baseTextColor, targetSpriteColor, targetTextColor, strength, scale, wobble);

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
            Vector3 baseScale = idleHintBaseScale == Vector3.zero ? Vector3.one : idleHintBaseScale;
            transform.localScale = baseScale * scaleMultiplier;
            transform.localRotation = idleHintBaseRotation * Quaternion.Euler(0f, 0f, rotationZDegrees);
        }
    }

    // --------------------------
    // WORLD Movement (restored)
    // --------------------------
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
        if (valueText == null) return;

        // TMP_Text can be either UGUI (RectTransform) or 3D (Transform)
        RectTransform rt = valueText.rectTransform;
        if (rt != null)
            rt.rotation = worldRotation;
        else
            valueText.transform.rotation = worldRotation;
    }

}
