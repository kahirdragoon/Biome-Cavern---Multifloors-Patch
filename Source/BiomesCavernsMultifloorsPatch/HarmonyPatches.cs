using HarmonyLib;
using MultiFloors.Maps;
using RimWorld;
using Verse;

namespace BiomesCavernsMultifloorsPatch;

/// <summary>
/// Repoints the biome of each Multifloors lower-level (basement) pocket map to a Biomes Caverns
/// cavern biome, chosen by depth via <see cref="BiomeSelector"/>.
///
/// Mechanism: MultiFloors.Maps.LevelMapGenerator.GenerateLevelMap calls vanilla
/// MapGenerator.GenerateMap, which bakes the pocket map's biome from
/// generatorDef.pocketMapProperties.biome (Assembly-CSharp, MapGenerator line ~121) into the
/// map's persistent pocketTileInfo. We temporarily swap that biome for the duration of the call,
/// then restore it, so the shared MF_Basement def is left untouched afterwards. Because the
/// chosen biome carries BiomesMap{isCavern}, the existing MF_UndercaveRocksFromGrid step is
/// auto-substituted with GenStep_CavernRocksFromGrid by Biomes Caverns' own Harmony prefix.
///
/// </summary>
[HarmonyPatch(typeof(LevelMapGenerator), nameof(LevelMapGenerator.GenerateLevelMap))]
public static class Patch_LevelMapGenerator_GenerateLevelMap
{
    // Prefix runs before the original; sets the per-level cavern biome and stashes the original
    // biome in __state for the postfix to restore. __state stays null when we do not override.
    public static void Prefix(MapGeneratorDef generatorDef, Map sourceMap, Map groundMap, int level, out BiomeDef? __state)
    {
        __state = null;

        if (generatorDef == null || generatorDef != BCMFDefOf.MF_Basement || generatorDef.pocketMapProperties == null)
            return;

        BiomeDef chosen = BiomeSelector.SelectBiome(level, sourceMap, groundMap);
        if (chosen == null)
            return;

        __state = generatorDef.pocketMapProperties.biome;
        generatorDef.pocketMapProperties.biome = chosen;
    }

    public static void Postfix(MapGeneratorDef generatorDef, BiomeDef? __state)
    {
        if (__state != null && generatorDef?.pocketMapProperties != null)
            generatorDef.pocketMapProperties.biome = __state;
    }
}
