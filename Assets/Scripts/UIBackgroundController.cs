using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBackgroundController : MonoBehaviour
{
    private static readonly string[] PanelNames =
    {
        "Window",
        "Card",
        "Dialog",
        "ThemeSection",
        "ThemeRow",
        "BottomBar",
        "TitleArea",
        "ScoresArea"
    };

    private static readonly Color GoldButtonFace = new Color32(0xE7, 0xB1, 0x3E, 0xFF);
    private static readonly Color GoldButtonShadow = new Color32(0x7E, 0x49, 0x10, 0xD8);
    private static readonly Color GoldButtonOutline = new Color32(0xFF, 0xE3, 0x9B, 0xFF);
    private static readonly Color GoldButtonContent = new Color32(0x24, 0x17, 0x06, 0xFF);

    [Header("Root Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image backgroundTintOverlay;
    [SerializeField] private Image modalOverlay;

    [Header("Theme Background Sprites")]
    [SerializeField] private Sprite darkBackgroundSprite;
    [SerializeField] private Sprite colorfulBackgroundSprite;
    [SerializeField] private Sprite lightBackgroundSprite;

    [Header("Theme Overlay Colors")]
    [SerializeField] private Color darkBackgroundOverlay = new Color(0.06f, 0.05f, 0.08f, 1f);
    [SerializeField] private Color colorfulBackgroundOverlay = new Color(0.14f, 0.08f, 0.18f, 1f);
    [SerializeField] private Color lightBackgroundOverlay = new Color(1f, 0.97f, 0.90f, 1f);

    [Header("Theme Overlay Alpha")]
    [SerializeField][Range(0f, 1f)] private float darkOverlayAlpha = 0.10f;
    [SerializeField][Range(0f, 1f)] private float colorfulOverlayAlpha = 0.12f;
    [SerializeField][Range(0f, 1f)] private float lightOverlayAlpha = 0.08f;

    [Header("Modal Overlay Alpha")]
    [SerializeField][Range(0f, 1f)] private float modalOverlayAlpha = 0.40f;

    private readonly List<Button> reusableButtons = new List<Button>(64);

    private void Start()
    {
        ApplyTheme();
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += ApplyTheme;

        ApplyTheme();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= ApplyTheme;
    }

    private void ApplyTheme()
    {
        ApplyRootBackgrounds();
        ApplyButtonStyles();

        if (ThemeManager.I == null)
            return;

        ThemeManager.UIThemeColors ui = ThemeManager.I.GetUIThemeColors();
        ApplyPanelStyles(ui);
        ApplyCanvasTextStyles(ui.panelTextColor);
    }

    private void ApplyRootBackgrounds()
    {
        if (backgroundImage != null)
            backgroundImage.enabled = false;

        if (backgroundTintOverlay != null)
            backgroundTintOverlay.enabled = false;

        if (modalOverlay != null)
        {
            TilePaletteDatabase.ThemeFamily family = GetCurrentFamily();

            Color modalColor = ThemeManager.I != null
                ? ThemeManager.I.GetUIThemeColors().overlayColor
                : GetFamilyOverlayColor(family);

            ApplyImageColor(modalOverlay, modalColor, modalOverlayAlpha);
        }
    }

    private TilePaletteDatabase.ThemeFamily GetCurrentFamily()
    {
        if (ThemeManager.I == null)
            return TilePaletteDatabase.ThemeFamily.Colorful;

        return ThemeManager.I.GetCurrentPaletteFamily();
    }

    private Sprite GetBackgroundSprite(TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return darkBackgroundSprite != null ? darkBackgroundSprite : colorfulBackgroundSprite;
            case TilePaletteDatabase.ThemeFamily.Light:
                return lightBackgroundSprite != null ? lightBackgroundSprite : colorfulBackgroundSprite;
            case TilePaletteDatabase.ThemeFamily.Colorful:
            default:
                return colorfulBackgroundSprite != null ? colorfulBackgroundSprite : darkBackgroundSprite;
        }
    }

    private Color GetFamilyOverlayColor(TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return darkBackgroundOverlay;
            case TilePaletteDatabase.ThemeFamily.Light:
                return lightBackgroundOverlay;
            case TilePaletteDatabase.ThemeFamily.Colorful:
            default:
                return colorfulBackgroundOverlay;
        }
    }

    private float GetFamilyOverlayAlpha(TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return darkOverlayAlpha;
            case TilePaletteDatabase.ThemeFamily.Light:
                return lightOverlayAlpha;
            case TilePaletteDatabase.ThemeFamily.Colorful:
            default:
                return colorfulOverlayAlpha;
        }
    }

    private void ApplyImageColor(Image target, Color color, float alpha)
    {
        if (target == null)
            return;

        color.a = Mathf.Clamp01(alpha);
        target.color = color;
    }

    private void ApplyPanelStyles(ThemeManager.UIThemeColors ui)
    {
        for (int i = 0; i < PanelNames.Length; i++)
        {
            List<Transform> roots = FindSceneTransforms(PanelNames[i]);
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                Transform root = roots[rootIndex];
                StylePanelImage(root.gameObject, ui, IsInnerPanel(root.name));
            }
        }
    }

    private void StylePanelImage(GameObject target, ThemeManager.UIThemeColors ui, bool useInnerColor)
    {
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image == null)
            return;

        image.color = useInnerColor ? ui.panelInnerColor : ui.panelColor;

        Outline outline = GetOrAddOutline(target);
        outline.effectColor = ui.panelOutlineColor;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = true;
    }

    private void ApplyButtonStyles()
    {
        reusableButtons.Clear();
        List<Button> allButtons = FindSceneComponents<Button>();

        for (int i = 0; i < allButtons.Count; i++)
        {
            Button button = allButtons[i];
            if (!ShouldThemeButton(button))
                continue;

            reusableButtons.Add(button);
        }

        for (int i = 0; i < reusableButtons.Count; i++)
        {
            Button button = reusableButtons[i];
            Image targetImage = button.targetGraphic as Image;
            if (targetImage == null)
                continue;

            RuntimeThemedButtonDepth depth = button.GetComponent<RuntimeThemedButtonDepth>();
            if (depth == null)
                depth = button.gameObject.AddComponent<RuntimeThemedButtonDepth>();

            depth.Apply(
                button,
                targetImage,
                GoldButtonFace,
                GoldButtonShadow,
                GoldButtonOutline,
                GoldButtonContent);
        }
    }

    private void ApplyCanvasTextStyles(Color textColor)
    {
        List<TMP_Text> tmpTexts = FindSceneComponents<TMP_Text>();
        for (int i = 0; i < tmpTexts.Count; i++)
        {
            TMP_Text text = tmpTexts[i];
            if (text == null || text.GetComponentInParent<Button>() != null)
                continue;
            if (text.GetComponentInParent<Toggle>() != null)
                continue;

            text.color = textColor;
        }

        List<Text> legacyTexts = FindSceneComponents<Text>();
        for (int i = 0; i < legacyTexts.Count; i++)
        {
            Text text = legacyTexts[i];
            if (text == null || text.GetComponentInParent<Button>() != null)
                continue;
            if (text.GetComponentInParent<Toggle>() != null)
                continue;

            text.color = textColor;
        }
    }

    private bool ShouldThemeButton(Button button)
    {
        if (button == null || button.targetGraphic == null)
            return false;
        if (button.GetComponentInParent<Canvas>() == null)
            return false;

        string lowerName = button.name.ToLowerInvariant();
        if (lowerName.Contains("overlay"))
            return false;
        if (lowerName.Contains("theme") ||
            lowerName.Contains("dark") ||
            lowerName.Contains("light") ||
            lowerName.Contains("colorful") ||
            lowerName.Contains("sfx") ||
            lowerName.Contains("soundeffect") ||
            lowerName.Contains("sound_effect"))
            return false;

        Image image = button.targetGraphic as Image;
        if (image == null)
            return false;

        RectTransform rt = button.transform as RectTransform;
        if (rt == null)
            return true;

        return rt.rect.width >= 70f && rt.rect.height >= 28f;
    }

    private bool IsInnerPanel(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("section") ||
               lowerName.Contains("row") ||
               lowerName.Contains("bar") ||
               lowerName.Contains("title") ||
               lowerName.Contains("scores");
    }

    private List<Transform> FindSceneTransforms(string objectName)
    {
        List<Transform> matches = new List<Transform>();
        List<Transform> transforms = FindSceneComponents<Transform>();

        for (int i = 0; i < transforms.Count; i++)
        {
            Transform current = transforms[i];
            if (current == null)
                continue;
            if (current.name != objectName)
                continue;

            matches.Add(current);
        }

        return matches;
    }

    private List<T> FindSceneComponents<T>() where T : Component
    {
        List<T> results = new List<T>();

#if UNITY_2023_1_OR_NEWER
        T[] found = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        T[] found = Resources.FindObjectsOfTypeAll<T>();
#endif

        for (int i = 0; i < found.Length; i++)
        {
            T item = found[i];
            if (item == null)
                continue;
            if (!item.gameObject.scene.IsValid())
                continue;

            results.Add(item);
        }

        return results;
    }

    private Outline GetOrAddOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline != null)
            return outline;

        return target.AddComponent<Outline>();
    }
}

