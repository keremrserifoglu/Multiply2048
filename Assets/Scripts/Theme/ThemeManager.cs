using System;
using System.Collections.Generic;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager I;

    private const string PP_THEME_SELECTION = "SETTINGS_THEME_SELECTION";
    private const int ThemeMaskDark = 1;
    private const int ThemeMaskColorful = 2;
    private const int ThemeMaskLight = 4;
    private const int ThemeMaskAll = ThemeMaskDark | ThemeMaskColorful | ThemeMaskLight;

    [SerializeField] private TilePaletteDatabase paletteDatabase;

    private int currentPaletteIndex = 0;

    private readonly List<int> reusablePaletteIndices = new List<int>(16);
    private readonly List<int> reusableFallbackPaletteIndices = new List<int>(16);
    private readonly List<TilePaletteDatabase.ThemeFamily> reusableFamilies = new List<TilePaletteDatabase.ThemeFamily>(3);

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
        if (HasAnyPalette())
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
        if (!TrySelectNextPalette(false))
            return;

        RefreshAllTiles();
    }

    private bool TrySelectNextPalette(bool forceDifferent)
    {
        int nextIndex = SelectPaletteIndex(forceDifferent);
        if (nextIndex < 0)
            return false;

        currentPaletteIndex = nextIndex;
        return true;
    }

    private int SelectPaletteIndex(bool forceDifferent)
    {
        if (!HasAnyPalette())
            return -1;

        int selectedMask = GetEffectiveThemeSelectionMask();

        reusableFamilies.Clear();
        AddEnabledFamily(selectedMask, ThemeMaskDark, TilePaletteDatabase.ThemeFamily.Dark);
        AddEnabledFamily(selectedMask, ThemeMaskColorful, TilePaletteDatabase.ThemeFamily.Colorful);
        AddEnabledFamily(selectedMask, ThemeMaskLight, TilePaletteDatabase.ThemeFamily.Light);

        reusablePaletteIndices.Clear();

        if (reusableFamilies.Count > 0)
        {
            TilePaletteDatabase.ThemeFamily chosenFamily = reusableFamilies[UnityEngine.Random.Range(0, reusableFamilies.Count)];
            CollectPaletteIndicesForFamily(chosenFamily, reusablePaletteIndices);
        }

        if (forceDifferent && reusablePaletteIndices.Count == 1 && reusablePaletteIndices[0] == currentPaletteIndex)
        {
            reusableFallbackPaletteIndices.Clear();
            CollectEligiblePaletteIndices(selectedMask, reusableFallbackPaletteIndices);

            if (reusableFallbackPaletteIndices.Count > 1)
            {
                reusablePaletteIndices.Clear();
                reusablePaletteIndices.AddRange(reusableFallbackPaletteIndices);
            }
        }

        if (reusablePaletteIndices.Count == 0)
            CollectAllPaletteIndices(reusablePaletteIndices);

        if (reusablePaletteIndices.Count == 0)
            return -1;

        int selectedIndex = reusablePaletteIndices[0];

        if (reusablePaletteIndices.Count > 1)
        {
            int safeGuard = 0;
            do
            {
                selectedIndex = reusablePaletteIndices[UnityEngine.Random.Range(0, reusablePaletteIndices.Count)];
                safeGuard++;
            }
            while (forceDifferent && selectedIndex == currentPaletteIndex && safeGuard < 16);

            if (forceDifferent && selectedIndex == currentPaletteIndex)
            {
                for (int i = 0; i < reusablePaletteIndices.Count; i++)
                {
                    if (reusablePaletteIndices[i] == currentPaletteIndex)
                        continue;

                    selectedIndex = reusablePaletteIndices[i];
                    break;
                }
            }
        }

        return selectedIndex;
    }

    private void AddEnabledFamily(int selectedMask, int familyMask, TilePaletteDatabase.ThemeFamily family)
    {
        if ((selectedMask & familyMask) == 0)
            return;

        if (!HasAnyPaletteForFamily(family))
            return;

        reusableFamilies.Add(family);
    }

    private void CollectEligiblePaletteIndices(int selectedMask, List<int> indices)
    {
        indices.Clear();

        if (paletteDatabase == null || paletteDatabase.palettes == null)
            return;

        for (int i = 0; i < paletteDatabase.palettes.Count; i++)
        {
            TilePaletteDatabase.Palette palette = paletteDatabase.palettes[i];
            TilePaletteDatabase.ThemeFamily family = ResolvePaletteFamily(palette);

            if (!IsFamilyEnabled(selectedMask, family))
                continue;

            indices.Add(i);
        }

        if (indices.Count > 0)
            return;

        CollectAllPaletteIndices(indices);
    }

    private void CollectAllPaletteIndices(List<int> indices)
    {
        indices.Clear();

        if (paletteDatabase == null || paletteDatabase.palettes == null)
            return;

        for (int i = 0; i < paletteDatabase.palettes.Count; i++)
            indices.Add(i);
    }

    private void CollectPaletteIndicesForFamily(TilePaletteDatabase.ThemeFamily family, List<int> indices)
    {
        indices.Clear();

        if (paletteDatabase == null || paletteDatabase.palettes == null)
            return;

        for (int i = 0; i < paletteDatabase.palettes.Count; i++)
        {
            TilePaletteDatabase.Palette palette = paletteDatabase.palettes[i];
            if (ResolvePaletteFamily(palette) != family)
                continue;

            indices.Add(i);
        }
    }

    private bool HasAnyPaletteForFamily(TilePaletteDatabase.ThemeFamily family)
    {
        if (paletteDatabase == null || paletteDatabase.palettes == null)
            return false;

        for (int i = 0; i < paletteDatabase.palettes.Count; i++)
        {
            if (ResolvePaletteFamily(paletteDatabase.palettes[i]) == family)
                return true;
        }

        return false;
    }

    private bool IsFamilyEnabled(int selectedMask, TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return (selectedMask & ThemeMaskDark) != 0;
            case TilePaletteDatabase.ThemeFamily.Light:
                return (selectedMask & ThemeMaskLight) != 0;
            default:
                return (selectedMask & ThemeMaskColorful) != 0;
        }
    }

    private bool HasAnyPalette()
    {
        return paletteDatabase != null &&
               paletteDatabase.palettes != null &&
               paletteDatabase.palettes.Count > 0;
    }

    private int GetEffectiveThemeSelectionMask()
    {
        int selection = PlayerPrefs.GetInt(PP_THEME_SELECTION, 0) & ThemeMaskAll;
        return selection == 0 ? ThemeMaskAll : selection;
    }

    private TilePaletteDatabase.ThemeFamily ResolvePaletteFamily(TilePaletteDatabase.Palette palette)
    {
        if (palette == null)
            return TilePaletteDatabase.ThemeFamily.Colorful;

        if (palette.family != TilePaletteDatabase.ThemeFamily.Unspecified)
            return palette.family;

        string paletteName = string.IsNullOrWhiteSpace(palette.name)
            ? string.Empty
            : palette.name.Trim().ToLowerInvariant();

        if (ContainsAny(paletteName, "dark", "night", "midnight", "shadow", "black", "deep", "moody"))
            return TilePaletteDatabase.ThemeFamily.Dark;

        if (ContainsAny(paletteName, "light", "soft", "pastel", "cream", "snow", "white", "paper"))
            return TilePaletteDatabase.ThemeFamily.Light;

        if (ContainsAny(paletteName, "color", "colour", "colorful", "rainbow", "vivid", "neon", "candy", "fruit", "tropical"))
            return TilePaletteDatabase.ThemeFamily.Colorful;

        AnalyzePaletteTone(palette, out float backgroundLuma, out float averageTileLuma, out float averageTileSaturation);

        if (backgroundLuma <= 0.42f && averageTileSaturation < 0.72f)
            return TilePaletteDatabase.ThemeFamily.Dark;

        if (backgroundLuma >= 0.50f)
            return TilePaletteDatabase.ThemeFamily.Light;

        if (averageTileSaturation >= 0.72f)
            return TilePaletteDatabase.ThemeFamily.Colorful;

        if (averageTileLuma >= 0.66f && averageTileSaturation <= 0.45f)
            return TilePaletteDatabase.ThemeFamily.Light;

        return TilePaletteDatabase.ThemeFamily.Colorful;
    }

    private bool ContainsAny(string source, params string[] terms)
    {
        if (string.IsNullOrEmpty(source) || terms == null)
            return false;

        for (int i = 0; i < terms.Length; i++)
        {
            if (string.IsNullOrEmpty(terms[i]))
                continue;

            if (source.Contains(terms[i]))
                return true;
        }

        return false;
    }

    private void AnalyzePaletteTone(TilePaletteDatabase.Palette palette, out float backgroundLuma, out float averageTileLuma, out float averageTileSaturation)
    {
        backgroundLuma = 0f;
        averageTileLuma = 0f;
        averageTileSaturation = 0f;

        if (palette == null)
            return;

        backgroundLuma = GetLuma(palette.backgroundColor.a > 0f ? palette.backgroundColor : palette.boardTint);

        if (palette.tileColors == null || palette.tileColors.Count == 0)
        {
            averageTileLuma = backgroundLuma;
            return;
        }

        for (int i = 0; i < palette.tileColors.Count; i++)
        {
            Color color = palette.tileColors[i];
            averageTileLuma += GetLuma(color);

            Color.RGBToHSV(color, out _, out float saturation, out _);
            averageTileSaturation += saturation;
        }

        averageTileLuma /= palette.tileColors.Count;
        averageTileSaturation /= palette.tileColors.Count;
    }

    private float GetLuma(Color color)
    {
        return 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
    }

    private TilePaletteDatabase.Palette CurrentPalette
    {
        get
        {
            if (!HasAnyPalette())
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

        Color c = p.backgroundColor.a > 0f ? p.backgroundColor : p.boardTint;
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

        float luma = GetLuma(tileColor);

        if (p == null)
            return luma > 0.6f ? Color.black : Color.white;

        Color preferredDark = p.textDark.a > 0f ? p.textDark : Color.black;
        Color preferredLight = p.textLight.a > 0f ? p.textLight : Color.white;

        Color c = luma > 0.6f ? preferredDark : preferredLight;
        c.a = 1f;
        return c;
    }

    public void NotifyValueCreated(int value)
    {
        if (value < 2048)
            return;

        if (!TrySelectNextPalette(true))
            return;

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
