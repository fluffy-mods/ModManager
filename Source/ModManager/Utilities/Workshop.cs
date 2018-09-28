// Workshop.cs
// Copyright Karel Kroeze, 2018-2018

using Harmony;
using RimWorld;
using Steamworks;
using Verse;
using Verse.Sound;

namespace ModManager
{
    public static class Workshop
    {
        public static void Unsubscribe( ModMetaData mod )
        {
            Find.WindowStack.Add( Dialog_MessageBox.CreateConfirmation( I18n.ConfirmUnsubscribe( mod.Name ), delegate
            {
                mod.enabled = false;
                AccessTools.Method( typeof( Verse.Steam.Workshop ), "Unsubscribe" ).Invoke( null, new object[] {mod} );
                // TODO: check callback to remove mod from list.
            }, true ) );
        }

        public static void Subscribe( PublishedFileId_t id )
        {
            
        }

        public static void Upload( ModMetaData mod )
        {
            if ( !VersionControl.IsWellFormattedVersionString( mod.TargetVersion ) )
            {
                Messages.Message( I18n.NeedsWellFormattedTargetVersion, MessageTypeDefOf.RejectInput, false );
            }
            else
            {
                Find.WindowStack.Add( Dialog_MessageBox.CreateConfirmation( I18n.ConfirmSteamWorkshopUpload, delegate
                {
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
                    Find.WindowStack.Add( dialog_MessageBox );
                }, true ) );
            }
        }
    }
}