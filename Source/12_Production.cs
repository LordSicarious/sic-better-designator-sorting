// BetterDesignatorSorting.Production
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Production {

        public static void Patch() {

            // Caching Variables
            HashSet<ThingDef>   facilities = new HashSet<ThingDef>(),
                                haulBuildings = new HashSet<ThingDef>(),
                                craftBuildings = new HashSet<ThingDef>(),
                                artBuildings = new HashSet<ThingDef>(),
                                tailorBuildings = new HashSet<ThingDef>(),
                                smithBuildings = new HashSet<ThingDef>();
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility
            List<List<RecipeDef>> recipeLists = new List<List<RecipeDef>>();
            bool sameRecipes;

            // Cache fixed BillGivers
            foreach (WorkGiverDef giver in DefDatabase<WorkGiverDef>.AllDefs) {
                if (giver.workType == null || giver.fixedBillGiverDefs.NullOrEmpty()) { continue; }
                else if (giver.workType == WorkTypeDefOf.Hauling) { haulBuildings.AddRange(giver.fixedBillGiverDefs); }
                else if (giver.workType == WorkTypeDefOf.Crafting) { craftBuildings.AddRange(giver.fixedBillGiverDefs); }
                else if (giver.workType == WorkTypeDefOf.Art) { artBuildings.AddRange(giver.fixedBillGiverDefs); }
                else if (giver.workType == BDS_DefOf.Tailoring) { tailorBuildings.AddRange(giver.fixedBillGiverDefs); }
                else if (giver.workType == WorkTypeDefOf.Smithing) { smithBuildings.AddRange(giver.fixedBillGiverDefs); }
            }
            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass) &&
                    thing.designationCategory != BDS_DefOf.Temperature &&
                    thing.designationCategory != BDS_DefOf.Technology &&
                    thing.designationCategory != BDS_DefOf.Science &&
                    thing.designationCategory != BDS_DefOf.Food  ); // don't poach workbenches from food, but facilities are fine


            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                // ORDERING
                int i = 0;
                if (craftBuildings.Contains(building)) {  // Generic Crafting Workbenches
                    i++;
                    building.uiOrder = 200f;
                }
                if (artBuildings.Contains(building))
                    { building.uiOrder = 300f; i++; } // Art Workbenches
                if (tailorBuildings.Contains(building))
                    { building.uiOrder = 400f; i++; } // Tailoring Workbenches
                if (smithBuildings.Contains(building)) { 
                    i++;
                    if (!building.RequiresResearch(BDS_DefOf.Smithing)){ // Not(Smithing)
                        building.uiOrder = 800f; // Non-Vanilla research-based Smithing Workbenches
                    } else if (!building.RequiresResearch(BDS_DefOf.Machining)) { // Smithing && Not(Machining)
                        building.uiOrder = 500f; // Smithies
                    } else if (!building.RequiresResearch(BDS_DefOf.Fabrication)) { // Machining && Not(Fabrication)
                        building.uiOrder = 600f;  // Machining Workbenches
                    } else { // Fabrication
                        building.uiOrder = 700f; // Fabrication Workbenches
                    }
                }
                if (haulBuildings.Contains(building))
                    { building.uiOrder = 900f; i++; } // Hauling  Workbenches like Crematorium

                if (i > 1) { building.uiOrder = 1000f; } // Multi-Worktype Workbenches

                // Is a Production workbench
                if (i >= 1) {
                    
                    building.designationCategory = BDS_DefOf.Production;
                    
                    // Buildings with the same recipes should be grouped together
                    sameRecipes = false;
                    foreach (List<RecipeDef> list in recipeLists) {
                        if(building.AllRecipes?.Intersects(list)??false) {
                            building.uiOrder += recipeLists.IndexOf(list)/10f;
                            sameRecipes = true;
                            break;
                        }
                    }
                    if (!sameRecipes) { recipeLists.Add(building.AllRecipes); }

                    building.uiOrder += (float)(building.size.x * building.size.z)/10000f;
                    if (building.comps?.Any(x => x is CompProperties_Refuelable)??false) { building.uiOrder += 1/100f; }
                    if (building.comps?.Any(x => x is CompProperties_Power)??false) { building.uiOrder += 1/1000f; }

                    if (building.altitudeLayer == AltitudeLayer.FloorEmplacement) { building.uiOrder = 100f; } // Spots go at the start
                }

                // Get Facilities
                if (building.designationCategory==BDS_DefOf.Production) {
                    if (!building.GetFacilities().NullOrEmpty()) { facilities.AddRange(building.GetFacilities()); }
                }
            }

            // Add Facilities
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                facility.designationCategory = BDS_DefOf.Production;
                if (facilityProps.linkableBuildings.NullOrEmpty()) { continue; }
                List<ThingDef> linked = facilityProps.linkableBuildings;

                if (linked.All(building => building.altitudeLayer == AltitudeLayer.FloorEmplacement))
                    { facility.uiOrder = 199f; } // Spot-specific Facilities
                else if (linked.All(building => haulBuildings.Contains(building)))
                    { facility.uiOrder = 299f; } // Hauling-specific Facilities
                else if (linked.All(building => craftBuildings.Contains(building)))
                    { facility.uiOrder = 399f; } // Crafting-specific Facilities
                else if (linked.All(building => artBuildings.Contains(building)))
                    { facility.uiOrder = 499f; } // Art-specific Facilities
                else if (linked.All(building => tailorBuildings.Contains(building)))
                    { facility.uiOrder = 599f; } // Tailoring-specific Facilities
                else if (linked.All(building => smithBuildings.Contains(building))) {
                    if (linked.None(building => building.RequiresResearch(BDS_DefOf.Smithing))) // Not(Smithing)
                        { facility.uiOrder = 999f; } // Non-Vanilla research-based Smith work Facilities
                    else if (linked.None(building => building.RequiresResearch(BDS_DefOf.Machining))) //Smithing && Not(Machining)
                        { facility.uiOrder = 699f; } // Smithing-specific Facilities
                    else if (linked.None(building => building.RequiresResearch(BDS_DefOf.Fabrication))) // Machining && Not(Fabrication)
                        { facility.uiOrder = 799f; } // Machining-specific Facilities
                    else { facility.uiOrder = 899f; } // Fabrication-specific Facilities
                } else if (linked.None(building => building.AllRecipes.NullOrEmpty()))
                    { facility.uiOrder = 1099f; } // Multi-Workbench Facilities
                else { facility.uiOrder = 1199f; }
            }
        }
    }
}