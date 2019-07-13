using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace CarpenterTable
{

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            var h = HarmonyInstance.Create("XeoNovaDan.CarpenterTable");
            //HarmonyInstance.DEBUG = true;

            // Automatic patches
            h.PatchAll();

            // Manual patches
            // Patch the initAction for Toils_Recipe.FinishRecipeAndStartStoringProduct
            h.Patch(typeof(Toils_Recipe).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First(t => t.Name.Contains("FinishRecipeAndStartStoringProduct")).
                GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).MaxBy(mi => mi.GetMethodBody()?.GetILAsByteArray().Length ?? -1),
                transpiler: new HarmonyMethod(typeof(Patch_Toils_Recipe.ManualPatch_FinishRecipeAndStartStoringProduct_InitAction), "Transpiler"));

        }

    }

}
