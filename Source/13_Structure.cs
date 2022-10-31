// BetterDesignatorSorting.Structure
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Structure {
        public static void Patch() {
            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass) &&
                    thing.designationCategory != BDS_DefOf.Temperature );

            Log.Message("Patching " + things.Count() + " possible Structures");

            ThingDef material; HashSet<StuffCategoryDef> stuff; // Material Information
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility

            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                stuff = building.StuffTypes();
                // Get the material the building is made from if it's made from a single material
                if (building.costList?.Count() == 1) { material = building.costList[0]?.thingDef; }
                else { material = null; }

                // ORDERING


                // Add to Facilities
                if (building.designationCategory==BDS_DefOf.Food) {
                    // Log.Message(furniture.defName + furniture.uiOrder);
                    if (!furniture.GetFacilities().NullOrEmpty()) { facilities.AddRange(furniture.GetFacilities()); }
                }
            }
        }
    }
}