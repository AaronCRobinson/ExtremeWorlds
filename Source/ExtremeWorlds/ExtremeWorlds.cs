using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;
using System.Linq;
using Verse.Sound;

namespace ExtremeWorlds
{

    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
        public static readonly Texture2D ReorderUp = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderUp", true);
        public static readonly Texture2D ReorderDown = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderDown", true);
    }

    // NOTE: GameComponents are the quickest to pick-up Scenario details (as world won't be generated yet)
    public class ExtremeWorlds_GameComponent : GameComponent
    {
        // TODO: find a way to drop these bools
        public bool CustomTeperatures = false;
        public bool CustomRainfalls = false;

        public ExposableCurve Curve_CustomTemperatures;
        public ExposableCurve Curve_CustomRainfalls;

        public ExtremeWorlds_GameComponent() { }
        public ExtremeWorlds_GameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ExposableCurve>(ref this.Curve_CustomTemperatures, "CustomTemperaturesCurve");
            Scribe_Deep.Look<ExposableCurve>(ref this.Curve_CustomRainfalls, "CustomRainfallsCurve");
        }

    }

    public class ScenPart_CustomTemperatures : CurveScenPart
    {
        public ScenPart_CustomTemperatures() : base()
        {
            // https://github.com/AaronCRobinson/ExtremeColds/blob/master/Source/ExtremeColds/OverallTemperatureUtility.cs
            this.curve = new SimpleCurve
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
        }

        public override void PreConfigure()
        {
#if DEBUG
            Log.Message("ScenPart_CustomTemperatures.PreConfigure");
#endif
            base.PreConfigure();
            ExtremeWorlds_GameComponent customTemperaturesComp = Current.Game.GetComponent<ExtremeWorlds_GameComponent>();
            customTemperaturesComp.CustomTeperatures = true;
            customTemperaturesComp.Curve_CustomTemperatures = this.curve;
        }

    }

    public class ScenPart_CustomRainfall : CurveScenPart
    {
        public ScenPart_CustomRainfall() : base()
        {
            this.curve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 750f),
                true
            },
            {
                new CurvePoint(125f, 2000f),
                true
            },
            {
                new CurvePoint(500f, 3000f),
                true
            },
            {
                new CurvePoint(1000f, 3800f),
                true
            },
            {
                new CurvePoint(5000f, 7500f),
                true
            },
            {
                new CurvePoint(12000f, 12000f),
                true
            },
            {
                new CurvePoint(99999f, 99999f),
                true
            }
        };
        }

        public override void PreConfigure()
        {
#if DEBUG
            Log.Message("ScenPart_CustomRainfall.PreConfigure");
#endif
            base.PreConfigure();
            ExtremeWorlds_GameComponent customTemperaturesComp = Current.Game.GetComponent<ExtremeWorlds_GameComponent>();
            customTemperaturesComp.CustomRainfalls = true;
            customTemperaturesComp.Curve_CustomRainfalls = this.curve;
        }

    }

    public abstract class CurveScenPart : ScenPart
    {
        private const float graphPaddingFactor = 0.1f;
        public SimpleCurve curve;

        // TODO: revisit
        private readonly List<SimpleCurveDrawInfo> curves = new List<SimpleCurveDrawInfo>();

        private SimpleCurveDrawInfo simpleCurveDrawInfo;
        private readonly SimpleCurveDrawerStyle simpleCurveDrawerStyle;

        protected Vector2 cursorPosition;
        protected Vector2 xLimits = default(Vector2);
        protected Vector2 yLimits = default(Vector2);
        private bool limitSet = false;

        public CurveScenPart()
        {
            this.simpleCurveDrawerStyle = new SimpleCurveDrawerStyle()
            {
                DrawMeasures = true,
                DrawCurveMousePoint = true,
                UseFixedScale = true,
                UseFixedSection = true,
            };
        }

        /*protected SimpleCurveDrawerStyle SimpleCurveDrawerStyle
        {
            get
            {
                if (this.simplCurveDrawerStyle == null)
                {
                    this.simplCurveDrawerStyle = new SimpleCurveDrawerStyle()
                    {
                        DrawMeasures = true,
                        DrawCurveMousePoint = true,
                    };

                }
                return this.simplCurveDrawerStyle;
            }
            set => this.simplCurveDrawerStyle = value;
        }*/

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            if (!limitSet)
                this.SetLimits(true);

            Rect scenPartRect = GetCustomScenPartRect(listing, this, 520f);

            GUI.BeginGroup(scenPartRect);
            Rect graphRect = new Rect(0f, 0f, scenPartRect.width, 450f);
            Rect legendRect = new Rect(0f, graphRect.yMax, scenPartRect.width, 40f);
            Rect buttonRect = new Rect(0f, legendRect.yMax, scenPartRect.width, 20f);

            simpleCurveDrawInfo = new SimpleCurveDrawInfo() { curve = this.curve };
            this.curves.Clear();
            this.curves.Add(simpleCurveDrawInfo);

            this.DrawCurves(graphRect, this.curves, this.simpleCurveDrawerStyle);

            //Rect inputRect

            void TextFieldNumericLabeled(Rect rect, string label, ref float val)
            {
                string buffer = val.ToString();
                Widgets.TextFieldNumericLabeled<float>(rect, label, ref val, ref buffer, -float.MaxValue, float.MaxValue);
            }

            TextFieldNumericLabeled(legendRect.TopPart(0.48f).LeftHalf(), "Min Input(x)", ref this.xLimits.x);
            TextFieldNumericLabeled(legendRect.TopPart(0.48f).RightHalf(), "Min Output(y)", ref this.yLimits.x);
            TextFieldNumericLabeled(legendRect.BottomPart(0.48f).LeftHalf(), "Max Input(x)", ref this.xLimits.y);
            TextFieldNumericLabeled(legendRect.BottomPart(0.48f).RightHalf(), "Max Output(y)", ref this.yLimits.y);

            if (Widgets.ButtonText(buttonRect, "Enhance"))
                this.SetLimits();

            // handle clicking
            if (Widgets.ButtonInvisible(graphRect))
            {
                Log.Message($"{this.cursorPosition}");
                if (Event.current.button == 0) // left click
                {
                    CurvePoint newPoint = new CurvePoint(this.cursorPosition);
                    this.curve.Points.Add(newPoint);
                    this.curve.SortPoints();
                }
                if (Event.current.button == 1 && this.curve.Points.Count() > 0) // right click
                {
                    CurvePoint removedPoint = this.curve.Points.OrderBy(p => Vector2.Distance(p, this.cursorPosition)).First();
                    this.curve.Points.Remove(removedPoint);
#if DEBUG
                    Log.Message($"{removedPoint}");
#endif
                }
            }

            GUI.EndGroup();
        }

        // some init
        private void SetLimits(bool init = false)
        {
            if (init)
            {
                this.xLimits = new Vector2(this.curve.Points.Min(p => p.x), this.curve.Points.Max(p => p.x));
                this.yLimits = new Vector2(this.curve.Points.Min(p => p.y), this.curve.Points.Max(p => p.y));
            }
            float xPadding = graphPaddingFactor * Mathf.Abs(this.xLimits.x - this.xLimits.y);
            float yPadding = graphPaddingFactor * Mathf.Abs(this.yLimits.x- this.yLimits.y);
            this.simpleCurveDrawerStyle.FixedSection = new FloatRange(this.xLimits.x - xPadding, this.xLimits.y + xPadding);
            this.simpleCurveDrawerStyle.FixedScale = new Vector2(this.yLimits.x - yPadding, this.yLimits.y + yPadding);
            this.limitSet = true;
        }

        private static FieldInfo FI_scen = AccessTools.Field(typeof(Listing_ScenEdit), "scen");

        public Rect GetCustomScenPartRect(Listing_ScenEdit listing, ScenPart part, float height)
        {
            Scenario GetScen() => (Scenario)FI_scen.GetValue(listing);

            // setup outer box (coloring)
            float labelRectHeight = Text.LineHeight + 5f;
            Rect rect = listing.GetRect(height + labelRectHeight);
            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.08f));

            Rect labelRect = rect.TopPartPixels(labelRectHeight);
            Widgets.Label(labelRect, part.Label);

            Rect graphRect = rect.BottomPartPixels(rect.height - labelRectHeight);
            WidgetRow widgetRow = new WidgetRow(graphRect.x, graphRect.y, UIDirection.RightThenDown, 72f, 0f);
            if (part.def.PlayerAddRemovable)
            {
                Color? mouseoverColor = new Color?(GenUI.SubtleMouseoverColor);
                if (widgetRow.ButtonIcon(Textures.DeleteX, null, mouseoverColor))
                {
                    GetScen().RemovePart(part);
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }
            }
            if (GetScen().CanReorder(part, ReorderDirection.Up) && widgetRow.ButtonIcon(Textures.ReorderUp, null, null))
            {
                GetScen().Reorder(part, ReorderDirection.Up);
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            if (GetScen().CanReorder(part, ReorderDirection.Down) && widgetRow.ButtonIcon(Textures.ReorderDown, null, null))
            {
                GetScen().Reorder(part, ReorderDirection.Down);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
            }
            listing.Gap(4f);

            return graphRect;
        }

        // COPY PASTA -> Verse.SimpleCurveDrawer
        public void DrawCurves(Rect rect, List<SimpleCurveDrawInfo> curves, SimpleCurveDrawerStyle style = null)
        {
            bool flag = true;
            Rect viewRect = default(Rect);
            for (int i = 0; i < curves.Count; i++)
            {
                SimpleCurveDrawInfo simpleCurveDrawInfo = curves[i];
                if (simpleCurveDrawInfo.curve != null)
                {
                    if (flag)
                    {
                        flag = false;
                        viewRect = simpleCurveDrawInfo.curve.View.rect;
                    }
                    else
                    {
                        viewRect.xMin = Mathf.Min(viewRect.xMin, simpleCurveDrawInfo.curve.View.rect.xMin);
                        viewRect.xMax = Mathf.Max(viewRect.xMax, simpleCurveDrawInfo.curve.View.rect.xMax);
                        viewRect.yMin = Mathf.Min(viewRect.yMin, simpleCurveDrawInfo.curve.View.rect.yMin);
                        viewRect.yMax = Mathf.Max(viewRect.yMax, simpleCurveDrawInfo.curve.View.rect.yMax);
                    }
                }
            }
            if (style.UseFixedScale)
            {
                viewRect.yMin = style.FixedScale.x;
                viewRect.yMax = style.FixedScale.y;
            }
            if (style.OnlyPositiveValues)
            {
                if (viewRect.xMin < 0f)
                    viewRect.xMin = 0f;
                if (viewRect.yMin < 0f)
                    viewRect.yMin = 0f;
            }
            if (style.UseFixedSection)
            {
                viewRect.xMin = style.FixedSection.min;
                viewRect.xMax = style.FixedSection.max;
            }
            Rect rect2 = rect;
            if (style.DrawMeasures)
            {
                rect2.xMin += 60f;
                rect2.yMax -= 30f;
            }
            if (style.DrawBackground)
            {
                GUI.color = new Color(0.302f, 0.318f, 0.365f);
                GUI.DrawTexture(rect2, BaseContent.WhiteTex);
            }
            if (style.DrawBackgroundLines)
                SimpleCurveDrawer.DrawGraphBackgroundLines(rect2, viewRect);
            if (style.DrawMeasures)
                SimpleCurveDrawer.DrawCurveMeasures(rect, viewRect, rect2, style.MeasureLabelsXCount, style.MeasureLabelsYCount, style.XIntegersOnly, style.YIntegersOnly);
            foreach (SimpleCurveDrawInfo current in curves)
                SimpleCurveDrawer.DrawCurveLines(rect2, current, style.DrawPoints, viewRect, style.UseAntiAliasedLines, style.PointsRemoveOptimization);
            // TODO: consider drawing without y-func alignment.
            if (style.DrawCurveMousePoint)
                SimpleCurveDrawer.DrawCurveMousePoint(curves, rect2, viewRect, style.LabelX);

            // Capture the cursors position on the graph
            GUI.BeginGroup(rect2);
            this.cursorPosition = SimpleCurveDrawer.ScreenToCurveCoords(rect2, viewRect, Event.current.mousePosition);
            GUI.EndGroup();

            return;
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

}
