using BiggerMarriageSpots.ModCompatibility;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BiggerMarriageSpots
{
    public class BiggerMarriageSpots : Mod
    {
        public static Settings Settings;
        public static Harmony Harmony;
        
        public BiggerMarriageSpots(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
            
            Harmony = new Harmony("Inglix.BiggerMarriageSpots");
            Harmony.PatchAll();

            if (ModLister.HasActiveModWithName("Romance On The Rim"))
            {
                RomanceOnTheRimPatches.PatchMod(Harmony);
            }
        }

        public override string SettingsCategory()
        {
            return "Inglix.WiderMarriageSpot".Translate();
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            base.DoSettingsWindowContents(canvas);

            var listing = new Listing_Standard();
            listing.Begin(canvas);
            listing.CheckboxLabeled("Inglix.ShareCenterCellDuringMarriageCeremony".Translate(), ref Settings.ShareCenterCell, "Inglix.ShareCenterCellTooltip".Translate());
            listing.End();
        }
    }
}