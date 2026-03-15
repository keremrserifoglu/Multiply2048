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
    [SerializeField] private Color boxNormalColor = Color.white;
    [SerializeField] private Color boxSelectedColor = Color.white;
    [SerializeField] private Color borderNormalColor = new Color(0.82f, 0.86f, 0.93f, 1f);
    [SerializeField] private Color borderSelectedColor = new Color(0.23f, 0.49f, 0.96f, 1f);
    [SerializeField] private Color labelNormalColor = new Color(0.18f, 0.22f, 0.28f, 1f);

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

        Color fill = isSelected ? boxSelectedColor : boxNormalColor;
        Color border = isSelected ? borderSelectedColor : borderNormalColor;

        if (boxImage != null)
        {
            boxImage.color = fill;
            boxImage.type = boxImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Outline boxOutline = boxImage.GetComponent<Outline>();
            if (boxOutline == null)
                boxOutline = boxImage.gameObject.AddComponent<Outline>();

            boxOutline.effectColor = border;
            boxOutline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            boxOutline.useGraphicAlpha = true;

            Shadow boxShadow = GetOrAddExactShadow(boxImage.gameObject);
            boxShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            boxShadow.effectDistance = new Vector2(0f, -1f);
            boxShadow.useGraphicAlpha = true;
        }

        if (button != null)
        {
            Image rootImage = button.GetComponent<Image>();
            if (rootImage != null && rootImage != boxImage)
                rootImage.color = Color.white;

            if (button.gameObject != (boxImage != null ? boxImage.gameObject : null))
            {
                RemoveComponentIfExists<Outline>(button.gameObject);
                RemoveComponentIfExists<Shadow>(button.gameObject);
            }

            ColorBlock colors = button.colors;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.98f, 0.98f, 0.98f, 1f);
            colors.pressedColor = new Color(0.90f, 0.94f, 1f, 1f);
            colors.selectedColor = new Color(0.96f, 0.98f, 1f, 1f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.65f);
            button.colors = colors;

            TintButtonContent(button, labelNormalColor, boxImage);
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

    private void TintButtonContent(Button button, Color color, Image imageToSkip)
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
            if (images[i] == null || images[i] == buttonImage || images[i] == imageToSkip)
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

    private void RemoveComponentIfExists<T>(GameObject target) where T : Component
    {
        if (target == null)
            return;

        T component = target.GetComponent<T>();
        if (component == null)
            return;

        if (Application.isPlaying)
            Destroy(component);
        else
            DestroyImmediate(component);
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
