// Extensions.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModManager
{
    public static class Extensions
    {
        public static string SanitizeYaml( this string msg )
        {
            // TODO: conditionally escape only when needed
            return $"'{msg.Replace( "'", "''" )}'";
        }

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
            if ( !activeMods.Any( am => am?.PackageId == mod.PackageId ) )
                return -1;
            return activeMods.FirstIndexOf( am => am.SamePackageId( mod.PackageId ) );
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
                mod.SamePackageId( m.Content.PackageId ) && 
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
        
        public static bool IsLocalCopy( this ModMetaData mod )
        {
            return mod.Source == ContentSource.ModsFolder && 
                mod.PackageId.StartsWith( IO.LocalCopyPrefix );
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

        
        // https://stackoverflow.com/a/4975942/2604271
        public static string ToStringSize( this long bytes )
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if ( bytes == 0 )
                return "0" + suf[0];
            var place = Convert.ToInt32( Math.Floor( Math.Log( bytes, 1024 ) ) );
            var num   = Math.Round( bytes / Math.Pow( 1024, place ), 1 );
            return ( Math.Sign( bytes ) * num ) + suf[place];
        }

        public static ModAttributes UserData( this ModMetaData mod ) => ModManager.UserData[mod];
    }
}