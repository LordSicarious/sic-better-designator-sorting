// BetterDesignatorSorting.Security
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_DefOf;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Security {
        public static void Patch() {

            // Caching Variables
            HashSet<ThingDef>   facilities = new HashSet<ThingDef>();
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility
            Dictionary<ModMetaData,float> modOffsets = new Dictionary<ModMetaData,float>();
            ModMetaData metadata;
            float modOffset = 0;

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    !thing.building.shipPart);

            foreach (ThingDef building in things) {
                comp = building.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                // ORDERING
                if (building.building.isInert && building.designationCategory == BDS_DefOf.Security) {
                    building.uiOrder = building.fillPercent * 1000f; // Security Structures like Barricades, sorted by cover effectiveness
                } else if (building.statBases.Any(stat => stat.stat == StatDefOf.TrapMeleeDamage)) { // Melee Traps
                    building.uiOrder = 1000f; 
                    building.uiOrder += building.statBases.GetStatValueFromList(StatDefOf.TrapMeleeDamage, 0f);
                    building.designationCategory = BDS_DefOf.Security;
                } else if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "IED trap") { // IED Traps
                    building.uiOrder = 1500f;
                    building.designatorDropdown = BDS_DefOf.IED_Trap;
                    building.designationCategory = BDS_DefOf.Security;
                } else if (building.building.isTrap) { // Other Traps
                    building.uiOrder = 2000f;
                    building.uiOrder += building.TotalResearchCost()/1000f;
                    building.designationCategory = BDS_DefOf.Security;
                } else if (building.RequiresResearch(BDS_DefOf.Firefoam)) { // Firefighting stuff
                    building.uiOrder = 3000f;
                    if(building.RequiresResearch(GunTurrets)) { building.uiOrder = 3500f;}
                    building.uiOrder += building.TotalResearchCost()/1000f;
                    building.designationCategory = BDS_DefOf.Security;
                    Log.Message("Patching " + building.defName + ", uiOrder = " + building.uiOrder);
                } else if (typeof(Building_TurretGun) == building.thingClass && building.building.ai_combatDangerous) { // Regular Turrets
                    building.uiOrder = 4000f;
                    building.uiOrder += building.TotalResearchCost()/1000f;
                    building.designationCategory = BDS_DefOf.Security;
                } else if (typeof(Building_TurretGun).IsAssignableFrom(building.thingClass) && building.building.ai_combatDangerous) { // Not-Quite Regular Turrets
                    building.uiOrder = 5000f;
                    building.uiOrder += building.TotalResearchCost()/1000f;
                    building.designationCategory = BDS_DefOf.Security;
                } else if (building.GetModExtension<BDS_DefModExtension>()?.buildingBase == "artillery") { // Mortars and similar
                    building.uiOrder = 6000f;
                    if (building.comps?.Any(x => x is CompProperties_Power)??false) { building.uiOrder += 1/10f; } // Powered after equivalent non-powered
                    building.uiOrder += building.TotalResearchCost()/10000f;
                    building.designationCategory = BDS_DefOf.Security;
                } else if (building.RequiresResearch(GunTurrets)) { // Weird other turret-based stuff
                    building.uiOrder = 7000f;
                    building.uiOrder += building.TotalResearchCost()/1000f;
                    building.designationCategory = BDS_DefOf.Security;
                } else if (building.designationCategory == BDS_DefOf.Security) {
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
                    building.uiOrder += building.TotalResearchCost()/1000f;
                }


                // Get Facilities
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