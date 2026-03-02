using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GradientBackgroundController : MonoBehaviour
{
    [Header("Texture")]
    public int textureWidth = 16;
    public int textureHeight = 256;

    [Header("Gradient Look")]
    [Range(0f, 1f)] public float topLighten = 0.18f;
    [Range(0f, 1f)] public float bottomDarken = 0.22f;
    [Range(0f, 1f)] public float vignetteInTexture = 0.18f;

    [Header("Motion")]
    [Tooltip("Seconds to blend from current palette bg to new palette bg.")]
    public float transitionDuration = 0.45f;

    [Tooltip("Small alive pulse amount (0 disables).")]
    [Range(0f, 0.15f)] public float pulseAmount = 0.04f;

    [Tooltip("Pulse speed in Hz-ish.")]
    public float pulseSpeed = 0.6f;

    private SpriteRenderer sr;
    private Texture2D tex;
    private Sprite sprite;

    private Coroutine initCo;
    private Coroutine transitionCo;

    private Color currentBase;
    private Color targetBase;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        CreateTextureIfNeeded();
    }

    private void OnEnable()
    {
        // ThemeManager may not exist yet; wait and then subscribe safely.
        initCo = StartCoroutine(InitRoutine());
    }

    private void OnDisable()
    {
        if (initCo != null) StopCoroutine(initCo);
        if (transitionCo != null) StopCoroutine(transitionCo);

        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= OnPaletteChanged;
    }

    private IEnumerator InitRoutine()
    {
        while (ThemeManager.I == null)
            yield return null;

        // Subscribe once ThemeManager exists
        ThemeManager.I.OnPaletteChanged += OnPaletteChanged;

        // Apply initial color
        currentBase = ThemeManager.I.GetBackgroundColor();
        currentBase.a = 1f;
        targetBase = currentBase;

        BakeFromBase(currentBase, 0f);
    }

    private void LateUpdate()
    {
        // Keep it stretched to camera view
        FitToCamera();

        // Alive subtle pulse (doesn't change base color, only gradient intensity)
        if (tex == null) return;

        float pulse = (pulseAmount <= 0f) ? 0f : Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        BakeFromBase(currentBase, pulse);
    }

    private void OnPaletteChanged()
    {
        if (ThemeManager.I == null) return;

        targetBase = ThemeManager.I.GetBackgroundColor();
        targetBase.a = 1f;

        if (transitionCo != null) StopCoroutine(transitionCo);
        transitionCo = StartCoroutine(TransitionRoutine(currentBase, targetBase));
    }

    private IEnumerator TransitionRoutine(Color from, Color to)
    {
        if (transitionDuration <= 0.001f)
        {
            currentBase = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            currentBase = Color.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        currentBase = to;
    }

    private void CreateTextureIfNeeded()
    {
        if (tex != null) return;

        tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // Create a sprite from the texture; it will be scaled to fit the camera.
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sr.sprite = sprite;
    }

    private void BakeFromBase(Color baseC, float pulse)
    {
        baseC.a = 1f;

        // Slight dynamic intensity for premium feel
        float topL = Mathf.Clamp01(topLighten + pulse);
        float botD = Mathf.Clamp01(bottomDarken - pulse);

        Color top = Color.Lerp(baseC, Color.white, topL);
        Color bottom = Color.Lerp(baseC, Color.black, botD);

        BakeVerticalGradient(top, bottom);
    }

    private void BakeVerticalGradient(Color top, Color bottom)
    {
        for (int y = 0; y < tex.height; y++)
        {
            float t = (tex.height <= 1) ? 0f : (y / (float)(tex.height - 1));
            Color c = Color.Lerp(bottom, top, t);

            // Subtle vignette baked into texture (edge darkening)
            float xMid = 0.5f;
            for (int x = 0; x < tex.width; x++)
            {
                float xf = (tex.width <= 1) ? 0f : (x / (float)(tex.width - 1));
                float edge = Mathf.Abs(xf - xMid) / xMid; // 0 center -> 1 edges
                float v = Mathf.Lerp(1f, 1f - vignetteInTexture, edge);

                Color cc = c * v;
                cc.a = 1f;
                tex.SetPixel(x, y, cc);
            }
        }

        tex.Apply(false, false);
    }

    private void FitToCamera()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;

        // Fit sprite to camera view
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        // Keep behind everything
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);

        Vector2 spriteSize = sr.sprite.bounds.size;
        transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
    }
}
