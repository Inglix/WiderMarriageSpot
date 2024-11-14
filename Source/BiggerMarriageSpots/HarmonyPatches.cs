using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BiggerMarriageSpots
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            
        }
    }

    [HarmonyPatch]
    static class PatchGetMarriageSpotAt
    {
        [HarmonyTargetMethod]
        static  MethodInfo GetTarget()
        {
            return AccessTools.Method(typeof(LordToil_MarriageCeremony).GetNestedType("<>c", BindingFlags.NonPublic), "<GetMarriageSpotAt>b__12_0");   
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> GetMarriageSpotAtTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var defField = AccessTools.Field(typeof(Thing), nameof(Thing.def));
            var thingClassField = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.thingClass));
            var replacedThingDefWithThingClassLeft = false;
            var replacedThingDefWithThingClassRight = false;
            
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsField(defField))
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldfld, thingClassField);
                    replacedThingDefWithThingClassLeft = true;
                }
                else if (instruction.opcode == OpCodes.Ldsfld)
                {
                    yield return new CodeInstruction(OpCodes.Ldtoken, typeof(Building_MarriageSpot));
                    yield return CodeInstruction.Call(typeof(Type), "GetTypeFromHandle");
                    replacedThingDefWithThingClassRight = true;
                }
                else
                {
                    yield return instruction;    
                }
            }
            
            if (!replacedThingDefWithThingClassLeft) Log.Error("Failed to patch left operand in GetMarriageSpotAt. Please report this to Inglix with your log file.");
            if (!replacedThingDefWithThingClassRight) Log.Error("Failed to patch right operand in GetMarriageSpotAt. Please report this to Inglix with your log file.");
        }
    }

    [HarmonyPatch(typeof(RCellFinder), nameof(RCellFinder.TryFindMarriageSite))]
    static class PatchTryFindMarriageSite
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TryFindMarriageSiteTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var bar = AccessTools.Method(typeof(ListerBuildings), nameof(ListerBuildings.AllBuildingsColonistOfDef));
            var removedMarriageSpotFieldLoading = false;
            var replaceBuildingsOfDefWithOfClass = false;
            
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldsfld && instruction.operand.ToString().Contains("MarriageSpot"))
                {
                    yield return new CodeInstruction(OpCodes.Nop);
                    removedMarriageSpotFieldLoading = true;
                }
                else if (instruction.Calls(bar))
                {
                    yield return CodeInstruction.Call(typeof(ListerBuildings), "AllBuildingsColonistOfClass", null, new[] { typeof(Building_MarriageSpot) });
                    replaceBuildingsOfDefWithOfClass = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            
            if (!removedMarriageSpotFieldLoading) Log.Error("Failed to patch out loading of MarriageSpot field in TryFindMarriageSite. Please report this to Inglix with your log file.");
            if (!replaceBuildingsOfDefWithOfClass)
                Log.Error("Failed to replace building search by def to search by class in TryFindMarriageSite. Please report this to Inglix with your log file.");
        }
    }

    [HarmonyPatch(typeof(LordToil_MarriageCeremony), nameof(LordToil_MarriageCeremony.FianceStandingSpotFor))]
    static class PatchFianceStandingSpotFor
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FianceStandingSpotForTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            var findCellMethod = AccessTools.Method(typeof(LordToil_MarriageCeremony), "FindCellForOtherPawnAtMarriageSpot");
            
            var labelTime = false;
            var foo = ilg.DefineLabel();

            var replaceFirstPawnPositionLogic = false;
            var replaceSecondPawnPositionLogic = false;
            var addNewJumpLabel = false;
            
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Bge_S)
                {
                    yield return instruction;
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadField(typeof(LordToil_MarriageCeremony), "spot");
                    yield return CodeInstruction.Call(typeof(LordToil_MarriageCeremony), "GetMarriageSpotAt");
                    yield return new CodeInstruction(OpCodes.Brfalse_S, foo);
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadField(typeof(LordToil_MarriageCeremony), "spot");
                    yield return CodeInstruction.Call(typeof(PatchFianceStandingSpotFor), "FindCellForFirstPawnAtMarriageSpot");
                    yield return new CodeInstruction(OpCodes.Ret);
                    labelTime = true;
                    replaceFirstPawnPositionLogic = true;
                }
                else if (labelTime)
                {
                    instruction.labels.Add(foo);
                    yield return instruction;
                    labelTime = false;
                    addNewJumpLabel = true;
                }
                else if (instruction.Calls(findCellMethod))
                {
                    yield return CodeInstruction.Call(typeof(PatchFianceStandingSpotFor), "FindCellForSecondPawnAtMarriageSpot");
                    replaceSecondPawnPositionLogic = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            
            if (!replaceFirstPawnPositionLogic) Log.Error("Failed to patch logic for standing position of first pawn. Please report this to Inglix with your log file.");
            if (!replaceSecondPawnPositionLogic) Log.Error("Failed to patch logic for standing position of second pawn. Please report this to Inglix with your log file.");
            if (!addNewJumpLabel) Log.Error("Failed to add new jump label in logic for standing positions. Please report this to Inglix with your log file.");
        }
        
        static IntVec3 FindCellForFirstPawnAtMarriageSpot(LordToil_MarriageCeremony toil, IntVec3 cell)
        {
            var marriageSpot = cell.GetThingList(toil.Map).Find(x => x.def.thingClass == typeof(Building_MarriageSpot));
            var cellRect = marriageSpot.OccupiedRect();
            switch (cellRect.Corners.Count())
            {
                case 1:
                    if (BiggerMarriageSpots.Settings.ShareCenterCell) return cell;
                    Log.Warning("Marriage spot is 1x1. There's no place for 2 pawns.");
                    return IntVec3.Invalid;
                case 2:
                    if (BiggerMarriageSpots.Settings.ShareCenterCell && cellRect.Cells.Count() % 2 == 1) return cell;
                    return new IntVec3(cellRect.minX, 0, cellRect.minZ);
                default:
                    Log.Warning("Marriage spot is " + cellRect.Width + "x" + cellRect.Height +". This shape is unsupported.");
                    return IntVec3.Invalid;
            }
        }
        
        static IntVec3 FindCellForSecondPawnAtMarriageSpot(LordToil_MarriageCeremony toil, IntVec3 cell)
        {
            var marriageSpot = cell.GetThingList(toil.Map).Find(x => x.def.thingClass == typeof(Building_MarriageSpot));
            var cellRect = marriageSpot.OccupiedRect();
            switch (cellRect.Corners.Count())
            {
                case 1:
                    if (BiggerMarriageSpots.Settings.ShareCenterCell) return cell;
                    Log.Warning("Marriage spot is 1x1. There's no place for 2 pawns.");
                    return IntVec3.Invalid;
                case 2:
                    if (BiggerMarriageSpots.Settings.ShareCenterCell && cellRect.Cells.Count() % 2 == 1) return cell;
                    return new IntVec3(cellRect.maxX, 0, cellRect.maxZ);
                default:
                    Log.Warning("Marriage spot is " + cellRect.Width + "x" + cellRect.Height +". This shape is unsupported.");
                    return IntVec3.Invalid;
            }
        }
    }

    [HarmonyPatch]
    static class PatchMakeNewToils
    {
        private static readonly MethodInfo AdjacentMethod = AccessTools.Method(typeof(GenAdj), nameof(GenAdj.AdjacentTo8WayOrInside), new[] {typeof(IntVec3), typeof(Thing)});
        
        [HarmonyTargetMethods]
        static IEnumerable<MethodInfo> GetTargetMethods()
        {
            yield return AccessTools.Method(typeof(JobDriver_MarryAdjacentPawn), "<MakeNewToils>b__8_0");
            /*yield return AccessTools.Method(AccessTools.Inner(typeof(JobDriver_MarryAdjacentPawn), "<MakeNewToils>d__8"), "MoveNext");
            yield return AccessTools.Method(typeof(JobDriver_MarryAdjacentPawn), "MakeNewToils");*/
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MakeNewToilsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var replaceAdjacentCheck = false;
            
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(AdjacentMethod))
                {
                    yield return CodeInstruction.Call(typeof(PatchMakeNewToils), "BothInMarriageSpotOrAdjacent");
                    replaceAdjacentCheck = true;
                }
                else
                {
                    yield return instruction;    
                }
            }
            
            if (!replaceAdjacentCheck) Log.Error("Failed to replace adjacency check with custom check for marriage spot. Please report this to Inglix with your log file.");
        }
        
        static bool BothInMarriageSpotOrAdjacent(this IntVec3 root, Thing t)
        {
            var marriageSpot = root.GetThingList(t.Map).Find(x => x.def.thingClass == typeof(Building_MarriageSpot));
            if (marriageSpot == null) return root.AdjacentTo8WayOrInside(t);
            var marriageSpotCells = marriageSpot.OccupiedRect().Cells.ToList();
            if (marriageSpotCells.Contains(root) && marriageSpotCells.Contains(t.Position))
            {
                return true;
            }

            return root.AdjacentTo8WayOrInside(t);
        }
    }

    [HarmonyPatch(typeof(JobGiver_MarryAdjacentPawn), "TryGiveJob")]
    static class PatchTryGiveJob
    {
        [HarmonyPostfix]
        static void TryGiveJobPostfix(ref Job __result, Pawn pawn, JobGiver_MarryAdjacentPawn __instance)
        {
            var canMarryMethod = AccessTools.Method(typeof(JobGiver_MarryAdjacentPawn), "CanMarry");

            if (__result != null || !pawn.RaceProps.IsFlesh) return;
            var marriageSpot = pawn.Position.GetThingList(pawn.Map).Find(x => x.def.thingClass == typeof(Building_MarriageSpot));
            if (marriageSpot == null) return;
            
            var marriageSpotCells = marriageSpot.OccupiedRect().Cells;
            var spotCells = marriageSpotCells.ToList();
            if (spotCells.EnumerableNullOrEmpty()) return;

            Predicate<Thing> canMarryPredicate = x => x is Pawn pawn1 && (bool)canMarryMethod.Invoke(__instance, new object[]{pawn, pawn1}) ;
            foreach (var cell in spotCells)
            {
                if (!cell.InBounds(pawn.Map)) continue;
                var thingList = cell.GetThingList(pawn.Map);
                var fiance = thingList.Find(canMarryPredicate);

                if (fiance != null)
                {
                    __result = JobMaker.MakeJob(JobDefOf.MarryAdjacentPawn, fiance);
                }
            }
        }
    }
}