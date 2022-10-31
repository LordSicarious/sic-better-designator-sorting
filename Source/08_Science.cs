// BetterDesignatorSorting.Science
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Science {
        public static void Patch() {

            // Caching Variables
            HashSet<ThingDef>   facilities = new HashSet<ThingDef>(),
                                scienceBenches = new HashSet<ThingDef>(),
                                scanners = new HashSet<ThingDef>();
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility
            List<List<RecipeDef>> recipeLists = new List<List<RecipeDef>>();
            bool sameRecipes;

            // Cache fixed BillGivers
            foreach (WorkGiverDef giver in DefDatabase<WorkGiverDef>.AllDefs) {
                if (giver.workType?.relevantSkills?.Contains(SkillDefOf.Intellectual)??false) {
                    if (!giver.fixedBillGiverDefs.NullOrEmpty()) { scienceBenches.AddRange(giver.fixedBillGiverDefs); }
                    else if(giver.scannerDef != null) { scanners.Add(giver.scannerDef); }
                }
            }

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass));

            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;

                // ORDERING
                if (typeof(Building_ResearchBench).IsAssignableFrom(building.thingClass)) { // Research Benches
                    building.uiOrder = 200f;
                    building.designationCategory = BDS_DefOf.Science;
                }
                else if (scanners.Contains(building)) { // Scanners Buildings
                    building.uiOrder = 400f;
                    building.designationCategory = BDS_DefOf.Science;
                }
                else if (building.AllRecipes?.Any(recipe => recipe.workSkill == SkillDefOf.Intellectual)??false) { // Intellectual Work Benches
                    if (building.AllRecipes.Where(recipe => recipe.workSkill == SkillDefOf.Intellectual).Count()
                            >= building.AllRecipes.Where(recipe => recipe.workSkill != SkillDefOf.Intellectual).Count()) {
                        building.uiOrder = 600f;
                        building.designationCategory = BDS_DefOf.Science;
                        sameRecipes = false;
                        foreach (List<RecipeDef> list in recipeLists) {
                            if(building.AllRecipes?.Intersects(list)??false) {
                                building.uiOrder += recipeLists.IndexOf(list); // Separate Intellectual Work Benches by shared recipes
                                sameRecipes = true;
                                break;
                            }
                        }
                        if (!sameRecipes) { recipeLists.Add(building.AllRecipes); }
                    }
                }
                else if (scienceBenches.Contains(building)) { // Other Research BillGivers
                    building.uiOrder = 800f;
                    building.designationCategory = BDS_DefOf.Science;
                }
                if (building.RequiresResearch(BDS_DefOf.Xenogermination)) { // Biotech Genetics Buildings
                    building.uiOrder = 1000f;
                    building.designationCategory = BDS_DefOf.Science;
                }


                // General Modifiers
                if (building.designationCategory==BDS_DefOf.Science) {
                    if (building.altitudeLayer == AltitudeLayer.FloorEmplacement) { building.uiOrder = 100f; } // Spots go at the start
                    if (building.comps?.Any(x => x is CompProperties_Refuelable)??false) { building.uiOrder += 1/1000f; }
                    if (building.comps?.Any(x => x is CompProperties_Power)??false) { building.uiOrder += 1/100f; }

                    // Get Facilities
                    if (!building.GetFacilities().NullOrEmpty()) { facilities.AddRange(building.GetFacilities()); }
                }
            }

            // Add Facilities
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                if (facilityProps.linkableBuildings.NullOrEmpty()) { continue; }
                List<ThingDef> linked = facilityProps.linkableBuildings;
                if (linked.All(building => building.designationCategory == BDS_DefOf.Science)) {
                    facility.designationCategory = BDS_DefOf.Science;
                } else { continue; }
                
                if (linked.Count() > 1) {
                    float maxOrder = -1f;
                    foreach (ThingDef building in linked) {
                        maxOrder = Math.Max(maxOrder, building.uiOrder);
                    }
                    facility.uiOrder = maxOrder + 100f;
                } else { facility.uiOrder = linked.FirstOrDefault().uiOrder + 1f; }
                if (facility.comps?.Any(x => x is CompProperties_Refuelable)??false) { facility.uiOrder += 1/10f; }
                if (facility.comps?.Any(x => x is CompProperties_Power)??false) { facility.uiOrder += 1/100f; }
            }
        }
    }
}