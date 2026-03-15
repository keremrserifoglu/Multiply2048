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
        "TitleArea"
    };

    private static readonly string[] ButtonRootNames =
    {
        "MainMenuPanel",
        "SettingPanel",
        "GameOverPanel",
        "LimitedCreditsPanel",
        "GameOverAdPanel",
        "BottomBar"
    };

    public Image mainMenuBackground;
    public Image hudBackground;
    public Image gameOverBackground;

    [Header("Root Background Alpha")]
    [SerializeField] private float mainMenuAlpha = 0f;
    [SerializeField] private float hudAlpha = 0f;
    [SerializeField] private float gameOverAlpha = 0.40f;

    private readonly List<Button> reusableButtons = new List<Button>(32);
    private readonly HashSet<Button> reusableButtonSet = new HashSet<Button>();

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
        if (ThemeManager.I == null)
        {
            MakePanelsTransparent();
            return;
        }

        ThemeManager.UIThemeColors ui = ThemeManager.I.GetUIThemeColors();

        ApplyRootBackground(mainMenuBackground, ThemeManager.I.GetBackgroundColor(), mainMenuAlpha);
        ApplyRootBackground(hudBackground, ThemeManager.I.GetBackgroundColor(), hudAlpha);
        ApplyRootBackground(gameOverBackground, ui.overlayColor, gameOverAlpha);

        ApplyPanelStyles(ui);
        ApplyButtonStyles(ui);
        ApplyPanelTextStyles(ui);
    }

    private void MakePanelsTransparent()
    {
        SetImageAlpha(mainMenuBackground, 0f);
        SetImageAlpha(hudBackground, 0f);
        SetImageAlpha(gameOverBackground, 0f);
    }

    private void ApplyRootBackground(Image image, Color baseColor, float alpha)
    {
        if (!image)
            return;

        Color c = baseColor;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (!image)
            return;

        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void ApplyPanelStyles(ThemeManager.UIThemeColors ui)
    {
        for (int i = 0; i < PanelNames.Length; i++)
        {
            List<Transform> targets = FindSceneTransforms(PanelNames[i]);
            for (int j = 0; j < targets.Count; j++)
                StylePanelImage(targets[j].gameObject, ui, IsInnerPanel(targets[j].name));
        }
    }

    private void ApplyPanelTextStyles(ThemeManager.UIThemeColors ui)
    {
        for (int i = 0; i < PanelNames.Length; i++)
        {
            List<Transform> roots = FindSceneTransforms(PanelNames[i]);
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                Transform root = roots[rootIndex];
                TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int j = 0; j < tmpTexts.Length; j++)
                {
                    if (tmpTexts[j] == null || tmpTexts[j].GetComponentInParent<Button>() != null)
                        continue;

                    tmpTexts[j].color = IsTitleLike(tmpTexts[j].name, tmpTexts[j].fontSize) ? ui.panelTitleColor : ui.panelTextColor;
                }

                Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
                for (int j = 0; j < legacyTexts.Length; j++)
                {
                    if (legacyTexts[j] == null || legacyTexts[j].GetComponentInParent<Button>() != null)
                        continue;

                    legacyTexts[j].color = IsTitleLike(legacyTexts[j].name, legacyTexts[j].fontSize) ? ui.panelTitleColor : ui.panelTextColor;
                }
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

        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();

        outline.effectColor = ui.panelOutlineColor;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = true;
    }

    private void ApplyButtonStyles(ThemeManager.UIThemeColors ui)
    {
        reusableButtons.Clear();
        reusableButtonSet.Clear();

        for (int i = 0; i < ButtonRootNames.Length; i++)
        {
            Transform root = FindSceneTransform(ButtonRootNames[i]);
            if (root == null)
                continue;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int j = 0; j < buttons.Length; j++)
            {
                Button button = buttons[j];
                if (!ShouldThemeButton(button))
                    continue;

                if (reusableButtonSet.Add(button))
                    reusableButtons.Add(button);
            }
        }

        for (int i = 0; i < reusableButtons.Count; i++)
        {
            Button button = reusableButtons[i];
            Image targetImage = button.targetGraphic as Image;
            if (targetImage == null)
                continue;

            Color face = ThemeManager.I.GetUIButtonFaceColor(i);
            Color shadow = ThemeManager.I.GetUIButtonShadowColor(i);
            Color outline = ThemeManager.I.GetUIButtonOutlineColor(i);
            Color highlight = ThemeManager.I.GetUIButtonHighlightColor(i);
            Color content = ThemeManager.I.GetReadableButtonContentColor(i);

            RuntimeThemedButtonDepth depth = button.GetComponent<RuntimeThemedButtonDepth>();
            if (depth == null)
                depth = button.gameObject.AddComponent<RuntimeThemedButtonDepth>();

            depth.Apply(button, targetImage, face, shadow, outline, highlight, content);
        }
    }

    private bool ShouldThemeButton(Button button)
    {
        if (button == null || button.targetGraphic == null)
            return false;

        string lowerName = button.name.ToLowerInvariant();
        if (lowerName.Contains("overlay"))
            return false;

        if (lowerName.Contains("themebutton"))
            return false;

        RectTransform rt = button.transform as RectTransform;
        if (rt == null)
            return true;

        return rt.rect.width >= 90f && rt.rect.height >= 36f;
    }

    private bool IsInnerPanel(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("section") ||
               lowerName.Contains("row") ||
               lowerName.Contains("bar") ||
               lowerName.Contains("title");
    }

    private bool IsTitleLike(string objectName, float fontSize)
    {
        string lowerName = objectName.ToLowerInvariant();
        return lowerName.Contains("title") || lowerName.Contains("header") || fontSize >= 30f;
    }

    private Transform FindSceneTransform(string objectName)
    {
        List<Transform> matches = FindSceneTransforms(objectName);
        return matches.Count > 0 ? matches[0] : null;
    }

    private List<Transform> FindSceneTransforms(string objectName)
    {
        List<Transform> matches = new List<Transform>();

#if UNITY_2023_1_OR_NEWER
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
#endif

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null)
                continue;

            if (current.name != objectName)
                continue;

            if (!current.gameObject.scene.IsValid())
                continue;

            matches.Add(current);
        }

        return matches;
    }
}

