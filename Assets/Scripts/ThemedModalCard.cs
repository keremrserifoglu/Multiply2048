using System.Collections;
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

    [Header("Layout")]
    [SerializeField] private bool autoFitToContent = true;
    [SerializeField] private Vector2 contentPadding = new Vector2(76f, 72f);
    [SerializeField] private Vector2 minPanelSize = new Vector2(560f, 360f);
    [SerializeField] private Vector2 maxPanelSize = new Vector2(900f, 760f);
    [SerializeField][Range(0.45f, 0.98f)] private float parentWidthRatio = 0.88f;
    [SerializeField][Range(0.35f, 0.95f)] private float parentHeightRatio = 0.78f;
    [SerializeField] private float innerInset = 18f;

    [Header("Visual Balance")]
    [SerializeField][Range(0f, 1f)] private float goldBlend = 0.34f;
    [SerializeField][Range(0f, 1f)] private float outlineGoldBlend = 0.76f;
    [SerializeField] private Vector2 outlineDistance = new Vector2(2f, -2f);
    [SerializeField] private Vector2 shadowDistance = new Vector2(0f, -14f);

    private readonly Vector3[] cornerBuffer = new Vector3[4];

    private RectTransform cachedHostRect;
    private RectTransform cachedContainerRect;
    private Image cachedContainerImage;
    private Image effectiveOverlayImage;
    private bool layoutDirty;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private Coroutine refreshRoutine;

    private void Awake()
    {
        cachedHostRect = transform as RectTransform;
        CacheResolvedTargets();
    }

    private void OnEnable()
    {
        Subscribe(true);
        MarkLayoutDirty();
        RestartRefreshRoutine();
        ApplyTheme();
    }

    private void OnDisable()
    {
        Subscribe(false);

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private void LateUpdate()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            MarkLayoutDirty();
        }

        if (!layoutDirty)
        {
            return;
        }

        RefreshCardPresentation(true);
        layoutDirty = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        MarkLayoutDirty();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cachedHostRect = transform as RectTransform;
        CacheResolvedTargets();

        if (!Application.isPlaying)
        {
            RefreshCardPresentation(true);
        }
    }
