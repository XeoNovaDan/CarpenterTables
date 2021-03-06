﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;

namespace CarpenterTable
{

    public static class Patch_RecipeDef
    {

        [HarmonyPatch(typeof(RecipeDef))]
        [HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static class Patch_AvailableNow_Getter
        {

            public static void Postfix(RecipeDef __instance, ref bool __result)
            {
                // If the RecipeDef would normally be available and was generated by the StaticConstructorClass
                if (__result && __instance.defName.Contains(StaticConstructorClass.GeneratedRecipeDefPrefix))
                {
                    // Change the result to whether or not the resulting ThingDef's research requirements have been met
                    __result = __instance.ProducedThingDef.IsResearchFinished;
                }
            }

        }

        [HarmonyPatch(typeof(RecipeDef))]
        [HarmonyPatch(nameof(RecipeDef.WorkAmountTotal))]
        public static class Patch_WorkAmountTotal
        {

            public static void Postfix(RecipeDef __instance, ThingDef stuffDef, ref float __result)
            {
                // If the RecipeDef was generated by the StaticConstructorClass
                if (__instance.defName.Contains(StaticConstructorClass.GeneratedRecipeDefPrefix))
                {
                    // Work amount is the product's WorkToBuild dependent on the stuffDef
                    __result = __instance.ProducedThingDef.GetStatValueAbstract(StatDefOf.WorkToBuild, stuffDef);
                }
            }

        }

    }

}
