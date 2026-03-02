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
