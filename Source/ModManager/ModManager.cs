using System.Reflection;
using HarmonyLib;
using RecursiveProfiler;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class ModManager: Mod
    {
        public ModManager( ModContentPack content ) : base( content )
        {
            Instance = this;
            var harmonyInstance = new Harmony( "fluffy.modmanager" );

#if DEBUG
            Harmony.DEBUG = true;
#endif
            harmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );

#if DEBUG
            LongEventHandler.ExecuteWhenFinished( () => new Profiler(
                                                      typeof( Page_BetterModConfig ).GetMethod(
                                                          nameof( Page_BetterModConfig.DoWindowContents ) ) ) );
#endif
        }

        public static ModManager Instance { get; private set; }
        public static ModManagerSettings Settings => Instance.GetSettings<ModManagerSettings>();

        public override string SettingsCategory() => I18n.SettingsCategory;

        public override void DoSettingsWindowContents( Rect canvas )
        {
            base.DoSettingsWindowContents( canvas );
            var listing = new Listing_Standard();
            listing.ColumnWidth = canvas.width;
            listing.Begin( canvas );
            listing.CheckboxLabeled( I18n.ShowPromotions, ref Settings.ShowPromotions, I18n.ShowPromotionsTip );

            if ( !Settings.ShowPromotions )
                GUI.color = Color.grey;

            listing.CheckboxLabeled( I18n.ShowPromotions_NotSubscribed, ref Settings.ShowPromotions_NotSubscribed );
            listing.CheckboxLabeled( I18n.ShowPromotions_NotActive, ref Settings.ShowPromotions_NotActive );

            GUI.color = Color.white;
            listing.Gap();

            listing.CheckboxLabeled( I18n.TrimTags, ref Settings.TrimTags, I18n.TrimTagsTip );
            if ( !Settings.TrimTags )
                GUI.color = Color.grey;
            listing.CheckboxLabeled( I18n.TrimVersionStrings, ref Settings.TrimVersionStrings, I18n.TrimVersionStringsTip );

            GUI.color = Color.white;
            listing.Gap();
            listing.CheckboxLabeled( I18n.AddModManagerToNewModList, ref Settings.AddModManagerToNewModLists,
                                     I18n.AddModManagerToNewModListTip );
            listing.End();
        }
    }
}