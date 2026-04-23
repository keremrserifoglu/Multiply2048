using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    private const string PP_SFX = "SFX_ENABLED";
    private const string PP_HINTS = BoardController.PP_HINTS;
    private const string PP_THEME_SELECTION = "SETTINGS_THEME_SELECTION";

    private static readonly Color GoldNormalFill = new Color32(0xD7, 0x9B, 0x2A, 0xFF);
    private static readonly Color GoldSelectedFill = new Color32(0xF2, 0xC6, 0x57, 0xFF);
    private static readonly Color GoldNormalBorder = new Color32(0xFF, 0xE0, 0x91, 0xFF);
    private static readonly Color GoldSelectedBorder = new Color32(0xFF, 0xF0, 0xBA, 0xFF);
    private static readonly Color GoldNormalShadow = new Color32(0x7A, 0x46, 0x10, 0xD8);
    private static readonly Color GoldSelectedShadow = new Color32(0x8D, 0x52, 0x12, 0xE4);
    private static readonly Color GoldContentColor = new Color32(0x22, 0x15, 0x06, 0xFF);

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

    [Header("Hints")]
    [SerializeField] private Button hintsStateButton;

    [Header("Theme Selection")]
    [SerializeField] private Button darkThemeButton;
    [SerializeField] private Button colorfulThemeButton;
    [SerializeField] private Button lightThemeButton;
    [SerializeField] private Image darkThemeBox;
    [SerializeField] private Image colorfulThemeBox;
    [SerializeField] private Image lightThemeBox;

    private ThemeSelection currentThemeSelection = ThemeSelection.None;
    private ThemeSelection AllThemes => ThemeSelection.Dark | ThemeSelection.Colorful | ThemeSelection.Light;

    private void Awake()
    {
        bool sfxEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;
        bool hintsEnabled = PlayerPrefs.GetInt(PP_HINTS, 1) == 1;

        currentThemeSelection = SanitizeThemeSelection(
            (ThemeSelection)PlayerPrefs.GetInt(PP_THEME_SELECTION, (int)ThemeSelection.None)
        );

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

        if (hintsStateButton != null)
        {
            hintsStateButton.onClick.RemoveListener(OnHintsStateButtonPressed);
            hintsStateButton.onClick.AddListener(OnHintsStateButtonPressed);
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
        ApplyHintsSetting(hintsEnabled);
        ApplyAllSelectionVisuals();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
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

    private void ApplyAllSelectionVisuals()
    {
        ApplyThemeSelectionVisuals();
        ApplySfxVisuals(GetPersistedSfxEnabled());
    }

    private bool GetPersistedSfxEnabled()
    {
        return PlayerPrefs.GetInt(PP_SFX, 1) == 1;
    }

    private bool GetPersistedHintsEnabled()
    {
        return PlayerPrefs.GetInt(PP_HINTS, 1) == 1;
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

    private void OnHintsStateButtonPressed()
    {
        bool nextValue = !GetPersistedHintsEnabled();
        SetHintsEnabledState(nextValue, true);
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

    private void SetHintsEnabledState(bool enabled, bool save)
    {
        if (save)
        {
            PlayerPrefs.SetInt(PP_HINTS, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        ApplyHintsSetting(enabled);
    }

    private void ApplySfxSetting(bool enabled)
    {
        if (AudioManager.I != null)
            AudioManager.I.SetSfxEnabled(enabled);
    }

    private void ApplyHintsSetting(bool enabled)
    {
        BoardController board = null;

        if (GameManager.I != null)
            board = GameManager.I.board;

        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        if (board != null)
            board.SetIdleHintsEnabled(enabled);
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

        if (boxImage != null)
            boxImage.type = boxImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        Image targetImage = button.targetGraphic as Image;

        if (targetImage != null && targetImage != boxImage)
            targetImage.type = targetImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        GameObject outlineTarget = boxImage != null ? boxImage.gameObject : button.gameObject;
        Outline outline = outlineTarget.GetComponent<Outline>();

        if (outline != null)
            outline.enabled = isSelected;
    }

    private ThemeManager.GoldButtonColors GetSelectionGoldButtonColors(bool isSelected)
    {
        if (ThemeManager.I != null)
            return ThemeManager.I.GetGoldButtonColors(ThemeManager.GoldButtonRole.SettingsSelection, isSelected);

        return new ThemeManager.GoldButtonColors
        {
            face = isSelected ? GoldSelectedFill : GoldNormalFill,
            outline = isSelected ? GoldSelectedBorder : GoldNormalBorder,
            shadow = isSelected ? GoldSelectedShadow : GoldNormalShadow,
            content = GoldContentColor
        };
    }

    private void SetSelectionButtonColors(Button button)
    {
        // Preserve inspector-configured Button ColorBlock values.
    }

    private void TintSelectionButtonContent(Button button, Color color)
    {
        // Preserve inspector-assigned content colors.
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
}