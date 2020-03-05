using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
    internal static class Harmony_DamageWorker_AddInjury_ApplyDamageToPart
    {
        private static bool _applyingSecondary = false;
        private static bool shieldAbsorbed = false;
        private static readonly int[] ArmorBlockNullOps = { 1, 3, 4, 5, 6 };  // Lines in armor block that need to be nulled out

        private static void ArmorReroute(Pawn pawn, ref DamageInfo dinfo, out bool deflectedByArmor, out bool diminishedByArmor)
        {
            var newDinfo = ArmorUtilityCE.GetAfterArmorDamage(dinfo, pawn, dinfo.HitPart, out deflectedByArmor, out diminishedByArmor, out shieldAbsorbed);
            if (dinfo.HitPart != newDinfo.HitPart)
            {
                if (pawn.Spawned) LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ArmorSystem, OpportunityType.Critical);   // Inform the player about armor deflection
            }
            dinfo = newDinfo;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("CE ApplyDamageToPart :: InitPatches ");
            var codes = instructions.ToList();
            Log.Message("CE ApplyDamageToPart :: InitPatches 1");

            // Find armor block
            var armorBlockEnd = codes.FirstIndexOf(c => c.operand == typeof(ArmorUtility).GetMethod("GetPostArmorDamage"));
            Log.Message("CE ApplyDamageToPart :: InitPatches 2");
            int armorBlockStart = -1;
            Log.Message("CE ApplyDamageToPart :: InitPatches 3");
            for (int i = armorBlockEnd; i > 0; i--)
            {
                Log.Message("CE ApplyDamageToPart :: InitPatches 3.1");
                // Find OpCode loading up first argument for GetPostArmorDamage (Pawn)
                if (codes[i].opcode == OpCodes.Ldarg_2)
                {
                    Log.Message("CE ApplyDamageToPart :: InitPatches 3.1.1");
                    armorBlockStart = i;
                    Log.Message("CE ApplyDamageToPart :: InitPatches 3.1.2");
                    break;
                }
                Log.Message("CE ApplyDamageToPart :: InitPatches 3.2");
            }
            Log.Message("CE ApplyDamageToPart :: InitPatches 4");
            if (armorBlockStart == -1)
            {
                Log.Message("CE ApplyDamageToPart :: InitPatches 4.1");
                Log.Error("CE failed to transpile DamageWorker_AddInjury: could not identify armor block start");
                return instructions;
            }

            Log.Message("CE ApplyDamageToPart :: InitPatches 5");
            // Replace armor block with our new instructions
            var armorCodes = codes.GetRange(armorBlockStart, armorBlockEnd - armorBlockStart);
            Log.Message("CE ApplyDamageToPart :: InitPatches 6");

            foreach (var index in ArmorBlockNullOps)
            {
                Log.Message("CE ApplyDamageToPart :: InitPatches 6.1");
                armorCodes[index].opcode = OpCodes.Nop;
                Log.Message("CE ApplyDamageToPart :: InitPatches 6.2");
                armorCodes[index].operand = null;
                Log.Message("CE ApplyDamageToPart :: InitPatches 6.3");
            }

            Log.Message("CE ApplyDamageToPart :: InitPatches 7");
            // Override armor method call
            codes[armorBlockEnd].operand = typeof(Harmony_DamageWorker_AddInjury_ApplyDamageToPart).GetMethod(nameof(ArmorReroute), AccessTools.all);

            Log.Message("CE ApplyDamageToPart :: InitPatches 8");
            // Prevent vanilla code from overriding changed damageDef
            codes[armorBlockEnd + 3] = new CodeInstruction(OpCodes.Call, typeof(DamageInfo).GetMethod($"get_{nameof(DamageInfo.Def)}"));
            Log.Message("CE ApplyDamageToPart :: InitPatches 9");
            codes[armorBlockEnd + 4] = new CodeInstruction(OpCodes.Stloc_S, 5);
            Log.Message("CE ApplyDamageToPart :: InitPatches 10");

            // Our method returns a Dinfo instead of float, we want to insert a call to Dinfo.Amount before stloc at ArmorBlockEnd+1
            codes.InsertRange(armorBlockEnd + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarga_S, 1),
                new CodeInstruction(OpCodes.Call, typeof(DamageInfo).GetMethod($"get_{nameof(DamageInfo.Amount)}"))
            });
            Log.Message("CE ApplyDamageToPart :: InitPatches 11");

            return codes;
        }

        internal static void Postfix(DamageInfo dinfo, Pawn pawn)
        {
            if (shieldAbsorbed) return;

            if (dinfo.Weapon?.projectile is ProjectilePropertiesCE props && !props.secondaryDamage.NullOrEmpty() && !_applyingSecondary)
            {
                _applyingSecondary = true;
                foreach (var sec in props.secondaryDamage)
                {
                    if (pawn.Dead)
                    {
                        break;
                    }
                    var secDinfo = sec.GetDinfo(dinfo);
                    pawn.TakeDamage(secDinfo);
                }

                _applyingSecondary = false;
            }
        }
    }

    [HarmonyPatch(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.ShouldReduceDamageToPreservePart))]
    static class Patch_ShouldReduceDamageToPreservePart
    {
        [HarmonyPrefix]
        static bool Prefix(ref bool __result, BodyPartRecord bodyPart)
        {
            __result = false;
            return false;
        }
    }
}
