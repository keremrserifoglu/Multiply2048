using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    private const string PP_SFX = "SFX_ENABLED";

    [Header("Refs")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button overlayButton;
    [SerializeField] private Toggle sfxToggle;

    private void Awake()
    {
        bool sfxEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;

        if (sfxToggle != null)
        {
            // Prevent firing event while we set initial value
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

        ApplySfxSetting(sfxEnabled);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(true);

        // Ensure settings is rendered above other panels in the same Canvas
        settingsPanel.transform.SetAsLastSibling();
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
}