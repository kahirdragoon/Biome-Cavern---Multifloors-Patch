using HarmonyLib;
using UnityEngine;
using Verse;

namespace BiomesCavernsMultifloorsPatch;

/// <summary>Mod entry point — installs the Harmony patch and handles settings.</summary>
public class BiomesCavernsMultifloorsPatchMod : Mod
{
    public static BiomesCavernsMultifloorsPatchSettings Settings { get; private set; } = null!;

    public BiomesCavernsMultifloorsPatchMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<BiomesCavernsMultifloorsPatchSettings>();

        new Harmony("kahirdragoon.biomescavernsmultifloorspatch").PatchAll();
    }

    public override string SettingsCategory() => "Biomes Caverns - Multifloors Patch";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.Label($"Earthen Depths starts at level −{Settings.earthenDepthsDepth} (and everything deeper).");
        listing.Gap(4f);
        Settings.earthenDepthsDepth = Mathf.RoundToInt(listing.Slider(Settings.earthenDepthsDepth, 2, 25));
        listing.Gap(4f);
        listing.Label("Levels above that alternate Fungal Forest / Crystal Caverns; the first level below is a climate-matched shallow cave on desert/ice surfaces.");

        listing.End();
    }
}

public class BiomesCavernsMultifloorsPatchSettings : ModSettings
{
    /// <summary>Depth (positive) at which Earthen Depths begins and continues for all deeper levels.</summary>
    public int earthenDepthsDepth = 3;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref earthenDepthsDepth, "earthenDepthsDepth", 3);
    }
}
