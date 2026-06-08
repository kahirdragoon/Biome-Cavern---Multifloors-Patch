using RimWorld;
using UnityEngine;
using Verse;

namespace BiomesCavernsMultifloorsPatch;

/// <summary>
/// Picks which Biomes Caverns cavern biome a given Multifloors lower level should use.
///
/// Depth ladder (level is negative going down; depth = -level):
///   * depth >= EarthenDepthsDepth setting  -> Earthen Depths (and everything deeper).
///   * depth == 1 (first level below):
///         desert surface  (Desert / ExtremeDesert; + Alpha Biomes Gallatross Graveyard / Rocky
///                          Crags when loaded)              -> Desert Shallows (wrapper)
///         ice surface     (IceSheet; + Alpha Biomes Propane Lakes when loaded)
///                                                          -> Glacial Hollows (wrapper)
///         otherwise                        -> falls through to the alternation (random Fungal/Crystal)
///   * alternation zone: mirror the level directly above —
///         above was Fungal Forest          -> Crystal Caverns
///         above was Crystal Caverns        -> Fungal Forest
///         above was neither                -> random Fungal Forest / Crystal Caverns
///     This yields "first one random, then strictly alternate" as you keep digging.
/// </summary>
public static class BiomeSelector
{
    // Optional Alpha Biomes (sarg.alphabiomes) surfaces, mirroring the climate gates of the
    // original Biomes Caverns variants. Resolved by name so they stay null when Alpha Biomes is
    // not loaded — this static ctor runs at first use (map gen), long after defs are loaded.
    private static readonly BiomeDef? AB_PropaneLakes = DefDatabase<BiomeDef>.GetNamedSilentFail("AB_PropaneLakes");
    private static readonly BiomeDef? AB_GallatrossGraveyard = DefDatabase<BiomeDef>.GetNamedSilentFail("AB_GallatrossGraveyard");
    private static readonly BiomeDef? AB_RockyCrags = DefDatabase<BiomeDef>.GetNamedSilentFail("AB_RockyCrags");

    public static BiomeDef SelectBiome(int level, Map sourceMap, Map groundMap)
    {
        int depth = -level;

        // Threshold is clamped to >= 2 so the first level below is never Earthen Depths.
        int threshold = Mathf.Max(2, BiomesCavernsMultifloorsPatchMod.Settings.earthenDepthsDepth);
        if (depth >= threshold)
            return BCMFDefOf.BMT_EarthenDepths;

        BiomeDef? surface = (groundMap ?? sourceMap)?.Biome;
        if (depth == 1)
        {
            if (IsDesertSurface(surface))
                return BCMFDefOf.BCMF_DesertShallows;
            if (IsIceSurface(surface))
                return BCMFDefOf.BCMF_GlacialHollows;
        }

        BiomeDef? above = sourceMap?.Biome;
        if (above == BCMFDefOf.BMT_FungalForest)
            return BCMFDefOf.BMT_CrystalCaverns;
        if (above == BCMFDefOf.BMT_CrystalCaverns)
            return BCMFDefOf.BMT_FungalForest;

        return Rand.Bool ? BCMFDefOf.BMT_FungalForest : BCMFDefOf.BMT_CrystalCaverns;
    }

    private static bool IsDesertSurface(BiomeDef? b) =>
        b == BCMFDefOf.Desert
        || b == BCMFDefOf.ExtremeDesert
        || (AB_GallatrossGraveyard != null && b == AB_GallatrossGraveyard)
        || (AB_RockyCrags != null && b == AB_RockyCrags);

    private static bool IsIceSurface(BiomeDef? b) =>
        b == BCMFDefOf.IceSheet
        || (AB_PropaneLakes != null && b == AB_PropaneLakes);
}
