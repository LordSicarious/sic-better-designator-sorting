// BetterDesignatorSorting.Misc
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Misc {
        public static void Patch() {
            HashSet<ThingDef>   facilities = new HashSet<ThingDef>(), // List of Facilities
                                altarBoosters = new HashSet<ThingDef>(); // Buildings that boost Altars
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility
            Dictionary<ModMetaData,float> modOffsets = new Dictionary<ModMetaData,float>();
            ModMetaData metadata;
            float modOffset = 0;

            foreach (ThingDef altar in DefDatabase<ThingDef>.AllDefs.Where(thing => thing.isAltar)) {
                foreach (ThingDef booster in altar.building.relatedBuildCommands) {
                    altarBoosters.Add(booster);
                }
            }

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    thing.designationCategory == BDS_DefOf.Misc );

            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;

                // ORDERING
                // Spots
                if (building.altitudeLayer == AltitudeLayer.FloorEmplacement &&
                    building.defName.Contains("Spot")) { // Graves are also Floor Emplacements, so need to be specific
                    building.uiOrder = 0f;
                // Pen Stuff
                } else if (building.comps.Any(x => x is CompProperties_AnimalPenMarker)) {
                    building.uiOrder = 1000f; // Pen Markers
                } else if (building.comps.Any(x => typeof(CompEggContainer).IsAssignableFrom(x.compClass))) {
                    building.uiOrder = 1500f; // Egg Box
                // Graves
                } else if (typeof(Building_CorpseCasket).IsAssignableFrom(building.thingClass)) {
                    building.uiOrder = 2000f; // Corpse Containers
                    building.uiOrder += (float)(building.size.x * building.size.z);
                    if(building.altitudeLayer != AltitudeLayer.FloorEmplacement) { building.uiOrder += 200f;}
                    if(building.statBases.Any(stat => stat.stat == StatDefOf.TerrorSource)) { building.uiOrder += 800f;}
                // Terror Sources
                } else if (building.statBases.Any(stat => stat.stat == StatDefOf.TerrorSource)) {
                    building.uiOrder = 3500f;
                    building.uiOrder += building.statBases.GetStatValueFromList(StatDefOf.TerrorSource, 0f);
                // Ritual stuff
                } else if (building.comps.Any(x => typeof(CompRelicContainer).IsAssignableFrom(x.compClass))) {
                    building.uiOrder = 4000f;
                    building.uiOrder += (float)(building.size.x * building.size.z);
                } else if (altarBoosters.Contains(building)) {
                    building.uiOrder = 4500f; // Altar Boosters
                    building.uiOrder += (float)(building.size.x * building.size.z);
                } else if (building.isAltar) {
                    building.uiOrder = 5000f; // Altars
                    building.uiOrder += (float)(building.size.x * building.size.z);
                } else if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "ideo building") {
                    building.uiOrder = 5500f; // Non-Consumable Ritual Foci
                    building.uiOrder += (float)(building.size.x * building.size.z);
                } else if (building.building.buildingTags.Contains("RitualFocus")) {
                    building.uiOrder = 6000f; // Non-Consumable Ritual Foci
                    building.uiOrder += (float)(building.size.x * building.size.z);
                } else if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "ideo consumable") {
                    building.uiOrder = 6500f; // Consumable Ritual Foci
                    building.uiOrder += (float)(building.size.x * building.size.z);
                // Meditation Sources
                } else if (building.comps.Any(x => x is CompProperties_MeditationFocus)) {
                    building.uiOrder = 7000f;
                    int i = 0, n = DefDatabase<MeditationFocusDef>.AllDefs.Count();
                    foreach (MeditationFocusDef focus in DefDatabase<MeditationFocusDef>.AllDefs) {
                        if ((building.comps.Any(x => (x as CompProperties_MeditationFocus)?.focusTypes.Contains(focus)??false))) {
                            building.uiOrder += i*1000/(float)n;
                        }
                        i++;
                    }
                    building.uiOrder += (float)(building.size.x * building.size.z);
                // Catch All
                } else {
                    if (building.modContentPack == null) {
                        building.uiOrder = 10000f;
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

                building.uiOrder += building.TotalResearchCost()/100f;
                // Add to Facilities
                if (!building.GetFacilities().NullOrEmpty()) { facilities.AddRange(building.GetFacilities()); }
            }

            // Add Facilities
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                if (facilityProps.linkableBuildings.NullOrEmpty()) { continue; }
                List<ThingDef> linked = facilityProps.linkableBuildings;
                if (linked.All(building => building.designationCategory == BDS_DefOf.Security)) {
                    facility.designationCategory = BDS_DefOf.Security;
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