using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ModManager
{
    public class ModManagerSettings : ModSettings
    {

        public bool ShowPromotions = true;
        public bool ShowPromotions_NotSubscribed = true;
        public bool ShowPromotions_NotActive = false;
        public bool TrimTags = true;
        public bool TrimVersionStrings = false;
        public bool AddModManagerToNewModLists = true;
        public bool ShowSatisfiedRequirements = false;
        public bool AddExpansionsToNewModLists = true;
        public bool ShowVersionChecksOnSteamMods = false;
        public bool AddHugsLibToNewModLists = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look( ref ShowPromotions, "ShowPromotions", true );
            Scribe_Values.Look( ref ShowPromotions_NotSubscribed, "ShowPromotions_NotSubscribed", true );
            Scribe_Values.Look( ref ShowPromotions_NotActive, "ShowPromotions_NotActive", false );
            Scribe_Values.Look( ref TrimTags, "TrimTags", true );
            Scribe_Values.Look( ref TrimVersionStrings, "TrimVersionStrings", false );
            Scribe_Values.Look( ref AddHugsLibToNewModLists, "AddHugsLibToNewModLists", true );
            Scribe_Values.Look( ref AddModManagerToNewModLists, "AddModManagerToNewModLists", true );
            Scribe_Values.Look( ref AddExpansionsToNewModLists, "AddExpansionsToNewModLists", true );
            Scribe_Values.Look( ref ShowSatisfiedRequirements, "ShowSatisfiedRequirements", false );
            Scribe_Values.Look( ref ShowVersionChecksOnSteamMods, "ShowVersionChecksOnSteamMods", false );
        }
    }
}