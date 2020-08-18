// SourceSync.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class SourceSync: Dependency
    {
        public SourceSync( Manifest parent, string packageId ) : base( parent, packageId )
        {
        }

        public SourceSync( Manifest parent, ModDependency depend ) : base( parent, depend )
        {
        }

        public override ModMetaData Target
        {
            get
            {
                if ( _targetResolved ) return _target;

                _target = ModLister.GetModWithIdentifier( packageId, false );
                _targetResolved = true;
                return _target;
            }
        }

        protected string _sourceHash;
        public string SourceHash
        {
            get
            {
                return _sourceHash ??= Target.RootDir.GetFolderHash();
            }
        }

        public override Color Color => IsSatisfied ? Color.white : GenUI.MouseoverColor;

        public override string Tooltip
        {
            get
            {
                if ( IsSatisfied )
                {
                    return I18n.XIsUpToDate( parent.Mod );
                }
                else
                {
                    return I18n.YHasUpdated( Target );
                }
            }
        }

        public override bool                  CheckSatisfied()
        {
            return parent.Mod.UserData().SourceHash == SourceHash;
        }

        public override List<FloatMenuOption> Resolvers
        {
            get
            {
                var options = Utilities.NewOptionsList;
                options.Add( new FloatMenuOption( I18n.UpdateLocalCopy( parent.Mod ), () => IO.TryUpdateLocalCopy( Target, parent.Mod ))  );
                return options;
            }
        }
    }
}