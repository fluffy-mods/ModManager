// Extensions.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public static class Extensions
    {
        public static string StringJoin( this IEnumerable<string> list, string glue )
        {
            return string.Join( glue, list.ToArray() );
        }

        public static int LoadOrder( this ModMetaData mod )
        {
            if ( mod == null )
            { 
                Log.Error( "Tried to get load order for NULL mod"  );
                return -1;
            }
            var activeMods = ModsConfig.ActiveModsInLoadOrder;
            if ( !activeMods.Any( am => am?.Identifier == mod.Identifier ) )
                return -1;
            return activeMods.FirstIndexOf( am => am.Identifier == mod.Identifier );
        }

        public static bool HasSettings( this ModMetaData mod )
        {
            return mod.SettingsCategory() != null;
        }

        public static string SettingsCategory( this ModMetaData mod )
        {
            return mod.ModClassWithSettings()?.SettingsCategory();
        }

        private static Dictionary<ModMetaData, Mod> _modClassWithSettingsCache = new Dictionary<ModMetaData, Mod>();

        public static Mod ModClassWithSettings( this ModMetaData mod )
        {
            if ( _modClassWithSettingsCache.TryGetValue( mod, out var modClass ) )
                return modClass;
            modClass = LoadedModManager.ModHandles.FirstOrDefault( m => 
                m.Content.Identifier == mod.Identifier &&
                !m.SettingsCategory().NullOrEmpty() );
            _modClassWithSettingsCache.Add( mod, modClass );
            return modClass;
        }

        public static int Compatibility( this ModMetaData mod, bool careAboutBuild = false )
        {
            if ( mod.VersionCompatible )
                return 1;
            return 0;
        }

        public static string VersionList( this IEnumerable<Version> versions )
        {
            return versions.Select( v => $"{v.Major}.{v.Minor}" ).StringJoin( ", " );
        }

        private static Dictionary<ModMetaData, VersionStatus> _versionStatusCache = new Dictionary<ModMetaData, VersionStatus>();
        public static VersionStatus GetVersionStatus( this ModMetaData mod )
        {
            VersionStatus status;
            if ( _versionStatusCache.TryGetValue( mod, out status ) )
                return status;
            status = VersionStatus.For( mod );
            _versionStatusCache.Add( mod, status );
            return status;
        }

        public static Color Desaturate( this Color color, float saturation = 0.5f )
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            s *= saturation;
            v *= saturation;
            return Color.HSVToRGB(h, s, v);
        }

        public static bool TryRemove<T>( this List<T> list, T element )
        {
            if ( element != null && list.Contains( element ) )
                return list.Remove( element );
            return false;
        }

        public static bool TryAdd<T>( this List<T> list, T element )
        {
            if ( element != null && !list.Contains( element ) )
            {
                list.Add( element );
                return true;
            }
            return false;
        }

        public static string AboutDir( this ModMetaData mod ) => Path.Combine( mod.RootDir.FullName, "About" );

        public static ModAttributes Attributes( this ModMetaData mod )
        {
            return ModManager.Settings[mod];
        }

        public static bool IsLocalCopy( this ModMetaData mod )
        {
            return mod.Source == ContentSource.LocalFolder && 
                mod.Identifier.StartsWith( IO.LocalCopyPrefix );
        }

        public static bool MatchesIdentifier( this ModMetaData mod, string identifier )
        {
            identifier = identifier.StripSpaces();
            return !identifier.NullOrEmpty() &&
                   ( mod.Identifier.StripSpaces() == identifier
                     || mod.Name.StripSpaces() == identifier
                     || Manifest.For( mod )?.identifier == identifier );
        }

        public static string StripSpaces( this string str )
        {
            if ( str == null ) return null;  // garbage in, garbage out.
            return str.Replace( " ", "" );
        }

        public static bool IsSteamWorkshopIdentifier( this string identifier )
        {
            ulong dump;
            return ulong.TryParse( identifier, out dump );
        }
    }
}