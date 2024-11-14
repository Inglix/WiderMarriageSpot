using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BiggerMarriageSpots.ModCompatibility
{
    public static class RomanceOnTheRimPatches
    {
        public static void PatchMod(Harmony harmony)
        {
            var getTargetsMethod = AccessTools.Method(AccessTools.Inner(AccessTools.TypeByName("RomanceOnTheRim.WeddingCeremony_TargetWorker"), "<GetTargets>d__2"), "MoveNext");
            var canUseTargetMethod = AccessTools.Method(AccessTools.TypeByName("RomanceOnTheRim.WeddingCeremony_TargetWorker"), "CanUseTargetInternal");
            harmony.Patch(getTargetsMethod, transpiler: new HarmonyMethod(typeof(RomanceOnTheRimPatches), nameof(WeddingCeremonyTargetWorkerGetTargetsTranspiler)));
            harmony.Patch(canUseTargetMethod, prefix: new HarmonyMethod(typeof(RomanceOnTheRimPatches), nameof(WeddingCeremonyTargetWorkerCanUseTargetInternalPrefix)));
        }

        public static List<Thing> MarriageSpotsByType(this ListerThings listerThings)
        {
            var marriageSpots = new List<Building_MarriageSpot>();
            listerThings.GetThingsOfType(marriageSpots);
            return marriageSpots.Cast<Thing>().ToList();
        }

        public static IEnumerable<CodeInstruction> WeddingCeremonyTargetWorkerGetTargetsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var thingsOfDefMethod = AccessTools.Method(typeof(ListerThings), "ThingsOfDef", new[] { typeof(ThingDef) });
            var thingDefOfField = AccessTools.Field(typeof(ThingDefOf), "MarriageSpot");
            
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsField(thingDefOfField))
                {
                    yield return new CodeInstruction(OpCodes.Nop);
                }
                else if (instruction.Calls(thingsOfDefMethod))
                {
                    yield return CodeInstruction.Call(typeof(RomanceOnTheRimPatches), "MarriageSpotsByType");
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        public static bool WeddingCeremonyTargetWorkerCanUseTargetInternalPrefix(ref RitualTargetUseReport __result, TargetInfo target, RitualObligation obligation)
        {
            __result = target.Thing.def.thingClass == typeof(Building_MarriageSpot);
            return false;
        }
    }
}