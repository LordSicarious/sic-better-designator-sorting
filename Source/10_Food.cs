// BetterDesignatorSorting.Food
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Food {
        public static void Patch() {
            // Caching Variables
            HashSet<ThingDef> facilities = new HashSet<ThingDef>();
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass) &&
                    thing.designationCategory != BDS_DefOf.Temperature );



            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                // ORDERING

                // Butchering 100-199
                if (building.AllRecipes?.Contains(BDS_DefOf.ButcherCorpseFlesh)??false) {
                    building.designationCategory = BDS_DefOf.Food;
                    if (building.altitudeLayer == AltitudeLayer.FloorEmplacement) { building.uiOrder = 100f; }
                    if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "workbench") {
                        if (!building.comps?.Any(x => x is CompProperties_Power)??false) { building.uiOrder = 120f; } // Butcher Tables
                        else { building.uiOrder = 150f; } // Electric Stoves
                        building.uiOrder += (float)(building.size.x * building.size.z);
                    }
                }

                // Butchering Facilities 200-299

                // Meal Sources 300-399
                if (building.building.isMealSource) {
                    building.designationCategory = BDS_DefOf.Food;
                    if (building.altitudeLayer == AltitudeLayer.FloorEmplacement) { building.uiOrder = 300f; }
                    else if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "workbench") {
                        if (building.comps?.Any(x => x is CompProperties_Refuelable)??false) { building.uiOrder = 320f; } // Fuelled Stoves
                        else if (building.comps?.Any(x => x is CompProperties_Power)??false) { building.uiOrder = 330f; } // Electric Stoves
                        else { building.uiOrder = 340f; } // Weird Stoves
                        if (!building.AllRecipes?.Contains(RecipeDefOf.CookMealSimple)??false) { building.uiOrder += 30f; } // Stoves that can't cook simple meals
                        building.uiOrder += (float)(building.size.x * building.size.z);
                    }
                }

                // General Cooking Facilities 400-499

                // Hopper-Based Processors 500-699
                if (building.building.wantsHopperAdjacent) {
                    building.designationCategory = BDS_DefOf.Food;
                    if (typeof(Building_NutrientPasteDispenser).IsAssignableFrom(building.thingClass)) 
                        { building.uiOrder = 500f; } // Nutrient Paste Dispensers
                    else { building.uiOrder = 550f; } // Non-Nutrient Paste Dispensers
                    if (!building.building.isMealSource) { building.uiOrder += 100f; } // Things that don't produce actual meals
                    building.uiOrder += (float)(building.size.x * building.size.z);
                }
                if (building.placeWorkers?.Contains(typeof(PlaceWorker_NextToHopperAccepter))??false) {
                    building.designationCategory = BDS_DefOf.Food;
                    building.uiOrder = 690f;
                }

                // Brewing 700-899
                if (building.RequiresResearch(BDS_DefOf.Brewing)) {
                    building.designationCategory = BDS_DefOf.Food;
                    if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "workbench") { building.uiOrder = 700f; }
                    else if (!building.AllRecipes?.NullOrEmpty()??false) { building.uiOrder = 750f; }
                    else { building.uiOrder = 800f; }
                }

                // Brewing Facilities 900-999

                // Planting
                if (typeof(Building_PlantGrower).IsAssignableFrom(building.thingClass) &&
                    !building.building.sowTag.Contains("Decor")) { // Avoids Decorative Plants
                    building.designationCategory = BDS_DefOf.Food;
                    building.uiOrder = 1200f;
                    building.uiOrder += building.TotalResearchCost()/100f;
                }

                // Get Facilities
                if (building.designationCategory==BDS_DefOf.Food) {
                    //Log.Message(building.defName + building.uiOrder);
                    if (!building.GetFacilities().NullOrEmpty()) { facilities.AddRange(building.GetFacilities()); }
                }
            }

            // Add Facilities
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                if (facilityProps?.linkableBuildings?.Any()??false) {
                    List<ThingDef> linked = facilityProps.linkableBuildings;
                    // General Cooking Facilities
                    if (linked.All(building => building.AllRecipes?.Contains(RecipeDefOf.CookMealSimple)??false)) {
                        facility.designationCategory = BDS_DefOf.Food;
                        facility.uiOrder = 400f; // Regular Cooking Facilities
                    }
                    else if (linked.All(building => building.building.isMealSource)) {
                        facility.designationCategory = BDS_DefOf.Food;
                        facility.uiOrder = 450f; // Facilities for misc. food sources
                    }
                    // Butcher Facilities
                    else if (linked.All(building => building.AllRecipes?.Contains(BDS_DefOf.ButcherCorpseFlesh)??false)) {
                        facility.designationCategory = BDS_DefOf.Food;
                        facility.uiOrder = 200f; // Butcher Facilities, out of order to give general cooking priority
                    }
                    // Brewing Facilities
                    else if (linked.All(building => building.RequiresResearch(BDS_DefOf.Brewing))) {
                        facility.designationCategory = BDS_DefOf.Food;
                        facility.uiOrder = 900f; // Brewing Facilities
                    }
                    else {
                        facility.designationCategory = BDS_DefOf.Food;
                        facility.uiOrder = 1000f; // Misc other Food-related facilities
                    }
                    if (linked.Any(building => typeof(Building_PlantGrower).IsAssignableFrom(building.thingClass))) {
                        facility.uiOrder = 1400f;
                    }
                    if (facility.comps?.Any(x => x is CompProperties_Power)??false) { facility.uiOrder += 20f; }
                    facility.uiOrder += facility.TotalResearchCost()/1000f;
                }
            }
        }
    }
}