using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace CarpenterTable
{

    public class CarpenterTablesSettings : ModSettings
    {

        public static bool deconstructInadequateProducts = true;

        public void DoWindowContents(Rect wrect)
        {
            var options = new Listing_Standard();
            var defaultColor = GUI.color;
            options.Begin(wrect);
            GUI.color = defaultColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Automatic deconstruction of low-quality furniture
            options.Gap();
            options.CheckboxLabeled("CarpenterTables.DeconstructInadequateProducts".Translate(), ref deconstructInadequateProducts, "CarpenterTables.DeconstructInadequateProducts_ToolTip".Translate());

            // Finish
            options.End();
            Mod.GetSettings<CarpenterTablesSettings>().Write();

        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref deconstructInadequateProducts, "deconstructInadequateProducts", true);
        }

    }

}