#endif

    private void Subscribe(bool subscribe)
    {
        if (ThemeManager.I == null)
        {
            return;
        }

        if (subscribe)
        {
            ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;
        }
        else
        {
            ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
        }
    }

    private void HandlePaletteChanged()
    {
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        RefreshCardPresentation(true);
    }

    private void RefreshCardPresentation(bool includeLayoutRefresh)
    {
        CacheResolvedTargets();

        if (includeLayoutRefresh)
        {
            PrepareRuntimeLayout();
        }

        ThemeManager.UIThemeColors ui = GetThemeColors();
        ThemeManager.GoldButtonColors gold = GetGoldColors();

        ApplyOverlayTheme(ui);
        ApplyPanelTheme(ui, gold);
        ApplyTextTheme(ui, gold);
        ApplyProgressTheme(ui, gold);
        RefreshLinkedButtons();
    }

    private void PrepareRuntimeLayout()
    {
        if (effectiveOverlayImage != null)
        {
            PrepareOverlayRect(effectiveOverlayImage.rectTransform);
        }

        if (cachedContainerRect == null)
        {
            return;
        }

        PrepareBackgroundImage(frameImage, 0f);
        PrepareBackgroundImage(innerImage, innerInset);
        AutoFitContainerToContent();
    }

    private void PrepareOverlayRect(RectTransform overlayRect)
    {
        if (overlayRect == null)
        {
            return;
        }

        RectTransform expectedParent = cachedHostRect != null ? cachedHostRect : transform as RectTransform;
        if (overlayRect.parent != expectedParent)
        {
            return;
        }

        StretchRect(overlayRect, 0f);
        overlayRect.SetAsFirstSibling();

        if (effectiveOverlayImage != null)
        {
            effectiveOverlayImage.enabled = true;
        }
    }

    private void PrepareBackgroundImage(Image image, float inset)
    {
        if (image == null)
        {
            return;
        }

        RectTransform backgroundRect = image.rectTransform;
        if (backgroundRect == null)
        {
            return;
        }

        if (cachedContainerRect != null && backgroundRect.parent != cachedContainerRect)
        {
            return;
        }

        image.enabled = true;
        image.raycastTarget = false;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        StretchRect(backgroundRect, inset);
        EnsureIgnoredByLayout(backgroundRect.gameObject);
        backgroundRect.SetSiblingIndex(inset <= 0.001f ? 0 : 1);
    }

    private void AutoFitContainerToContent()
    {
        if (!autoFitToContent || cachedContainerRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cachedContainerRect);

        bool foundAnyBounds = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < cachedContainerRect.childCount; i++)
        {
            RectTransform child = cachedContainerRect.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (frameImage != null && child == frameImage.rectTransform)
            {
                continue;
            }

            if (innerImage != null && child == innerImage.rectTransform)
            {
                continue;
            }

            child.GetWorldCorners(cornerBuffer);

            for (int c = 0; c < cornerBuffer.Length; c++)
            {
                Vector3 localCorner = cachedContainerRect.InverseTransformPoint(cornerBuffer[c]);
                minX = Mathf.Min(minX, localCorner.x);
                maxX = Mathf.Max(maxX, localCorner.x);
                minY = Mathf.Min(minY, localCorner.y);
                maxY = Mathf.Max(maxY, localCorner.y);
                foundAnyBounds = true;
            }
        }

        if (!foundAnyBounds)
        {
            return;
        }

        float halfWidth = Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX)) + contentPadding.x;
        float halfHeight = Mathf.Max(Mathf.Abs(minY), Mathf.Abs(maxY)) + contentPadding.y;
        Vector2 desiredSize = new Vector2(halfWidth * 2f, halfHeight * 2f);

        RectTransform parentRect = cachedContainerRect.parent as RectTransform;
        if (parentRect != null)
        {
            desiredSize.x = Mathf.Min(desiredSize.x, parentRect.rect.width * parentWidthRatio);
            desiredSize.y = Mathf.Min(desiredSize.y, parentRect.rect.height * parentHeightRatio);
        }

        desiredSize.x = Mathf.Clamp(desiredSize.x, minPanelSize.x, maxPanelSize.x);
        desiredSize.y = Mathf.Clamp(desiredSize.y, minPanelSize.y, maxPanelSize.y);

        cachedContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredSize.x);
        cachedContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredSize.y);

        LayoutElement layoutElement = cachedContainerRect.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.minWidth = desiredSize.x;
            layoutElement.minHeight = desiredSize.y;
            layoutElement.preferredWidth = desiredSize.x;
            layoutElement.preferredHeight = desiredSize.y;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cachedContainerRect);
    }

    private void ApplyOverlayTheme(ThemeManager.UIThemeColors ui)
    {
        if (effectiveOverlayImage == null)
        {
            return;
        }

        effectiveOverlayImage.enabled = true;
        effectiveOverlayImage.type = effectiveOverlayImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        effectiveOverlayImage.raycastTarget = true;

        if (ShouldSkipOverlayTint(effectiveOverlayImage))
        {
            return;
        }

        effectiveOverlayImage.color = ui.overlayColor;
    }

    private static bool ShouldSkipOverlayTint(Image image)
    {
        if (image == null)
        {
            return true;
        }

        return image.GetComponent<Button>() != null;
    }

    private void ApplyPanelTheme(ThemeManager.UIThemeColors ui, ThemeManager.GoldButtonColors gold)
    {
        Color frameColor = ForceOpaque(Color.Lerp(ui.panelColor, gold.face, goldBlend));
        Color innerColor = ForceOpaque(Color.Lerp(ui.panelInnerColor, Color.Lerp(gold.face, Color.white, 0.84f), goldBlend * 0.55f));
        Color outlineColor = ForceOpaque(Color.Lerp(ui.panelOutlineColor, gold.outline, outlineGoldBlend));
        Color shadowColor = ForceOpaque(Color.Lerp(ui.panelOutlineColor, gold.shadow, outlineGoldBlend));
        shadowColor.a = 0.62f;

        if (cachedContainerImage != null)
        {
            Color clearContainer = Color.clear;
            clearContainer.a = 0f;
            cachedContainerImage.color = clearContainer;
            cachedContainerImage.type = cachedContainerImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        if (frameImage != null)
        {
            frameImage.enabled = true;
            frameImage.color = frameColor;
            frameImage.type = frameImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        if (innerImage != null)
        {
            innerImage.enabled = true;
            innerImage.color = innerColor;
            innerImage.type = innerImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        Outline targetOutline = GetResolvedOutline();
        if (targetOutline != null)
        {
            targetOutline.enabled = true;
            targetOutline.effectColor = outlineColor;
            targetOutline.effectDistance = outlineDistance;
            targetOutline.useGraphicAlpha = true;
        }

        Shadow targetShadow = GetResolvedShadow();
        if (targetShadow != null)
        {
            targetShadow.enabled = true;
            targetShadow.effectColor = shadowColor;
            targetShadow.effectDistance = shadowDistance;
            targetShadow.useGraphicAlpha = true;
        }
    }

    private void ApplyTextTheme(ThemeManager.UIThemeColors ui, ThemeManager.GoldButtonColors gold)
    {
        Color titleColor = ForceOpaque(Color.Lerp(ui.panelTitleColor, gold.content, 0.48f));
        Color bodyColor = ForceOpaque(Color.Lerp(ui.panelTextColor, gold.content, 0.30f));
        Color secondaryColor = bodyColor;
        secondaryColor.a *= 0.82f;

        if (titleText != null)
        {
            titleText.color = titleColor;
        }

        ApplyTextArray(bodyTexts, bodyColor);
        ApplyTextArray(secondaryTexts, secondaryColor);
    }

    private void ApplyProgressTheme(ThemeManager.UIThemeColors ui, ThemeManager.GoldButtonColors gold)
    {
        if (progressTrack != null)
        {
            Color track = ForceOpaque(Color.Lerp(ui.panelOutlineColor, gold.outline, 0.38f));
            track.a = 0.42f;
            progressTrack.color = track;
            progressTrack.type = progressTrack.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        if (progressFill != null)
        {
            Color fill = ForceOpaque(Color.Lerp(gold.face, Color.white, 0.08f));
            progressFill.color = fill;
            progressFill.type = progressFill.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        if (progressText != null)
        {
            progressText.color = ForceOpaque(Color.Lerp(ui.panelTitleColor, gold.content, 0.42f));
        }
    }

    private void RefreshLinkedButtons()
    {
        if (refreshButtons == null)
        {
            return;
        }

        for (int i = 0; i < refreshButtons.Length; i++)
        {
            if (refreshButtons[i] != null)
            {
                refreshButtons[i].ApplyCurrentTheme(true);
            }
        }
    }

    private void ApplyTextArray(TMP_Text[] texts, Color color)
    {
        if (texts == null)
        {
            return;
        }

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                texts[i].color = color;
            }
        }
    }

    private void CacheResolvedTargets()
    {
        if (cachedHostRect == null)
        {
            cachedHostRect = transform as RectTransform;
        }

        cachedContainerRect = ResolveContainerRect();
        cachedContainerImage = cachedContainerRect != null ? cachedContainerRect.GetComponent<Image>() : null;
        effectiveOverlayImage = ResolveOverlayImage();

        if (overlayImage != null && overlayImage != effectiveOverlayImage)
        {
            overlayImage.enabled = false;
        }
    }

    private RectTransform ResolveContainerRect()
    {
        RectTransform container = GetParentRect(frameImage);
        if (container != null)
        {
            return container;
        }

        container = GetParentRect(innerImage);
        if (container != null)
        {
            return container;
        }

        container = GetParentRect(titleText);
        if (container != null && container != cachedHostRect)
        {
            return container;
        }

        container = GetParentRect(progressTrack);
        if (container != null && container != cachedHostRect)
        {
            return container;
        }

        if (refreshButtons != null)
        {
            for (int i = 0; i < refreshButtons.Length; i++)
            {
                container = GetParentRect(refreshButtons[i]);
                if (container != null && container != cachedHostRect)
                {
                    return container;
                }
            }
        }

        return cachedHostRect;
    }

    private Image ResolveOverlayImage()
    {
        Image hostImage = GetComponent<Image>();
        bool hostLooksLikeOverlay = hostImage != null && hostImage.color.a > 0.001f;

        if (overlayImage != null)
        {
            if (overlayImage.gameObject == gameObject)
            {
                return overlayImage;
            }

            if (hostLooksLikeOverlay)
            {
                return hostImage;
            }

            return overlayImage;
        }

        return hostImage;
    }

    private Outline GetResolvedOutline()
    {
        if (frameOutline != null)
        {
            return frameOutline;
        }

        Graphic targetGraphic = frameImage != null ? frameImage : cachedContainerImage;
        if (targetGraphic == null)
        {
            return null;
        }

        Outline outline = targetGraphic.GetComponent<Outline>();
        if (outline != null)
        {
            return outline;
        }

        return targetGraphic.gameObject.AddComponent<Outline>();
    }

    private Shadow GetResolvedShadow()
    {
        if (frameShadow != null)
        {
            return frameShadow;
        }

        Graphic targetGraphic = frameImage != null ? frameImage : cachedContainerImage;
        if (targetGraphic == null)
        {
            return null;
        }

        Component[] components = targetGraphic.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].GetType() == typeof(Shadow))
            {
                return (Shadow)components[i];
            }
        }

        return targetGraphic.gameObject.AddComponent<Shadow>();
    }

    private ThemeManager.UIThemeColors GetThemeColors()
    {
        if (ThemeManager.I != null)
        {
            return ThemeManager.I.GetUIThemeColors();
        }

        return new ThemeManager.UIThemeColors
        {
            panelColor = new Color32(0xF2, 0xEC, 0xE2, 0xFF),
            panelInnerColor = new Color32(0xFB, 0xF7, 0xF1, 0xFF),
            panelOutlineColor = new Color32(0xB8, 0xAA, 0x9A, 0xFF),
            panelTitleColor = Color.black,
            panelTextColor = Color.black,
            overlayColor = new Color(0f, 0f, 0f, 0.26f)
        };
    }

    private ThemeManager.GoldButtonColors GetGoldColors()
    {
        if (ThemeManager.I != null)
        {
            return ThemeManager.I.GetGoldButtonColors(ThemeManager.GoldButtonRole.HudBottomBar, true);
        }

        return new ThemeManager.GoldButtonColors
        {
            face = new Color32(0xF2, 0xC6, 0x57, 0xFF),
            shadow = new Color32(0x8D, 0x52, 0x12, 0xFF),
            outline = new Color32(0xFF, 0xE0, 0x91, 0xFF),
            content = new Color32(0x22, 0x15, 0x06, 0xFF)
        };
    }

    private static RectTransform GetParentRect(Component component)
    {
        if (component == null)
        {
            return null;
        }

        RectTransform rect = component.transform as RectTransform;
        if (rect == null)
        {
            return null;
        }

        return rect.parent as RectTransform;
    }

    private static void StretchRect(RectTransform rect, float inset)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void EnsureIgnoredByLayout(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
    }

    private static Color ForceOpaque(Color color)
    {
        color.a = 1f;
        return color;
    }

    private void RestartRefreshRoutine()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }

        refreshRoutine = StartCoroutine(RefreshLayoutRoutine());
    }

    private IEnumerator RefreshLayoutRoutine()
    {
        for (int i = 0; i < 4; i++)
        {
            yield return null;
            MarkLayoutDirty();
        }

        refreshRoutine = null;
    }

    private void MarkLayoutDirty()
    {
        layoutDirty = true;
    }
}
