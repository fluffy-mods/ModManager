// Copyright Karel Kroeze, 2018-2018

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModManager
{
    public struct VersionStatus
    {
        public bool match;
        public List<Version> versions;
        public string tip;

        public VersionStatus( bool match, List<Version> versions, string tip )
        {
            this.match = match;
            this.versions = versions;
            this.tip = tip;
        }

        public static VersionStatus For( ModMetaData mod )
        {
            var manifest = Manifest.For( mod );
            if ( manifest != null && !manifest.TargetVersions.NullOrEmpty() )
                return For( mod, manifest );
            return For( mod, mod.SupportedGameVersionsReadOnly );
        }

        public static VersionStatus For( ModMetaData mod, Manifest manifest )
        {
            return For( mod, manifest.TargetVersions );
        }

        public static VersionStatus For( ModMetaData mod, List<Version> versions)
        {
            return For(mod, versions.Any(v => VersionControl.IsCompatible(v)), versions);
        }

        public static VersionStatus For( ModMetaData mod, bool match, List<Version> versions )
        {
            return new VersionStatus( match, versions, Tip( mod, match, versions ) );
        }


        public static string Tip( ModMetaData mod, bool match, List<Version> versions )
        {
            if ( match )
                return I18n.CurrentVersion;
            else 
                return I18n.DifferentVersion( mod );
        }

        public void Label( Rect canvas )
        {
            Label( canvas, UnityEngine.Color.white );
        }

        public void Label( Rect canvas, Color okColor )
        {
            GUI.color = Color( match, okColor );
            Widgets.Label( canvas, versions.VersionList() );
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

        public static Color Color( bool match, Color? okColor = null )
        {
            return match ? okColor ?? UnityEngine.Color.white : UnityEngine.Color.red;
        }
    }
}