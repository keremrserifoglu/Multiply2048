using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ThemedModalCard : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private Image overlayImage;

    [Header("Panel")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image innerImage;
    [SerializeField] private Outline frameOutline;
    [SerializeField] private Shadow frameShadow;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text[] bodyTexts;
    [SerializeField] private TMP_Text[] secondaryTexts;

    [Header("Progress")]
    [SerializeField] private Image progressTrack;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;

    [Header("Optional Buttons Refresh")]
    [SerializeField] private ThemedGoldButton[] refreshButtons;

    private void OnEnable()
    {
        Subscribe(true);
        ApplyTheme();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyTheme();
    }
#endif

    private void Subscribe(bool subscribe)
    {
        if (ThemeManager.I == null)
            return;

        if (subscribe)
            ThemeManager.I.OnPaletteChanged += ApplyTheme;
        else
            ThemeManager.I.OnPaletteChanged -= ApplyTheme;
    }

    public void ApplyTheme()
    {
        ThemeManager.UIThemeColors ui = GetThemeColors();

        if (overlayImage != null)
            overlayImage.color = ui.overlayColor;

        if (frameImage != null)
        {
            frameImage.color = ui.panelColor;
            if (frameImage.sprite != null)
                frameImage.type = Image.Type.Sliced;
        }

        if (innerImage != null)
        {
            innerImage.color = ui.panelInnerColor;
            if (innerImage.sprite != null)
                innerImage.type = Image.Type.Sliced;
        }

        if (frameOutline != null)
        {
            frameOutline.effectColor = ui.panelOutlineColor;
            frameOutline.effectDistance = new Vector2(2f, -2f);
            frameOutline.useGraphicAlpha = true;
            frameOutline.enabled = true;
        }

        if (frameShadow != null)
        {
            Color shadowColor = ui.panelOutlineColor;
            shadowColor.a = 0.45f;
            frameShadow.effectColor = shadowColor;
            frameShadow.effectDistance = new Vector2(0f, -10f);
            frameShadow.useGraphicAlpha = true;
            frameShadow.enabled = true;
        }

        if (titleText != null)
            titleText.color = ui.panelTitleColor;

        ApplyTextArray(bodyTexts, ui.panelTextColor);

        Color secondary = ui.panelTextColor;
        secondary.a *= 0.85f;
        ApplyTextArray(secondaryTexts, secondary);

        if (progressTrack != null)
        {
            Color track = ui.panelOutlineColor;
            track.a = 0.45f;
            progressTrack.color = track;
        }

        if (progressFill != null)
        {
            Color fill = GetProgressFillColor(ui);
            progressFill.color = fill;
        }

        if (progressText != null)
            progressText.color = ui.panelTitleColor;

        if (refreshButtons != null)
        {
            for (int i = 0; i < refreshButtons.Length; i++)
            {
                if (refreshButtons[i] != null)
                    refreshButtons[i].ApplyCurrentTheme(true);
            }
        }
    }

    private void ApplyTextArray(TMP_Text[] texts, Color color)
    {
        if (texts == null)
            return;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].color = color;
        }
    }

    private ThemeManager.UIThemeColors GetThemeColors()
    {
        if (ThemeManager.I != null)
            return ThemeManager.I.GetUIThemeColors();

        ThemeManager.UIThemeColors fallback = new ThemeManager.UIThemeColors
        {
            panelColor = new Color32(0xF2, 0xEC, 0xE2, 0xFF),
            panelInnerColor = new Color32(0xFB, 0xF7, 0xF1, 0xFF),
            panelOutlineColor = new Color32(0xB8, 0xAA, 0x9A, 0xFF),
            panelTitleColor = Color.black,
            panelTextColor = Color.black,
            overlayColor = new Color(0f, 0f, 0f, 0.2f)
        };

        return fallback;
    }

    private Color GetProgressFillColor(ThemeManager.UIThemeColors ui)
    {
        if (ThemeManager.I != null)
            return ThemeManager.I.GetGoldButtonColors(ThemeManager.GoldButtonRole.HudBottomBar, true).face;

        return ui.panelOutlineColor;
    }
}