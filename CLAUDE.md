# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

Read About/About.xml to get a description of the mod.

DO NOT GUESS, ASK IF UNSURE

## Project Overview

**Biomes Caverns - Multifloors Patch** is a RimWorld 1.6 mod by kahirdragoon.
Package ID: `kahirdragoon.biomescavernsmultifloorspatch`
Def prefix: `BCMF_`

The goal of this patch is to make the lower-level (basement) maps spawned by the
**Multifloors** mod use **Biomes Caverns** map generation instead of the plain
`MF_Basement` generator.

## Bridge Points (CONFIRMED by investigating the source mods)

Both mods are hard `modDependencies`, so patches need NO `<mods>` guards.

**Multifloors lower-level gen** (`telardo.MultiFloors`):
- Going *down* generates a pocket map via `MapGeneratorDef` `MF_Basement` (or
  `MF_BasementWithoutCaves` when the "generate basement without caves" setting is on).
  Selection is hardcoded in `MultiFloors.Maps.LevelMapGenerator.SetupMapGenerator`
  (Direction.Down → `MiscDefOfs.MF_Basement` / `MF_BasementWithoutCaves`).
- Defined in `Multifloor/Defs/MapGeneration/BasementMapGenerator.xml`.
  - `pocketMapProperties/biome` = `MF_BasementBiome` (a near-empty placeholder biome:
    no plants/animals/terrain, just `MF_UndergroundWeather` + diseases).
  - `pocketMapProperties/tileMutators` = `MF_UndergroundCave`.
  - genSteps include `MF_UndercaveRocksFromGrid` — a `GenStepDef` whose
    `genStep Class="GenStep_RocksFromGrid"`. **This is the key seam.**
- `MF_BasementBiome` is NOT referenced in Multifloors C# — safe to repoint/replace the
  basement biome at the def level.

**Biomes Caverns gen** (`BiomesTeam.BiomesCaverns`, prefix `BMT_`, needs `BiomesTeam.BiomesCore`):
- SIX cavern types in TWO mechanisms:
  - Full cavern `BiomeDef`s (deep): `BMT_EarthenDepths`, `BMT_FungalForest`, `BMT_CrystalCaverns`.
    Carry `BiomesMap{isCavern,cavernShapes}` -> auto carve+roof. Assignable via
    `pocketMapProperties/biome` directly.
  - `BiomeVariantDef` overlays (shallow, surface-connected): `BMT_ShallowCave`, `BMT_GlacialHollows`,
    `BMT_DesertShallows`. NOT biomes -- they re-skin only the roofed cave cells
    (`mapGridConditions roof=RoofRockThick`, `applyToCaves=true`) of a base map, gated by
    `worldTileConditions` (surface cave landforms; climate-partitioned: ShallowCave excludes
    desert/ice, DesertShallows=desert, GlacialHollows=ice). `PocketMapProperties` has NO
    biomeVariant field, so a variant CANNOT be attached via XML to a Multifloors pocket map --
    needs C# to populate the map's applied-variant list during generation, over a cavern base biome.
- The "cavern look" (carve a tunnel network into solid rock, then roof it) is **biome-driven**,
  NOT world-tile-driven. Biomes Caverns Harmony-patches `GenStep_RocksFromGrid.Generate`
  (`BiomesCaverns.Patches.GenStep_RocksFromGrid_Generate_Patch`): if
  `map.Biome.GetModExtension<BiomesCore.DefModExtensions.BiomesMap>()` has `isCavern == true`
  and non-empty `cavernShapes` (e.g. `TunnelNetwork`, a `CavernShape` C# enum), it runs
  `BiomesCore.MapGeneration.GenStep_CavernRocksFromGrid` instead. No dependency on the world tile.
- Content scatter is added by patching `MapGeneratorDef[@Name="MapCommonBase"]/genSteps` with
  `BMT_CrystalsGenerator` (scatter crystals) and `BMT_ScatterStalagmiteGenerator`. These
  genSteps self-guard to cavern biomes. `MF_Basement` does NOT inherit `MapCommonBase`,
  so these must be added to `MF_Basement` explicitly to get crystals/stalagmites.

**Integration recipe** (what this patch must do):
1. Make the basement map's biome a cavern biome (carries `BiomesMap{isCavern, cavernShapes}`
   plus terrain/plants/animals/weather). Either repoint `MF_Basement` `pocketMapProperties/biome`
   to a `BMT_` biome, OR inject the `BiomesMap` extension + content into a basement biome.
