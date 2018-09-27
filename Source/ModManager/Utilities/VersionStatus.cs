// ModButton_Installed.cs
// Copyright Karel Kroeze, 2018-2018

using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public enum VersionMatch
    {
        CurrentVersion,
        InvalidVersion,
        DifferentBuild,
        DifferentVersion
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

        public VersionStatus( ModMetaData mod )
        {
            version = mod.TargetVersion;
            if ( !VersionControl.IsWellFormattedVersionString( mod.TargetVersion ) )
            {
                match = VersionMatch.InvalidVersion;
                tip = I18n.InvalidVersion( version );
                return;
            }

            var _version = VersionControl.VersionFromString( version );
            if ( _version.Major != VersionControl.CurrentMajor || _version.Minor != VersionControl.CurrentMinor )
            {
                match = VersionMatch.DifferentVersion;
                tip = I18n.DifferentVersion( mod );
                return;
            }
            if ( _version.Build != VersionControl.CurrentBuild )
            {
                match = VersionMatch.DifferentBuild;
                tip = I18n.DifferentBuild( mod );
                return;
            }
            match = VersionMatch.CurrentVersion;
            tip = I18n.CurrentVersion;
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