using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace CarpenterTable
{

    public class CarpenterTables : Mod
    {
        public CarpenterTablesSettings settings;

        public CarpenterTables(ModContentPack content) : base(content)
        {
            GetSettings<CarpenterTablesSettings>();
        }

        public override string SettingsCategory() => "CarpenterTables.SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GetSettings<CarpenterTablesSettings>().DoWindowContents(inRect);
        }

    }

}
