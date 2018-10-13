// Extensions.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static int Compatibility( this ModMetaData mod, bool careAboutBuild = false )
        {
            if ( mod.VersionCompatible )
            {
                if ( careAboutBuild && VersionControl.CurrentBuild == VersionControl.BuildFromVersionString( mod.TargetVersion ) )
                {
                    return 2;
                }
                return 1;
            }
            return 0;
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
            return ModManager.Attributes[mod];
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
            return str.Replace( " ", "" );
        }

        public static bool IsSteamWorkshopIdentifier( this string identifier )
        {
            ulong dump;
            return ulong.TryParse( identifier, out dump );
        }
    }
}