using System;
using System.Collections.Generic;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public struct UIThemeColors
    {
        public Color panelColor;
        public Color panelInnerColor;
        public Color panelOutlineColor;
        public Color panelTitleColor;
        public Color panelTextColor;
        public Color overlayColor;
        public Color buttonTextColor;
        public Color buttonFaceColor;
        public Color buttonShadowColor;
        public Color buttonOutlineColor;
        public Color selectionNormalColor;
        public Color selectionSelectedColor;
        public Color selectionBorderNormalColor;
        public Color selectionBorderSelectedColor;
    }

    public static ThemeManager I;

    private const string PP_THEME_SELECTION = "SETTINGS_THEME_SELECTION";
    private const int ThemeMaskDark = 1;
    private const int ThemeMaskColorful = 2;
    private const int ThemeMaskLight = 4;
    private const int ThemeMaskAll = ThemeMaskDark | ThemeMaskColorful | ThemeMaskLight;

    private static readonly Color DarkThemeTextColor = new Color32(0xF2, 0xEE, 0xE8, 0xFF);

    [SerializeField] private TilePaletteDatabase paletteDatabase;

    private int currentPaletteIndex = 0;
    private int cachedUiPaletteIndex = -1;

    private readonly List<int> reusablePaletteIndices = new List<int>(16);
    private readonly List<int> reusableFallbackPaletteIndices = new List<int>(16);
    private readonly List<TilePaletteDatabase.ThemeFamily> reusableFamilies = new List<TilePaletteDatabase.ThemeFamily>(3);
    private readonly List<Color> cachedUiAccentColors = new List<Color>(8);

    private UIThemeColors cachedUiTheme;
    private Color cachedButtonFaceColor = new Color32(0x3F, 0xC6, 0xFE, 0xFF);
    private Color cachedButtonShadowColor = new Color32(0x2A, 0x8B, 0xB8, 0xFF);
    private Color cachedButtonOutlineColor = new Color32(0x1F, 0x6A, 0x8C, 0xFF);
    private Color cachedButtonHighlightColor = new Color32(0x71, 0xD9, 0xFF, 0xFF);

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
        InvalidateUiCache();
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

    public TilePaletteDatabase.ThemeFamily GetCurrentPaletteFamily()
    {
        return ResolvePaletteFamily(CurrentPalette);
    }

    public TilePaletteDatabase.ThemeFamily ResolvePaletteFamily(TilePaletteDatabase.Palette palette)
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

    public float GetLuma(Color color)
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
        TilePaletteDatabase.Palette p = CurrentPalette;
        if (p == null)
            return Color.black;

        Color c = p.backgroundColor.a > 0f ? p.backgroundColor : p.boardTint;
        c.a = 1f;
        return c;
    }

    public Color GetTileColor(int value)
    {
        TilePaletteDatabase.Palette p = CurrentPalette;
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
        TilePaletteDatabase.Palette p = CurrentPalette;

        if (p == null)
            return Color.black;

        switch (ResolvePaletteFamily(p))
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return DarkThemeTextColor;

            case TilePaletteDatabase.ThemeFamily.Light:
            case TilePaletteDatabase.ThemeFamily.Colorful:
            default:
                return Color.black;
        }
    }

    public UIThemeColors GetUIThemeColors()
    {
        EnsureUiCache();
        return cachedUiTheme;
    }

    public Color GetUIButtonFaceColor(int ordinal)
    {
        EnsureUiCache();
        Color c = cachedButtonFaceColor;
        c.a = 1f;
        return c;
    }

    public Color GetUIButtonShadowColor(int ordinal)
    {
        EnsureUiCache();
        Color c = cachedButtonShadowColor;
        c.a = 1f;
        return c;
    }

    public Color GetUIButtonOutlineColor(int ordinal)
    {
        EnsureUiCache();
        Color c = cachedButtonOutlineColor;
        c.a = 1f;
        return c;
    }

    public Color GetUIButtonHighlightColor(int ordinal)
    {
        EnsureUiCache();
        Color c = cachedButtonHighlightColor;
        c.a = 1f;
        return c;
    }

    public Color GetReadableButtonContentColor(int ordinal)
    {
        EnsureUiCache();
        Color c = cachedUiTheme.buttonTextColor;
        c.a = 1f;
        return c;
    }

    private void EnsureUiCache()
    {
        if (cachedUiPaletteIndex == currentPaletteIndex && cachedUiAccentColors.Count > 0)
            return;

        BuildUiCache();
    }

    private void BuildUiCache()
    {
        cachedUiPaletteIndex = currentPaletteIndex;
        cachedUiAccentColors.Clear();

        TilePaletteDatabase.Palette palette = CurrentPalette;
        TilePaletteDatabase.ThemeFamily family = ResolvePaletteFamily(palette);

        cachedButtonFaceColor = BuildPrimaryUiAccent(palette, family);
        cachedButtonShadowColor = BuildButtonShadowColor(cachedButtonFaceColor, family);
        cachedButtonOutlineColor = BuildButtonOutlineColor(cachedButtonFaceColor, family);
        cachedButtonHighlightColor = BuildButtonHighlightColor(cachedButtonFaceColor, family);

        cachedUiAccentColors.Add(cachedButtonFaceColor);
        cachedUiTheme = BuildUiThemeColors(family, cachedButtonFaceColor, cachedButtonShadowColor, cachedButtonOutlineColor);
    }

    private void InvalidateUiCache()
    {
        cachedUiPaletteIndex = -1;
        cachedUiAccentColors.Clear();
    }

    private UIThemeColors BuildUiThemeColors(TilePaletteDatabase.ThemeFamily family, Color buttonFaceColor, Color buttonShadowColor, Color buttonOutlineColor)
    {
        UIThemeColors ui = new UIThemeColors();

        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                ui.panelColor = new Color32(0x7A, 0x5A, 0x45, 0xFF);
                ui.panelInnerColor = new Color32(0x63, 0x47, 0x35, 0xFF);
                ui.panelOutlineColor = new Color32(0x3E, 0x29, 0x1E, 0xFF);
                ui.panelTitleColor = DarkThemeTextColor;
                ui.panelTextColor = DarkThemeTextColor;
                ui.overlayColor = new Color(0.07f, 0.05f, 0.04f, 0.64f);
                ui.buttonTextColor = DarkThemeTextColor;
                break;

            case TilePaletteDatabase.ThemeFamily.Light:
                ui.panelColor = new Color32(0xF2, 0xEC, 0xE2, 0xFF);
                ui.panelInnerColor = new Color32(0xFB, 0xF7, 0xF1, 0xFF);
                ui.panelOutlineColor = new Color32(0xB8, 0xAA, 0x9A, 0xFF);
                ui.panelTitleColor = Color.black;
                ui.panelTextColor = Color.black;
                ui.overlayColor = new Color(0.12f, 0.10f, 0.08f, 0.20f);
                ui.buttonTextColor = Color.black;
                break;

            default:
                ui.panelColor = new Color32(0xE7, 0xF1, 0xFF, 0xFF);
                ui.panelInnerColor = new Color32(0xF5, 0xFA, 0xFF, 0xFF);
                ui.panelOutlineColor = new Color32(0x7F, 0xA8, 0xD4, 0xFF);
                ui.panelTitleColor = DarkThemeTextColor;
                ui.panelTextColor = DarkThemeTextColor;
                ui.overlayColor = new Color(0.08f, 0.18f, 0.32f, 0.20f);
                ui.buttonTextColor = DarkThemeTextColor;
                break;
        }

        ui.buttonFaceColor = buttonFaceColor;
        ui.buttonShadowColor = buttonShadowColor;
        ui.buttonOutlineColor = buttonOutlineColor;

        ui.selectionNormalColor = Color.Lerp(ui.panelInnerColor, buttonFaceColor, family == TilePaletteDatabase.ThemeFamily.Dark ? 0.22f : 0.16f);
        ui.selectionSelectedColor = Color.Lerp(buttonFaceColor, Color.white, family == TilePaletteDatabase.ThemeFamily.Dark ? 0.10f : 0.18f);
        ui.selectionBorderNormalColor = buttonOutlineColor;
        ui.selectionBorderSelectedColor = buttonShadowColor;

        return ui;
    }

    private Color BuildPrimaryUiAccent(TilePaletteDatabase.Palette palette, TilePaletteDatabase.ThemeFamily family)
    {
        Color candidate = GetDefaultPrimaryUiAccent(family);
        float bestScore = float.MinValue;

        if (palette != null && palette.tileColors != null)
        {
            for (int i = 0; i < palette.tileColors.Count; i++)
            {
                Color source = palette.tileColors[i];
                Color.RGBToHSV(source, out _, out float saturation, out float value);
                float luma = GetLuma(source);
                float score = (saturation * 1.4f) + (value * 0.55f) - Mathf.Abs(luma - 0.60f);

                if (score <= bestScore)
                    continue;

                bestScore = score;
                candidate = source;
            }
        }

        candidate = AdjustPrimaryUiAccentForFamily(candidate, family);

        if (family == TilePaletteDatabase.ThemeFamily.Dark)
            candidate = EnsureMaximumLuma(candidate, 0.34f);
        else
            candidate = EnsureMinimumLuma(candidate, 0.68f);

        candidate.a = 1f;
        return candidate;
    }

    private Color AdjustPrimaryUiAccentForFamily(Color color, TilePaletteDatabase.ThemeFamily family)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);

        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                saturation = Mathf.Clamp(saturation * 0.50f + 0.10f, 0.10f, 0.55f);
                value = Mathf.Clamp(value * 0.42f, 0.18f, 0.42f);
                break;

            case TilePaletteDatabase.ThemeFamily.Light:
                saturation = Mathf.Clamp(saturation * 0.36f, 0.10f, 0.38f);
                value = Mathf.Clamp(Mathf.Max(0.92f, value), 0f, 0.98f);
                break;

            default:
                saturation = Mathf.Clamp(Mathf.Max(0.24f, saturation * 0.48f), 0.24f, 0.52f);
                value = Mathf.Clamp(Mathf.Max(0.86f, value), 0f, 0.98f);
                break;
        }

        Color adjusted = Color.HSVToRGB(hue, saturation, value);
        adjusted.a = 1f;
        return adjusted;
    }

    private Color BuildButtonShadowColor(Color face, TilePaletteDatabase.ThemeFamily family)
    {
        float valueMultiplier = family == TilePaletteDatabase.ThemeFamily.Dark ? 0.55f : 0.72f;
        float saturationMultiplier = family == TilePaletteDatabase.ThemeFamily.Light ? 0.85f : 0.95f;
        return MultiplyValue(face, valueMultiplier, saturationMultiplier);
    }

    private Color BuildButtonOutlineColor(Color face, TilePaletteDatabase.ThemeFamily family)
    {
        float valueMultiplier = family == TilePaletteDatabase.ThemeFamily.Dark ? 0.42f : 0.62f;
        float saturationMultiplier = family == TilePaletteDatabase.ThemeFamily.Light ? 0.70f : 0.90f;
        return MultiplyValue(face, valueMultiplier, saturationMultiplier);
    }

    private Color BuildButtonHighlightColor(Color face, TilePaletteDatabase.ThemeFamily family)
    {
        float lift = family == TilePaletteDatabase.ThemeFamily.Dark ? 0.08f : 0.05f;
        Color c = Color.Lerp(face, Color.white, lift);
        c.a = 1f;
        return c;
    }

    private Color GetDefaultPrimaryUiAccent(TilePaletteDatabase.ThemeFamily family)
    {
        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                return new Color32(0x5E, 0x67, 0x61, 0xFF);
            case TilePaletteDatabase.ThemeFamily.Light:
                return new Color32(0xE6, 0xDE, 0xD0, 0xFF);
            default:
                return new Color32(0xC7, 0xE3, 0xF6, 0xFF);
        }
    }

    private Color EnsureMinimumLuma(Color color, float minLuma)
    {
        int safeGuard = 0;
        while (GetLuma(color) < minLuma && safeGuard < 12)
        {
            color = Color.Lerp(color, Color.white, 0.18f);
            safeGuard++;
        }

        color.a = 1f;
        return color;
    }

    private Color EnsureMaximumLuma(Color color, float maxLuma)
    {
        int safeGuard = 0;
        while (GetLuma(color) > maxLuma && safeGuard < 12)
        {
            color = Color.Lerp(color, Color.black, 0.18f);
            safeGuard++;
        }

        color.a = 1f;
        return color;
    }

    private Color GetFirstAccentOrDefault(TilePaletteDatabase.ThemeFamily family)
    {
        if (cachedUiAccentColors.Count > 0)
            return cachedUiAccentColors[0];

        return GetDefaultPrimaryUiAccent(family);
    }

    private void BuildUiAccentColors(TilePaletteDatabase.Palette palette, TilePaletteDatabase.ThemeFamily family, List<Color> results)
    {
        results.Clear();

        if (palette != null && palette.tileColors != null)
        {
            for (int i = 0; i < palette.tileColors.Count; i++)
            {
                Color accent = AdjustAccentForFamily(palette.tileColors[i], family);
                if (TryAddDistinctAccent(results, accent))
                {
                    if (results.Count >= 6)
                        break;
                }
            }
        }

        if (results.Count == 0)
            AddDefaultAccentColors(family, results);
    }

    private Color AdjustAccentForFamily(Color color, TilePaletteDatabase.ThemeFamily family)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);

        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                saturation = Mathf.Clamp01(Mathf.Max(0.68f, saturation));
                value = Mathf.Clamp(Mathf.Max(0.78f, value), 0f, 0.96f);
                break;

            case TilePaletteDatabase.ThemeFamily.Light:
                saturation = Mathf.Clamp(Mathf.Max(0.42f, saturation * 0.82f), 0f, 0.88f);
                value = Mathf.Clamp(Mathf.Max(0.76f, value), 0f, 0.94f);
                break;

            default:
                saturation = Mathf.Clamp(Mathf.Max(0.74f, saturation), 0f, 1f);
                value = Mathf.Clamp(Mathf.Max(0.84f, value), 0f, 0.98f);
                break;
        }

        Color adjusted = Color.HSVToRGB(hue, saturation, value);
        adjusted.a = 1f;
        return adjusted;
    }

    private bool TryAddDistinctAccent(List<Color> colors, Color candidate)
    {
        for (int i = 0; i < colors.Count; i++)
        {
            if (ColorDistance(colors[i], candidate) < 0.22f)
                return false;
        }

        colors.Add(candidate);
        return true;
    }

    private float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt((dr * dr) + (dg * dg) + (db * db));
    }

    private void AddDefaultAccentColors(TilePaletteDatabase.ThemeFamily family, List<Color> colors)
    {
        colors.Clear();

        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                colors.Add(new Color32(0x36, 0xDE, 0x1C, 0xFF));
                colors.Add(new Color32(0xE9, 0xAB, 0x13, 0xFF));
                colors.Add(new Color32(0x33, 0xC4, 0xEE, 0xFF));
                break;

            case TilePaletteDatabase.ThemeFamily.Light:
                colors.Add(new Color32(0x3F, 0xBF, 0xF1, 0xFF));
                colors.Add(new Color32(0x5E, 0xD6, 0xA7, 0xFF));
                colors.Add(new Color32(0xF2, 0xA5, 0x51, 0xFF));
                break;

            default:
                colors.Add(new Color32(0xF6, 0xA7, 0x14, 0xFF));
                colors.Add(new Color32(0x29, 0xD2, 0xAF, 0xFF));
                colors.Add(new Color32(0x3F, 0xC6, 0xFE, 0xFF));
                break;
        }
    }

    private Color MultiplyValue(Color color, float valueMultiplier, float saturationMultiplier)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        saturation = Mathf.Clamp01(saturation * saturationMultiplier);
        value = Mathf.Clamp01(value * valueMultiplier);
        Color c = Color.HSVToRGB(hue, saturation, value);
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
        CandyTile[] tiles = FindObjectsByType<CandyTile>(FindObjectsSortMode.None);
#else
        CandyTile[] tiles = FindObjectsOfType<CandyTile>();
#endif

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null)
                tiles[i].RefreshColor();
        }
    }
}
