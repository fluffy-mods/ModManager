// Manifest.cs
// Copyright Karel Kroeze, 2018-2018
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace ModManager
{
    public static class ManifestExtensions
    {
        public static Manifest GetManifest( this ModMetaData mod )
        {
            return Manifest.For( mod );
        }
    }

    public class Manifest
    {
        public ModMetaData Mod;
        private ModContentPack _pack;
        public ModContentPack Pack {
            get
            {
                return _pack ?? ( _pack =
                           LoadedModManager.RunningModsListForReading.Find( mcp => Mod.SamePackageId( mcp.PackageId ) ) ??
                           new ModContentPack( Mod.RootDir, Mod.PackageId, Mod.PackageIdPlayerFacing, int.MaxValue, Mod.Name ) );
            }
        }
        private const string ManifestFileName = "Manifest.xml";
        private const string AssembliesFolder = "Assemblies/";

        public List<Dependency> Dependencies = new List<Dependency>();
        public List<Dependency> Incompatibilities = new List<Dependency>();

        // dependencies and incompatibilities are still relevant in the manifest because versioning is not a thing in vanilla
        private List<VersionedDependency> dependencies = new List<VersionedDependency>();
        private List<Incompatible> incompatibleWith = new List<Incompatible>();

        // idem for version itself, also version checking requires this
        private string  version;
        private Version _version;

        public Version Version
        {
            get
            {
                if ( _version == null )
                    SetVersion( false );
                return _version;
            }
            private set => _version = value;
        }

        private static readonly Version _zero = new Version( 0, 0 );
        public bool HasVersion => Version > _zero;

        // version checking
#pragma warning disable 649
        internal string manifestUri;
#pragma warning restore 649
        public string downloadUri;
        public VersionCheck versionCheck;

        // suggestions
        public List<string> suggests            = new List<string>();
        public bool         showCrossPromotions = true;

        // source mod
        public SourceSync sourceSync;


        [Obsolete( "Multiple target versions have been implemented in RW since 1.0" )]
#pragma warning disable 169
        private List<string> targetVersions;
#pragma warning restore 169

        [Obsolete("mods should implement a packageId in About.xml")]
        public string identifier;

        public List<Dependency> LoadBefore = new List<Dependency>();
        public List<Dependency> LoadAfter = new List<Dependency>();
        [Obsolete( "dependency management has been implemented in RW from 1.1 onwards." )]
        private List<LoadOrder_Before> loadBefore = new List<LoadOrder_Before>();
        [Obsolete( "dependency management has been implemented in RW from 1.1 onwards." )]
        private List<LoadOrder_After> loadAfter = new List<LoadOrder_After>();
        
        public ModButton_Installed Button => ModButton_Installed.For( Mod );
        
        private static readonly Dictionary<ModMetaData, Manifest> _manifestCache = new Dictionary<ModMetaData, Manifest>();

        public Manifest() {}

        public Manifest( ModMetaData mod )
        {
            Mod = mod;
        }

        public Manifest( ModMetaData mod, string version ): this( mod )
        {
            this.version = version;
        }

        public static Manifest For( ModMetaData mod )
        {
            if ( mod == null )
                return null;
            if ( _manifestCache.TryGetValue( mod, out Manifest manifest ) )
                return manifest;

            manifest = new Manifest( mod );

            // get from file.
            var manifestPath = Path.Combine( mod.AboutDir(), ManifestFileName );

            // manifest is first choice
            if ( File.Exists( manifestPath ) )
            {
                try
                {
                    manifest = DirectXmlLoader.ItemFromXmlFile<Manifest>( manifestPath );
                    manifest.Mod = mod;
                    
                    // create them data!
                    manifest.Dependencies.AddRange( manifest.dependencies );
                    manifest.Incompatibilities.AddRange( manifest.incompatibleWith );
#pragma warning disable 618
                    manifest.LoadBefore.AddRange( manifest.loadBefore );
                    manifest.LoadAfter.AddRange( manifest.loadAfter );
#pragma warning restore 618


                    if ( !manifest.manifestUri.NullOrEmpty() )
                        manifest.versionCheck = new VersionCheck( manifest );

                }
                catch ( Exception e )
                {
                    manifest = new Manifest( mod );
                    Log.Error( $"Error loading manifest for '{mod.Name}':\n{e.Message}\n\n{e.StackTrace}" );
                }
            }

            // copy any information from vanilla metadata
            foreach ( var before in mod.LoadBefore )
                if ( !manifest.LoadBefore.Any( d => d.packageId == before ) )
                    manifest.LoadBefore.Add( new LoadOrder_Before( manifest, before ) );
            foreach ( var after in mod.LoadAfter )
                if ( !manifest.LoadAfter.Any( d => d.packageId == after ) )
                    manifest.LoadAfter.Add( new LoadOrder_After( manifest, after ) );
            foreach ( var incomp in mod.IncompatibleWith )
                if ( !manifest.Incompatibilities.Any( d => d.packageId == incomp ) )
                    manifest.Incompatibilities.Add( new Incompatible( manifest, incomp ) );
            foreach ( var depend in mod.Dependencies )
                if ( !manifest.Dependencies.Any( d => d.packageId == depend.packageId ) )
                    manifest.Dependencies.Add( new VersionedDependency( manifest, depend ) );

            // implicit dependencies from user data
            if ( mod.UserData().Source != null )
                manifest.sourceSync = new SourceSync( manifest, mod.UserData().Source.PackageId );

            foreach ( var dependency in manifest.Dependencies
                                                .Concat( manifest.Incompatibilities )
                                                .Concat( manifest.LoadBefore )
                                                .Concat( manifest.LoadAfter )
                                                .Concat( manifest.versionCheck )
                                                .Concat( manifest.sourceSync )
                                                .Where( d => d != null ) )
            {
                dependency.parent = manifest;
                Debug.TraceDependencies( $"parent={dependency.parent?.Mod.ToString() ?? "NULL"}, targetId={dependency.packageId}, target={dependency.Target}, type={dependency.GetType()}" );
            }

            // resolve version - if set in manifest that takes priority,
            // otherwise try to read version from assemblies.
            manifest.SetVersion();
            _manifestCache.Add( mod, manifest );
            return manifest;
        }

        public static List<Dependency> EmptyRequirementList = new List<Dependency>();
        public List<Dependency> _requirements;
        public IEnumerable<Dependency> Requirements
        {
            get
            {
                if ( _requirements == null)
                    _requirements = Dependencies
                                   .Concat( Incompatibilities )
                                   .Concat( LoadBefore )
                                   .Concat( LoadAfter )
                                   .Concat( versionCheck )
                                   .Concat( sourceSync )
                                   .Where( d => d != null )
                                   .ToList();
                return _requirements.Where( r => r.IsApplicable );
            }
        }

        public void Notify_Recache()
        {
            if (_requirements.NullOrEmpty() )
                return;
            foreach ( var dependency in _requirements )
                dependency.Notify_Recache();
        }

        public void Notify_ModAddedOrRemoved( ModMetaData mod )
        {
            foreach ( var dependency in _requirements )
                dependency.Notify_Recache();
        }
        
        private Version ParseVersion( string version )
        {
            return ParseVersion( version, Mod );
        }

        internal static Version ParseVersion( string version, ModMetaData mod )
        {
            try
            {
                return new Version(version);
            }
            catch
            {
                try
                {
                    var pattern = @"[^0-9\.]";
                    return new Version(Regex.Replace(version, pattern, ""));
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to parse version string '{version}' for {mod?.Name ?? "??"}: {e.Message}\n\n{e.StackTrace}");
                    return new Version();
                }
            }
        }

        public void SetVersion( bool fromAssemblies = true )
        {
            if ( !version.NullOrEmpty() )
            {
                // if version was set, this is simple
                Version = ParseVersion( version );
            }
            else if ( fromAssemblies )
            {
                // Always get Assembly FILE Version, as the actual assembly version may be intentionally kept static so as not to break references.
                var assemblies =
                    ModContentPack.GetAllFilesForMod( Pack, AssembliesFolder, ext => ext.ToLower() == ".dll" );

                if ( assemblies.Any() )
                    Version = ParseVersion( FileVersionInfo
                                           .GetVersionInfo( assemblies.Last().Value.FullName ).FileVersion );
            }
            else
                Version = new Version( 0, 0, 0 );
        }
    }
}