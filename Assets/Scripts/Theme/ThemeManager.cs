using System.Collections.Generic;
using UnityEngine;
using static TilePaletteDatabase;

public class ThemeManager : MonoBehaviour
{
    
    public static ThemeManager I;

    [Header("Palette Database")]
    public TilePaletteDatabase paletteDatabase;
    private List<TilePaletteDatabase.TilePalette> availablePalettes = new List<TilePaletteDatabase.TilePalette>();
    private List<int> shuffledOrder = new List<int>();

    private int palettePointer = 0;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        BuildAvailablePaletteList();
        ShufflePalettes();
    }

    void BuildAvailablePaletteList()
    {
        availablePalettes.Clear();

        bool allowDark = PlayerPrefs.GetInt("palette_dark", 1) == 1;
        bool allowLight = PlayerPrefs.GetInt("palette_light", 1) == 1;
        bool allowColor = PlayerPrefs.GetInt("palette_color", 1) == 1;

        foreach (var p in paletteDatabase.palettes)
        {
            if (p.type == PaletteType.Dark && allowDark)
                availablePalettes.Add(p);

            if (p.type == PaletteType.Light && allowLight)
                availablePalettes.Add(p);

            if (p.type == PaletteType.Colorful && allowColor)
                availablePalettes.Add(p);
        }

        if (availablePalettes.Count == 0)
        {
            availablePalettes.AddRange(paletteDatabase.palettes);
        }
    }

    void ShufflePalettes()
    {
        shuffledOrder.Clear();

        for (int i = 0; i < availablePalettes.Count; i++)
            shuffledOrder.Add(i);

        for (int i = 0; i < shuffledOrder.Count; i++)
        {
            int j = Random.Range(i, shuffledOrder.Count);

            int tmp = shuffledOrder[i];
            shuffledOrder[i] = shuffledOrder[j];
            shuffledOrder[j] = tmp;
        }

        palettePointer = 0;
    }

    public TilePalette GetNextPalette()
    {
        if (availablePalettes.Count == 0)
            return null;

        if (palettePointer >= shuffledOrder.Count)
        {
            ShufflePalettes();
        }

        int index = shuffledOrder[palettePointer];
        palettePointer++;

        return availablePalettes[index];
    }

    public void ApplyRandomPalette()
    {
        TilePalette palette = GetNextPalette();

        if (palette == null)
            return;

        ColorThemeManager.I.ApplyPalette(palette);
    }

    public void OnGameRestart()
    {
        BuildAvailablePaletteList();
        ShufflePalettes();
        ApplyRandomPalette();
    }

    public void OnSettingsChanged()
    {
        BuildAvailablePaletteList();
        ShufflePalettes();
    }
}