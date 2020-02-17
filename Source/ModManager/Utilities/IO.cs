// IO.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using RimWorld;
using Verse;

namespace ModManager
{
    public static class IO
    {
        public static string ModsDir => GenFilePaths.ModsFolderPath;

        public static bool TryCreateLocalCopy( ModMetaData mod, out ModMetaData copy )
        {
            copy = null;

            if ( mod.Source != ContentSource.SteamWorkshop )
            {
                Log.Error( "Can only create local copies of steam workshop mods." );
                return false;
            }

            var baseTargetDir = mod.GetLocalCopyFolder();
            var targetDir = baseTargetDir;
            var i = 2;
            while ( Directory.Exists( targetDir ) )
                targetDir = $"{baseTargetDir} ({i++})";

            return TryCopyMod( mod, ref copy, targetDir );
        }

        public static bool TryUpdateLocalCopy( ModMetaData source, ModMetaData local )
        {
            // delete and re-copy mod.
            var updateResult = TryRemoveLocalCopy( local ) && TryCopyMod( source, ref local, local.RootDir.FullName, false );
            if ( !updateResult )
                return false;

            // rename settings file
            TryCopySettings( source, local, true );

            // update version 
            ModButton_Installed.For( source ).Notify_VersionUpdated( local );
            return true;
        }

        private static bool TryCopyMod( ModMetaData mod, ref ModMetaData copy, string targetDir, bool copySettings = true )
        {
            try
            {
                // copy mod
                mod.RootDir.Copy( targetDir, true );
                copy = new ModMetaData( targetDir );
                ( ModLister.AllInstalledMods as List<ModMetaData> )?.Add( copy );

                // copy settings and color attribute
                if ( copySettings )
                {
                    TryCopySettings( mod, copy );
                    ModManager.Settings[copy].Color = ModManager.Settings[mod].Color;
                }

                // set source attribute
                ModManager.Settings[copy].Source = mod;

                return true;
            }
            catch ( Exception e )
            {
                Log.Error( $"Creating local copy failed: {e.Message} \n\n{e.StackTrace}" );
                return false;
            }
        }

        private static Regex SettingsMask( string identifier )
        {
            return new Regex( $@"Mod_{GenText.SanitizeFilename( identifier )}_(.*)\.xml" );
        }

        private static string NewSettingsFilePath( string source, Regex mask, string targetIdentifier )
        {
            var match = mask.Match( source );
            return Path.Combine( GenFilePaths.ConfigFolderPath,
                GenText.SanitizeFilename( $"Mod_{targetIdentifier}_{match.Groups[1].Value}.xml" ) );
        }

        private static void TryCopySettings( ModMetaData source, ModMetaData target, bool deleteOld = false )
        {
            // find any settings files that belong to the source mod
            var mask = SettingsMask( source.PackageId );
            var settings = Directory.GetFiles( GenFilePaths.ConfigFolderPath )
                .Where( f => mask.IsMatch( f ) )
                .Select( f => new
                {
                    source = f,
                    target = NewSettingsFilePath( f, mask, target.PackageId )
                } );

            // copy settings files, overwriting existing - if any.
            foreach ( var setting in settings )
            {
                Debug.Log( $"Copying settings :: {setting.source} => {setting.target}"  );
                if ( deleteOld )
                    File.Move( setting.source, setting.target );
                else 
                    File.Copy( setting.source, setting.target, true );
            }
        }

        public static bool TryRemoveLocalCopy( ModMetaData mod )
        {
            if ( mod.Source != ContentSource.ModsFolder )
            {
                Log.Error( "Can only delete locally installed non-steam workshop mods." );
                return false;
            }

            try
            {
                mod.RootDir.Delete( true );
                ( ModLister.AllInstalledMods as List<ModMetaData> )?.TryRemove( mod );
                return true;
            }
            catch ( Exception e )
            {
                Log.Error( e.Message );
                Log.Warning( "Deleting failed. Retrying may help." );
                return false;
            }
        }

        public static string GetLocalCopyFolder( this ModMetaData mod )
        {
            return Path.Combine( ModsDir, $"{LocalCopyPrefix}_{mod.Name}_{DateStamp}".SanitizeFileName() );
        }

