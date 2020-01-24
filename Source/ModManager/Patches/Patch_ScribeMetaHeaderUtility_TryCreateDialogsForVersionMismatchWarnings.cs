// Patch_ScribeMetaHeaderUtility_TryCreateDialogsForVersionMismatchWarnings.cs
// Copyright Karel Kroeze, 2019-2019

using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ModManager
{
//    [HarmonyPatch(typeof( ScribeMetaHeaderUtility ), nameof( ScribeMetaHeaderUtility.TryCreateDialogsForVersionMismatchWarnings ) )]
    public class Patch_ScribeMetaHeaderUtility_TryCreateDialogsForVersionMismatchWarnings
    {
        public static bool Prefix( bool __result, Action confirmedAction, ScribeMetaHeaderUtility.ScribeHeaderMode ___lastMode )
        {
            string message = null;
            string title = null;

            if (!BackCompatibility.IsSaveCompatibleWith(ScribeMetaHeaderUtility.loadedGameVersion) &&
                // ScribeMetaHeaderUtility.VersionsMatch is private, it's component parts are not...
                VersionControl.BuildFromVersionString( ScribeMetaHeaderUtility.loadedGameVersion ) != VersionControl.BuildFromVersionString( VersionControl.CurrentVersionStringWithRev ) )
            {
                title = "VersionMismatch".Translate();
                var loadedVersion = !ScribeMetaHeaderUtility.loadedGameVersion.NullOrEmpty() ? ScribeMetaHeaderUtility.loadedGameVersion : ("(" + "UnknownLower".Translate() + ")");
                switch ( ___lastMode )
                {
                    case ScribeMetaHeaderUtility.ScribeHeaderMode.Map:
                        message = "SaveGameIncompatibleWarningText".Translate(loadedVersion, VersionControl.CurrentVersionString);
                        break;
                    case ScribeMetaHeaderUtility.ScribeHeaderMode.World:
                        message = "WorldFileVersionMismatch".Translate(loadedVersion, VersionControl.CurrentVersionString);
                        break;
                    default:
                        message = "FileIncompatibleWarning".Translate(loadedVersion, VersionControl.CurrentVersionString);
                        break;
                }
            }
            bool modMismatch = false;
            if (!ScribeMetaHeaderUtility.LoadedModsMatchesActiveMods(out string loadedMods, out string activeMods))
            {
                modMismatch = true;
                string modsMismatchMessage = "We're terribly sorry this message is so useless" + "ModsMismatchWarningText".Translate(loadedMods, activeMods);
                if (message == null)
                {
                    message = modsMismatchMessage;
                }
                else
                {
                    message = message + "\n\n" + modsMismatchMessage;
                }
                if (title == null)
                {
                    title = "ModsMismatchWarningTitle".Translate();
                }
            }
            if (message != null)
            {
                var dialog = Dialog_MessageBox.CreateConfirmation( message, confirmedAction, false, title );
                dialog.buttonAText = "LoadAnyway".Translate();

                if (modMismatch)
                {

                    dialog.buttonCText = "ChangeLoadedMods".Translate();
                    dialog.buttonCAction = delegate ()
                    {
                        // TODO: load mods from save game, mod manager style.
                        // Probably want to open the mod menu?
                        // ModsConfig.RestartFromChangedMods();
                    };
                }
                Find.WindowStack.Add( dialog );
                __result = true;
            }
            else
            {
                __result = false;
            }

            return false;
        }
    }
}