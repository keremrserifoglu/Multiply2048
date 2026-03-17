using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBackgroundController : MonoBehaviour
{
    private static readonly string[] PanelNames =
    {
        "Window", "Card", "Dialog", "ThemeSection", "ThemeRow", "BottomBar", "TitleArea", "ScoresArea"
    };

    [Header("Root Backgrounds")]
    [SerializeField] private Image mainMenuBackground;
    [SerializeField] private Image hudBackground;
    [SerializeField] private Image gameOverBackground;

    [Header("Root Background Alpha")]
    [SerializeField] private float mainMenuAlpha = 0f;
    [SerializeField] private float hudAlpha = 0f;
    [SerializeField] private float gameOverAlpha = 0.40f;

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
        ApplyCanvasTextStyles(ui.panelTextColor);
    }

    private void MakePanelsTransparent()
    {
        SetImageAlpha(mainMenuBackground, 0f);
        SetImageAlpha(hudBackground, 0f);
        SetImageAlpha(gameOverBackground, 0f);
    }

    private void ApplyRootBackground(Image target, Color color, float alpha)
    {
        if (target == null)
            return;

        color.a = Mathf.Clamp01(alpha);
        target.color = color;
    }

    private void SetImageAlpha(Image target, float alpha)
    {
        if (target == null)
            return;

        Color c = target.color;
        c.a = Mathf.Clamp01(alpha);
        target.color = c;
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

    private void ApplyCanvasTextStyles(Color textColor)
    {
        List<TMP_Text> tmpTexts = FindSceneComponents<TMP_Text>();
        for (int i = 0; i < tmpTexts.Count; i++)
        {
            TMP_Text text = tmpTexts[i];
            if (text == null)
                continue;

            if (text.GetComponentInParent<Button>() != null)
                continue;

            text.color = textColor;
        }

        List<Text> legacyTexts = FindSceneComponents<Text>();
        for (int i = 0; i < legacyTexts.Count; i++)
        {
            Text text = legacyTexts[i];
            if (text == null)
                continue;

            if (text.GetComponentInParent<Button>() != null)
                continue;

            text.color = textColor;
        }
    }

    private bool IsInnerPanel(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("section")
               || lowerName.Contains("row")
               || lowerName.Contains("bar")
               || lowerName.Contains("title")
               || lowerName.Contains("scores");
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