public class RuntimeThemedButtonDepth : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const string LegacyHighlightObjectName = "__UIThemeHighlight";
    private const string LegacyLipObjectName = "__UIThemeLip";

    private Button cachedButton;
    private Image cachedTargetImage;
    private RectTransform cachedTargetRect;
    private Shadow cachedShadow;
    private Outline cachedOutline;
    private Vector2 releasedAnchoredPosition;

    public void Apply(Button button, Image targetImage, Color face, Color shadow, Color outline, Color content)
    {
        cachedButton = button;
        cachedTargetImage = targetImage;
        cachedTargetRect = targetImage.rectTransform;
        releasedAnchoredPosition = cachedTargetRect != null ? cachedTargetRect.anchoredPosition : Vector2.zero;

        RemoveLegacyDecor();

        cachedTargetImage.color = face;
        cachedTargetImage.type = cachedTargetImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        cachedShadow = GetOrAddShadow(gameObject);
        cachedShadow.effectColor = shadow;
        cachedShadow.effectDistance = new Vector2(0f, -8f);
        cachedShadow.useGraphicAlpha = true;

        cachedOutline = GetOrAddOutline(gameObject);
        cachedOutline.effectColor = outline;
        cachedOutline.effectDistance = new Vector2(2f, -2f);
        cachedOutline.useGraphicAlpha = true;

        RestoreDepth();
        ApplyContentTint(content);
        ApplyButtonStateColors();
    }

    private void RemoveLegacyDecor()
    {
        Transform legacyHighlight = transform.Find(LegacyHighlightObjectName);
        if (legacyHighlight != null)
        {
            if (Application.isPlaying)
                Destroy(legacyHighlight.gameObject);
            else
                DestroyImmediate(legacyHighlight.gameObject);
        }

        Transform legacyLip = transform.Find(LegacyLipObjectName);
        if (legacyLip != null)
        {
            if (Application.isPlaying)
                Destroy(legacyLip.gameObject);
            else
                DestroyImmediate(legacyLip.gameObject);
        }
    }

    private void ApplyContentTint(Color color)
    {
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmpTexts.Length; i++)
        {
            if (tmpTexts[i] != null)
                tmpTexts[i].color = color;
        }

        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            if (legacyTexts[i] != null)
                legacyTexts[i].color = color;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == cachedTargetImage)
                continue;

            image.color = color;
        }
    }

    private void ApplyButtonStateColors()
    {
        if (cachedButton == null)
            return;

        ColorBlock colors = cachedButton.colors;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.65f);
        cachedButton.colors = colors;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cachedShadow != null)
            cachedShadow.effectDistance = new Vector2(0f, -4f);

        if (cachedTargetRect != null)
            cachedTargetRect.anchoredPosition = releasedAnchoredPosition + new Vector2(0f, -3f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RestoreDepth();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreDepth();
    }

    private void RestoreDepth()
    {
        if (cachedShadow != null)
            cachedShadow.effectDistance = new Vector2(0f, -8f);

        if (cachedTargetRect != null)
            cachedTargetRect.anchoredPosition = releasedAnchoredPosition;
    }

    private Outline GetOrAddOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline != null)
            return outline;

        return target.AddComponent<Outline>();
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
}
