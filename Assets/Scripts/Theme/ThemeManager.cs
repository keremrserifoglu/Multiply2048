using System;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager I { get; private set; }
    [Header("Database")]
    public TilePaletteDatabase db;

    [Header("State")]
    [SerializeField] private int currentPaletteIndex = 0;

    public event Action OnPaletteChanged;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public int CurrentPaletteIndex => currentPaletteIndex;

    public TilePaletteDatabase.Palette CurrentPalette
        => db != null ? db.GetPalette(currentPaletteIndex) : null;

    public void NextPalette()
    {
        if (db == null || db.palettes == null || db.palettes.Count == 0) return;

        currentPaletteIndex = (currentPaletteIndex + 1) % db.palettes.Count;
        OnPaletteChanged?.Invoke();

        // Ensure existing tiles update their colors immediately
        RefreshAllTiles();
    }
    

    private int lastTriggerFrame = -1;

    public void NotifyValueCreated(int value)
    {
        if (value < 2048) return;

        // Avoid multiple palette changes in the same frame
        if (Time.frameCount == lastTriggerFrame) return;
        lastTriggerFrame = Time.frameCount;

        NextPalette();
    }

    public Color GetBackgroundColor()
    {
        var p = CurrentPalette;
        if (p == null) return Color.black;

        // Prefer backgroundColor
        Color c = p.backgroundColor;

        // Existing assets may have default alpha 0 after adding a new field
        if (c.a <= 0.001f)
            c = p.boardTint;

        c.a = 1f;
        return c;
    }

    // 2 -> 0, 4 -> 1, 8 -> 2...
    public static int PowerIndex(int value)
    {
        int idx = 0;
        int v = value;
        while (v > 2) { v >>= 1; idx++; }
        return idx;
    }

    public Color GetTileColor(int value)
    {
        var p = CurrentPalette;
        if (p == null || p.tileColors == null || p.tileColors.Count == 0)
            return Color.white;

        int idx = PowerIndex(value);
        idx = Mathf.Clamp(idx, 0, p.tileColors.Count - 1);

        Color c = p.tileColors[idx];
        c.a = 1f;
        return c;
    }

    public Color GetTextColorForTile(Color tileColor)
    {
        float l = 0.2126f * tileColor.r + 0.7152f * tileColor.g + 0.0722f * tileColor.b;
        var p = CurrentPalette;

        if (p == null) return (l > 0.6f) ? Color.black : Color.white;

        Color c = (l > 0.6f) ? p.textDark : p.textLight;
        c.a = 1f;
        return c;
    }
    public void RefreshAllTiles()
    {
        var tiles = UnityEngine.Object.FindObjectsByType<CandyTile>(
    FindObjectsInactive.Include,
    FindObjectsSortMode.None);
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null) tiles[i].RefreshColor();
        }
    }
    public void ResetTheme()
    {
        currentPaletteIndex = 0;
        lastTriggerFrame = -1;
        OnPaletteChanged?.Invoke();
        RefreshAllTiles();
    }
}
