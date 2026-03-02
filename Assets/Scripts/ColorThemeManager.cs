using System.Collections.Generic;
using UnityEngine;

public class ColorThemeManager : MonoBehaviour
{
    [System.Serializable]
    public class ColorGroup
    {
        public List<Color> palette = new List<Color>();
    }

    [Header("Base Palette (<=512)")]
    public List<Color> basePalette = new List<Color>()
    {
        Color.white, Color.yellow, Color.green, Color.cyan, Color.blue,
        new Color(0.6f,0f,1f), Color.red, new Color(1f,0.5f,0f), Color.magenta
    };

    [Header("Special Groups (>512)")]
    public List<ColorGroup> specialGroups = new List<ColorGroup>();

    private int activeGroup = 0;

    public Color GetColorForValue(int value)
    {
        int p = Mathf.RoundToInt(Mathf.Log(Mathf.Max(2, value), 2f)); // 2->1, 4->2...
        int baseIdx = Mathf.Clamp(p - 1, 0, basePalette.Count - 1);

        if (value <= 512) return basePalette[baseIdx];

        if (specialGroups == null || specialGroups.Count == 0)
            return basePalette[basePalette.Count - 1];

        var g = specialGroups[Mathf.Clamp(activeGroup, 0, specialGroups.Count - 1)];
        if (g.palette == null || g.palette.Count == 0)
            return basePalette[basePalette.Count - 1];

        int idx = Mathf.Abs(p) % g.palette.Count;
        return g.palette[idx];
    }

    // Name kept for compatibility; now switches instantly.
    public void ShiftThemeSmooth()
    {
        // Only allow theme shift when there is a 2048+ tile on the board
        int maxValue = 0;
        var tilesCheck = FindObjectsByType<CandyTile>(FindObjectsSortMode.None);
        for (int i = 0; i < tilesCheck.Length; i++)
        {
            if (tilesCheck[i] == null) continue;
            maxValue = Mathf.Max(maxValue, tilesCheck[i].Value);
        }

        if (maxValue < 2048)
            return;

        if (specialGroups == null || specialGroups.Count < 2) return;

        int next = UnityEngine.Random.Range(0, specialGroups.Count);
        if (next == activeGroup) next = (next + 1) % specialGroups.Count;

        activeGroup = next;

        var tiles = FindObjectsByType<CandyTile>(FindObjectsSortMode.None);
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null) tiles[i].RefreshColor();
        }
    }
}
