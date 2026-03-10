using System;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager I;

    [SerializeField] private TilePaletteDatabase paletteDatabase;

    private int currentPaletteIndex = 0;
    private bool paletteChangedAfter2048ThisRun = false;

    public event Action OnPaletteChanged;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (paletteDatabase == null)
            paletteDatabase = Resources.Load<TilePaletteDatabase>("TilePaletteDatabase");
    }

    private void Start()
    {
        if (paletteDatabase != null && paletteDatabase.palettes != null && paletteDatabase.palettes.Count > 0)
            ResetTheme();
    }

    public void OnGameStart()
    {
        ResetTheme();
    }

    public void OnGameRestart()
    {
        ResetTheme();
    }

    public void OnSettingsChanged()
    {
        ResetTheme();
    }

    public void ResetTheme()
    {
        paletteChangedAfter2048ThisRun = false;
        ApplyRandomPalette();
        RefreshAllTiles();
    }

    private void ApplyRandomPalette()
    {
        if (paletteDatabase == null || paletteDatabase.palettes == null || paletteDatabase.palettes.Count == 0)
            return;

        if (paletteDatabase.palettes.Count == 1)
        {
            currentPaletteIndex = 0;
        }
        else
        {
            int newIndex = UnityEngine.Random.Range(0, paletteDatabase.palettes.Count);

            if (newIndex == currentPaletteIndex)
                newIndex = (newIndex + 1) % paletteDatabase.palettes.Count;

            currentPaletteIndex = newIndex;
        }

        OnPaletteChanged?.Invoke();
    }

    private TilePaletteDatabase.Palette CurrentPalette
    {
        get
        {
            if (paletteDatabase == null || paletteDatabase.palettes == null || paletteDatabase.palettes.Count == 0)
                return null;

            currentPaletteIndex = Mathf.Clamp(currentPaletteIndex, 0, paletteDatabase.palettes.Count - 1);
            return paletteDatabase.palettes[currentPaletteIndex];
        }
    }

    public static int PowerIndex(int value)
    {
        if (value <= 2)
            return 0;

        int idx = 0;
        int v = value;

        while (v > 2)
        {
            v >>= 1;
            idx++;
        }

        return idx;
    }

    public Color GetBackgroundColor()
    {
        var p = CurrentPalette;
        if (p == null)
            return Color.black;

        Color c = p.backgroundColor;
        c.a = 1f;
        return c;
    }

    public Color GetTileColor(int value)
    {
        var p = CurrentPalette;
        if (p == null || p.tileColors == null || p.tileColors.Count == 0)
            return Color.white;

        int index = PowerIndex(value);
        index = Mathf.Clamp(index, 0, p.tileColors.Count - 1);

        Color c = p.tileColors[index];
        c.a = 1f;
        return c;
    }

    public Color GetTextColorForTile(Color tileColor)
    {
        var p = CurrentPalette;

        float luma = 0.2126f * tileColor.r + 0.7152f * tileColor.g + 0.0722f * tileColor.b;

        if (p == null)
            return luma > 0.6f ? Color.black : Color.white;

        Color c = luma > 0.6f ? p.textDark : p.textLight;
        c.a = 1f;
        return c;
    }

    public void NotifyValueCreated(int value)
    {
        if (value < 2048)
            return;

        if (paletteChangedAfter2048ThisRun)
            return;

        paletteChangedAfter2048ThisRun = true;

        ApplyRandomPalette();
        RefreshAllTiles();
    }

    public void RefreshAllTiles()
    {
        OnPaletteChanged?.Invoke();

#if UNITY_2023_1_OR_NEWER
        var tiles = FindObjectsByType<CandyTile>(FindObjectsSortMode.None);
#else
        var tiles = FindObjectsOfType<CandyTile>();
#endif

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null)
                tiles[i].RefreshColor();
        }
    }
}