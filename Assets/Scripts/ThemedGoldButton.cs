using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class ThemedGoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ButtonVisualMode
    {
        MainMenuIcon,
        CountButton,
        TextOnlyFrame
    }

    private enum ThemeFamilyStyle
    {
        Dark,
        Colorful,
        Light
    }

    [Header("Mode")]
    [SerializeField] private ButtonVisualMode visualMode = ButtonVisualMode.MainMenuIcon;

    [Header("Target")]
    [SerializeField] private Button targetButton;
    [SerializeField] private Image targetImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text countText;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Sprite Rendering")]
    [SerializeField] private bool useSlicedSprite = false;
    [SerializeField] private bool preserveAspect = false;

    [Header("Button Sizing")]
    [SerializeField] private bool applyButtonSizing = true;
    [SerializeField] private Vector2 mainMenuButtonSize = new Vector2(760f, 150f);
    [SerializeField] private Vector2 bottomBarButtonSize = new Vector2(300f, 96f);
    [SerializeField] private Vector2 countButtonSize = new Vector2(300f, 96f);

    [Header("Theme Tints")]
    [SerializeField] private Color darkButtonTint = new Color32(232, 201, 118, 255);
    [SerializeField] private Color colorfulButtonTint = new Color32(255, 255, 255, 255);
    [SerializeField] private Color lightButtonTint = new Color32(250, 244, 224, 255);

    [Header("Content Colors")]
    [SerializeField] private Color darkContentColor = new Color32(74, 43, 10, 255);
    [SerializeField] private Color colorfulContentColor = new Color32(84, 48, 12, 255);
    [SerializeField] private Color lightContentColor = new Color32(92, 56, 18, 255);

    [Header("Text Layout")]
    [SerializeField] private bool autoSizeText = true;
    [SerializeField] private float mainMenuMinFont = 28f;
    [SerializeField] private float mainMenuMaxFont = 44f;
    [SerializeField] private float bottomBarMinFont = 18f;
    [SerializeField] private float bottomBarMaxFont = 30f;
    [SerializeField] private float countMinFont = 12f;
    [SerializeField] private float countMaxFont = 22f;
    [SerializeField] private Vector4 mainMenuMargins = new Vector4(28f, 6f, 28f, 8f);
    [SerializeField] private Vector4 bottomBarMargins = new Vector4(18f, 4f, 18f, 5f);
    [SerializeField] private Vector4 countMargins = new Vector4(14f, 3f, 14f, 4f);

    [Header("Pressed State")]
    [Range(0.85f, 1f)][SerializeField] private float pressedTintMultiplier = 0.97f;

    private RectTransform cachedRectTransform;
    private LayoutElement cachedLayoutElement;
    private bool isPressed;
    private ThemeFamilyStyle lastAppliedFamily;

    private void Reset()
    {
        targetButton = GetComponent<Button>();
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        cachedRectTransform = GetComponent<RectTransform>();
        cachedLayoutElement = GetComponent<LayoutElement>();
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;

        ApplyCurrentTheme(true);
        SetPressed(false);
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (cachedRectTransform == null)
            cachedRectTransform = GetComponent<RectTransform>();

        if (cachedLayoutElement == null)
            cachedLayoutElement = GetComponent<LayoutElement>();
    }
#endif

    private void HandlePaletteChanged()
    {
        ApplyCurrentTheme(true);
    }

    public void ApplyCurrentTheme(bool force)
    {
        ThemeFamilyStyle family = GetCurrentFamily();
        if (!force && family == lastAppliedFamily)
            return;

        ApplyButtonSizing();
        ApplyModeVisibility();
        ApplyButtonStateColors();
        ApplyTextLayout();
        ApplyVisualState();

        lastAppliedFamily = family;
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

    private void ApplyButtonSizing()
    {
        if (!applyButtonSizing)
            return;

        Vector2 size = GetTargetButtonSize();

        if (cachedRectTransform != null)
        {
            cachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            cachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        if (cachedLayoutElement != null)
        {
            cachedLayoutElement.minWidth = size.x;
            cachedLayoutElement.minHeight = size.y;
            cachedLayoutElement.preferredWidth = size.x;
            cachedLayoutElement.preferredHeight = size.y;
            cachedLayoutElement.flexibleWidth = -1f;
            cachedLayoutElement.flexibleHeight = -1f;
        }
    }

    private Vector2 GetTargetButtonSize()
    {
        switch (visualMode)
        {
            case ButtonVisualMode.CountButton:
                return countButtonSize;
            case ButtonVisualMode.TextOnlyFrame:
                return bottomBarButtonSize;
            default:
                return mainMenuButtonSize;
        }
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

    private void ApplyTextLayout()
    {
        if (!autoSizeText)
            return;

        if (labelText != null)
        {
            if (visualMode == ButtonVisualMode.TextOnlyFrame)
                ConfigureText(labelText, bottomBarMinFont, bottomBarMaxFont, bottomBarMargins);
            else
                ConfigureText(labelText, mainMenuMinFont, mainMenuMaxFont, mainMenuMargins);
        }

        if (countText != null)
            ConfigureText(countText, countMinFont, countMaxFont, countMargins);
    }

    private void ConfigureText(TMP_Text text, float minFont, float maxFont, Vector4 margins)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minFont;
        text.fontSizeMax = maxFont;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.margin = margins;

        RectTransform rt = text.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(margins.x, margins.w);
        rt.offsetMax = new Vector2(-margins.z, -margins.y);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private void ApplyVisualState()
    {
        ThemeFamilyStyle family = GetCurrentFamily();
        Color buttonTint = GetButtonTint(family);
        Color contentColor = GetContentColor(family);

        if (isPressed)
            buttonTint = MultiplyRgb(buttonTint, pressedTintMultiplier);

        bool interactable = targetButton == null || targetButton.interactable;
        float alpha = interactable ? 1f : 0.55f;

        buttonTint.a *= alpha;
        contentColor.a *= alpha;

        if (targetImage != null)
        {
            if (normalSprite != null && pressedSprite != null)
                targetImage.sprite = isPressed ? pressedSprite : normalSprite;
            else if (normalSprite != null)
                targetImage.sprite = normalSprite;

            targetImage.color = buttonTint;
            targetImage.type = useSlicedSprite ? Image.Type.Sliced : Image.Type.Simple;
            targetImage.preserveAspect = useSlicedSprite && preserveAspect;
            targetImage.raycastTarget = true;
        }

        if (labelText != null)
            labelText.color = contentColor;

        if (countText != null)
            countText.color = contentColor;

        if (iconImage != null)
        {
            iconImage.color = contentColor;
            iconImage.raycastTarget = false;
        }
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

    private Color GetButtonTint(ThemeFamilyStyle family)
    {
        switch (family)
        {
            case ThemeFamilyStyle.Dark:
                return darkButtonTint;
            case ThemeFamilyStyle.Light:
                return lightButtonTint;
            default:
                return colorfulButtonTint;
        }
    }

    private Color GetContentColor(ThemeFamilyStyle family)
    {
        switch (family)
        {
            case ThemeFamilyStyle.Dark:
                return darkContentColor;
            case ThemeFamilyStyle.Light:
                return lightContentColor;
            default:
                return colorfulContentColor;
        }
    }

    private static Color MultiplyRgb(Color color, float multiplier)
    {
        color.r *= multiplier;
        color.g *= multiplier;
        color.b *= multiplier;
        return color;
    }
}