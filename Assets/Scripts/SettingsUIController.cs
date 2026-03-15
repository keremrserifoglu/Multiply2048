using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    private const string PP_SFX = "SFX_ENABLED";
    private const string PP_THEME_SELECTION = "SETTINGS_THEME_SELECTION";

    [System.Flags]
    private enum ThemeSelection
    {
        None = 0,
        Dark = 1,
        Colorful = 2,
        Light = 4
    }

    [Header("Refs")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button overlayButton;
    [SerializeField] private Toggle sfxToggle;

    [Header("Theme Selection")]
    [SerializeField] private Button darkThemeButton;
    [SerializeField] private Button colorfulThemeButton;
    [SerializeField] private Button lightThemeButton;

    [SerializeField] private Image darkThemeBox;
    [SerializeField] private Image colorfulThemeBox;
    [SerializeField] private Image lightThemeBox;

    [Header("Fallback Colors")]
    [SerializeField] private Color boxNormalColor = new Color(0.93f, 0.95f, 1f, 1f);
    [SerializeField] private Color boxSelectedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color borderNormalColor = new Color(0.76f, 0.82f, 0.92f, 1f);
    [SerializeField] private Color borderSelectedColor = new Color(0.19f, 0.33f, 0.72f, 1f);

    private ThemeSelection currentThemeSelection = ThemeSelection.None;

    private ThemeSelection AllThemes => ThemeSelection.Dark | ThemeSelection.Colorful | ThemeSelection.Light;

    private void Awake()
    {
        bool sfxEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;
        currentThemeSelection = SanitizeThemeSelection(
            (ThemeSelection)PlayerPrefs.GetInt(PP_THEME_SELECTION, (int)ThemeSelection.None));

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
            sfxToggle.isOn = sfxEnabled;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);

        if (overlayButton != null)
            overlayButton.onClick.AddListener(CloseSettings);

        RegisterThemeButton(darkThemeButton, ThemeSelection.Dark);
        RegisterThemeButton(colorfulThemeButton, ThemeSelection.Colorful);
        RegisterThemeButton(lightThemeButton, ThemeSelection.Light);

        ApplySfxSetting(sfxEnabled);
        ApplyThemeSelectionVisuals();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += ApplyThemeSelectionVisuals;
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= ApplyThemeSelectionVisuals;
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        ApplyThemeSelectionVisuals();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnSfxToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(PP_SFX, isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxSetting(isOn);
    }

    private void ApplySfxSetting(bool enabled)
    {
        if (AudioManager.I != null)
            AudioManager.I.SetSfxEnabled(enabled);
    }

    private void RegisterThemeButton(Button button, ThemeSelection selection)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnThemeButtonPressed(selection));
    }

    private void OnThemeButtonPressed(ThemeSelection selection)
    {
        ThemeSelection effectiveSelection = GetEffectiveThemeSelection();
        ThemeSelection nextSelection;

        if ((effectiveSelection & selection) != 0)
            nextSelection = effectiveSelection & ~selection;
        else
            nextSelection = effectiveSelection | selection;

        currentThemeSelection = NormalizeStoredSelection(nextSelection);

        PlayerPrefs.SetInt(PP_THEME_SELECTION, (int)currentThemeSelection);
        PlayerPrefs.Save();

        ApplyThemeSelectionVisuals();
        ThemeManager.I?.OnSettingsChanged();
    }

    private ThemeSelection SanitizeThemeSelection(ThemeSelection selection)
    {
        return selection & AllThemes;
    }

    private ThemeSelection NormalizeStoredSelection(ThemeSelection selection)
    {
        selection = SanitizeThemeSelection(selection);
        return selection == AllThemes ? ThemeSelection.None : selection;
    }

    private ThemeSelection GetEffectiveThemeSelection()
    {
        ThemeSelection sanitized = SanitizeThemeSelection(currentThemeSelection);
        return sanitized == ThemeSelection.None ? AllThemes : sanitized;
    }

    private bool HasThemeEffective(ThemeSelection selection)
    {
        return (GetEffectiveThemeSelection() & selection) != 0;
    }

    private void ApplyThemeSelectionVisuals()
    {
        ApplyThemeBoxVisual(darkThemeButton, darkThemeBox, HasThemeEffective(ThemeSelection.Dark), ThemeSelection.Dark);
        ApplyThemeBoxVisual(colorfulThemeButton, colorfulThemeBox, HasThemeEffective(ThemeSelection.Colorful), ThemeSelection.Colorful);
        ApplyThemeBoxVisual(lightThemeButton, lightThemeBox, HasThemeEffective(ThemeSelection.Light), ThemeSelection.Light);
    }

    private void ApplyThemeBoxVisual(Button button, Image explicitBoxImage, bool isSelected, ThemeSelection selection)
    {
        Image boxImage = explicitBoxImage;

        if (boxImage == null && button != null)
            boxImage = button.GetComponent<Image>();

        Color previewColor = GetPreviewColor(selection);
        Color normalFill = boxNormalColor;
        Color selectedFill = boxSelectedColor;
        Color normalBorder = borderNormalColor;
        Color selectedBorder = borderSelectedColor;
        Color contentColor = Color.white;

        if (ThemeManager.I != null)
        {
            ThemeManager.UIThemeColors ui = ThemeManager.I.GetUIThemeColors();
            normalFill = Color.Lerp(ui.selectionNormalColor, previewColor, 0.42f);
            selectedFill = Color.Lerp(ui.selectionSelectedColor, previewColor, 0.72f);
            normalBorder = ui.selectionBorderNormalColor;
            selectedBorder = ui.selectionBorderSelectedColor;
            contentColor = GetReadableTextColor(isSelected ? selectedFill : normalFill, ui.panelTitleColor, ui.buttonTextColor);
        }

        if (boxImage != null)
            boxImage.color = isSelected ? selectedFill : normalFill;

        if (button != null)
        {
            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = button.gameObject.AddComponent<Outline>();

            outline.effectColor = isSelected ? selectedBorder : normalBorder;
            outline.effectDistance = isSelected ? new Vector2(3f, -6f) : new Vector2(2f, -4f);
            outline.useGraphicAlpha = true;

            Shadow shadow = GetOrAddExactShadow(button.gameObject);

            shadow.effectColor = isSelected ? selectedBorder : normalBorder;
            shadow.effectDistance = isSelected ? new Vector2(0f, -8f) : new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;

            TintButtonContent(button, contentColor);
        }
    }

    private Color GetPreviewColor(ThemeSelection selection)
    {
        switch (selection)
        {
            case ThemeSelection.Dark:
                return new Color32(0x53, 0x56, 0x66, 0xFF);
            case ThemeSelection.Light:
                return new Color32(0xF1, 0xE8, 0xD8, 0xFF);
            default:
                return new Color32(0x63, 0xC8, 0xFF, 0xFF);
        }
    }

    private Color GetReadableTextColor(Color background, Color darkColor, Color lightColor)
    {
        float luma = (0.2126f * background.r) + (0.7152f * background.g) + (0.0722f * background.b);
        return luma > 0.72f ? darkColor : lightColor;
    }

    private void TintButtonContent(Button button, Color color)
    {
        if (button == null)
            return;

        TMPro.TMP_Text[] tmpTexts = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
        for (int i = 0; i < tmpTexts.Length; i++)
        {
            if (tmpTexts[i] != null)
                tmpTexts[i].color = color;
        }

        Text[] texts = button.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].color = color;
        }

        Image[] images = button.GetComponentsInChildren<Image>(true);
        Image buttonImage = button.GetComponent<Image>();

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i] == buttonImage)
                continue;

            images[i].color = color;
        }
    }


    private Shadow GetOrAddExactShadow(GameObject target)
    {
        Component[] components = target.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].GetType() == typeof(Shadow))
                return (Shadow)components[i];
        }

        return target.AddComponent<Shadow>();
    }

    public bool IsDarkThemeSelected()
    {
        return HasThemeEffective(ThemeSelection.Dark);
    }

    public bool IsColorfulThemeSelected()
    {
        return HasThemeEffective(ThemeSelection.Colorful);
    }

    public bool IsLightThemeSelected()
    {
        return HasThemeEffective(ThemeSelection.Light);
    }

    public int GetSelectedThemeMask()
    {
        return (int)GetEffectiveThemeSelection();
    }

    public int GetVisualThemeMask()
    {
        return (int)currentThemeSelection;
    }
}
