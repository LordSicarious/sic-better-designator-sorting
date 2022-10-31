// BetterDesignatorSorting.AllPatches
using System.Linq;
using RimWorld;
using Verse;

namespace BetterDesignatorSorting {
    [StaticConstructorOnStartup]
    public static class AllPatches {

        static AllPatches() {
            // Fix Mod Extension Inheritance
            DefModExtension modExtension;
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs.Where(thing => thing.GetModExtension<BDS_DefModExtension>()?.buildingBase != null)) {
                modExtension = thing.modExtensions.FindLast(x => x is BDS_DefModExtension);
                thing.modExtensions.RemoveAll(x => x is BDS_DefModExtension);
                thing.modExtensions.Add(modExtension);
            }

            // Resolve References to fix duplicate designators and fix order
            foreach (DesignationCategoryDef def in DefDatabase<DesignationCategoryDef>.AllDefs) {
                def.ResolveReferences();
                switch(def.label) {
                    case "misc" :        def.order=00; break;
                    case "ship" :        def.order=01; break;
                    case "temperature" : def.order=02; break;
                    case "power" :       def.order=03; break;
                    case "security" :    def.order=04; break;
                    case "technology" :  def.order=05; break;
                    case "recreation" :  def.order=06; break;
                    case "science" :     def.order=07; break;
                    case "rest" :        def.order=08; break;
                    case "food" :        def.order=09; break;
                    case "furniture" :   def.order=10; break;
                    case "production" :  def.order=11; break;
                    case "structure" :   def.order=12; break;
                    case "floors" :      def.order=13; break;
                    case "orders" :      def.order=14; break;
                    case "zone" :        def.order=15; break;
                    default: 
                        foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs.Where(thing => thing.designationCategory == def))
                            { thing.designationCategory = BDS_DefOf.Misc; }
                        // Add stuff to remove extraneous designators here
                        break;
                }
                // Set Preferred Column to preserve ordering if extra tabs are modded in
                def.preferredColumn = def.order%2;
            }

            // Apply Patches
            Misc.Patch();
            Security.Patch();
            Technology.Patch();
            Recreation.Patch();
            Science.Patch();
            Rest.Patch();
            Food.Patch();
            Furniture.Patch();
            Production.Patch();
            Floors.Patch();
	    }
    }
}