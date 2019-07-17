using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace CarpenterTable
{

    public static class CarpenterTableUtility
    {

        public static bool IsFurniture(this BuildableDef def)
        {
            if (def is ThingDef tDef)
            {
                // Not minifiable
                if (!tDef.Minifiable)
                    return false;

                var designationCategory = def.designationCategory;
                return
                    designationCategory == DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("Furniture") || // Furniture
                    designationCategory == DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("Joy") || // Recreation
                    designationCategory == DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("ANON2MF"); // Furniture+ (More Furniture)
            }
            return false;
        }

    }

}
