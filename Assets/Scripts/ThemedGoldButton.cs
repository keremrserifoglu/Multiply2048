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

    [Header("Theme Tints")]
    [SerializeField] private Color darkButtonTint = new Color32(232, 201, 118, 255);
    [SerializeField] private Color colorfulButtonTint = new Color32(255, 255, 255, 255);
    [SerializeField] private Color lightButtonTint = new Color32(250, 244, 224, 255);

    [Header("Content Colors")]
    [SerializeField] private Color darkContentColor = new Color32(74, 43, 10, 255);
    [SerializeField] private Color colorfulContentColor = new Color32(84, 48, 12, 255);
    [SerializeField] private Color lightContentColor = new Color32(92, 56, 18, 255);

    [Header("Pressed State")]
    [Range(0.85f, 1f)][SerializeField] private float pressedTintMultiplier = 0.97f;

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

        ApplyModeVisibility();
        ApplyButtonStateColors();
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
            targetImage.type = Image.Type.Sliced;
        }

        if (labelText != null)
            labelText.color = contentColor;

        if (countText != null)
            countText.color = contentColor;

        if (iconImage != null)
            iconImage.color = contentColor;
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