using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Harmony;

namespace ExtremeWorlds
{

    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
    }

    public class ScenPart_CustomTemperatures : ScenPart
    {
        // https://github.com/AaronCRobinson/ExtremeColds/blob/master/Source/ExtremeColds/OverallTemperatureUtility.cs
        public SimpleCurve customTemperatures = new SimpleCurve
        {
            { new CurvePoint(-9999f, -9999f), true },
            { new CurvePoint(-100f, -125f), true },
            { new CurvePoint(-90f, -110f), true },
            { new CurvePoint(-50f, -85f), true },
            { new CurvePoint(-30f, -78f), true },
            { new CurvePoint(-25f, -68f), true },
            { new CurvePoint(-20f, -58.5f), true },
            { new CurvePoint(0f, -57f), true }
        };

        public override void PreConfigure()
        {
#if DEBUG
            Log.Message("ScenPart_CustomTemperatures.PreConfigure");
#endif
            base.PreConfigure();
            CustomTemperatures_GameComponent customTemperaturesComp = Current.Game.GetComponent<CustomTemperatures_GameComponent>();
            customTemperaturesComp.CustomTeperatures = true;
            customTemperaturesComp.Curve_CustomTemperatures = this.customTemperatures;
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            float rectHeight = ScenPart.RowHeight * (this.customTemperatures.Count() + 1);
            Rect scenPartRect = listing.GetScenPartRect(this, rectHeight);
            Listing_Standard scenListing = new Listing_Standard();

            scenListing.Begin(scenPartRect);

            for (int i=0; i<this.customTemperatures.Points.Count(); i++)
            {
                Rect rect = scenListing.GetRect(ScenPart.RowHeight);
                Rect left = rect.LeftPartPixels(scenListing.ColumnWidth - 25f);

                Vector2 v = (Vector2)this.customTemperatures[i];

                string strX = v.x.ToString();
                string strY = v.y.ToString();
                float tempX = v.x;
                float tempY = v.y;

                Widgets.TextFieldNumeric<float>(left.LeftHalf(), ref tempX, ref strX, -99999f, -99999f);
                Widgets.TextFieldNumeric<float>(left.RightHalf(), ref tempY, ref strY, -99999f, -99999f);

                v.x = tempX;
                v.y = tempY;

                // right
                if (Widgets.ButtonImage(rect.RightPartPixels(24f), Textures.DeleteX))
                    this.customTemperatures.Points.RemoveAt(i);
            }

            if (Widgets.ButtonText(scenListing.GetRect(ScenPart.RowHeight), "Add Point"))
            {
                if (this.customTemperatures.Count() > 0)
                    this.customTemperatures.Points.Add(new CurvePoint(this.customTemperatures.Last()));
                else
                    this.customTemperatures.Points.Add(new CurvePoint(0f, 0f));
            }

            scenListing.End();
        }
    }

    public class ExposableCurve : IExposable
    {
        public List<CurvePoint> points = new List<CurvePoint>();

        public static implicit operator ExposableCurve(SimpleCurve curve)
        {
            ExposableCurve newObj = new ExposableCurve();
            newObj.points.AddRange(curve.Points);
            return newObj;
        }

        public static implicit operator SimpleCurve(ExposableCurve curve)
        {
            SimpleCurve newObj = new SimpleCurve();
            newObj.Points.AddRange(curve.points);
            return newObj;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look<CurvePoint>(ref this.points, "points");
        }
    }

    // NOTE: GameComponents are the quickest to pick-up Scenario details (as world won't be generated yet)
    public class CustomTemperatures_GameComponent : GameComponent
    {
        public bool CustomTeperatures= false;
        public ExposableCurve Curve_CustomTemperatures;

        public CustomTemperatures_GameComponent() { }
        public CustomTemperatures_GameComponent(Game game) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Log.Message("FinalizeInit");
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ExposableCurve>(ref this.Curve_CustomTemperatures, "CustomTemperaturesCurve");
        }

    }

    // NOTE: couldn't get calls to work when calling Func directly, so using this helper
    public static class TemperatureCurveHelper
    {
        public static SimpleCurve GetTemperatureCurveHelper(OverallTemperature overallTemperature)
        {
            CustomTemperatures_GameComponent customTemperature = Current.Game.GetComponent<CustomTemperatures_GameComponent>();
            if (customTemperature?.CustomTeperatures == true)
                return customTemperature.Curve_CustomTemperatures;
            return OverallTemperatureUtility.GetTemperatureCurve(overallTemperature); // default
        }
    }

    // https://github.com/AaronCRobinson/ExtremeColds/blob/master/Source/ExtremeColds/OverallTemperatureUtility.cs
    [StaticConstructorOnStartup]
    public class OverallTemperatureUtilityPatches
    {
        static OverallTemperatureUtilityPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.extremecolds.overalltemputility");
            harmony.Patch(AccessTools.Method(typeof(WorldGenStep_Terrain), "GenerateTileFor"), null, null, new HarmonyMethod(typeof(OverallTemperatureUtilityPatches), nameof(Leollswisharoo)));
#if DEBUG
            HarmonyInstance.DEBUG = false;
#endif
        }

        private static readonly MethodInfo oldGetTemperatureCurve = AccessTools.Method(typeof(RimWorld.Planet.OverallTemperatureUtility), nameof(RimWorld.Planet.OverallTemperatureUtility.GetTemperatureCurve));
        private static readonly MethodInfo getTemperatureCurveHelperMethodInfo = AccessTools.Method(typeof(TemperatureCurveHelper), nameof(TemperatureCurveHelper.GetTemperatureCurveHelper));

        public static IEnumerable<CodeInstruction> Leollswisharoo(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand == oldGetTemperatureCurve)
                    yield return new CodeInstruction(OpCodes.Call, getTemperatureCurveHelperMethodInfo);
                else
                    yield return instruction;
            }
        }

    }

}
