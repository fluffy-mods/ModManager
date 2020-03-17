// LoadOrder.cs
// Copyright Karel Kroeze, -2020

using System;
using System.IO;
using System.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{

    public abstract class LoadOrder : Dependency
    {
        protected LoadOrder( Manifest parent, string packageId ) : base( parent, packageId )
        {
        }

        protected LoadOrder( Manifest parent, ModDependency _depend ) : base( parent, _depend )
        {
        }

        public override bool IsApplicable => ( parent?.Mod?.Active ?? false ) && ( target?.Active ?? false );

        public override Color Color => IsSatisfied ? Color.white : Color.red;

        public override int Severity => IsSatisfied ? 0 : 2;
    }

    public class LoadOrder_Before : LoadOrder
    {
        public LoadOrder_Before() : base( null, string.Empty ){}

        public LoadOrder_Before( Manifest parent, string packageId ) : base( parent, packageId ){}

        public override void OnClicked( Page_ModsConfig window )
        {
            // do something
        }

        public override string Tooltip
        {
            get
            {
                if ( !IsApplicable ) return "Not applicable";
                return IsSatisfied
                    ? I18n.LoadedBefore( target.Name )
                    : I18n.ShouldBeLoadedBefore( target.Name );
            }
        }

        public override bool CheckSatisfied()
        {
            var mods  = ModsConfig.ActiveModsInLoadOrder.ToList();
            var other = ModLister.GetModWithIdentifier( packageId );
            return mods.Contains( other )
                && mods.Contains( parent.Mod )
                && mods.IndexOf( other ) > mods.IndexOf( parent.Mod );
        }

        public override string RequirementTypeLabel => "loadOrder".Translate();
        
        public void LoadDataFromXmlCustom( XmlNode root )
        {
            try
            {
                var text = root.InnerText.Trim();
                if ( !packageIdFormatRegex.IsMatch( text ) )
                {
                    if ( TryGetPackageIdFromIdentifier( text, out packageId ) )
                    {
                        if ( Prefs.DevMode )
                        {
                            Log.Message( $"Invalid packageId '{text}' resolved to '{packageId}'" );
                        }
                    }
                    else
                    {
                        throw new InvalidDataException( $"Invalid packageId: '{text}'" );
                    }
                }

                target = ModLister.GetModWithIdentifier( packageId, true );
            }
            catch ( Exception ex )
            {
#if DEBUG
                Log.Message( $"Failed to parse dependency: {root.OuterXml}.\nInner exception: {ex}" );
#else
                if (Prefs.DevMode)
                    Log.Warning( $"Failed to parse dependency: {root.OuterXml}.\nInner exception: {ex}" );
#endif
            }
        }
    }

    public class LoadOrder_After : LoadOrder
    {
        public LoadOrder_After() : base( null, string.Empty ){}
        public LoadOrder_After( Manifest parent, string packageId ) : base( parent, packageId ){}
        public override void OnClicked( Page_ModsConfig window )
        {
            // do something
        }
        public override string Tooltip
        {
            get
            {
                if ( !IsApplicable ) return "Not applicable";
                return IsSatisfied
                    ? I18n.LoadedAfter( target.Name )
                    : I18n.ShouldBeLoadedAfter( target.Name );
            }
        }

        public override bool CheckSatisfied()
        {
            var mods  = ModsConfig.ActiveModsInLoadOrder.ToList();
            var other = ModLister.GetModWithIdentifier( packageId );
            return mods.Contains( other )
                && mods.Contains( parent.Mod )
                && mods.IndexOf( other ) < mods.IndexOf( parent.Mod );
        }

        public override string RequirementTypeLabel => "loadOrder".Translate();

        public void LoadDataFromXmlCustom( XmlNode root )
        {
            try
            {
                var text = root.InnerText.Trim();
                if ( !packageIdFormatRegex.IsMatch( text ) )
                {
                    if ( TryGetPackageIdFromIdentifier( text, out packageId ) )
                    {
                        if ( Prefs.DevMode )
                        {
                            Log.Message( $"Invalid packageId '{text}' resolved to '{packageId}'" );
                        }
                    }
                    else
                    {
                        throw new InvalidDataException( $"Invalid packageId: '{text}'" );
                    }
                }

                target = ModLister.GetModWithIdentifier( packageId, true );
            }
            catch ( Exception ex )
            {
#if DEBUG
                Log.Message( $"Failed to parse dependency: {root.OuterXml}.\nInner exception: {ex}" );
#else
                if (Prefs.DevMode)
                    Log.Warning( $"Failed to parse dependency: {root.OuterXml}.\nInner exception: {ex}" );
#endif
            }
        }
    }
}