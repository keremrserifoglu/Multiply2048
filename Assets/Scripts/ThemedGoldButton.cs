using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ThemedGoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ButtonVisualMode
    {
        MainMenuIcon,
        CountButton,
        TextOnlyFrame
    }

    public enum ThemeFamilyStyle
    {
        Dark,
        Colorful,
        Light
    }

    [System.Serializable]
    public struct FamilyPalette
    {
        public Color frameColor;
        public Color fillColor;
        public Color highlightColor;
        public Color textColor;
        public Color iconColor;
        public Color shadowColor;
        public Color outlineColor;
    }

    [Header("Mode")]
    [SerializeField] private ButtonVisualMode visualMode = ButtonVisualMode.MainMenuIcon;

    [Header("Target")]
    [SerializeField] private Button targetButton;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text countText;

    [Header("Theme Palettes")]
    [SerializeField] private FamilyPalette darkPalette;
    [SerializeField] private FamilyPalette colorfulPalette;
    [SerializeField] private FamilyPalette lightPalette;

    [Header("Pressed Visual")]
    [Range(0f, 1f)][SerializeField] private float normalHighlightAlpha = 0.22f;
    [Range(0f, 1f)][SerializeField] private float pressedHighlightAlpha = 0.08f;
    [Range(0.7f, 1f)][SerializeField] private float pressedFillMultiplier = 0.94f;
    [SerializeField] private Vector2 releasedShadowDistance = new Vector2(0f, -8f);
    [SerializeField] private Vector2 pressedShadowDistance = new Vector2(0f, -4f);
    [Range(0.5f, 1.2f)][SerializeField] private float pressedShadowAlphaMultiplier = 0.82f;
    [Range(0f, 1f)][SerializeField] private float outlineAlpha = 0.18f;

    private Shadow cachedShadow;
    private Outline cachedOutline;
    private ThemeFamilyStyle lastAppliedFamily;
    private FamilyPalette activePalette;
    private bool isPressed;

    private void Reset()
    {
        targetButton = GetComponent<Button>();
        fillImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (fillImage == null)
            fillImage = GetComponent<Image>();

        cachedShadow = GetOrAddShadow(gameObject);
        cachedOutline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;

        ApplyCurrentTheme(true);
        SetPressed(false);
    }

    private void Start()
    {
        ApplyCurrentTheme(true);
        SetPressed(false);
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;

        isPressed = false;
    }

    private void HandlePaletteChanged()
    {
        ApplyCurrentTheme(true);
    }

    public void ApplyCurrentTheme(bool force)
    {
        ThemeFamilyStyle family = GetCurrentFamily();
        if (!force && family == lastAppliedFamily)
            return;

        activePalette = GetPalette(family);
        ApplyPalette(activePalette);
        lastAppliedFamily = family;
    }

    private ThemeFamilyStyle GetCurrentFamily()
    {
        if (ThemeManager.I == null)
            return ThemeFamilyStyle.Colorful;

        switch (ThemeManager.I.GetCurrentPaletteFamily())
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return ThemeFamilyStyle.Dark;
            case TilePaletteDatabase.ThemeFamily.Light:
                return ThemeFamilyStyle.Light;
            default:
                return ThemeFamilyStyle.Colorful;
        }
    }

    private FamilyPalette GetPalette(ThemeFamilyStyle family)
    {
        switch (family)
        {
            case ThemeFamilyStyle.Dark:
                return darkPalette;
            case ThemeFamilyStyle.Light:
                return lightPalette;
            default:
                return colorfulPalette;
        }
    }

    private void ApplyPalette(FamilyPalette palette)
    {
        if (frameImage != null)
            frameImage.color = palette.frameColor;

        ApplyModeVisibility();
        ApplyButtonStateColors();
        ApplyVisualState();
    }

    private void ApplyModeVisibility()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(visualMode == ButtonVisualMode.MainMenuIcon);

        if (labelText != null)
            labelText.gameObject.SetActive(visualMode != ButtonVisualMode.CountButton);

        if (countText != null)
            countText.gameObject.SetActive(visualMode == ButtonVisualMode.CountButton);
    }

    private void ApplyButtonStateColors()
    {
        if (targetButton == null)
            return;

        ColorBlock colors = targetButton.colors;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
        targetButton.colors = colors;
        targetButton.transition = Selectable.Transition.ColorTint;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        isPressed = pressed;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        bool interactable = targetButton == null || targetButton.interactable;
        float disabledAlpha = interactable ? 1f : 0.55f;

        Color fillColor = activePalette.fillColor;
        if (isPressed)
            fillColor = MultiplyRgb(fillColor, pressedFillMultiplier);
        fillColor.a *= disabledAlpha;

        Color textColor = activePalette.textColor;
        textColor.a *= disabledAlpha;

        Color iconColor = activePalette.iconColor;
        iconColor.a *= disabledAlpha;

        Color highlightColor = activePalette.highlightColor;
        highlightColor.a = (isPressed ? pressedHighlightAlpha : normalHighlightAlpha) * disabledAlpha;

        if (fillImage != null)
            fillImage.color = fillColor;

        if (highlightImage != null)
            highlightImage.color = highlightColor;

        if (labelText != null)
            labelText.color = textColor;

        if (countText != null)
            countText.color = textColor;

        if (iconImage != null)
            iconImage.color = iconColor;

        if (cachedShadow != null)
        {
            Color shadowColor = activePalette.shadowColor;
            if (isPressed)
                shadowColor.a *= pressedShadowAlphaMultiplier;
            shadowColor.a *= disabledAlpha;
            cachedShadow.effectColor = shadowColor;
            cachedShadow.effectDistance = isPressed ? pressedShadowDistance : releasedShadowDistance;
            cachedShadow.useGraphicAlpha = true;
        }

        if (cachedOutline != null)
        {
            Color outlineColor = activePalette.outlineColor;
            outlineColor.a = outlineAlpha * disabledAlpha;
            cachedOutline.effectColor = outlineColor;
            cachedOutline.effectDistance = new Vector2(1f, -1f);
            cachedOutline.useGraphicAlpha = true;
        }
    }

    private static Color MultiplyRgb(Color color, float multiplier)
    {
        color.r *= multiplier;
        color.g *= multiplier;
        color.b *= multiplier;
        return color;
    }

    private static Shadow GetOrAddShadow(GameObject target)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow != null)
            return shadow;

        return target.AddComponent<Shadow>();
    }
}