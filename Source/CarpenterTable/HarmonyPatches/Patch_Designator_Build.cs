using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;

namespace CarpenterTable
{

    public static class Patch_Designator_Build
    {

        [HarmonyPatch(typeof(Designator_Build))]
        [HarmonyPatch(nameof(Designator_Build.Visible), MethodType.Getter)]
        public static class Patch_Visible_Getter
        {

            public static void Postfix(Designator_Build __instance, ref bool __result)
            {
                // If the PlacingDef is a piece of furniture and the 'restrict furniture to carpenter tables' setting is enabled, return false
                if (CarpenterTablesSettings.restrictFurnitureConstruction && __instance.PlacingDef.IsFurniture())
                    __result = false;
            }

        }

    }

}
