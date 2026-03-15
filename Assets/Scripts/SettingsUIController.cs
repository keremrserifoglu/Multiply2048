using TMPro;
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

    [Header("Sound Effects")]
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Button sfxStateButton;
    [SerializeField] private TMP_Text sfxStateLabel;
    [SerializeField] private Image sfxStateBox;

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
    [SerializeField] private Color selectionTextColor = new Color32(0x58, 0x5D, 0x66, 0xFF);

    private ThemeSelection currentThemeSelection = ThemeSelection.None;

    private ThemeSelection AllThemes => ThemeSelection.Dark | ThemeSelection.Colorful | ThemeSelection.Light;

    private void Awake()
    {
        bool sfxEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;
        currentThemeSelection = SanitizeThemeSelection((ThemeSelection)PlayerPrefs.GetInt(PP_THEME_SELECTION, (int)ThemeSelection.None));

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
            sfxToggle.isOn = sfxEnabled;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }

        if (sfxStateButton != null)
        {
            sfxStateButton.onClick.RemoveListener(OnSfxStateButtonPressed);
            sfxStateButton.onClick.AddListener(OnSfxStateButtonPressed);
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
        ApplyAllSelectionVisuals();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        ApplyAllSelectionVisuals();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void HandlePaletteChanged()
    {
        ApplyAllSelectionVisuals();
    }

    private void ApplyAllSelectionVisuals()
    {
        ApplyThemeSelectionVisuals();
        ApplySfxVisuals(GetPersistedSfxEnabled());
    }

    private bool GetPersistedSfxEnabled()
    {
        return PlayerPrefs.GetInt(PP_SFX, 1) == 1;
    }

    private void OnSfxToggleChanged(bool isOn)
    {
        SetSfxEnabledState(isOn, true);
    }

    private void OnSfxStateButtonPressed()
    {
        bool nextValue = !GetPersistedSfxEnabled();
        SetSfxEnabledState(nextValue, true);
    }

    private void SetSfxEnabledState(bool enabled, bool save)
    {
        if (save)
        {
            PlayerPrefs.SetInt(PP_SFX, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (sfxToggle != null && sfxToggle.isOn != enabled)
            sfxToggle.isOn = enabled;

        ApplySfxSetting(enabled);
        ApplySfxVisuals(enabled);
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
        ApplySelectionButtonVisual(darkThemeButton, darkThemeBox, HasThemeEffective(ThemeSelection.Dark));
        ApplySelectionButtonVisual(colorfulThemeButton, colorfulThemeBox, HasThemeEffective(ThemeSelection.Colorful));
        ApplySelectionButtonVisual(lightThemeButton, lightThemeBox, HasThemeEffective(ThemeSelection.Light));
    }

    private void ApplySfxVisuals(bool enabled)
    {
        if (sfxStateLabel != null)
            sfxStateLabel.text = enabled ? "On" : "Off";

        ApplySelectionButtonVisual(sfxStateButton, sfxStateBox, enabled);
    }

    private void ApplySelectionButtonVisual(Button button, Image explicitBoxImage, bool isSelected)
    {
        if (button == null)
            return;

        Image boxImage = explicitBoxImage != null ? explicitBoxImage : button.GetComponent<Image>();
        ThemeManager.UIThemeColors ui = ThemeManager.I != null ? ThemeManager.I.GetUIThemeColors() : default;

        Color normalFill = ThemeManager.I != null ? ui.selectionNormalColor : boxNormalColor;
        Color selectedFill = ThemeManager.I != null ? ui.selectionSelectedColor : boxSelectedColor;
        Color normalBorder = ThemeManager.I != null ? ui.selectionBorderNormalColor : borderNormalColor;
        Color selectedBorder = ThemeManager.I != null ? ui.selectionBorderSelectedColor : borderSelectedColor;
        Color contentColor = ThemeManager.I != null ? ui.selectionTextColor : selectionTextColor;

        Color fill = isSelected ? selectedFill : normalFill;
        Color border = isSelected ? selectedBorder : normalBorder;

        if (boxImage != null)
            boxImage.color = fill;

        Image targetImage = button.targetGraphic as Image;
        if (targetImage != null && targetImage != boxImage)
            targetImage.color = fill;

        Outline outline = GetOrAddOutline(button.gameObject);
        outline.effectColor = border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
        outline.enabled = true;

        SetSelectionButtonColors(button);
        TintSelectionButtonContent(button, contentColor);
    }

    private void SetSelectionButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.97f, 0.97f, 0.97f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.65f);
        button.colors = colors;
    }

    private void TintSelectionButtonContent(Button button, Color color)
    {
        TMP_Text[] tmpTexts = button.GetComponentsInChildren<TMP_Text>(true);
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
    }

    private Outline GetOrAddOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline != null)
            return outline;

        return target.AddComponent<Outline>();
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
