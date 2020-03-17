// Dependency.cs
// Copyright Karel Kroeze, 2020-2020

using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace ModManager
{
    public abstract class Dependency : ModDependency
    {
        public Manifest parent;
        public ModMetaData target;

        public virtual int Severity => 1;

        public virtual Color Color => Color.white;

        public virtual bool ShouldShow => !IsSatisfied;

        protected bool? satisfied;

        public virtual void Notify_Recheck()
        {
            satisfied = null;
        }

        public override bool IsSatisfied {
            get
            {
                if ( !satisfied.HasValue )
                    satisfied = CheckSatisfied();
                return satisfied.Value;
            }
        }

        public abstract bool CheckSatisfied();

        public virtual bool IsApplicable => true;

        public override Texture2D StatusIcon => Resources.Warning;

        public static Regex packageIdFormatRegex = new Regex(@"(?=.{1,60}$)^(?:[a-z0-9]+\.)+[a-z0-9]+$", RegexOptions.IgnoreCase );

        public const string InvalidPackageId = "invalid.package.id";

        public static bool TryGetPackageIdFromIdentifier( string identifier, out string packageId )
        {
            var allMods = ModLister.AllInstalledMods.ToList();
            var modByFolder = allMods.Find( m => m.FolderName.StripSpaces() == identifier );
            if ( modByFolder != null )
            {
                packageId = modByFolder.PackageId;
                return true;
            }
            var modByName = allMods.Find( m => m.Name.StripSpaces() == identifier );
            if ( modByName != null )
            {
                packageId = modByName.PackageId;
                return true;
            }

            packageId = InvalidPackageId;
            return false;
        }

        public Dependency( Manifest parent, string packageId )
        {
            this.parent    = parent;
            this.packageId = packageId;
            target         = ModLister.GetModWithIdentifier( packageId, true );
        }

        public Dependency( Manifest parent, ModDependency depend ): this( parent, depend.packageId )
        {
            displayName = depend.displayName;
            downloadUrl = depend.downloadUrl;
            steamWorkshopUrl = depend.steamWorkshopUrl;
        }
    }
}