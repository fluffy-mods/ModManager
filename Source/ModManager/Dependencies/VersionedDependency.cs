// VersionedDependency.cs
// Copyright Karel Kroeze, 2020-2020

using SemVer;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class VersionedDependency: Dependency
    {
        private Range _range = new Range( ">= 0.0.0" );
        protected bool versioned = false;

        public Range Range
        {
            get => _range;
            set
            {
                versioned = true;
                _range = value;
            }
        }

        public override int Severity
        {
            get
            {
                if ( IsSatisfied )
                    return 0;
                if ( IsActive && !IsInRange )
                    return 2;
                return 3;
            }
        }

        public override Color Color => IsSatisfied ? Color.white : Color.red;

        public VersionedDependency() : base( null, string.Empty ){}

        public VersionedDependency( Manifest parent, ModDependency depend ) : base( parent, depend ){}

        public VersionedDependency( Manifest parent, string packageId ): base( parent, packageId) {}

        protected static Regex SteamIdRegex = new Regex( @"(\d*)$" );
        public override List<FloatMenuOption> Resolvers
        {
            get
            {
                var options = Utilities.NewOptionsList;
                // if available, activate
                // else
                // if has steam id, subscribe + link
                // if has download location, link
                // else 
                // search forum
                // search steam

                if ( IsAvailable && IsInRange )
                {
                    options.Add( new FloatMenuOption( I18n.ActivateMod( Target ), () => Target.GetManifest().Button.Active = true ) );
                }
                else if ( !downloadUrl.NullOrEmpty() || !steamWorkshopUrl.NullOrEmpty() )
                {
                    if ( !downloadUrl.NullOrEmpty() )
                    {
                        options.Add( new FloatMenuOption( I18n.OpenDownloadUri( downloadUrl ), () => SteamUtility.OpenUrl( downloadUrl ) ) );
                    }

                    if ( !steamWorkshopUrl.NullOrEmpty() )
                    {
                        var steamId = SteamIdRegex.Match( steamWorkshopUrl ).Groups[1].Value;
                        Debug.Log( $"steamUrl: {steamWorkshopUrl}, id: {steamId}" );
                        options.Add( new FloatMenuOption( I18n.WorkshopPage( displayName ?? packageId ), () => SteamUtility.OpenUrl( downloadUrl ) ) );
                        options.Add( new FloatMenuOption( I18n.Subscribe( displayName ?? packageId ), () => Workshop.Subscribe( steamId ) ) );
                    }
                }
                else
                {
                    options.Add( new FloatMenuOption( I18n.SearchForum( displayName ?? packageId ), () => SteamUtility.OpenUrl( "http://rimworldgame.com/getmods" ) ) );
                    options.Add( new FloatMenuOption( I18n.SearchSteamWorkshop( displayName ?? packageId ), () => SteamUtility.OpenUrl( $"https://steamcommunity.com/workshop/browse/?appid=294100&searchtext={displayName ?? packageId}"))  );
                }

                return options;
            }
        }

        public override string Tooltip
        {
            get
            {
                if ( !IsAvailable )
                    return I18n.DependencyNotFound( displayName ?? packageId );
                if ( !IsActive )
                    return I18n.DependencyNotActive( Target );
                if ( !IsInRange )
                    return I18n.DependencyWrongVersion( Target, this );
                if ( IsSatisfied )
                    return I18n.DependencyMet( Target );
                return "Something weird happened.";
            }
        }

        public bool IsAvailable => Target != null;
        public bool IsActive => Target?.GetManifest().Button.Active ?? false;
        public override bool IsApplicable => parent?.Mod?.Active ?? false;
        public bool IsInRange
        {
            get
            {
                var v = Target?.GetManifest().Version;
                return v != null && Range.IsSatisfied($"{v.Major}.{v.Minor}.{v.Build}", true );
            }
        }

        public override bool CheckSatisfied() => IsAvailable && IsActive && IsInRange;

        public override string RequirementTypeLabel => "dependsOn".Translate();

        public void LoadDataFromXmlCustom( XmlNode root )
        {
            var parts      = root.InnerText.Split( ' ' );
            string _packageId;

            Debug.TraceDependencies( $"Trying to parse '{root.OuterXml}'");

            // can have 1, 2 or 3 parts
            // 1 part: packageId only.
            // 2 parts: packageId op:version     || where version is attached to the op, e.g. >1.0.0
            // 3 parts: packageId op version
            switch ( parts.Length )
            {
                case 1:
                    _packageId = parts[0];
                    break;
                case 2:
                    _packageId = parts[0];
                    Range      = new Range( parts[1], true );
                    break;
                case 3:
                    _packageId = parts[0];
                    Range      = new Range( parts.Skip( 1 ).StringJoin( "" ) );
                    break;
                default:
                    _packageId = root.InnerText;
                    break;
            }

            TryParseIdentifier( _packageId, root );
        }
    }
}