using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
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

    [Header("Mode")]
    [SerializeField] private ButtonVisualMode visualMode = ButtonVisualMode.MainMenuIcon;

    [Header("Target")]
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

    [Header("Text Layout")]
    [SerializeField]
    [FormerlySerializedAs("autoSizeText")]
    private bool applyTextLayout = true;

    [SerializeField] private Vector4 mainMenuMargins = new Vector4(28f, 6f, 28f, 8f);
    [SerializeField] private Vector4 bottomBarMargins = new Vector4(18f, 4f, 18f, 5f);
    [SerializeField] private Vector4 countMargins = new Vector4(14f, 3f, 14f, 4f);

    private RectTransform cachedRectTransform;
    private LayoutElement cachedLayoutElement;
    private bool isPressed;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        cachedRectTransform = GetComponent<RectTransform>();
        cachedLayoutElement = GetComponent<LayoutElement>();
    }

    private void OnEnable()
    {
        if (!ShouldApplyThemeAutomatically())
            return;

        isPressed = false;
        ApplyCurrentTheme(true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (cachedRectTransform == null)
            cachedRectTransform = GetComponent<RectTransform>();

        if (cachedLayoutElement == null)
            cachedLayoutElement = GetComponent<LayoutElement>();
    }
#endif

    public void ApplyCurrentTheme(bool force)
    {
        if (!ShouldApplyThemeAutomatically())
            return;

        ApplyButtonSizing();
        ApplyModeVisibility();
        ApplyTextLayout();
        ApplyVisualState();
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

    private void ApplyTextLayout()
    {
        if (!applyTextLayout)
            return;

        if (labelText != null)
        {
            Vector4 margins = visualMode == ButtonVisualMode.TextOnlyFrame
                ? bottomBarMargins
                : mainMenuMargins;

            ConfigureText(labelText, margins);
        }

        if (countText != null)
            ConfigureText(countText, countMargins);
    }

    private void ConfigureText(TMP_Text text, Vector4 margins)
    {
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
        if (targetImage != null)
        {
            if (normalSprite != null && pressedSprite != null)
                targetImage.sprite = isPressed ? pressedSprite : normalSprite;
            else if (normalSprite != null)
                targetImage.sprite = normalSprite;

            targetImage.type = useSlicedSprite ? Image.Type.Sliced : Image.Type.Simple;
            targetImage.preserveAspect = useSlicedSprite && preserveAspect;
            targetImage.raycastTarget = true;
        }

        if (iconImage != null)
            iconImage.raycastTarget = false;
    }

    private bool ShouldApplyThemeAutomatically()
    {
        return Application.isPlaying;
    }
}