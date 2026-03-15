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

        [Header("UI")]
        [Tooltip("Optional panel base color override. Alpha 0 means ThemeManager fallback.")]
        public Color uiPanelColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional regular button face color override. Alpha 0 means ThemeManager fallback.")]
        public Color uiButtonFaceColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Optional active selection border color override. Alpha 0 means ThemeManager fallback.")]
        public Color uiSelectionBorderSelectedColor = new Color(0f, 0f, 0f, 0f);
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
