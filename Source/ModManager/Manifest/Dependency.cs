// Manifest.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;
using static ModManager.Constants;
using static ModManager.Resolvers;

namespace ModManager
{
    public enum DependencyStatus
    {
        NotFound,
        UnknownVersion,
        WrongVersion,
        Met
    }

    public class Dependency
    {
        public EqualityOperator Operator { get; private set; }
        public Manifest Owner { get; set; }
        public ModButton_Installed Target { get; private set; }
        public Version Version { get; private set; }
        public string Identifier { get; private set; }

        public static string OperatorToString( EqualityOperator op )
        {
            switch (op)
            {
                case EqualityOperator.Equal:
                    return "==";
                case EqualityOperator.GreaterEqual:
                    return ">=";
                case EqualityOperator.LesserEqual:
                    return "<=";
                default:
                    return "";
            }
        }

        public string OperatorString => OperatorToString( Operator );

        public void LoadDataFromXmlCustom( XmlNode root )
        {
            try
            {
                var dep = root.InnerText;

                /**
                 * Can have either 1 or 3 parts, separated by spaces;
                 *  - 1 part; mod name.
                 *  - 3 parts; mod name, equality operator, version
                 *  
                 *  Note that the mod name is matched to visible mod name (without spaces), 
                 *  mod folder (what RW calls the identifier) and identifier (specified in mods' manifest).
                 *  
                 *  Valid equality operators are;
                 *  <= (before version)
                 *  == (exact version)
                 *  >= (equal or later version)
                 */

                var parts = dep.Split( " ".ToCharArray() );
                switch ( parts.Length )
                {
                    case 1:
                        Identifier = parts[0];
                        Operator = EqualityOperator.Exists;
                        break;
                    case 3:
                        Identifier = parts[0];
                        Operator = ParseEqualityOperator( parts[1] );
                        Version = new Version( parts[2] );
                        break;
                    default:
                        throw new FormatException( "invalid dependency format" );
                }
            }
            catch ( Exception e )
            {
                Log.Error( $"Failed to parse dependency '{root.InnerText}': {e.Message}\n\n{e.StackTrace}" );
            }
        }

        public static EqualityOperator ParseEqualityOperator( string op )
        {
            switch ( op )
            {
                case "<=":
                    return EqualityOperator.LesserEqual;
                case "==":
                    return EqualityOperator.Equal;
                case ">=":
                    return EqualityOperator.GreaterEqual;
                default:
                    throw new FormatException( "unknown equality operator" );
            }
        }

        public enum EqualityOperator
        {
            LesserEqual,
            Equal,
            GreaterEqual,
            Exists
        }

        public override string ToString()
        {
            return $"{Identifier} {OperatorString} {Version?.ToString() ?? ""}";
        }

        public DependencyStatus Met
        {
            get
            {
                // check if mod is loaded
                var mod = ModButtonManager.ActiveButtons
                    .OfType<ModButton_Installed>()
                    .FirstOrDefault( b => b.MatchesIdentifier( Identifier ) );
                if (mod == null )
                    return DependencyStatus.NotFound;

                Target = mod;

                // mod exists, check version
                // we don't care about version
                if ( Version == null )
                    return DependencyStatus.Met;

                // other mod has no version, no way to check
                var otherVersion = mod.Manifest?.Version;
                if ( otherVersion == null )
                    return DependencyStatus.UnknownVersion;

                switch ( Operator )
                {
                    case EqualityOperator.Equal:
                        if ( Version == otherVersion )
                            return DependencyStatus.Met;
                        return DependencyStatus.WrongVersion;
                    case EqualityOperator.GreaterEqual:
                        if ( otherVersion >= Version )
                            return DependencyStatus.Met;
                        return DependencyStatus.WrongVersion;
                    case EqualityOperator.LesserEqual:
                        if ( otherVersion <= Version )
                            return DependencyStatus.Met;
                        return DependencyStatus.WrongVersion;
                    default:
                        return DependencyStatus.UnknownVersion;
                }
            }
        }

        public bool MatchesVersion( ModMetaData mod, bool strict = true )
        {
            return MatchesVersion( mod, Operator, Version, !strict );
        }

        public static bool MatchesVersion( ModMetaData mod, EqualityOperator op, Version version, bool unknownResult = false )
        {
            var modVersion = Manifest.For( mod )?.Version;
            if ( modVersion == null || version == null )
                return unknownResult;

            switch ( op )
            {
                case EqualityOperator.Equal:
                    return version == modVersion;
                case EqualityOperator.Exists:
                    return mod != null;
                case EqualityOperator.GreaterEqual:
                    return modVersion >= version;
                case EqualityOperator.LesserEqual:
                    return modVersion <= version;
                default:
                    return unknownResult;
            }
        }

        public string Tooltip
        {
            get {
                switch ( Met )
                {
                    case DependencyStatus.NotFound:
                        return I18n.DependencyNotFound( Identifier );
                    case DependencyStatus.WrongVersion:
                        return I18n.DependencyWrongVersion( this, Target );
                    case DependencyStatus.UnknownVersion:
                        return I18n.DependencyUnknownVersion( this, Target );
                    case DependencyStatus.Met:
                        return I18n.DependencyMet( Target );
                    default: return null;
                }
            }
        }

        public void Draw( Rect canvas )
        {
            var statusRect = new Rect( 
                canvas.xMax - SmallIconSize - SmallMargin, 
                0f,
                SmallIconSize,
                SmallIconSize );
            statusRect = statusRect.CenteredOnYIn( canvas );
            Texture2D icon;
            switch ( Met )
            {
                case DependencyStatus.Met:
                    icon = Widgets.CheckboxOnTex;
                    break;
                case DependencyStatus.WrongVersion:
                    Utilities.ActionButton( canvas, () => ResolveWrongVersion( Target, this ) );
                    icon = Widgets.CheckboxOffTex;
                    break;
                case DependencyStatus.NotFound:
                    Utilities.ActionButton( canvas, () => ResolveFindMod( this, Owner.Button ) );
                    icon = Widgets.CheckboxOffTex;
                    break;
                case DependencyStatus.UnknownVersion:
                default:
                    Utilities.ActionButton( canvas, () => ResolveFindMod( this, Owner.Button ) );
                    icon = Widgets.CheckboxPartialTex;
                    break;
            }
            TooltipHandler.TipRegion(canvas, Tooltip);
            Widgets.Label(canvas, ToString());
            GUI.DrawTexture( statusRect, icon );
        }
    }
}