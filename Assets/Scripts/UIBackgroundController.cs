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
            ThemeManager.I.OnPaletteChanged += ApplyAll;

        ApplyAll();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= ApplyAll;
    }

    private void ApplyAll()
    {
        ApplyTransparentPanelVisibility();
        ApplyThemeBackground();
        ApplyModalOverlay();
    }

    private void ApplyTransparentPanelVisibility()
    {
        SetVisibleIfAlphaPositive(mainMenuBackground);
        SetVisibleIfAlphaPositive(hudBackground);
        SetVisibleIfAlphaPositive(gameOverBackground);
    }

    private void ApplyThemeBackground()
    {
        TilePaletteDatabase.ThemeFamily family = GetCurrentFamily();

        if (backgroundThemeArt != null)
        {
            backgroundThemeArt.sprite = GetSpriteForFamily(family);
            backgroundThemeArt.enabled = backgroundThemeArt.sprite != null;
        }

        SetVisibleIfAlphaPositive(backgroundThemeTint);
    }

    private void ApplyModalOverlay()
    {
        SetVisibleIfAlphaPositive(modalOverlay);
    }

    private TilePaletteDatabase.ThemeFamily GetCurrentFamily()
    {
        if (ThemeManager.I != null)
            return ThemeManager.I.GetCurrentPaletteFamily();

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

    private static void SetVisibleIfAlphaPositive(Image image)
    {
        if (image == null)
            return;

        image.enabled = image.color.a > 0.001f;
    }
}