2. The existing `MF_UndercaveRocksFromGrid` (a `GenStep_RocksFromGrid`) then auto-substitutes to
   `GenStep_CavernRocksFromGrid` via the Harmony prefix — caverns carve for free.
3. Add `BMT_CrystalsGenerator` + `BMT_ScatterStalagmiteGenerator` to `MF_Basement` genSteps for
   crystal/stalagmite content.
4. (Optional, needs C#) Randomise which cavern biome each level uses, or vary by depth —
   `pocketMapProperties/biome` is a single BiomeDef, so per-level variety requires a Harmony hook.

**Implementation (current):**
- `Patch_LevelMapGenerator_GenerateLevelMap` (manual Harmony patch on
  `MultiFloors.Maps.LevelMapGenerator:GenerateLevelMap`, via `AccessTools` — MultiFloors is not a
  compile reference) temporarily swaps `MF_Basement.pocketMapProperties.biome` to the chosen
  cavern biome for the duration of generation, then restores it (the biome is baked into the
  pocket map's persistent `pocketTileInfo.PrimaryBiome` at gen time — `MapGenerator` ~line 121 —
  so the restore is safe and the map keeps the biome).
- `BiomeSelector.SelectBiome(level, sourceMap, groundMap)` is the depth ladder:
  depth = -level; `earthenDepthsDepth` setting (clamped >=2):
  - depth >= setting -> `BMT_EarthenDepths`.
  - depth == 1: Desert/ExtremeDesert surface -> `BCMF_DesertShallows`; IceSheet -> `BCMF_GlacialHollows`;
    else fall through.
  - else mirror the level above: Fungal->Crystal, Crystal->Fungal, neither->random Fungal/Crystal
    (= "first random, then strict alternation"). `sourceMap` is the level above, `groundMap` is surface.
- `BCMF_DesertShallows` / `BCMF_GlacialHollows` (`Defs/Biomes/`) are wrapper BiomeDefs baking the
  shallow `BiomeVariantDef` content + the `BiomesMap{isCavern}` extension (`generatesNaturally=false`).
- `Patches/MF_Basement_GenSteps.xml` adds `BMT_CrystalsGenerator` + `BMT_ScatterStalagmiteGenerator`
  to `MF_Basement`. `MF_BasementWithoutCaves` is intentionally left vanilla (no `RocksFromGrid` step
  -> no carving; it is the player's opt-out of caves). `BMT_ShallowCave` variant is unused by design.

Source paths for reference:
- Multifloors source: `d:\Rimworld Modding\Multifloor`
- Biomes Caverns source: `d:\Rimworld Modding\BiomesCaverns`
- Biomes Caverns installed (1.6 XML): `e:\SteamLibrary\steamapps\workshop\content\294100\2969748433\1.6`
- Biomes Core (DLL only): `e:\SteamLibrary\steamapps\workshop\content\294100\2038000893`

## Build & Development

**Build**: `dotnet build` from `Source/BiomesCavernsMultifloorsPatch/`
**Output DLL**: `1.6/Assemblies/BiomesCavernsMultifloorsPatch.dll`

There are no automated tests — verification is done by loading the mod in-game.
Register debug actions in a `DebugActions_BCMF.cs` file; they appear in the dev mode menu.

Always check if something can be done with vanilla mechanics before writing custom C# code. Prefer XML-only solutions when possible.

Performance is important. Cache when it makes sense. Be very careful with everything that executes per tick.

BUILD the DLL at the end of every task to verify there are no compile errors.

## Source Lookup Paths

- RimWorld decompiled source: `d:\Rimworld Modding\Rimworld Source`
- Steam workshop mods: `e:\SteamLibrary\steamapps\workshop\content\294100\`

## XML Conventions

- All mod-specific def names use the `BCMF_` prefix (e.g. `BCMF_MyThing`).
- Patches go in `Patches/`; use `<mods>` guards for optional mod compatibility.
- Conditional content goes in `Mods/<ModName>/` folders loaded via `LoadFolders.xml`.

## C# Conventions

- Use C# latest features (`LangVersion latest` is set in the .csproj).
- Target framework is `net481` (Mono/.NET 4.8) — .NET 5+ runtime APIs are unavailable.
- Static def references go in `DefOf.cs` using the `[DefOf]` attribute.
