// BetterDesignatorSorting.Floors
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Floors {
        public static void Patch() {
            TerrainPatch();
            FloorCoveringPatch();
        }

        // TERRAINS
        private static void TerrainPatch() {
            IEnumerable<TerrainDef> floors = DefDatabase<TerrainDef>.AllDefs.Where(terrain => terrain.BuildableByPlayer);
            ThingDef material; HashSet<StuffCategoryDef> stuff;
            foreach (TerrainDef floor in floors) {
                // Fix Designation Category first if broken
                if (floor.bridge || (floor.label?.Contains("bridge") ?? false)) {
                    floor.designationCategory = BDS_DefOf.Structure;
                } else { floor.designationCategory = BDS_DefOf.Floors; }
                stuff = floor.StuffTypes();
                // Get the material the floor is made from if it's made from a single material
                if (floor.costList?.Count() == 1) { material = floor.costList.FirstOrDefault().thingDef; }
                else { material = null; }

                // ORDERING
                if (material?.defName=="Hay") {floor.uiOrder=1000f;} // Straw Matting or similar
                // Everything made with a single Stuff Category
                else if ((stuff?.Any()??false) && stuff?.Count()==1) {
                    switch(stuff.FirstOrDefault().noun) {
                        case "wood" :
                            if (material==ThingDefOf.WoodLog) {floor.uiOrder=1100f;}
                            else {floor.uiOrder=1150f;}
                            break;
                        case "fabric" :
                            if (material==ThingDefOf.Cloth) {floor.uiOrder=1200f;}
                            else if (material.IsWool) {floor.uiOrder=1220f;}
                            else {floor.uiOrder=1240f;}
                            if (!floor.IsCarpet) {floor.uiOrder+=100f;}
                            break;
                        case "leather" :
                            if (material?.IsLeather ?? false) {floor.uiOrder=1400f;}
                            else {floor.uiOrder=1450f;}
                            break;
                        case "stone" :
                            if (material?.IsWithinCategory(ThingCategoryDefOf.StoneBlocks) ?? false) {floor.uiOrder=1500f;}
                            else {floor.uiOrder=1550f;}
                            break;
                        case "metal" :
                            if (floor.defName=="Concrete" || floor.defName=="PavedTile") {floor.uiOrder=1600f;}
                            else if (floor.designatorDropdown?.defName == "Floor_Tile_Metal") {floor.uiOrder=1620;}
                            else if (material==ThingDefOf.Steel) {floor.uiOrder=1640;}
                            else if ((floor.statBases?.GetStatValueFromList(StatDefOf.Cleanliness,0f) ?? 0f) >= 0.4f) {floor.uiOrder=1660;}
                            else {floor.uiOrder=1680f;}
                            break;
                        default :
                            floor.uiOrder=1800f;
                            break;
                    }
                    // Generic uiOrder modifiers
                    if (floor.IsFine) {floor.uiOrder+=10f;}
                    floor.uiOrder+=floor.statBases?.GetStatValueFromList(StatDefOf.Beauty,0f) ?? 0f;
                    floor.uiOrder+=floor.statBases?.GetStatValueFromList(StatDefOf.Cleanliness,0f) ?? 0f;
                    floor.uiOrder+=floor.statBases?.GetStatValueFromList(StatDefOf.StyleDominance,0f) ?? 0f;
                } else { floor.uiOrder=1850f; }
            }
	    }

        // COVERINGS
        private static void FloorCoveringPatch() {
            IEnumerable<ThingDef> coverings = DefDatabase<ThingDef>.AllDefs.Where( thing => 
                    thing.BuildableByPlayer &&
                    thing.altitudeLayer==AltitudeLayer.FloorCoverings );
            
            ThingDef material; HashSet<StuffCategoryDef> stuff;
            foreach (ThingDef cover in coverings) {
                // Fix Designation Category first if broken
                cover.designationCategory = BDS_DefOf.Floors;
                stuff = cover.StuffTypes();
                // Get the material the covercovering is made from if it's made from a single material
                if (cover.costList?.Count() == 1) { material = cover.costList[0]?.thingDef; }
                else { material = null; }
                // ORDERING
                if (stuff.Any() && stuff.Count()==1) {
                    switch(stuff.FirstOrDefault().noun) {
                        case "wood" :
                            if (material==ThingDefOf.WoodLog) {cover.uiOrder=2000f;}
                            else {cover.uiOrder=2050f;}
                            break;
                        case "fabric" :
                            if (material==ThingDefOf.Cloth) {cover.uiOrder=2100f;}
                            else if (material.IsWool) {cover.uiOrder=2150f;}
                            else {cover.uiOrder=2200f;}
                            break;
                        case "leather" :
                            if (material?.IsLeather ?? false) {cover.uiOrder=2250;}
                            else {cover.uiOrder=2300;}
                            break;
                        case "stone" :
                            if (material?.IsWithinCategory(ThingCategoryDefOf.StoneBlocks) ?? false) {cover.uiOrder=2350;}
                            else {cover.uiOrder=2400;}
                            break;
                        case "metal" :
                            if (material==ThingDefOf.Steel) {cover.uiOrder=2450;}
                            else {cover.uiOrder=2500;}
                            break;
                        default :
                            cover.uiOrder=2800;
                            break;
                    }
                    // Generic uiOrder modifiers
                    cover.uiOrder+=cover.statBases?.GetStatValueFromList(StatDefOf.Beauty,0f) ?? 0f;
                    cover.uiOrder+=cover.statBases?.GetStatValueFromList(StatDefOf.Cleanliness,0f) ?? 0f;
                    cover.uiOrder+=cover.statBases?.GetStatValueFromList(StatDefOf.StyleDominance,0f) ?? 0f;
                } else { cover.uiOrder=3000f; }
            }
        }
    }
}