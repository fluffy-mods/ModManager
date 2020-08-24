using UnityEngine;
using Verse;

namespace ModManager
{
    public class ModManagerSettings : ModSettings
    {

        public bool ShowPromotions                  = true;
        public bool ShowPromotions_NotSubscribed    = true;
        public bool ShowPromotions_NotActive        = false;
        public bool TrimTags                        = true;
        public bool TrimVersionStrings              = false;
        public bool AddModManagerToNewModLists      = true;
        public bool ShowSatisfiedRequirements       = false;
        public bool AddExpansionsToNewModLists      = true;
        public bool ShowVersionChecksOnSteamMods    = false;
        public bool AddHugsLibToNewModLists         = false;
        public bool UseTempFolderForCrossPromotions = false;

        public bool SurveyNotificationShown = false;

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
            Scribe_Values.Look( ref UseTempFolderForCrossPromotions, "UseTempFolderForCrossPromotions", false  );
            Scribe_Values.Look( ref SurveyNotificationShown, "SurveyNotificationShown", false  );
        }


        public void DoWindowContents(Rect canvas)
        {
            var listing = new Listing_Standard();
            listing.ColumnWidth = canvas.width;
            listing.Begin(canvas);
            listing.CheckboxLabeled(I18n.ShowAllRequirements, ref ShowSatisfiedRequirements,
                                     I18n.ShowAllRequirementsTip);
            listing.CheckboxLabeled(I18n.ShowVersionChecksForSteamMods, ref ShowVersionChecksOnSteamMods,
                                     I18n.ShowVersionChecksForSteamModsTip);

            listing.Gap();
            listing.CheckboxLabeled(I18n.ShowPromotions, ref ShowPromotions, I18n.ShowPromotionsTip);

            if (!ShowPromotions)
                GUI.color = Color.grey;

            listing.CheckboxLabeled(I18n.ShowPromotions_NotSubscribed, ref ShowPromotions_NotSubscribed);
            listing.CheckboxLabeled(I18n.ShowPromotions_NotActive, ref ShowPromotions_NotActive);
            var before                                                = UseTempFolderForCrossPromotions;
            if ( CrossPromotionManager.cachePathOverriden ) GUI.color = Color.grey;
            listing.CheckboxLabeled( I18n.UseTempFolderForCrossPromotionCache, ref UseTempFolderForCrossPromotions,
                                     I18n.UseTempFolderForCrossPromotionCacheTip );
            if ( before != UseTempFolderForCrossPromotions ) CrossPromotionManager.Notify_CrossPromotionPathChanged();
            if ( CrossPromotionManager.CacheCount > 0 )
            {
                GUI.color = Color.white;
                if ( listing.ButtonTextLabeled( I18n.CrossPromotionCacheFolderSize( CrossPromotionManager.CacheSize ),
                                                I18n.DeleteCrossPromotionCache ) )
                {
                    CrossPromotionManager.DeleteCache();
                }
            }
            else
            {
                GUI.color = Color.grey;
                listing.Label( I18n.CrossPromotionCacheFolderSize( CrossPromotionManager.CacheSize ) );
            }

            GUI.color = Color.white;
            listing.Gap();

            listing.CheckboxLabeled(I18n.TrimTags, ref TrimTags, I18n.TrimTagsTip);
            if (!TrimTags)
                GUI.color = Color.grey;
            listing.CheckboxLabeled(I18n.TrimVersionStrings, ref TrimVersionStrings,
                                     I18n.TrimVersionStringsTip);

            GUI.color = Color.white;
            listing.Gap();
            listing.CheckboxLabeled(I18n.AddModManagerToNewModList, ref AddModManagerToNewModLists,
                                     I18n.AddModManagerToNewModListTip);
            listing.CheckboxLabeled(I18n.AddHugsLibToNewModList, ref AddHugsLibToNewModLists,
                                     I18n.AddHugsLibToNewModListTip);
            listing.CheckboxLabeled(I18n.AddExpansionsToNewModList, ref AddExpansionsToNewModLists,
                                     I18n.AddExpansionsToNewModListTip);
            listing.End();
        }
    }
}