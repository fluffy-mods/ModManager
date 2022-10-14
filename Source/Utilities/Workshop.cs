// Workshop.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Steamworks;
using Verse;
using Verse.Sound;

namespace ModManager {
    public static class Workshop {
        public static void Unsubscribe(ModMetaData mod, bool force = false) {
            if (force) {
                mod.enabled = false;
                AccessTools.Method(typeof(Verse.Steam.Workshop), "Unsubscribe").Invoke(null, new object[] { mod });
                return;
            }
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(I18n.ConfirmUnsubscribe(mod.Name), delegate {
                Unsubscribe(mod, true);
            }, true));
        }

        public static void Unsubscribe(IEnumerable<ModMetaData> mods) {
            string modList = mods
                         .Select( m => $"{m.Name} ({m.SupportedVersionsReadOnly.Select( v => v.ToString() ).StringJoin( ", " )})" )
                         .ToLineList();
            Dialog_MessageBox dialog = Dialog_MessageBox.CreateConfirmation(
                I18n.MassUnSubscribeConfirm( mods.Count(), modList ),
                () =>
                {
                    foreach ( ModMetaData mod in mods ) {
                        Unsubscribe( mod, true );
                    }
                },
                true );
            Find.WindowStack.Add(dialog);
        }

        public static void Subscribe(PublishedFileId_t fileId) {
            SteamUGC.SubscribeItem(fileId);
        }
        public static void Subscribe(string identifier) {
            Subscribe(new PublishedFileId_t(ulong.Parse(identifier)));
        }

        public static void Subscribe(IEnumerable<string> identifiers) {
            foreach (string identifier in identifiers) {
                Subscribe(identifier);
            }
        }

        public static void Upload(ModMetaData mod) {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(I18n.ConfirmSteamWorkshopUpload, delegate {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Dialog_MessageBox dialog_MessageBox = Dialog_MessageBox.CreateConfirmation(
                    I18n.ConfirmContentAuthor, delegate
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        AccessTools.Method( typeof( Verse.Steam.Workshop ), "Upload" )
                            .Invoke( null, new object[] {mod} );
                    }, true );
                dialog_MessageBox.buttonAText = I18n.Yes;
                dialog_MessageBox.buttonBText = I18n.No;
                dialog_MessageBox.interactionDelay = 6f;
                Find.WindowStack.Add(dialog_MessageBox);
            }, true));
        }

        public static void MassUnsubscribeFloatMenu() {
            List<FloatMenuOption> options = Utilities.NewOptionsList;
            IEnumerable<ModMetaData> steamMods = ModButtonManager.AllMods.Where( m => m.Source == ContentSource.SteamWorkshop );
            IEnumerable<ModMetaData> outdated = steamMods.Where( m => !m.VersionCompatible && !m.MadeForNewerVersion );
            IEnumerable<ModMetaData> inactive = ModButtonManager.AvailableMods.Where( m => m.Source == ContentSource.SteamWorkshop );

            options.Add(new FloatMenuOption(I18n.MassUnSubscribeAll, () => Unsubscribe(steamMods)));
            if (outdated.Any()) {
                options.Add(new FloatMenuOption(I18n.MassUnSubscribeOutdated, () => Unsubscribe(outdated)));
            }

            if (inactive.Any()) {
                options.Add(new FloatMenuOption(I18n.MassUnSubscribeInactive, () => Unsubscribe(inactive)));
            }

            Utilities.FloatMenu(options);
        }
    }
}