public class RuntimeThemedButtonDepth : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const string HighlightObjectName = "__UIThemeHighlight";
    private const string LipObjectName = "__UIThemeLip";

    private Button cachedButton;
    private Image cachedTargetImage;
    private RectTransform cachedRectTransform;
    private Vector3 releasedScale = Vector3.one;
    private bool isPressed;

    private Shadow cachedShadow;
    private Outline cachedOutline;
    private Image topHighlight;
    private Image bottomLip;

    public void Apply(Button button, Image targetImage, Color face, Color shadow, Color outline, Color highlight, Color content)
    {
        cachedButton = button;
        cachedTargetImage = targetImage;
        cachedRectTransform = transform as RectTransform;

        if (!isPressed)
            releasedScale = transform.localScale;

        cachedTargetImage.color = face;
        cachedTargetImage.type = Image.Type.Sliced;

        cachedShadow = GetOrAddShadow(gameObject);
        cachedShadow.effectColor = shadow;
        cachedShadow.effectDistance = new Vector2(0f, -10f);
        cachedShadow.useGraphicAlpha = true;

        cachedOutline = GetOrAddOutline(gameObject);
        cachedOutline.effectColor = outline;
        cachedOutline.effectDistance = new Vector2(2f, -2f);
        cachedOutline.useGraphicAlpha = true;

        EnsureDecorImages();
        ApplyDecorLayout();

        if (topHighlight != null)
            topHighlight.color = highlight;

        if (bottomLip != null)
            bottomLip.color = shadow;

        ApplyContentTint(content);
        ApplyButtonStateColors(face, content);
    }

    private void EnsureDecorImages()
    {
        topHighlight = GetOrCreateDecorImage(HighlightObjectName);
        bottomLip = GetOrCreateDecorImage(LipObjectName);
    }

    private void ApplyDecorLayout()
    {
        if (topHighlight != null)
        {
            RectTransform rt = topHighlight.rectTransform;
            rt.anchorMin = new Vector2(0.06f, 0.58f);
            rt.anchorMax = new Vector2(0.94f, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            topHighlight.raycastTarget = false;
        }

        if (bottomLip != null)
        {
            RectTransform rt = bottomLip.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 0.06f);
            rt.anchorMax = new Vector2(0.96f, 0.22f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            bottomLip.raycastTarget = false;
        }
    }

    private Image GetOrCreateDecorImage(string objectName)
    {
        Transform child = transform.Find(objectName);
        Image image;

        if (child == null)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex(0);
            image = go.GetComponent<Image>();
            image.sprite = cachedTargetImage != null ? cachedTargetImage.sprite : null;
            image.type = Image.Type.Sliced;
            image.maskable = true;
        }
        else
        {
            image = child.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = cachedTargetImage != null ? cachedTargetImage.sprite : null;
                image.type = Image.Type.Sliced;
            }
        }

        return image;
    }

    private void ApplyContentTint(Color color)
    {
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmpTexts.Length; i++)
        {
            if (tmpTexts[i] == null)
                continue;

            tmpTexts[i].color = color;
        }

        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            if (legacyTexts[i] == null)
                continue;

            legacyTexts[i].color = color;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == cachedTargetImage || image == topHighlight || image == bottomLip)
                continue;

            image.color = color;
        }
    }

    private void ApplyButtonStateColors(Color face, Color content)
    {
        if (cachedButton == null)
            return;

        ColorBlock colors = cachedButton.colors;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, content, 0.04f);
        colors.pressedColor = Color.Lerp(Color.white, Color.black, 0.08f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.65f);
        cachedButton.colors = colors;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        transform.localScale = releasedScale * 0.985f;

        if (cachedShadow != null)
            cachedShadow.effectDistance = new Vector2(0f, -5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        isPressed = false;
        transform.localScale = releasedScale;

        if (cachedShadow != null)
            cachedShadow.effectDistance = new Vector2(0f, -10f);
    }

    private Outline GetOrAddOutline(GameObject target)
    {
        Outline[] components = target.GetComponents<Outline>();
        if (components != null && components.Length > 0)
            return components[0];

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
