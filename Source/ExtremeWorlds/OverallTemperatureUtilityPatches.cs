using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;
using RimWorld.Planet;
using Harmony;

using HarmonyTools;

namespace ExtremeWorlds
{
    // NOTE: couldn't get calls to work when calling Func directly, so using this helper
    public static class CurveHelpers
    {

        public static SimpleCurve GetRainfallCurveHelper(OverallRainfall overallRainfall)
        {
            ExtremeWorlds_GameComponent gameComp = Current.Game.GetComponent<ExtremeWorlds_GameComponent>();
            if (gameComp?.CustomRainfalls == true)
                return gameComp.Curve_CustomRainfalls;
            return OverallRainfallUtility.GetRainfallCurve(overallRainfall); // default
        }

        public static SimpleCurve GetTemperatureCurveHelper(OverallTemperature overallTemperature)
        {
            ExtremeWorlds_GameComponent gameComp = Current.Game.GetComponent<ExtremeWorlds_GameComponent>();
            if (gameComp?.CustomTeperatures == true)
                return gameComp.Curve_CustomTemperatures;
            return OverallTemperatureUtility.GetTemperatureCurve(overallTemperature); // default
        }
    }

    // https://github.com/AaronCRobinson/ExtremeColds/blob/master/Source/ExtremeColds/OverallTemperatureUtility.cs
    [StaticConstructorOnStartup]
    public class ExtremeWorldPatches
    {
        static ExtremeWorldPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.extremecolds.overalltemputility");
            harmony.Patch(AccessTools.Method(typeof(WorldGenStep_Terrain), "GenerateTileFor"), null, null, new HarmonyMethod(typeof(ExtremeWorldPatches), nameof(TemperatureCurveSwitchroo)));
            harmony.Patch(AccessTools.Method(typeof(WorldGenStep_Terrain), "SetupRainfallNoise"), null, null, new HarmonyMethod(typeof(ExtremeWorldPatches), nameof(RainfallCurveSwitchroo)));
#if DEBUG
            HarmonyInstance.DEBUG = false;
#endif
        }

        public static IEnumerable<CodeInstruction> TemperatureCurveSwitchroo(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo oldGetTemperatureCurve = AccessTools.Method(typeof(RimWorld.Planet.OverallTemperatureUtility), nameof(RimWorld.Planet.OverallTemperatureUtility.GetTemperatureCurve));

            List<CodeInstruction> newInstructions = new List<CodeInstruction>() { new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CurveHelpers), nameof(CurveHelpers.GetTemperatureCurveHelper))) };
            return HarmonyHelpers.Bigofactorunie(OpCodes.Call, oldGetTemperatureCurve, newInstructions).Invoke(instructions);
        }

        public static IEnumerable<CodeInstruction> RainfallCurveSwitchroo(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo oldGetRainfallCurve = AccessTools.Method(typeof(RimWorld.Planet.OverallRainfallUtility), nameof(RimWorld.Planet.OverallRainfallUtility.GetRainfallCurve));

            List<CodeInstruction> newInstructions = new List<CodeInstruction>() { new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CurveHelpers), nameof(CurveHelpers.GetRainfallCurveHelper))) };
            return HarmonyHelpers.Bigofactorunie(OpCodes.Call, oldGetRainfallCurve, newInstructions).Invoke(instructions);
        }

    }
}