        public static string DateStamp => $"-{DateTime.Now.Day}-{DateTime.Now.Month}";
        public static string LocalCopyPrefix => "__LocalCopy";


        public static string SanitizeFileName( this string str)
        {
            var invalidReStr = $@"[^0-9a-zA-Z_\-]+";

            var reservedWords = new[]
            {
                "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
                "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
                "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            var fileSystemSanitized = Regex.Replace(str, invalidReStr, "_");
            foreach (var reservedWord in reservedWords)
            {
                var reservedWordPattern = $@"^{reservedWord}\.";
                fileSystemSanitized = Regex.Replace(fileSystemSanitized, reservedWordPattern, "_", RegexOptions.IgnoreCase);
            }

            return fileSystemSanitized;
        }


        private static void Copy( this DirectoryInfo source, string destination, bool recursive )
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo[] dirs = source.GetDirectories();

            // If the destination directory doesn't exist, create it.
            if ( !Directory.Exists( destination ) )
            {
                Directory.CreateDirectory( destination );
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = source.GetFiles();
            foreach ( FileInfo file in files )
            {
                string temppath = Path.Combine( destination, file.Name );
                file.CopyTo( temppath, false );
            }

            // If copying subdirectories, copy them and their contents to new location.
            if ( recursive )
            {
                foreach ( DirectoryInfo subdir in dirs )
                {
                    string temppath = Path.Combine( destination, subdir.Name );
                    subdir.Copy( temppath, true );
                }
            }
        }
        
        internal static void CreateLocalCopy( ModMetaData mod, bool batch = false )
        {
            LongEventHandler.QueueLongEvent(() =>
            {

                ModMetaData copy;
                LongEventHandler.SetCurrentEventText(I18n.CreatingLocal(mod.Name));
                if (TryCreateLocalCopy(mod, out copy))
                {
                    var button = ModButton_Installed.For( copy );
                    if (batch)
                    {
                        _batchCreatedCopies.Add( new Pair<ModButton_Installed, ModMetaData>( button, copy ) );
                    }
                    else
                    {
                        Messages.Message(I18n.CreateLocalSucceeded(mod.Name), MessageTypeDefOf.NeutralEvent, false);
                        LongEventHandler.QueueLongEvent(() => button.Notify_VersionAdded( copy, true ), "", true, null);
                    }
                }
                else
                {
                    Messages.Message(I18n.CreateLocalFailed(mod.Name), MessageTypeDefOf.RejectInput, false);
                }
            }, null, true, null);
        }

        internal static void CreateLocalCopies( IEnumerable<ModMetaData> mods, bool force = false )
        {
            var steamMods = mods.Where( m => m.Source == ContentSource.SteamWorkshop );
            if ( !force && steamMods.Count() > 5 )
            {
                Find.WindowStack.Add( Dialog_MessageBox.CreateConfirmation(
                    I18n.CreateLocalCopiesConfirmation( steamMods.Count() ),
                    () => CreateLocalCopies( steamMods, true ) ) );
                return;
            }
            foreach ( var mod in steamMods )
                CreateLocalCopy( mod, true );
            FinishBatchCreate();
        }

        internal static void FinishBatchCreate()
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                foreach (var localCopy in _batchCreatedCopies)
                {
                    var button = localCopy.First;
                    var copy = localCopy.Second;
                    button.Notify_VersionAdded( copy, true );
                }
                _batchCreatedCopies.Clear();
            }, string.Empty, true, null);
        }
        private static List<Pair<ModButton_Installed, ModMetaData>> _batchCreatedCopies = new List<Pair<ModButton_Installed, ModMetaData>>();

        internal static void DeleteLocalCopies( IEnumerable<ModMetaData> mods )
        {
            var modList = mods.Select(m => $"{m.Name} ({m.SupportedGameVersionsReadOnly.Select( v => v.ToString() ).StringJoin( ", " )})").ToLineList(); 
            var dialog = Dialog_MessageBox.CreateConfirmation(
                I18n.MassUnSubscribeConfirm(mods.Count(), modList),
                () =>
                {
                    foreach ( var mod in new List<ModMetaData>( mods ) )
                        DeleteLocal( mod, true );
                },
                true);
            Find.WindowStack.Add(dialog);
        }

        internal static void DeleteLocal( ModMetaData mod, bool force = false )
        {
            if ( force )
            {
                LongEventHandler.QueueLongEvent( () =>
                {
                    LongEventHandler.SetCurrentEventText(I18n.RemovingLocal(mod.Name));
                    if (TryRemoveLocalCopy(mod))
                    {
                        Messages.Message(I18n.RemoveLocalSucceeded(mod.Name),
                            MessageTypeDefOf.NeutralEvent, false);
                    }
                    else
                    {
                        Messages.Message(I18n.RemoveLocalFailed(mod.Name),
                            MessageTypeDefOf.RejectInput, false);
                    }

                    // remove this version either way, as it's likely to be borked.
                    ModButton_Installed.For(mod).Notify_VersionRemoved(mod);
                }, null, true, null );
                return;
            }
            Find.WindowStack.Add( Dialog_MessageBox.CreateConfirmation(
                I18n.ConfirmRemoveLocal( mod.Name ), () => DeleteLocal( mod, true ), true ) );
        }

        private static Dictionary<string, string> _hashCache = new Dictionary<string, string>();
        internal static string GetFolderHash( this DirectoryInfo folder )
        {
            if ( _hashCache.TryGetValue( folder.FullName, out string hash ) )
                return hash;

            var files = folder.GetFiles( "*", SearchOption.AllDirectories )
                .OrderBy( p => p.FullName );

            using ( var md5 = MD5.Create() )
            {
                foreach ( var file in files )
                {
                    // hash path
                    byte[] pathBytes = Encoding.UTF8.GetBytes( file.FullName.Substring( folder.FullName.Length ) );
                    md5.TransformBlock( pathBytes, 0, pathBytes.Length, pathBytes, 0 );

                    // hash contents
                    byte[] contentBytes = File.ReadAllBytes( file.FullName );

                    md5.TransformBlock( contentBytes, 0, contentBytes.Length, contentBytes, 0 );
                }

                //Handles empty filePaths case
                md5.TransformFinalBlock( new byte[0], 0, 0 );

                hash = BitConverter.ToString( md5.Hash ).Replace( "-", "" ).ToLower();
                _hashCache.Add( folder.FullName, hash );
                return hash;
            }
        }

        public static T ItemFromXmlString<T>(string xml, bool resolveCrossRefs = true) where T : new()
        {
            if (resolveCrossRefs && DirectXmlCrossRefLoader.LoadingInProgress)
            {
                Log.Error("Cannot call ItemFromXmlFile with resolveCrossRefs=true while loading is already in progress.", false);
            }
            if (xml.NullOrEmpty())
            {
                return Activator.CreateInstance<T>();
            }
            T result;
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                // Trim should theoretically also get rid of BOMs
                xmlDocument.LoadXml( xml.Trim() );
                T t = DirectXmlToObject.ObjectFromXml<T>(xmlDocument.DocumentElement, false);
                if (resolveCrossRefs)
                {
                    DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
                }
                result = t;
            }
            catch (Exception ex)
            {
                Log.Error( $"Exception loading item from string. Loading defaults instead. \nXML: {xml}\n\nException: {ex}" );
                result = Activator.CreateInstance<T>();
            }
            return result;
        }

        public static void MassRemoveLocalFloatMenu()
        {
            var options = Utilities.NewOptions;
            var localCopies = ModButtonManager.AllMods.Where( m => m.IsLocalCopy() );
            var outdated = localCopies.Where( m => !m.VersionCompatible && !m.MadeForNewerVersion );
            var inactive = ModButtonManager.AvailableMods.Where( m => m.IsLocalCopy() );
            options.Add( new FloatMenuOption( I18n.MassRemoveLocalAll, () => DeleteLocalCopies( localCopies ) ) );
            if ( outdated.Any() )
                options.Add( new FloatMenuOption( I18n.MassRemoveLocalOutdated, () => DeleteLocalCopies( outdated ) ) );
            if ( inactive.Any() )
                options.Add( new FloatMenuOption( I18n.MassRemoveLocalInactive, () => DeleteLocalCopies( inactive ) ) );
            Utilities.FloatMenu( options );
        }
    }
}