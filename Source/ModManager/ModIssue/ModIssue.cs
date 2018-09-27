// ModIssue.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public enum Severity
    {
        Notice,
        Minor,
        Major,
        Critical
    }

    public enum Subject
    {
        Version,
        Dependency,
        LoadOrder,
        Other
    }


    public struct ModIssue
    {
        public Severity severity;
        public Subject subject;
        public ModButton button;
        public string tip;
        public string targetId;
        public Action resolver;

        public ModIssue( Severity severity, Subject subject, ModButton button, string targetId, string tip = null, Action resolver = null )
        {
            this.severity = severity;
            this.subject = subject;
            this.button = button;
            this.targetId = targetId;
            this.tip = tip;
            this.resolver = resolver;
        }

        public static ModIssue DifferentBuild( ModButton_Installed button )
        {
            return new ModIssue( Severity.Notice, Subject.Version, button, button.Identifier,
                I18n.DifferentBuild( button.Selected ) );
        }

        public static ModIssue UpdateAvailable( ModButton_Installed button )
        {
            return new ModIssue( Severity.Minor, Subject.Version, button, button.Identifier,
                I18n.UpdateAvailable( button.Manifest.Version, button.Manifest.Version));
        }

        public static ModIssue DifferentVersion( ModButton_Installed button )
        {
            return new ModIssue( Severity.Critical, Subject.Version, button, button.Identifier,
                I18n.DifferentVersion( button.Selected ) );
        }

        public static ModIssue InvalidVersion( ModButton_Installed button )
        {
            return new ModIssue( Severity.Minor, Subject.Version, button, button.Identifier,
                I18n.InvalidVersion( button.Selected.TargetVersion ) );
        }

        public static ModIssue MissingMod( ModButton_Missing button )
        {
            return new ModIssue( Severity.Major, Subject.Other, button, button.Identifier,
                I18n.MissingMod( button.Name, button.Identifier ),
                () => Resolvers.ResolveFindMod( button.Name, button, replace: true ) );
        }

        public static ModIssue CoreNotFirst( ModButton_Installed core )
        {
            return new ModIssue( Severity.Critical, Subject.LoadOrder, core, core.Identifier,
                I18n.CoreNotFirst, () => Resolvers.ResolveCoreShouldLoadFirst( core ) );
        }

        public Color Color
        {
            get
            {
                switch ( severity )
                {
                    case Severity.Notice:
                        return Color.grey;
                    case Severity.Minor:
                        return Color.yellow;
                    case Severity.Major:
                        return new Color( 1f, .55f, 0f );
                    default:
                        return Color.red;
                }
            }
        }
    }
}