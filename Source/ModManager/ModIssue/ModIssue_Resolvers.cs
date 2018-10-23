// ModIssue_Resolvers.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using static ModManager.Utilities;
using Version = System.Version;

namespace ModManager
{
    public static class Resolvers
    {
        private static FloatMenuOption WorkshopSearchOption( string name )
        {
            return new FloatMenuOption( I18n.SearchSteamWorkshop( name ),
                () => SteamUtility.OpenUrl( $"https://steamcommunity.com/workshop/browse/?appid=294100&searchtext={name}&browsesort=textsearch" ) );
        }

        private static FloatMenuOption ForumSearchOption( string name)
        {
            return new FloatMenuOption( I18n.SearchForum( name ),
                () => Application.OpenURL( $"https://ludeon.com/forums/index.php?action=search2&search={name}&brd[15]=15" ) );
        }

        private static FloatMenuOption DeactivateModOption( ModButton_Installed mod )
        {
            return new FloatMenuOption( I18n.DeactivateMod( mod ), () => mod.Active = false );
        }

        internal static FloatMenuOption SubscribeOption( string name, string identifier )
        {
            return new FloatMenuOption( I18n.Subscribe( name ), () => Workshop.Subscribe( identifier ) );
        }

        public static void ResolveFindMod( Dependency dependency, ModButton_Installed requester )
        {
            ResolveFindMod( dependency.Identifier, requester, dependency.Version, dependency.Operator );
        }

        public static void ResolveFindMod( 
            string identifier, 
            ModButton requester = null,
            Version desired = null, 
            Dependency.EqualityOperator op = Dependency.EqualityOperator.GreaterEqual, 
            Version current = null,
            bool replace = false )
        {
            // find identifier in available
            var mods = ModButtonManager.AvailableMods
                .Where( m => m.MatchesIdentifier( identifier ) )
                .Where( m => m.VersionCompatible )
                .Where( m => desired == null || Dependency.MatchesVersion( m, op, desired, true ) )
                .Where( m => current == null || Manifest.For( m )?.Version > current )
                .OrderByDescending( m => Manifest.For( m )?.Version );

            var options = NewOptions;
            if ( mods.Any() )
            {
                var insertIndex = requester != null
                    ? ModButtonManager.ActiveButtons.IndexOf( requester )
                    : ModButtonManager.ActiveButtons.Count;
                foreach ( var mod in mods )
                {
                    options.Add( new FloatMenuOption( I18n.ActivateMod( mod ), () =>
                    {
                        var button = ModButton_Installed.For( mod );
                        button.Selected = mod;
                        ModButtonManager.Insert( button, insertIndex );
                        if ( replace && requester != null && requester != button )
                            requester.Active = false;
                    } ) );
                }
            }
            else
            {
                if ( desired != null )
                    options.Add( new FloatMenuOption( I18n.NoMatchingModInstalled( identifier, desired, op ), null ) );
                else
                    options.Add( new FloatMenuOption( I18n.NoMatchingModInstalled( identifier ), null ) );
            }
            if ( requester is ModButton_Missing missing && missing.Identifier.IsSteamWorkshopIdentifier())
                options.Add( SubscribeOption( missing.Name, missing.Identifier ) );
            options.Add( WorkshopSearchOption( requester?.TrimmedName ?? identifier ) );
            options.Add( ForumSearchOption( requester?.TrimmedName ?? identifier ) );
            FloatMenu( options );
        }

        public static void ResolveWrongVersion( ModButton_Installed mod, Dependency desired )
        {
            ResolveFindMod( mod.Identifier, mod, desired.Version, desired.Operator, mod.Manifest?.Version );
        }

        public static void ResolveWrongTargetVersion(ModButton_Installed mod )
        {
            ResolveFindMod( mod.Identifier );
        }

        public static void ResolveIncompatible(ModButton_Installed mod, ModButton_Installed incompatible )
        {
            var options = NewOptions;
            options.Add( DeactivateModOption( incompatible ) );
            options.Add( DeactivateModOption( mod ) );
            FloatMenu( options );
        }

        private static FloatMenuOption MoveBeforeOption( ModButton_Installed from, ModButton_Installed to )
        {
            return new FloatMenuOption( I18n.MoveBefore( from, to ),
                () => ModButtonManager.Insert( from, ModButtonManager.ActiveButtons.IndexOf( to ) ) );
        }

        private static FloatMenuOption MoveAfterOption(ModButton_Installed from, ModButton_Installed to)
        {
            return new FloatMenuOption( I18n.MoveAfter( from, to ),
                () => ModButtonManager.Insert( from, ModButtonManager.ActiveButtons.IndexOf( to ) + 1 ) );
        }

        public static void ResolveShouldLoadBefore( ModButton_Installed mod, ModButton_Installed otherMod )
        {
            var options = NewOptions;
            options.Add( MoveBeforeOption( mod, otherMod ) );
            options.Add( MoveAfterOption( otherMod, mod ) );
            FloatMenu( options );
        }

        public static void ResolveShouldLoadAfter( ModButton_Installed mod, ModButton_Installed otherMod )
        {
            var options = NewOptions;
            options.Add( MoveAfterOption( mod, otherMod ) );
            options.Add( MoveBeforeOption( otherMod, mod ) );
            FloatMenu( options );
        }

        public static void ResolveCoreShouldLoadFirst(ModButton core)
        {
            var options = NewOptions;
            options.Add( new FloatMenuOption( I18n.MoveCoreToFirst, () => ModButtonManager.Insert( core, 0 ) ) );
            FloatMenu( options );
        }

        public static void ResolveUpdateLocalCopy( ModMetaData source, ModMetaData local )
        {
            var options = NewOptions;
            options.Add( new FloatMenuOption( I18n.UpdateLocalCopy( TrimModName( local.Name ) ), () => IO.TryUpdateLocalCopy( source, local ) ) );
            FloatMenu( options );
        }
    }
}