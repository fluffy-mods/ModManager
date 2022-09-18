// Patch_WorkshopItems_NotifySubscribed.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Steamworks;
using Verse;
using Verse.Steam;

namespace ModManager {
    public class Patch_WorkshopItems_Events {
        /**
         * RimWorld rebuilds the entire mod list whenever an item is installed, uninstalled or even subscribed to. 
         * 
         * This is patently rediculous, as we know exactly what items were manipulated.
         */

        private static List<ModMetaData> modlister => Traverse.CreateWithType("ModLister")
            .Field("mods")
            .GetValue<List<ModMetaData>>();

        private static List<WorkshopItem> workshopitems => Traverse.CreateWithType("WorkshopItems")
            .Field("subbedItems")
            .GetValue<List<WorkshopItem>>();

        [HarmonyPatch(typeof(WorkshopItems), "Notify_Subscribed")]
        public class WorkshopItems_Notify_Subscribed {
            public static bool Prefix(PublishedFileId_t pfid) {
                Debug.Log("Notify_Subscribed");

                // check if item was already present.
                WorkshopItem item = WorkshopItem.MakeFrom( pfid );

                if (item is WorkshopItem_Mod item_installed) {
                    // register item in WorkshopItems
                    workshopitems.Add(item_installed);

                    // register item in ModLister
                    ModMetaData mod = new ModMetaData( item_installed );
                    modlister.Add(mod);

                    // show a message
                    Messages.Message(I18n.ModInstalled(mod.Name), MessageTypeDefOf.PositiveEvent, false);

                    // notify button manager that we done stuff.
                    ModButtonManager.Notify_DownloadCompleted(mod);
                } else {
                    // add dowloading item to MBM
                    ModButton_Downloading button = new ModButton_Downloading(pfid);
                    ModButtonManager.TryAdd(button);
                    Page_BetterModConfig.Instance.Selected = button;
                }

                // do whatever needs doing for ScenarioLister.
                ScenarioLister.MarkDirty();
                return false;
            }
        }

        [HarmonyPatch(typeof(WorkshopItems), "Notify_Unsubscribed")]
        public class WorkshopItems_Notify_Unsubscribed {
            public static bool Prefix(PublishedFileId_t pfid) {
                Debug.Log("Notify_Unsubscribed");

                // deregister item in WorkshopItems
                WorkshopItem item = workshopitems.FirstOrDefault( i => i.PublishedFileId == pfid );
                workshopitems.TryRemove(item);

                // deregister item in ModLister
                ModMetaData mod = modlister.FirstOrDefault( m => m.Source == ContentSource.SteamWorkshop &&
                                                         m.PackageId == pfid.ToString() );
                modlister.TryRemove(mod);

                // remove button
                ModButtonManager.Notify_Unsubscribed(pfid.ToString());

                ScenarioLister.MarkDirty();
                return false;
            }
        }

        [HarmonyPatch(typeof(WorkshopItems), "Notify_Installed")]
        public class WorkshopItems_Notify_Installed {
            public static bool Prefix(PublishedFileId_t pfid) {
                Debug.Log("Notify_Installed");

                // register item in WorkshopItems
                WorkshopItem item = WorkshopItem.MakeFrom( pfid );
                workshopitems.Add(item);

                // register item in ModLister
                ModMetaData mod = new ModMetaData( item );
                modlister.Add(mod);

                // show a message
                Messages.Message(I18n.ModInstalled(mod.Name), MessageTypeDefOf.PositiveEvent, false);

                // notify button manager that we done stuff.
                ModButtonManager.Notify_DownloadCompleted(mod);

                ScenarioLister.MarkDirty();
                return false;
            }
        }
    }
}
