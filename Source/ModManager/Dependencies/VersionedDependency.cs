// VersionedDependency.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.IO;
using System.Linq;
using System.Xml;
using RimWorld;
using SemVer;
using UnityEngine;
using Verse;
using Version = System.Version;

namespace ModManager
{
    public class VersionedDependency: Dependency
    {
        public Range version = new Range( ">= 0.0.0" );

        public override int Severity => IsSatisfied ? 0 : 3;

        public override Color Color => IsSatisfied ? Color.white : Color.red;

        public VersionedDependency() : base( null, string.Empty ){}

        public VersionedDependency( Manifest parent, ModDependency depend ) : base( parent, depend ){}

        public VersionedDependency( Manifest parent, string packageId ): base( parent, packageId) {}
        
        public override void OnClicked( Page_ModsConfig window )
        {
            // do something
        }

        public override string Tooltip
        {
            get
            {
                if (!IsAvailable)
                    return I18n.DependencyNotFound( displayName ?? packageId );
                if ( !IsActive )
                    return I18n.DependencyNotActive( target );
                if ( !IsInRange )
                    return I18n.DependencyWrongVersion( target, this );
                if ( IsSatisfied )
                    return I18n.DependencyMet( target );
                return "Something weird happened.";
            }
        }

        public bool IsAvailable => target != null;
        public bool IsActive => target?.Active ?? false;

        public bool IsInRange
        {
            get
            {
                var v = target?.GetManifest().Version;
                return v != null && version.IsSatisfied($"{v.Major}.{v.Minor}.{v.Build}", true );
            }
        }

        public override bool IsSatisfied => IsAvailable && IsActive && IsInRange;

        public override string RequirementTypeLabel => "dependsOn".Translate();

        public void LoadDataFromXmlCustom( XmlNode root )
        {
            try
            {
                var parts = root.InnerText.Split( ' ' );
                var _packageId = string.Empty;

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
                        version = new Range( parts[1], true );
                        break;
                    case 3:
                        _packageId = parts[0];
                        version = new Range( parts.Skip( 1 ).StringJoin( "" ) );
                        break;
                }

                if ( !packageIdFormatRegex.IsMatch( _packageId ) )
                {
                    if ( TryGetPackageIdFromIdentifier( _packageId, out packageId ) )
                    {
                        Log.Message( $"Invalid packageId '{_packageId}' resolved to '{packageId}'" );
                    }
                    else
                    {
                        throw new InvalidDataException( $"Invalid packageId: '{_packageId}'" );
                    }
                }
            }
            catch ( Exception ex )
            {
                if ( Prefs.DevMode )
                    Log.Warning( $"Failed to parse dependency: {root.OuterXml}.\nInner exception: {ex}" );
                else
                    Log.Warning( $"Failed to parse dependency: {root.OuterXml}.\nInner exception: {ex}" );
                packageId = "invalid.package.id";
                version = new Range( ">= 0.0.0", true );
            }
        }
    }
}