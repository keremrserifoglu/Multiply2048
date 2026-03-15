using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PowerCandy/Tile Palette Database", fileName = "TilePaletteDatabase")]
public class TilePaletteDatabase : ScriptableObject
{
    public enum ThemeFamily
    {
        Unspecified = 0,
        Dark = 1,
        Colorful = 2,
        Light = 3
    }

    [Serializable]
    public class Palette
    {
        public bool forceWhiteText = false;
        public string name;

        [Tooltip("Optional manual category override. Leave Unspecified to use automatic detection.")]
        public ThemeFamily family = ThemeFamily.Unspecified;

        [Tooltip("2,4,8,16,... in order. index 0 => 2, index 1 => 4, ...")]
        public List<Color> tileColors = new List<Color>();

        [Tooltip("Legacy / fallback tint (can also be used as background if backgroundColor is not set).")]
        public Color boardTint = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("Background")]
        [Tooltip("UI background color for this palette.")]
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        [Tooltip("Text color for light tiles")]
        public Color textDark = Color.black;

        [Tooltip("Text color for dark tiles")]
        public Color textLight = Color.white;

        [Header("UI - Panels")]
        [Tooltip("Optional override. Alpha 0 means ThemeManager fallback.")]
        public Color uiPanelColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional override. Alpha 0 means ThemeManager fallback.")]
        public Color uiPanelInnerColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional override. Alpha 0 means ThemeManager fallback.")]
        public Color uiPanelOutlineColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional override. Alpha 0 means ThemeManager fallback.")]
        public Color uiPanelTitleColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional override. Alpha 0 means ThemeManager fallback.")]
        public Color uiPanelTextColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional override. Alpha 0 means ThemeManager fallback.")]
        public Color uiOverlayColor = new Color(0f, 0f, 0f, 0f);

        [Header("UI - Main Buttons")]
        [Tooltip("Shared face color for regular buttons. Alpha 0 means ThemeManager fallback.")]
        public Color uiButtonFaceColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Shared shadow color for regular buttons. Alpha 0 means ThemeManager fallback.")]
        public Color uiButtonShadowColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Shared outline color for regular buttons. Alpha 0 means ThemeManager fallback.")]
        public Color uiButtonOutlineColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Shared text/icon color for regular buttons. Alpha 0 means ThemeManager fallback.")]
        public Color uiButtonTextColor = new Color(0f, 0f, 0f, 0f);

        [Header("UI - Selection Buttons")]
        [Tooltip("Theme buttons and Sound Effects On/Off button fill when inactive. Alpha 0 means ThemeManager fallback.")]
        public Color uiSelectionNormalColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Theme buttons and Sound Effects On/Off button fill when active. Alpha 0 means ThemeManager fallback.")]
        public Color uiSelectionSelectedColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Theme buttons and Sound Effects On/Off button border when inactive. Alpha 0 means ThemeManager fallback.")]
        public Color uiSelectionBorderNormalColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Theme buttons and Sound Effects On/Off button border when active. Alpha 0 means ThemeManager fallback.")]
        public Color uiSelectionBorderSelectedColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Theme buttons and Sound Effects On/Off text color. Alpha 0 means ThemeManager fallback.")]
        public Color uiSelectionTextColor = new Color(0f, 0f, 0f, 0f);
    }

    public List<Palette> palettes = new List<Palette>();

    public Palette GetPalette(int index)
    {
        if (palettes == null || palettes.Count == 0)
            return null;

        index = Mathf.Clamp(index, 0, palettes.Count - 1);
        return palettes[index];
    }
}
