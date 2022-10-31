// BetterDesignatorSorting.Furniture
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using static BetterDesignatorSorting.BDS_Utility;
using static RimWorld.StatUtility;

namespace BetterDesignatorSorting {
    public static class Furniture {

        public static void Patch() {
            // Caching Variables
            HashSet<ThingDef> royalRequirements = new HashSet<ThingDef>();
            HashSet<ThingDef> facilities = new HashSet<ThingDef>();
            ThingDef BlackboardDef = DefDatabase<ThingDef>.GetNamed("Blackboard");
            ThingDef SchoolDeskDef = DefDatabase<ThingDef>.GetNamed("SchoolDesk");
            ThingDef material; HashSet<StuffCategoryDef> stuff; // Material Information
            CompProperties_AffectedByFacilities comp; // Get Facilities from a building
            CompProperties_Facility facilityProps; // Get properties from a facility

            // Cache everything used in a Royal Title's throne room requirements
            foreach (RoyalTitleDef title in DefDatabase<RoyalTitleDef>.AllDefs.Where(t => t.throneRoomRequirements != null)) {
                foreach (RoomRequirement req in title.throneRoomRequirements) {
                    if (req is RoomRequirement_Thing) { royalRequirements.Add((req as RoomRequirement_Thing).thingDef); }
                    else if (req is RoomRequirement_ThingAnyOf) { royalRequirements.AddRange((req as RoomRequirement_ThingAnyOf).things); }
                }
            }

            // Selection Criteria
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where( thing =>
                    thing.BuildableByPlayer &&
                    typeof(Building).IsAssignableFrom(thing.thingClass) &&
                    thing.designationCategory != BDS_DefOf.Joy &&
                    thing.designationCategory != BDS_DefOf.Rest &&
                    thing.designationCategory != BDS_DefOf.Food );

            
            foreach (ThingDef furniture in things) {
                comp = furniture.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
                stuff = furniture.StuffTypes();
                // Get the material the furniture is made from if it's made from a single material
                if (furniture.costList?.Count() == 1) { material = furniture.costList[0]?.thingDef; }
                else { material = null; }
                // ORDERING
                if (furniture.IsTable) { // TABLES
                    furniture.designationCategory = BDS_DefOf.Furniture;
                    if (furniture.altitudeLayer==AltitudeLayer.FloorEmplacement) { furniture.uiOrder = 50f; }
                    else if (material==null) { furniture.uiOrder = 100f; }
                    else { furniture.uiOrder = 150f; }
                    furniture.uiOrder += (float)(furniture.size.x * furniture.size.z);
                }
                // CHAIRS
                else if (furniture.building.isSittable) {
                    furniture.designationCategory = BDS_DefOf.Furniture;
                    furniture.uiOrder = 200f; // Regular Chairs
                    if (furniture.building.buildingTags.Contains("RitualSeat")) { furniture.uiOrder = 220f; } // Ritual Seating
                    if (typeof(Building_Throne).IsAssignableFrom(furniture.thingClass)) { furniture.uiOrder = 240f; } // Thrones
                    else if (royalRequirements.Contains(furniture)) { furniture.uiOrder = 260f;} // Other Royal Seating
                    else if (furniture.comps.Any(x => x is CompProperties_MeditationFocus)) { furniture.uiOrder = 300f; } // Other Meditation Seating
                }
                // STORAGE
                else if (typeof(Building_Storage).IsAssignableFrom(furniture.thingClass)) {
                    furniture.designationCategory = BDS_DefOf.Furniture;
                    if (furniture.GetModExtension<BDS_DefModExtension>()?.buildingBase == "shelf")
                        { furniture. uiOrder = 400f; } // Shelves
                    else { furniture.uiOrder = 450f; } // Non-Shelves
                    furniture.uiOrder += (float)(furniture.size.x * furniture.size.z);
                }
                // LIGHTING
                else if (furniture.comps.Any(x => x is CompProperties_Glower)) {
                    if (furniture.GetModExtension<BDS_DefModExtension>()?.buildingBase != null) {
                        switch(furniture.GetModExtension<BDS_DefModExtension>().buildingBase) {
                            case "torch" :
                                furniture.uiOrder = 500f; // Torches
                                furniture.designationCategory = BDS_DefOf.Furniture;
                                break;
                            case "brazier" :
                                furniture.uiOrder = 510f; // Braziers
                                furniture.designationCategory = BDS_DefOf.Furniture;
                                break;
                            case "lamp" :
                                furniture.uiOrder = 520f; // Lamps
                                furniture.designationCategory = BDS_DefOf.Furniture;
                                break;
                            case "furniture" :
                                furniture.uiOrder = 530f; // Other Furniture
                                furniture.designationCategory = BDS_DefOf.Furniture;
                                break;
                            default :
                                if (furniture.designationCategory==BDS_DefOf.Furniture) { furniture.uiOrder = 540f; } // Other furniture not based on vanilla furniture
                                break;
                        }
                        if (furniture.designationCategory==BDS_DefOf.Furniture &&
                            furniture.comps?.FirstOrDefault(x => x is CompProperties_Glower) as CompProperties_Glower == null) {
                            furniture.uiOrder += 50f; // Sunlamps go after normal lamps
                        }
                    } else if (furniture.designationCategory == BDS_DefOf.Furniture) {
                        furniture.uiOrder = 545f; // Other furniture not based on vanilla furniture
                    }
                }
                // PLANT POTS
                else if (typeof(Building_PlantGrower).IsAssignableFrom(furniture.thingClass)) {
                    if (furniture.GetModExtension<BDS_DefModExtension>()?.buildingBase=="furniture") {
                        furniture.designationCategory=BDS_DefOf.Furniture;
                    }
                    if (furniture.designationCategory==BDS_DefOf.Furniture) {
                        furniture.uiOrder = 600f; // Plant pots
                        furniture.uiOrder += (float)(furniture.size.x * furniture.size.z); // Larger plant pots later
                    }
                }
                // DECOR
                else if (royalRequirements.Contains(furniture)) {
                    if (furniture.GetModExtension<BDS_DefModExtension>()?.buildingBase=="furniture") {
                        furniture.designationCategory=BDS_DefOf.Furniture;
                    }
                    if (furniture.designationCategory==BDS_DefOf.Furniture) {
                        furniture.uiOrder = 700f; // Throne room decor
                    }
                }
                else if (furniture.statBases.Any(x => x.stat == StatDefOf.TerrorSource)) {
                    if (furniture.GetModExtension<BDS_DefModExtension>()?.buildingBase=="furniture") {
                        furniture.designationCategory=BDS_DefOf.Furniture;
                    }
                    if (furniture.designationCategory==BDS_DefOf.Furniture) {
                        furniture.uiOrder = 750f; // Terror stuff
                    }
                }
                // STYLING STATION
                else if (typeof(Building_StylingStation).IsAssignableFrom(furniture.thingClass)) {
                    furniture.designationCategory=BDS_DefOf.Furniture;
                    furniture.uiOrder = 800f; // Styling Station
                }
                // LEARNING
                else if (comp?.linkableFacilities.Contains(BlackboardDef)??false) {
                    furniture.designationCategory=BDS_DefOf.Furniture;
                    furniture.uiOrder = 900f; // Desks
                }
                else if ( (SchoolDeskDef.comps.FirstOrDefault(x => x is CompProperties_AffectedByFacilities)
                            as CompProperties_AffectedByFacilities).linkableFacilities.Contains(furniture)) {
                    furniture.designationCategory=BDS_DefOf.Furniture;
                    furniture.uiOrder = 950f; // Desk facilities
                }
                // MISCELLANEOUS OTHER FURNITURE
                else if (furniture.GetModExtension<BDS_DefModExtension>()?.buildingBase == "furniture") {
                    furniture.designationCategory = BDS_DefOf.Furniture;
                    furniture.uiOrder=1000f;
                }
                else if (furniture.designationCategory==BDS_DefOf.Furniture) {
                    furniture.uiOrder=1100f;
                }

                // Add to Facilities
                if (furniture.designationCategory==BDS_DefOf.Furniture) {
                    // Log.Message(furniture.defName + furniture.uiOrder);
                    if (!furniture.GetFacilities().NullOrEmpty()) { facilities.AddRange(furniture.GetFacilities()); }
                }
            }

            // MODDED FACILITIES
            foreach (ThingDef facility in facilities.Where(f => f.BuildableByPlayer)) {
                facility.designationCategory = BDS_DefOf.Furniture;
                facilityProps = facility.comps?.FirstOrDefault(x => x is CompProperties_Facility) as CompProperties_Facility;
                facility.uiOrder = 1200f;
            }
        }       
    }
}