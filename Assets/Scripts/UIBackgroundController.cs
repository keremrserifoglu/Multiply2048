using UnityEngine;
using UnityEngine.UI;

public class UIBackgroundController : MonoBehaviour
{
    [Header("Panels To Clear")]
    [SerializeField] private Image mainMenuBackground;
    [SerializeField] private Image hudBackground;
    [SerializeField] private Image gameOverBackground;

    [Header("Theme Background")]
    [SerializeField] private Image backgroundThemeArt;
    [SerializeField] private Image backgroundThemeTint;
    [SerializeField] private Image modalOverlay;

    [Header("Theme Background Sprites")]
    [SerializeField] private Sprite darkBackgroundSprite;
    [SerializeField] private Sprite colorfulBackgroundSprite;
    [SerializeField] private Sprite lightBackgroundSprite;

    [Header("Tint Strength")]
    [Range(0f, 1f)]
    [SerializeField] private float darkTintAlpha = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float colorfulTintAlpha = 0.06f;

    [Range(0f, 1f)]
    [SerializeField] private float lightTintAlpha = 0.03f;

    [Header("Modal Overlay")]
    [Range(0f, 1f)]
    [SerializeField] private float modalOverlayAlpha = 0.35f;

    private void Awake()
    {
        ApplyAll();
    }

    private void Start()
    {
        ApplyAll();
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
        {
            ThemeManager.I.OnPaletteChanged += ApplyAll;
        }

        ApplyAll();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
        {
            ThemeManager.I.OnPaletteChanged -= ApplyAll;
        }
    }

    private void ApplyAll()
    {
        MakePanelsTransparent();
        ApplyThemeBackground();
        ApplyModalOverlay();
    }

    private void MakePanelsTransparent()
    {
        SetAlpha(mainMenuBackground, 0f);
        SetAlpha(hudBackground, 0f);
        SetAlpha(gameOverBackground, 0f);
    }

    private void ApplyThemeBackground()
    {
        TilePaletteDatabase.ThemeFamily family = GetCurrentFamily();

        if (backgroundThemeArt != null)
        {
            backgroundThemeArt.sprite = GetSpriteForFamily(family);
            backgroundThemeArt.enabled = backgroundThemeArt.sprite != null;
        }

        if (backgroundThemeTint != null)
        {
            backgroundThemeTint.enabled = backgroundThemeTint.color.a > 0.001f;
        }
    }

    private void ApplyModalOverlay()
    {
        if (modalOverlay == null)
        {
            return;
        }

        modalOverlay.enabled = modalOverlay.color.a > 0.001f;
    }

    private TilePaletteDatabase.ThemeFamily GetCurrentFamily()
    {
        if (ThemeManager.I != null)
        {
            return ThemeManager.I.GetCurrentPaletteFamily();
        }

        return TilePaletteDatabase.ThemeFamily.Colorful;
    }

    private Sprite GetSpriteForFamily(TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return darkBackgroundSprite != null ? darkBackgroundSprite : colorfulBackgroundSprite;

            case TilePaletteDatabase.ThemeFamily.Light:
                return lightBackgroundSprite != null ? lightBackgroundSprite : colorfulBackgroundSprite;

            default:
                return colorfulBackgroundSprite != null ? colorfulBackgroundSprite : darkBackgroundSprite;
        }
    }

    private float GetTintAlpha(TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return darkTintAlpha;

            case TilePaletteDatabase.ThemeFamily.Light:
                return lightTintAlpha;

            default:
                return colorfulTintAlpha;
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null)
        {
            return;
        }

        img.enabled = img.color.a > 0.001f;
    }
}