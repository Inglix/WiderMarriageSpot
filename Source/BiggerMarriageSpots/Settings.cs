using Verse;

namespace BiggerMarriageSpots
{
    public class Settings : ModSettings
    {
        public bool ShareCenterCell;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ShareCenterCell, "shareCenterCell");
        }
    }
}