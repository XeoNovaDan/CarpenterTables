using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
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

        public static class ManualPatch_FinishRecipeAndStartStoringProduct_InitAction
        {

            private static CodeInstruction toilInstruction;

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                toilInstruction = instructionList.First(i => i.operand is FieldInfo fi && fi.Name == "toil");
                bool listExists = false;

                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // After the first 'stloc 6' instruction which stores the 'list' local variable has been declared...
                    if (listExists)
                    {
                        // If it is possible to look 5 instructions ahead...
                        if (i < instructionList.Count - 5)
                        {
                            var fifthInstructionAhead = instructionList[i + 5];

                            
                            if (fifthInstructionAhead.opcode == OpCodes.Callvirt)
                            {
                                // Add calls to the auto-deconstruct helper method before each call for EndCurrentJob
                                if (fifthInstructionAhead.operand == AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob)))
                                {
                                    foreach (CodeInstruction helperInstruction in TranspilerHelperAutoDeconstructCallInstructions(false))
                                        yield return helperInstruction;
                                }

                                // Add calls to the 'don't count iteration' helper method before each call for Notify_IterationCompleted
                                else if (fifthInstructionAhead.operand == AccessTools.Method(typeof(Bill), nameof(Bill.Notify_IterationCompleted)))
                                {
                                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                                    yield return toilInstruction; // this.toil
                                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6); // list
                                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ManualPatch_FinishRecipeAndStartStoringProduct_InitAction), nameof(TranspilerHelper_DontCountIteration))); // TranspilerHelper_DontCountIteration(this.toil, list)
                                }
                            }
                        }

                        // Add call to the auto-deconstruct helper method if the current line is 'curJob.count = 99999' (i.e. if product would normally be carried to a stockpile)
                        if (instruction.opcode == OpCodes.Stfld && instruction.operand == AccessTools.Field(typeof(Job), nameof(Job.count)) && (int)instructionList[i - 1].operand == 99999)
                        {
                            yield return instruction;
                            foreach (CodeInstruction helperInstruction in TranspilerHelperAutoDeconstructCallInstructions(true))
                                yield return helperInstruction;
                            instruction = new CodeInstruction(OpCodes.Nop);
                        }
                    }

                    yield return instruction;

                    if (!listExists && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 6)
                        listExists = true;
                }
            }

            public static IEnumerable<CodeInstruction> TranspilerHelperAutoDeconstructCallInstructions(bool endCurrent)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                yield return toilInstruction; // this.toil
                yield return new CodeInstruction(OpCodes.Ldloc_S, 6); // list
                yield return new CodeInstruction(endCurrent ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); // endCurrent (effectively)
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ManualPatch_FinishRecipeAndStartStoringProduct_InitAction), nameof(TranspilerHelper_AutoDeconstruct))); // TranspilerHelper_AutoDeconstruct(this.toil, list, endCurrent)
            }

            public static void TranspilerHelper_DontCountIteration(Toil resultToil, List<Thing> products)
            {
                // If JobDriver's job's recipe def was generated by the static constructor class, the bill is 'do  x times' and the product doesn't meet quality requirements, effectively don't count the product
                var curJob = resultToil.actor.CurJob;
                if (curJob.RecipeDef.defName.Contains(StaticConstructorClass.GeneratedRecipeDefPrefix))
                {
                    var product = products.First();
                    var productionBill = (Bill_Production)curJob.bill;
                    if (productionBill.repeatMode == BillRepeatModeDefOf.RepeatCount && !ProductMeetsQualityRequirement(product, productionBill.qualityRange))
                        productionBill.repeatCount++;
                }
            }

            public static void TranspilerHelper_AutoDeconstruct(Toil resultToil, List<Thing> products, bool endCurrent)
            {
                if (!CarpenterTablesSettings.deconstructInadequateProducts)
                    return;

                var actor = resultToil.actor;
                var curJob = actor.CurJob;

                // If automatic deconstruct setting is enabled and the JobDriver's job's recipe def was generated by the static constructor class...
                if (curJob.RecipeDef.defName.Contains(StaticConstructorClass.GeneratedRecipeDefPrefix))
                {
                    // If the product has quality and the quality doesn't meet the bill's standards, designate a deconstruction on the product
                    var product = products.First();
                    var qualityComp = product.GetInnerIfMinified().TryGetComp<CompQuality>();
                    var productionBill = (Bill_Production)curJob.bill;
                    if (!ProductMeetsQualityRequirement(product, productionBill.qualityRange))
                    {
                        var designationManager = actor.Map.designationManager;
                        if (actor.carryTracker.CarriedThing != null)
                            actor.carryTracker.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out product);
                        designationManager.RemoveAllDesignationsOn(product); // Prevent double designation errors
                        designationManager.AddDesignation(new Designation(product, DesignationDefOf.Deconstruct));
                        if (endCurrent)
                            actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                        actor.jobs.jobQueue.EnqueueFirst(new Job(JobDefOf.Deconstruct, product), JobTag.MiscWork);
                    }
                }
            }

            public static bool ProductMeetsQualityRequirement(Thing product, QualityRange qualityRange)
            {
                var qualityComp = product.GetInnerIfMinified().TryGetComp<CompQuality>();
                return qualityComp == null || qualityRange.Includes(qualityComp.Quality);
            }

        }

    }

}
