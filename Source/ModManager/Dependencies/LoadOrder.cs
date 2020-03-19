// LoadOrder.cs
// Copyright Karel Kroeze, -2020

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FluffyUI.FloatMenu;
using RimWorld;
using UnityEngine;
using Verse;
using static ModManager.Utilities;

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

        public override int Severity => IsSatisfied ? 0 : 3;
    }

    public class LoadOrder_Before : LoadOrder
    {
        public LoadOrder_Before() : base( null, string.Empty ){}

        public LoadOrder_Before( Manifest parent, string packageId ) : base( parent, packageId ){}

        public override List<FloatMenuOption> Resolvers
        {
            get
            {
                var options = NewOptionsList;
                options.Add( new FloatMenuOption( I18n.MoveBefore( parent.Button, target.GetManifest().Button ),
                                                  () => ModButtonManager.MoveBefore(
                                                      parent.Button, target.GetManifest().Button ) ) );
                options.Add( new FloatMenuOption( I18n.MoveAfter( target.GetManifest().Button, parent.Button ),
                                                  () => ModButtonManager.MoveAfter(
                                                      target.GetManifest().Button, parent.Button ) ) );
                return options;
            }
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
            var text = root.InnerText.Trim();
            TryParseIdentifier( text, root );
        }

    }

    public class LoadOrder_After : LoadOrder
    {
        public LoadOrder_After() : base( null, string.Empty ){}
        public LoadOrder_After( Manifest parent, string packageId ) : base( parent, packageId ){}

        public override List<FloatMenuOption> Resolvers
        {
            get
            {
                var options = NewOptionsList;
                options.Add( new FloatMenuOption( I18n.MoveAfter( parent.Button, target.GetManifest().Button ),
                                                  () => ModButtonManager.MoveAfter(
                                                      parent.Button, target.GetManifest().Button ) ) );
                options.Add( new FloatMenuOption( I18n.MoveBefore( target.GetManifest().Button, parent.Button ),
                                                  () => ModButtonManager.MoveBefore(
                                                      target.GetManifest().Button, parent.Button ) ) );
                return options;
            }
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
            var text = root.InnerText.Trim();
            TryParseIdentifier( text, root );
        }
    }
}