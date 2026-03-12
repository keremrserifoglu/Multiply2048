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

    [Header("Theme Box Colors")]
    [SerializeField] private Color boxNormalColor = new Color(0.93f, 0.95f, 1f, 1f);
    [SerializeField] private Color boxSelectedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color borderNormalColor = new Color(0.76f, 0.82f, 0.92f, 1f);
    [SerializeField] private Color borderSelectedColor = new Color(0.19f, 0.33f, 0.72f, 1f);

    private ThemeSelection currentThemeSelection = ThemeSelection.Colorful;

    private ThemeSelection AllThemes =>
        ThemeSelection.Dark | ThemeSelection.Colorful | ThemeSelection.Light;

    private void Awake()
    {
        bool sfxEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;
        currentThemeSelection = (ThemeSelection)PlayerPrefs.GetInt(PP_THEME_SELECTION, (int)ThemeSelection.Colorful);

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
        if (HasThemeVisual(selection))
            currentThemeSelection &= ~selection;
        else
            currentThemeSelection |= selection;

        PlayerPrefs.SetInt(PP_THEME_SELECTION, (int)currentThemeSelection);
        PlayerPrefs.Save();

        ApplyThemeSelectionVisuals();
    }

    private bool HasThemeVisual(ThemeSelection selection)
    {
        return (currentThemeSelection & selection) != 0;
    }

    private ThemeSelection GetEffectiveThemeSelection()
    {
        return currentThemeSelection == ThemeSelection.None
            ? AllThemes
            : currentThemeSelection;
    }

    private bool HasThemeEffective(ThemeSelection selection)
    {
        return (GetEffectiveThemeSelection() & selection) != 0;
    }

    private void ApplyThemeSelectionVisuals()
    {
        ApplyThemeBoxVisual(darkThemeButton, darkThemeBox, HasThemeVisual(ThemeSelection.Dark));
        ApplyThemeBoxVisual(colorfulThemeButton, colorfulThemeBox, HasThemeVisual(ThemeSelection.Colorful));
        ApplyThemeBoxVisual(lightThemeButton, lightThemeBox, HasThemeVisual(ThemeSelection.Light));
    }

    private void ApplyThemeBoxVisual(Button button, Image explicitBoxImage, bool isSelected)
    {
        Image boxImage = explicitBoxImage;

        if (boxImage == null && button != null)
            boxImage = button.GetComponent<Image>();

        if (boxImage != null)
            boxImage.color = isSelected ? boxSelectedColor : boxNormalColor;

        if (button != null)
        {
            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = isSelected ? borderSelectedColor : borderNormalColor;
                outline.effectDistance = isSelected ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
            }
        }
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