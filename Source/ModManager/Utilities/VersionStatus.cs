// ModButton_Installed.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public enum VersionMatch
    {
        CurrentVersion,
        DifferentBuild,
        DifferentVersion,
        InvalidVersion
    }

    public struct VersionStatus
    {
        public VersionMatch match;
        public string version;
        public string tip;

        public VersionStatus( VersionMatch match, string version, string tip )
        {
            this.match = match;
            this.version = version;
            this.tip = tip;
        }

        public static VersionStatus For( ModMetaData mod )
        {
            var manifest = Manifest.For( mod );
            if ( manifest != null && !manifest.TargetVersions.NullOrEmpty() )
                return For( mod, manifest );
            return For( mod, Match( mod.TargetVersion ), mod.TargetVersion );
        }

        public static VersionStatus For( ModMetaData mod, Manifest manifest )
        {
            var best = manifest.TargetVersions
                .Select( v => new {version = v, match = Match( v )} )
                .MinBy( m => m.match );
            return For( mod, best.match, best.version );
        }

        public static VersionStatus For( ModMetaData mod, VersionMatch match, string version )
        {
            return new VersionStatus( match, version, Tip( mod, match, version ) );
        }

        public static VersionStatus For( ModMetaData mod, VersionMatch match, Version version )
        {
            return For( mod, match, version.ToString() );
        }

        public static string Tip( ModMetaData mod, VersionMatch match, string version )
        {
            switch ( match )
            {
                case VersionMatch.CurrentVersion:
                    return I18n.CurrentVersion;
                case VersionMatch.DifferentBuild:
                    return I18n.DifferentBuild( mod, version );
                case VersionMatch.DifferentVersion:
                    return I18n.DifferentVersion( mod, version );
                case VersionMatch.InvalidVersion:
                default:
                    return I18n.InvalidVersion( version );

            }
        }

        public static VersionMatch Match( string version )
        {
            if (!VersionControl.IsWellFormattedVersionString(version))
                return VersionMatch.InvalidVersion;
            return Match( Manifest.ParseVersion( version, null ) );
        }

        public static VersionMatch Match( Version version )
        {
            if (version.Major != VersionControl.CurrentMajor || version.Minor != VersionControl.CurrentMinor)
                return VersionMatch.DifferentVersion;
            if (version.Build != VersionControl.CurrentBuild)
                return VersionMatch.DifferentBuild;
            return VersionMatch.CurrentVersion;
        }

        public static VersionStatus Unknown => new VersionStatus( VersionMatch.DifferentVersion, "?????",
            "Unknown Version" );

        public void Label( Rect canvas )
        {
            Label( canvas, UnityEngine.Color.white );
        }

        public void Label( Rect canvas, Color okColor )
        {
            GUI.color = Color( match, okColor );
            Widgets.Label( canvas, version );
            GUI.color = UnityEngine.Color.white;
            Tooltip( canvas );
        }

        public void Tooltip( Rect canvas )
        {
            TooltipHandler.TipRegion( canvas, tip );
        }

        public Color Color( Color? okColor = null )
        {
            return Color( match, okColor );
        }

        public static Color Color( VersionMatch match, Color? okColor = null )
        {
            switch ( match )
            {
                case VersionMatch.InvalidVersion:
                    return UnityEngine.Color.magenta;
                case VersionMatch.DifferentVersion:
                    return UnityEngine.Color.red;
                case VersionMatch.DifferentBuild:
                    return new Color( .9f, .9f, .9f );
                default:
                    return okColor ?? UnityEngine.Color.white;
            }
        }
    }
}