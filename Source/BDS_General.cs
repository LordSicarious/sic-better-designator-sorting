// BetterDesignatorSorting.BDS_DefOf
//              and
// BetterDesignatorSorting.BDS_DefModExtension
using RimWorld;
using Verse;

namespace BetterDesignatorSorting {
    [DefOf]
    public static class BDS_DefOf {
        // Designation Sorting
        public static DesignationCategoryDef Misc;
        public static DesignationCategoryDef Ship;
        public static DesignationCategoryDef Temperature;
        public static DesignationCategoryDef Power;
        public static DesignationCategoryDef Security;
        public static DesignationCategoryDef Technology;
        public static DesignationCategoryDef Joy;
        public static DesignationCategoryDef Science;
        public static DesignationCategoryDef Rest;
        public static DesignationCategoryDef Food;
        public static DesignationCategoryDef Furniture;
        public static DesignationCategoryDef Production;
        public static DesignationCategoryDef Structure;
        public static DesignationCategoryDef Floors;
        public static DesignationCategoryDef Orders;
        public static DesignationCategoryDef Zone;
        

        // Research Projects
        public static ResearchProjectDef Electricity;
        public static ResearchProjectDef Firefoam;
        public static ResearchProjectDef GunTurrets;
        public static ResearchProjectDef Brewing;
        public static ResearchProjectDef Smithing;
        public static ResearchProjectDef Machining;
        public static ResearchProjectDef Fabrication;
        public static ResearchProjectDef TransportPod;
        [MayRequireIdeology]
        public static ResearchProjectDef Biosculpting;
        [MayRequireBiotech]
        public static ResearchProjectDef Xenogermination;
        [MayRequireBiotech]
        public static ResearchProjectDef BasicMechtech;

        // Other shit
        public static WorkTypeDef Tailoring;
        public static DesignatorDropdownGroupDef IED_Trap;
        public static RecipeDef ButcherCorpseFlesh;


        // Initialise
        static BDS_DefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(BDS_DefOf));
        }
    }

    // Use for tracing XML inheritance
    public class BDS_DefModExtension : DefModExtension {
        public string buildingBase;
    }
}