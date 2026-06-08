using RimWorld;
using Verse;

namespace BiomesCavernsMultifloorsPatch;

/// <summary>
/// Static def references resolved by the game at startup. All referenced defs come from hard
/// dependencies (vanilla, Multifloors, Biomes Caverns) so no [MayRequire] guards are needed.
/// </summary>
[DefOf]
public static class BCMFDefOf
{
    // Multifloors basement (caves) map generator — the one this patch repoints per level.
    public static MapGeneratorDef MF_Basement;

    // Biomes Caverns deep cavern biomes (full BiomeDefs carrying BiomesMap{isCavern}).
    public static BiomeDef BMT_FungalForest;
    public static BiomeDef BMT_CrystalCaverns;
    public static BiomeDef BMT_EarthenDepths;

    // This patch's wrapper biomes that bake the shallow BiomeVariantDefs into assignable biomes.
    public static BiomeDef BCMF_DesertShallows;
    public static BiomeDef BCMF_GlacialHollows;

    // Vanilla surface biomes used to pick the first-level shallow cavern.
    public static BiomeDef Desert;
    public static BiomeDef ExtremeDesert;
    public static BiomeDef IceSheet;

    static BCMFDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BCMFDefOf));
}
