using TMPro;
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

    [Header("Temporary Button Text Color")]
    [SerializeField] private Color buttonTextColor = new Color32(0x18, 0x12, 0x0B, 0xFF);

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
        ApplyTemporaryButtonTextColor();
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

            Color artColor = backgroundThemeArt.color;
            artColor.a = 1f;
            backgroundThemeArt.color = artColor;
        }

        if (backgroundThemeTint != null)
        {
            Color tint = ThemeManager.I != null ? ThemeManager.I.GetBackgroundColor() : Color.black;
            tint.a = GetTintAlpha(family);
            backgroundThemeTint.color = tint;
            backgroundThemeTint.enabled = tint.a > 0.001f;
        }
    }

    private void ApplyModalOverlay()
    {
        if (modalOverlay == null)
        {
            return;
        }

        Color c = Color.black;
        c.a = modalOverlayAlpha;
        modalOverlay.color = c;
    }

    private void ApplyTemporaryButtonTextColor()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].TryGetComponent<ThemedGoldButton>(out _))
                continue;

            TMP_Text[] tmpTexts = buttons[i].GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < tmpTexts.Length; j++)
            {
                if (tmpTexts[j] != null)
                    tmpTexts[j].color = buttonTextColor;
            }

            Text[] legacyTexts = buttons[i].GetComponentsInChildren<Text>(true);
            for (int j = 0; j < legacyTexts.Length; j++)
            {
                if (legacyTexts[j] != null)
                    legacyTexts[j].color = buttonTextColor;
            }
        }
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

        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}