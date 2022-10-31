// BetterDesignatorSorting.RestPatches
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {

    public static class Rest {

        public static void Patch() {
            // Caching Variables
            HashSet<ThingDef> royalRequirements = new HashSet<ThingDef>();
            HashSet<ThingDef> facilities = new HashSet<ThingDef>();
            ThingDef SlabBedDef = DefDatabase<ThingDef>.GetNamed("SlabBed");
            ThingDef HospitalBedDef = DefDatabase<ThingDef>.GetNamed("HospitalBed");
            ThingDef material; HashSet<StuffCategoryDef> stuff; // Material Information
            CompProperties_AssignableToPawn assignProps; // Properties to get bed size
            CompProperties_Facility facilityProps; // Properties to get facility data

            // Cache everything used in a Royal Title's bedroom requirements
            foreach (RoyalTitleDef title in DefDatabase<RoyalTitleDef>.AllDefs.Where(t => t.bedroomRequirements != null)) {
                foreach (RoomRequirement req in title.bedroomRequirements.Where(r => r is RoomRequirement_ThingAnyOf)) {
                    royalRequirements.AddRange((req as RoomRequirement_ThingAnyOf).things);
                }
            }

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building_Bed).IsAssignableFrom(thing.thingClass) );

            foreach (ThingDef bed in things) {
                assignProps = bed.comps?.FirstOrDefault(x => x is CompProperties_AssignableToPawn) as CompProperties_AssignableToPawn;
                // All Beds are 100% part of Rest
                bed.designationCategory = BDS_DefOf.Rest;
                if (!bed.GetFacilities().NullOrEmpty()) { facilities.AddRange(bed.GetFacilities()); }
                stuff = bed.StuffTypes();
                // Get the material the bed is made from if it's made from a single material
                if (bed.costList?.Count() == 1) { material = bed.costList[0]?.thingDef; }
                else { material = null; }
                // MOSTLY NORMAL BEDS
                if (bed.altitudeLayer==AltitudeLayer.FloorEmplacement) { bed.uiOrder = 1000f; }
                else if (bed.building?.bed_caravansCanUse??false) { 
                    if (bed.label?.Contains("bedroll")??false) { bed.uiOrder = 2000f; } // Bedrolls
                    else { bed.uiOrder = 2000f; } // Misc Caravan Beds
                } else if (bed.isSimilar(ThingDefOf.Bed)) {
                    bed.uiOrder = 5000f; // Regular Beds
                } else if (bed.isSimilar(ThingDefOf.RoyalBed)) {
                    if (bed.label?.Contains("royal")??false) { bed.uiOrder = 7000f;} // Royal Beds
                    else { bed.uiOrder = 8000f;} // Beds that are somehow similar to Royal Beds but aren't called that
                } else if (royalRequirements.Contains(bed)) {
                    bed.uiOrder = 9000f; // Beds used by nobles that aren't Royal Beds
                } else if (bed.building.bed_slabBed) {
                    if (bed.isSimilar(SlabBedDef)) { bed.uiOrder = 10000f;} // Slab Beds with vanilla stats
                    else if (bed.label?.Contains("slab")??false) { bed.uiOrder = 11000f;} // Non-Vanilla Slab Beds with the word "slab" in them
                    else { bed.uiOrder = 12000f;} // Other Slab Beds
                } else { 
                    bed.uiOrder = 13000f; // Catchall
                    bed.uiOrder += (float)(assignProps?.maxAssignedPawnsCount??0);
                }
                // SMALL BEDS
                if ((bed.building?.bed_maxBodySize??500f) < 1f) {
                    if (bed.altitudeLayer==AltitudeLayer.FloorEmplacement) { bed.uiOrder = 14000f; } // Baby Sleeping Spot
                    else if (bed.building?.bed_crib??false) { bed.uiOrder = 14100f; } // Cribs and such
                    else { bed.uiOrder = 14500f; } // Any other small beds
                }
                // ANIMAL BEDS
                if (bed.building.bed_humanlike==false) {
                    if (bed.altitudeLayer==AltitudeLayer.FloorEmplacement) { bed.uiOrder = 15000f; } // Animal Sleeping Spot
                    else { bed.uiOrder = 15500f; } // Animal Beds
                    bed.uiOrder += Math.Min(bed.building.bed_maxBodySize, 10f); // Larger beds later
                }
                // 1600-1699 reserved for general bed facilities
                // MEDICAL BEDS
                else if (bed.statBases.Any(stat => stat.stat == StatDefOf.MedicalTendQualityOffset && stat.value > 00f)) {
                    if (bed.isSimilar(HospitalBedDef)) { bed.uiOrder = 17000f; } // Hospital Beds and similar
                    else { bed.uiOrder = 1725f; } // Other Medical Beds
                    bed.uiOrder += Math.Min(1000f*bed.statBases.GetStatValueFromList(StatDefOf.MedicalTendQualityOffset,00f), 9.99f); // Higher bonus later
                }
                else if (bed.statBases.Any(stat => stat.stat == StatDefOf.ImmunityGainSpeedFactor && stat.value >= 1.1f)) {
                    bed.uiOrder = 17500f; // Other Medical Beds with no Tend Quality offset
                    bed.uiOrder += Math.Min(1000f*bed.statBases.GetStatValueFromList(StatDefOf.ImmunityGainSpeedFactor,00f), 9.99f); // Higher bonus later
                }
                else if (bed.statBases.Any(stat => stat.stat == StatDefOf.SurgerySuccessChanceFactor && stat.value >= 1.1f)) {
                    bed.uiOrder = 1775f; // Other Medical Beds with no Immunity offset
                    bed.uiOrder += Math.Min(1000f*bed.statBases.GetStatValueFromList(StatDefOf.SurgerySuccessChanceFactor,00f), 9.99f); // Higher bonus later
                }
                //1800-1899 reserved for medical bed facilities
                // DEATHREST CASKETS
                if (bed.comps.Any(x => x is CompProperties_DeathrestBindable)) {
                    if (bed.isSimilar(ThingDefOf.DeathrestCasket)) { bed.uiOrder = 19000f; } // Vanilla Deathrest Caskets and similar
                    else { bed.uiOrder = 19500f; } // Other Deathrest Caskets
                }
                bed.uiOrder += (float)(assignProps?.maxAssignedPawnsCount??0);
            }

            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facility.designationCategory = BDS_DefOf.Rest;
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                if (facilityProps?.statOffsets.Any()??false) {
                    if (facilityProps.statOffsets.Any(stat => stat.stat == StatDefOf.Comfort)) {
                        facility.uiOrder = 16000f; // Regular Comfort Facilities
                        facility.uiOrder += facilityProps.statOffsets.GetStatValueFromList(StatDefOf.Comfort,0f);
                    } else { facility.uiOrder = 16500f; }
                    // MEDICAL BED FACILITIES
                    if (facilityProps.statOffsets.Any(stat => stat.stat == StatDefOf.MedicalTendQualityOffset && stat.value > 0f)) {
                        facility.uiOrder = 18000f; // Tend Quality
                        facility.uiOrder += facilityProps.statOffsets.GetStatValueFromList(StatDefOf.MedicalTendQualityOffset,0f);    
                    } else if (facilityProps.statOffsets.Any(stat => stat.stat == StatDefOf.ImmunityGainSpeedFactor && stat.value > 0f)) {
                        facility.uiOrder = 18100f; // Immunity Gain
                        facility.uiOrder += facilityProps.statOffsets.GetStatValueFromList(StatDefOf.ImmunityGainSpeedFactor,0f);
                    } else if (facilityProps.statOffsets.Any(stat => stat.stat == StatDefOf.SurgerySuccessChanceFactor && stat.value > 0f)) {
                        facility.uiOrder = 18100f; // Surgery Success Chance
                        facility.uiOrder += facilityProps.statOffsets.GetStatValueFromList(StatDefOf.SurgerySuccessChanceFactor,0f);
                    } 
                } else { facility.uiOrder = 16999; }
            }

            HashSet<ThingDef> deathrestFacilities = new HashSet<ThingDef>();
            deathrestFacilities.AddRange(DefDatabase<ThingDef>.AllDefs.Where(
                thing => thing.BuildableByPlayer &&
                thing.comps.Any(x => x is CompProperties_DeathrestBindable) &&
                !(thing.thingClass == typeof(Building_Bed))));
            foreach (ThingDef facility in deathrestFacilities) { 
                facility.designationCategory = BDS_DefOf.Rest;
                facility.uiOrder = 20000f;
            }
        }

        private static bool isSimilar(this ThingDef bed1, ThingDef bed2) {
            bool result = true;
            result &= (bed1.statBases?.GetStatValueFromList(StatDefOf.BedRestEffectiveness,0)??0) == (bed2.statBases?.GetStatValueFromList(StatDefOf.BedRestEffectiveness,0)??0);
            Log.Message("Are " + bed1.defName + " and " + bed2.defName + " similar? " + result);
            result &= (bed1.statBases?.GetStatValueFromList(StatDefOf.Comfort,0)??0) == (bed2.statBases?.GetStatValueFromList(StatDefOf.Comfort,0)??0);
            Log.Message("Are " + bed1.defName + " and " + bed2.defName + " similar? " + result);
            if (bed1.researchPrerequisites.NullOrEmpty() || bed2.researchPrerequisites.NullOrEmpty())
                { result &= (bed1.researchPrerequisites.NullOrEmpty() && bed2.researchPrerequisites.NullOrEmpty()); }
            else if (bed1.researchPrerequisites.Any(r => !bed2.researchPrerequisites.Contains(r))) { result = false; }
            else if (bed2.researchPrerequisites.Any(r => !bed1.researchPrerequisites.Contains(r))) { result = false; }
            Log.Message("Are " + bed1.defName + " and " + bed2.defName + " similar? " + result);
            return result;
        }
    }
}