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
///         Desert / ExtremeDesert surface   -> Desert Shallows (wrapper)
///         Ice Sheet surface                -> Glacial Hollows (wrapper)
///         otherwise                        -> falls through to the alternation (random Fungal/Crystal)
///   * alternation zone: mirror the level directly above —
///         above was Fungal Forest          -> Crystal Caverns
///         above was Crystal Caverns        -> Fungal Forest
///         above was neither                -> random Fungal Forest / Crystal Caverns
///     This yields "first one random, then strictly alternate" as you keep digging.
/// </summary>
public static class BiomeSelector
{
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
            if (surface == BCMFDefOf.Desert || surface == BCMFDefOf.ExtremeDesert)
                return BCMFDefOf.BCMF_DesertShallows;
            if (surface == BCMFDefOf.IceSheet)
                return BCMFDefOf.BCMF_GlacialHollows;
        }

        BiomeDef? above = sourceMap?.Biome;
        if (above == BCMFDefOf.BMT_FungalForest)
            return BCMFDefOf.BMT_CrystalCaverns;
        if (above == BCMFDefOf.BMT_CrystalCaverns)
            return BCMFDefOf.BMT_FungalForest;

        return Rand.Bool ? BCMFDefOf.BMT_FungalForest : BCMFDefOf.BMT_CrystalCaverns;
    }
}
