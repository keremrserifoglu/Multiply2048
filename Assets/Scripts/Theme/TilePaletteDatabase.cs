
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PowerCandy/Tile Palette Database", fileName = "TilePaletteDatabase")]
public class TilePaletteDatabase : ScriptableObject
{
    [Serializable]
    public class Palette
    {
        public string name;

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
    }

    public List<Palette> palettes = new List<Palette>();

    public Palette GetPalette(int index)
    {
        if (palettes == null || palettes.Count == 0) return null;
        index = Mathf.Clamp(index, 0, palettes.Count - 1);
        return palettes[index];
    }
}
