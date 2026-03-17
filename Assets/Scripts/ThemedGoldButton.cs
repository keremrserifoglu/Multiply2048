using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("Press Effect")]
    [SerializeField] private RectTransform pressRoot;
    [SerializeField] private float releasedY = 0f;
    [SerializeField] private float pressedY = -6f;
    [SerializeField] private Vector2 releasedShadowDistance = new Vector2(0f, -8f);
    [SerializeField] private Vector2 pressedShadowDistance = new Vector2(0f, -4f);

    private Shadow cachedShadow;
    private Outline cachedOutline;
    private ThemeFamilyStyle lastAppliedFamily;

    private void Reset()
    {
        targetButton = GetComponent<Button>();
        pressRoot = transform as RectTransform;
    }

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (pressRoot == null)
            pressRoot = transform as RectTransform;

        cachedShadow = GetOrAddShadow(gameObject);
        cachedOutline = GetOrAddOutline(gameObject);
    }

    private void Start()
    {
        ApplyCurrentTheme(true);
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;

        ApplyCurrentTheme(true);
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
    }

    private void HandlePaletteChanged()
    {
        ApplyCurrentTheme(false);
    }

    public void ApplyCurrentTheme(bool force)
    {
        ThemeFamilyStyle family = GetCurrentFamily();
        if (!force && family == lastAppliedFamily)
            return;

        FamilyPalette palette = GetPalette(family);
        ApplyPalette(palette);
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

        if (fillImage != null)
            fillImage.color = palette.fillColor;

        if (highlightImage != null)
            highlightImage.color = palette.highlightColor;

        if (labelText != null)
            labelText.color = palette.textColor;

        if (countText != null)
            countText.color = palette.textColor;

        if (iconImage != null)
            iconImage.color = palette.iconColor;

        if (cachedShadow != null)
        {
            cachedShadow.effectColor = palette.shadowColor;
            cachedShadow.effectDistance = releasedShadowDistance;
            cachedShadow.useGraphicAlpha = true;
        }

        if (cachedOutline != null)
        {
            cachedOutline.effectColor = palette.outlineColor;
            cachedOutline.effectDistance = new Vector2(2f, -2f);
            cachedOutline.useGraphicAlpha = true;
        }

        ApplyButtonStateColors();
        ApplyModeVisibility();
        SetPressed(false);
    }

    private void ApplyModeVisibility()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(visualMode == ButtonVisualMode.MainMenuIcon);

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
        colors.highlightedColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.disabledColor = new Color(0.70f, 0.70f, 0.70f, 0.65f);
        targetButton.colors = colors;
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
        if (pressRoot != null)
        {
            Vector2 pos = pressRoot.anchoredPosition;
            pos.y = pressed ? pressedY : releasedY;
            pressRoot.anchoredPosition = pos;
        }

        if (cachedShadow != null)
            cachedShadow.effectDistance = pressed ? pressedShadowDistance : releasedShadowDistance;
    }

    private Shadow GetOrAddShadow(GameObject target)
    {
        Component[] components = target.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].GetType() == typeof(Shadow))
                return (Shadow)components[i];
        }

        return target.AddComponent<Shadow>();
    }

    private Outline GetOrAddOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline != null)
            return outline;

        return target.AddComponent<Outline>();
    }
}