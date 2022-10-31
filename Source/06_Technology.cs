// BetterDesignatorSorting.Technology
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_DefOf;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Technology {
        public static void Patch() {

            // Caching Variables
            HashSet<ThingDef>   facilities = new HashSet<ThingDef>();
            Dictionary<ThingDef,float> ideoTech = new Dictionary<ThingDef,float>();
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility
            Dictionary<ModMetaData,float> modOffsets = new Dictionary<ModMetaData,float>();
            ModMetaData metadata;
            float modOffset = 0;

            float offset = 0;
            foreach (MemeDef meme in DefDatabase<MemeDef>.AllDefs) {
                if(!meme.AllDesignatorBuildables.NullOrEmpty()) {
                    foreach (BuildableDef building in meme.AllDesignatorBuildables) {
                        if (building is ThingDef) { ideoTech.Add(building as ThingDef, offset); }
                    }
                    offset += 1f;
                }
            }
            foreach (ThingDef key in ideoTech.Keys.ToList()) {
                ideoTech[key] *= 200f/(offset);
            }

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass) &&
                    !thing.building.shipPart &&
                    (thing.designationCategory == BDS_DefOf.Misc || thing.designationCategory == BDS_DefOf.Technology) &&
                    thing.RequiresResearch(Electricity));

            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                building.designationCategory = BDS_DefOf.Technology;

                bool set = false;

                // ORDERING
                // Communication Technologies
                if (building.comps?.Any(x => x is CompProperties_ShipLandingBeacon)??false) {
                    building.uiOrder = 1000f; // Ship Beacons
                    set = true;
                } else if (typeof(Building_OrbitalTradeBeacon).IsAssignableFrom(building.thingClass)) {
                    building.uiOrder = 1500f; // Trade Beacons
                    set = true;
                } else if (typeof(Building_CommsConsole).IsAssignableFrom(building.thingClass)) {
                    building.uiOrder = 2000f;
                    set = true;
                }
                // Pumps
                if (building.defName.Contains("Pump")) {
                    building.uiOrder = 3000f;
                    set = true;
                }
                // Transportation
                if (building.RequiresResearch(TransportPod)) {
                    building.uiOrder = 4000f;
                    set = true;
                }
                // Cryptosleep Casket
                if (typeof(Building_CryptosleepCasket).IsAssignableFrom(building.thingClass)) {
                    building.uiOrder = 5000f;
                    set = true;
                }
                // Biosculpter Pod
                if (building.RequiresResearch(Biosculpting)) {
                    building.uiOrder = 6000f;
                    set = true;
                }
                // Ideoligious Technology Buildings
                if (building.ritualFocus != null) {
                    building.uiOrder = 7000f; // Ritual Foci
                    set = true;
                } else if (ideoTech.ContainsKey(building)) {
                    building.uiOrder = 8000f; // Useful stuff unlocked by Ideoligions
                    building.uiOrder += ideoTech[building];
                    set = true;
                }
                // Mechanitor Stuff
                if (building.RequiresResearch(BasicMechtech)) {
                    if (typeof(Building_MechGestator).IsAssignableFrom(building.thingClass)) {
                        building.uiOrder = 9000f; // Mech Gestators
                    } else if (!building.AllRecipes.NullOrEmpty()) { 
                        building.uiOrder = 10000f; // General Mechanitor Workbenches
                        if (building.label.Contains("subcore")) { building.uiOrder = 11000f;}
                    } else if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "subcore scanner") {
                        building.uiOrder = 12000f;
                    } else if (typeof(Building_MechCharger).IsAssignableFrom(building.thingClass)) {
                        building.uiOrder = 13000f; // Rechargers
                    } else if (building.comps?.Any(x => x is CompProperties_Useable_CallBossgroup)??false) {
                        building.uiOrder = 14000f; // Mech Callers
                    } else { building.uiOrder = 15000f; } // Other Mechanitor Shit
                    set = true;
                }

                // Modded Tech Stuff
                if (!set) {
                    if (building.modContentPack == null) {
                        building.uiOrder = 20000f;
                    } else {
                        metadata = building.modContentPack.ModMetaData;
                        building.uiOrder = 100000f;
                        try { building.uiOrder += modOffsets[metadata]; }
                        catch (KeyNotFoundException) {
                            if (modOffsets.Keys.Any(mod => metadata.Dependencies?.Any(dep => dep.packageId == mod.PackageId)??false)) {
                                float dependentOffset = modOffsets[modOffsets.Keys.First(mod => metadata.Dependencies.Any(dep => dep.packageId == mod.PackageId))];
                                while (modOffsets.Values.Contains(dependentOffset)) { dependentOffset += 100f; } // Dependent Mods are offset by intervals of 100 from base mod
                                building.uiOrder += dependentOffset;
                                modOffsets.Add(metadata, dependentOffset);
                            } else {
                                building.uiOrder += modOffset;
                                modOffsets.Add(metadata, modOffset);
                                modOffset += 100000f; // Mods are offset from each other by 100,000 to allow for up to 1000 mods based on the one dependency
                            }
                        }
                    }
                }

                building.uiOrder += building.TotalResearchCost() / 1000f;

                // Get Facilities
                if (!building.GetFacilities().NullOrEmpty()) { facilities.AddRange(building.GetFacilities()); }
            }

            // Add Facilities
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                if (facilityProps.linkableBuildings.NullOrEmpty()) { continue; }
                List<ThingDef> linked = facilityProps.linkableBuildings;
                if (linked.All(building => building.designationCategory == BDS_DefOf.Technology)) {
                    facility.designationCategory = BDS_DefOf.Technology;
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