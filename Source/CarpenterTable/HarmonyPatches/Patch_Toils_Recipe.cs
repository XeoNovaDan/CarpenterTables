using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace CarpenterTable
{

    public static class Patch_Toils_Recipe
    {

        [HarmonyPatch(typeof(Toils_Recipe))]
        [HarmonyPatch(nameof(Toils_Recipe.DoRecipeWork))]
        public static class Patch_DoRecipeWork
        {

            public static void Postfix(Toil __result)
            {
                // Change the tickAction
                var tickAction = __result.tickAction;
                __result.tickAction = () =>
                {
                    // If the thing being worked on is an unfinished building, check for construction failure
                    var actor = __result.actor;
                    var unfinishedBuilding = actor.CurJob.GetTarget(TargetIndex.B).Thing as UnfinishedBuilding;
                    if (unfinishedBuilding != null)
                    {
                        var successChance = actor.GetStatValue(StatDefOf.ConstructSuccessChance);
                        var constructionSpeed = actor.GetStatValue(StatDefOf.ConstructionSpeed);
                        var workToBuild = unfinishedBuilding.Recipe.WorkAmountTotal(unfinishedBuilding.Stuff);
                        if (Rand.Chance(1 - Mathf.Pow(successChance, constructionSpeed / workToBuild)))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            MoteMaker.ThrowText(unfinishedBuilding.DrawPos, unfinishedBuilding.Map, "TextMote_ConstructionFail".Translate(), 6);
                            Messages.Message("MessageConstructionFailed".Translate(unfinishedBuilding.Label.UncapitalizeFirst(), actor.LabelShort, actor.Named("WORKER")), new TargetInfo(unfinishedBuilding.Position, unfinishedBuilding.Map, false), MessageTypeDefOf.NegativeEvent, true);
                            unfinishedBuilding.Destroy(DestroyMode.FailConstruction);
                            return;
                        }
                    }

                    tickAction();
                };
            }

        }

    }

}
