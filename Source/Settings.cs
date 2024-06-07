using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimLog
{
    public class RimLogSettings : ModSettings
    {
        public bool isEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isEnabled, "isEnabled", true, true);
        }

        public static void DoWindowContents(Rect canvas)
        {
            var columnWidth = (canvas.width - 30)/2 - 2;
            var list = new Listing_Standard { ColumnWidth = columnWidth };
            list.Begin(canvas);
            list.Gap(4);

            list.CheckboxLabeled("RL.isEnabled".Translate(),
                                 ref RimLog.Settings.isEnabled,
                                 "RL.isEnabled".Translate());

            list.End();
        }
    }
}
