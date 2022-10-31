// BetterDesignatorSorting.Recreation
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Recreation {
        public static void Patch() {
            // Caching Variables
            HashSet<ThingDef>   facilities = new HashSet<ThingDef>();
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility
            int joyKindCount = DefDatabase<JoyKindDef>.AllDefs.Count();

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass) &&
                    thing.statBases.Any(stat => stat.stat == StatDefOf.JoyGainFactor || stat.stat == StatDefOf.BabyPlayGainFactor));

            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                building.designationCategory = BDS_DefOf.Joy;

                // ORDERING
                int i = 0; bool hasJoyKind = false;
                foreach (JoyKindDef joy in DefDatabase<JoyKindDef>.AllDefs) {
                    i++;
                    if (building.building.joyKind == joy) {
                        building.uiOrder = 200*i; // Order by Joy Type
                        hasJoyKind = true;
                        break;
                    }
                }
                if (!hasJoyKind) {
                    building.uiOrder = 200*(joyKindCount+1); // No Joy Type, but has Recreation Power
                }
                building.uiOrder += building.statBases.GetStatValueFromList(StatDefOf.JoyGainFactor, 0f) * 10; // Rank within category by Recreation Power

                if (building.statBases.Any(stat => stat.stat == StatDefOf.BabyPlayGainFactor)) {
                    building.uiOrder = 200*(joyKindCount+2); // Baby Toys
                }
                building.uiOrder += building.statBases.GetStatValueFromList(StatDefOf.BabyPlayGainFactor, 0f) * 10; // Rank Baby Toys by Play Power

                // General Modifiers
                if (building.comps?.Any(x => x is CompProperties_Refuelable)??false) { building.uiOrder += 1/100f; } // Fuelled after equivalent non-fuelled
                if (building.comps?.Any(x => x is CompProperties_Power)??false) { building.uiOrder += 1/10f; } // Powered after equivalent non-powered

                // Add to Facilities
                if (!building.GetFacilities().NullOrEmpty()) { facilities.AddRange(building.GetFacilities()); }
            }

            // Add Facilities
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                if (facilityProps.linkableBuildings.NullOrEmpty()) { continue; }
                List<ThingDef> linked = facilityProps.linkableBuildings;
                if (linked.All(building => building.designationCategory == BDS_DefOf.Joy)) {
                    facility.designationCategory = BDS_DefOf.Joy;
                } else { continue; }
                
                if (linked.Count() > 1) {
                    float maxOrder = -1f;
                    foreach (ThingDef building in linked) {
                        maxOrder = Math.Max(maxOrder, building.uiOrder);
                    }
                    facility.uiOrder = maxOrder + 100f;
                } else { facility.uiOrder = linked.FirstOrDefault().uiOrder + 1f; }
                if (facility.comps?.Any(x => x is CompProperties_Refuelable)??false) { facility.uiOrder += 1/100f; }
                if (facility.comps?.Any(x => x is CompProperties_Power)??false) { facility.uiOrder += 1/10f; }
            }
        }
    }
}