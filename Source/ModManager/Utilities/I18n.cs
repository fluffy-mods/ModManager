// I18n.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Security.Policy;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public static class I18n
    {
        private const string PREFIX = "Fluffy.ModManager";

        private static string Key( string key )
        {
            return $"{PREFIX}.{key}";
        }

        private static string Key( params string[] keys )
        {
            return $"{PREFIX}.{string.Join( ".", keys )}";
        }

        public static string AvailableMods = Key( "AvailableMods" ).Translate();
        public static string ActiveMods = Key( "ActiveMods" ).Translate();
        public static string NoModSelected = Key( "NoModSelected" ).Translate();

        public static string Preview = Key( "Preview" ).Translate();
        public static string Title = "Title".Translate(); // core
        public static string Author = "Author".Translate(); // core
        public static string TargetVersion = Key( "TargetVersion" ).Translate();
        public static string Version = Key( "Version" ).Translate();
        public static string Unknown = Key( "Unknown" ).Translate();
        public static string ModsChanged = "ModsChanged".Translate();
        public static string Later = Key( "Later" ).Translate();
        public static string Dependencies = Key( "Dependencies" ).Translate();
        public static string Details = Key( "Details" ).Translate();

        public static string OK = "OK".Translate(); // core
        public static string Yes = "Yes".Translate(); // core
        public static string No = "No".Translate(); // core

        public static string InvalidVersion( string version )
        {
            return Key( "InvalidVersion" ).Translate( version );
        }

        public static string DifferentVersion( ModMetaData mod, string versionString = null )
        {
            var version = new Version( versionString ?? mod.TargetVersion );
            return Key( "DifferentVersion" ).Translate( mod.Name, version.Major + "." + version.Minor,
                VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor );
        }

        public static string DifferentBuild( ModMetaData mod, string versionString = null )
        {
            var version = new Version(versionString ?? mod.TargetVersion );
            return Key( "DifferentBuild" ).Translate( mod.Name, version.Build, VersionControl.CurrentBuild );
        }

        public static string CurrentVersion = Key("CurrentVersion").Translate();

        public static string UpdateAvailable( Version current, Version latest )
        {
            return Key( "UpdateAvailable" ).Translate( current, latest );
        }

        public static string MissingMod( string name, string id )
        {
            return Key( "MissingMod" ).Translate( name, id );
        }

        public static string CoreNotFirst = Key( "CoreNotFirst" ).Translate();

        public static string DependencyNotFound( string id )
        {
            return Key( "DependencyNotFound" ).Translate( id );
        }

        public static string DependencyUnknownVersion( Dependency dep, ModButton_Installed tgt )
        {
            return Key( "DependencyUnknownVersion" ).Translate( dep, tgt.Name );
        }

        public static string DependencyWrongVersion( Dependency dep, ModButton_Installed tgt )
        {
            return Key( "DependencyWrongVersion" ).Translate( dep, tgt.Manifest.Version );
        }

        public static string DependencyMet( ModButton_Installed tgt )
        {
            return Key( "DependencyMet" ).Translate( tgt.Name, tgt.Manifest.Version );
        }

        public static string IncompatibleMod( string mod, string identifier )
        {
            return Key( "IncompatibleMod" ).Translate( identifier );
        }

        public static string ShouldBeLoadedBefore( string identifier )
        {
            return Key( "ShouldBeLoadedBefore" ).Translate( identifier );
        }

        public static string ShouldBeLoadedAfter( string identifier )
        {
            return Key( "ShouldBeLoadedAfter" ).Translate( identifier );
        }

        public static string GetMoreMods_SteamWorkshop = Key( "GetMoreMods_SteamWorkshop" ).Translate();
        public static string GetMoreMods_LudeonForums = Key( "GetMoreMods_LudeonForums" ).Translate();
        public static string UnSubscribe = Key( "SteamWorkshop.UnSubscribe" ).Translate();
        public static string ConfirmSteamWorkshopUpload = "ConfirmSteamWorkshopUpload".Translate(); // core
        public static string ConfirmContentAuthor = "ConfirmContentAuthor".Translate(); // core
        public static string RebuildingModList_Key = Key( "RebuildingModList" );
        public static string LoadModList = Key( "LoadModList" ).Translate();
        public static string AddModList = Key( "AddModList" ).Translate();
        public static string SaveModList = Key( "SaveModList" ).Translate();
        public static string DeleteModList = Key( "DeleteModList" ).Translate();
        public static string RenameModList = Key( "RenameModList" ).Translate();

        public static string CreateLocalCopies = Key( "CreateLocalCopies" ).Translate();

        public static string CreateLocalCopiesConfirmation( int count )
        {
            return Key( "CreateLocalCopiesConfirmation" ).Translate( count );
        }

        public static string CreateLocalCopy( string name )
        {
            return Key( "CreateLocalCopy" ).Translate( name );
        }

        public static string CreateLocalSucceeded( string name )
        {
            return Key( "CreateLocalSucceeded" ).Translate( name );
        }

        public static string CreateLocalFailed( string name )
        {
            return Key( "CreateLocalFailed" ).Translate( name );
        }

        public static string CreatingLocal( string name )
        {
            return Key( "CreatingLocal" ).Translate( name );
        }

        public static string DeleteLocalCopy( string name )
        {
            return Key( "DeleteLocalCopy" ).Translate( name );
        }

        public static string ConfirmRemoveLocal( string name )
        {
            return Key( "ConfirmRemoveLocal" ).Translate( name );
        }

        public static string RemoveLocalSucceeded( string name )
        {
            return Key( "RemoveLocalSucceeded" ).Translate( name );
        }

        public static string RemoveLocalFailed( string name )
        {
            return Key( "RemoveLocalFailed" ).Translate( name );
        }

        public static string ConfirmUnsubscribe( string name )
        {
            return "ConfirmUnsubscribe".Translate( name );
        } // core 

        public static string NeedsWellFormattedTargetVersion = "MessageModNeedsWellFormattedTargetVersion".Translate(
            VersionControl.CurrentVersionString );

        // Modlists
        public static string ModListsTip = Key( "ModListsTip" ).Translate();
        public static string InvalidName( string name, string invalidChars )
        {
            return Key( "InvalidName" ).Translate( name, invalidChars );
        }

        public static string ModListExists( string name )
        {
            return Key( "ModListExists" ).Translate( name );
        }

        public static string NameTooShort = Key( "NameTooShort" ).Translate();
        public static string LoadModListFromSave = Key( "LoadModListFromSave" ).Translate();
        public static string Problems = Key( "Problems" ).Translate();

        public static string ModListRenamed( string oldName, string newName )
        {
            return Key( "ModListRenamed" ).Translate( oldName, newName );
        }

        public static string ModListCreated( string name )
        {
            return Key( "ModListCreated" ).Translate( name );
        }

        public static string ModListLoaded( string name )
        {
            return Key( "ModListLoaded" ).Translate( name );
        }

        public static string ModListDeleted(string name)
        {
            return Key( "ModListDeleted" ).Translate( name );
        }

        public static string AddToModList = Key( "AddToModList" ).Translate();
        public static string RemoveFromModList = Key( "RemoveFromModList" ).Translate();

        public static string AddToModListX(string name)
        {
            return Key("AddToModListX").Translate(name);
        }
        public static string RemoveFromModListX(string name)
        {
            return Key("RemoveFromModListX").Translate(name);
        }


        // resolvers
        public static string MoveCoreToFirst = Key( "MoveCoreToFirst" ).Translate();

        public static string SearchSteamWorkshop( string name )
        {
            return Key( "SearchSteamWorkshop" ).Translate( name );
        }

        public static string SearchForum( string name )
        {
            return Key( "SearchForum" ).Translate( name );
        }

        public static string ActivateMod( ModMetaData mod )
        {
            return Key( "ActivateMod" ).Translate( mod.Name, Manifest.For( mod )?.Version,
                Key( "ContentSource", mod.Source.ToString() ).Translate() );
        }

        public static string NoMatchingModInstalled(string name, Version desired, Dependency.EqualityOperator op )
        {
            return Key( "NoMatchingModInstalled_Version" )
                .Translate( name, VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor, desired,
                    Dependency.OperatorToString( op ) );
        }
        public static string NoMatchingModInstalled(string name)
        {
            return Key("NoMatchingModInstalled")
                .Translate(name, VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor);
        }

        public static string DeactivateMod( ModButton_Installed mod )
        {
            return Key( "DeactivateMod" ).Translate( mod.Name );
        }

        public static string MoveBefore(ModButton_Installed from, ModButton_Installed to)
        {
            return Key("MoveBefore").Translate(from.Name, to.Name);
        }

        public static string MoveAfter(ModButton_Installed from, ModButton_Installed to)
        {
            return Key("MoveAfter").Translate(from.Name, to.Name);
        }

        public static string SourceModChanged = Key( "SourceModChanged" ).Translate();

        public static string UpdateLocalCopy( string name )
        {
            return Key("UpdateLocalCopy").Translate( name );
        }

        public static string ChangeColour = Key( "ChangeColour" ).Translate();
        public static string ChangeModColour( string name ) => Key( "ChangeModColour" ).Translate( name );
        public static string ChangeButtonColour( string name ) => Key( "ChangeButtonColour" ).Translate( name );
        public static string ChangeListColour = Key( "ChangeListColour" ).Translate();

        public static string ModHomePage( string url )
        {
            return Key( "ModHomePage" ).Translate( url );
        }

        public static string WorkshopPage( string subject )
        {
            return Key( "WorkshopPage" ).Translate( subject );
        }

        public static string DialogConfirmIssues( string description )
        {
            return Key( "DialogConfirmIssues" ).Translate( description );
        }

        public static string DialogConfirmIssuesTitle(int count )
        {
            return Key("DialogConfirmIssuesTitle").Translate(count);
        }

        // manifest
        public static string ManifestNotImplemented = Key( "ManifestNotImplemented" ).Translate();
        public static string FetchingOnlineManifest = Key( "FetchingOnlineManifest" ).Translate();
        public static string FetchingOnlineManifestFailed( string error)
        {
            return Key( "FetchingOnlineManifestFailed" ).Translate( error );
        }
        public static string NewVersionAvailable( Version current, Version latest )
        {
            return Key( "NewVersionAvailable" ).Translate( current, latest );
        }
        public static string LatestVersion = Key( "LatestVersion" ).Translate();

        // workshop
        public static string DownloadPending = Key( "DownloadPending" ).Translate();
        public static string ModInstalled( string name)
        {
            return Key( "ModInstalled" ).Translate( name );
        }
        public static string Subscribe( string name )
        {
            return Key( "Subscribe" ).Translate( name );
        }

        public static string SubscribeAllMissing = Key( "SubscribeAllMissing" ).Translate();
        public static string ResetMods = Key( "ResetMods" ).Translate();

        // promotions
        public static string PromotionsFor( string author ) => Key( "PromotionsFor" ).Translate( author );

        // options
        public static string SettingsCategory => Key( "SettingsCategory" ).Translate();
        public static string ShowPromotions => Key( "ShowPromotions" ).Translate();
        public static string ShowPromotionsTip => Key( "ShowPromotionsTip" ).Translate();
        public static string ShowPromotions_NotSubscribed => Key("ShowPromotions_NotSubscribed").Translate();
        public static string ShowPromotions_NotActive => Key("ShowPromotions_NotActive").Translate();

        // settings
        public static string ModSettings => Key( "ModSettings" ).Translate();
    }
}