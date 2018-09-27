// Patch_Page_ModsConfig_NotifySteamItemUnsubscribed.cs
// Copyright Karel Kroeze, 2018-2018

using Harmony;
using RimWorld;
using Steamworks;

namespace ModManager
{
    [HarmonyPatch( typeof( Page_ModsConfig ), "Notify_SteamItemUnsubscribed" )]
    public class Patch_Page_ModsConfig_NotifySteamItemUnsubscribed
    {
        public static void Postfix( PublishedFileId_t pfid )
        {
            ModButtonManager.Notify_Unsubscribed( pfid.ToString() );
        }
    }
}