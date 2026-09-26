using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    private const string PP_SFX = "SFX_ENABLED";
    private const string PP_HINTS = BoardController.PP_HINTS;
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
    [SerializeField] private TMP_Text hintsStateLabel;

    private void Awake()
    {
        bool sfxEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;
        bool hintsEnabled = PlayerPrefs.GetInt(PP_HINTS, 1) == 1;

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

        ApplySfxSetting(sfxEnabled);
        ApplyHintsSetting(hintsEnabled);
        ApplyHintsVisuals(hintsEnabled);
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
        ApplyHintsVisuals(enabled);
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

    private void ApplySfxVisuals(bool enabled)
    {
        if (sfxStateLabel != null)
            sfxStateLabel.text = enabled ? "On" : "Off";

        ApplySelectionButtonVisual(sfxStateButton, sfxStateBox, enabled);
    }

    private void ApplyHintsVisuals(bool enabled)
    {
        if (hintsStateLabel != null)
            hintsStateLabel.text = enabled ? "On" : "Off";

        ApplySelectionButtonVisual(hintsStateButton, null, enabled);
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

}
