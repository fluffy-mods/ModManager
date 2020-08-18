// Incompatible.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ModManager
{
    public class Incompatible: VersionedDependency
    {
        public Incompatible() : base( null, String.Empty ){}
        public Incompatible( Manifest parent, string packageId ) : base( parent, packageId ){}
        public Incompatible( Manifest parent, ModDependency depend) : base( parent, depend ){}

        public override int Severity => IsSatisfied ? 0 : 3;
        public override bool CheckSatisfied() => !IsActive || !IsInRange;

        public override List<FloatMenuOption> Resolvers
        {
            get
            {
                var options = Utilities.NewOptionsList;
                var targetButton = Target?.GetManifest()?.Button;
                if ( targetButton != null )
                {
                    options.Add( new FloatMenuOption( I18n.DeactivateMod( targetButton ),
                                                      () => targetButton.Active = false ) );
                }

                options.Add( new FloatMenuOption( I18n.DeactivateMod( parent.Button ),
                                                  () => parent.Button.Active = false ) );
                return options;
            }
        }

        public override string Tooltip => I18n.IncompatibleMod( versioned ? Target?.Name + " v" + Target?.GetManifest().Version : Target?.Name );
    }
}