// BetterDesignatorSorting.BDS_Utility
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterDesignatorSorting {
    public static class BDS_Utility {

        // Get all stuff types used in a building
        private static HashSet<StuffCategoryDef> stuffTemp = new HashSet<StuffCategoryDef>();
        public static HashSet<StuffCategoryDef> StuffTypes(this BuildableDef building){
            stuffTemp.Clear();
            if (building.MadeFromStuff) {
                stuffTemp.AddRange(building.stuffCategories);
            } else if (!building.costList.NullOrEmpty()) {
                stuffTemp = new HashSet<StuffCategoryDef>();
                foreach (ThingDefCountClass cost in building.costList) {
                    if (cost.thingDef?.stuffProps?.categories == null) { continue; }
                    stuffTemp?.AddRange(cost.thingDef?.stuffProps?.categories);
                }
            }
            return stuffTemp;
        }

        // Get Facilities for building
        private static CompProperties_AffectedByFacilities comp;
        public static List<ThingDef> GetFacilities(this ThingDef building){
            comp = building.comps?.FirstOrDefault(x => x is CompProperties_AffectedByFacilities) as CompProperties_AffectedByFacilities;
            if (comp?.linkableFacilities.Any() ?? false) {
                return comp.linkableFacilities;
            } else { 
                return null;
            }
        }

        // Facility category priority for facilities that aid buildings from multiple categories
        public static bool PriorityOver(this DesignationCategoryDef cat1, DesignationCategoryDef cat2){
            if (GetCategoryPriority(cat1.defName) < GetCategoryPriority(cat2.defName)) 
                { return true; }
            return false;

            // Lower value = higher priority
            int GetCategoryPriority(string catDefName) {
                switch(catDefName) {
                    case "ship" :           return 0;
                    case "production" :     return 1;
                    case "food" :           return 2;
                    case "science" :        return 3;
                    case "technology" :     return 4;
                    case "rest" :           return 5;
                    case "joy" :            return 6;
                    case "furniture" :      return 7;
                    case "security" :       return 8;
                    case "temperature" :    return 9;
                    case "power" :          return 10;
                    case "structure" :      return 11;
                    case "misc" :           return 12;
                    default: return 100;
                }
            }
        }

        private static IEnumerable<T> UnionSafe<T>(this IEnumerable<T> a, IEnumerable<T> b) {
            if (!((a?.Any()??false) || (b?.Any()??false))) { return Enumerable.Empty<T>(); }
            if (!(a?.Any()??false)) { return b; }
            if (!(b?.Any()??false)) { return a; }
            return a.Union(b);
        }

        // Get All Research Prequisites
        private static HashSet<ResearchProjectDef> projects = new HashSet<ResearchProjectDef>();
        public static HashSet<ResearchProjectDef> AllResearchPrerequisites(this ThingDef thing) {
            if (thing.researchPrerequisites.NullOrEmpty()) { return null; }
            projects.Clear();
            foreach (ResearchProjectDef project in thing.researchPrerequisites) {
               addAllPrerequisites(project);
            }
            return projects;

            // Recursor
            void addAllPrerequisites(ResearchProjectDef pre1) {
                if (projects.Contains(pre1)) { return; }
                projects.Add(pre1);
                foreach (ResearchProjectDef pre2 in pre1.prerequisites.UnionSafe(pre1.hiddenPrerequisites)) {
                        addAllPrerequisites(pre2);
                }
            }
        }

        // Finds if Thing has target Research Prerequisite, even indirectly
        // More Performant than AllResearchPrerequisites(), but less manipulatable
        public static bool RequiresResearch(this ThingDef thing, ResearchProjectDef target) {
            if (thing.researchPrerequisites.NullOrEmpty()) { return false; }
            foreach (ResearchProjectDef project in thing.researchPrerequisites) {
                if (IncludesTarget(project)) { return true; }
            }
            return false;

            // Recursor
            bool IncludesTarget(ResearchProjectDef project) {
                if (project == target) { return true; }
                foreach (ResearchProjectDef pre in project.prerequisites.UnionSafe(project.hiddenPrerequisites)) {
                    if (IncludesTarget(pre)) { return true; }
                }
                return false;
            }
        }

        // Get Total Cost of all Research Prerequisites from 
        public static float TotalResearchCost(this ThingDef thing) {
            if(thing.researchPrerequisites.NullOrEmpty()) { return 0f;}
            float totalCost = 0f;
            foreach (ResearchProjectDef project in thing.AllResearchPrerequisites()) {
                totalCost += project.baseCost;
            }
            return totalCost;
        }

        // Finds if Thing has any recipes of associated WorkType
        public static bool HasRecipeOfType(this ThingDef thing, WorkTypeDef work) {
            if (thing.recipes.NullOrEmpty()) { return false; }
            return thing.recipes.Any(recipe => recipe.requiredGiverWorkType == work);
        }

        // Returns true if no elements match predicate
        public static bool None<T>(this IEnumerable<T> collection, Func<T, bool> predicate) {
            return !collection.Any(predicate);
        }
        // Returns true if two collections share any item
        public static bool Intersects<T>(this IEnumerable<T> a, IEnumerable<T> b) {
            if(a == null || b == null) { return false; }
            return a.Intersect(b)?.Any()??false;
        }
    }
}