// ModListManager.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class ModListManager
    {
        public static string BasePath
        {
            get
            {
                var path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ModLists");
                var dir = new DirectoryInfo(path);
                if (!dir.Exists)
                    dir.Create();
                return path;
            }
        }

        public static void Notify_ModListsChanged()
        {
            _modLists = null;
        }
        private static List<ModList> _modLists;
        public static List<ModList> ModLists
        {
            get
            {
                if ( _modLists != null )
                    return _modLists;

                _modLists = new List<ModList>();
                foreach ( var path in Directory.GetFiles( BasePath ) )
                {
                    try
                    {
                        _modLists.Add( ModList.FromFile( path ) );
                    }
                    catch ( Exception e )
                    {
                        Log.Error( $"Loading ModList from {path} failed: {e.Message}" );
                    }
                }
                return _modLists;
            }
        }

        public static List<FloatMenuOption> SavedModListsOptions => ModLists.Select( SavedModListOption ).ToList();

        public static FloatMenuOption SavedModListOption( ModList list )
        {
            var options = Utilities.NewOptions;
            options.Add( new FloatMenuOption( I18n.LoadModList, () => list.Apply( false ) ) );
            options.Add( new FloatMenuOption( I18n.AddModList, () => list.Apply( true ) ) );
            options.Add( new FloatMenuOption( I18n.RenameModList, () => Find.WindowStack.Add( new Dialog_Rename_ModList( list ) ) ) );
            options.Add( new FloatMenuOption( I18n.DeleteModList, () => TryDelete( list ) ) );
            return new FloatMenuOption( list.Name, () => Utilities.FloatMenu( options ) );
        }

        public static List<FloatMenuOption> SaveFileOptions
        {
            get
            {
                return GenFilePaths.AllSavedGameFiles.Select( fi =>
                {
                    var name = Path.GetFileNameWithoutExtension( fi.Name );
                    return new FloatMenuOption( name, () =>
                    {
                        ModList.FromSave( name, fi.FullName ).Apply( Event.current.shift );
                    } );
                } ).ToList();
            }
        }

        public const string RootElement = "ModList";

        public static string FilePath( ModList list )
        {
            return FilePath( list.Name );
        }

        public static string FilePath( string name )
        {
            return Path.Combine( BasePath, name + ".xml" );
        }

        public static bool TryRename( ModList list, string oldName, bool force = false )
        {

            if ( !File.Exists( FilePath(oldName) ) )
            {
                Log.Warning( $"List not saved: {oldName} ({FilePath(oldName)})" );
                return false;
            }
            if (File.Exists( FilePath( list.Name ) ) && !force )
            {
                Log.Warning( $"File exists: {list.Name} ({FilePath( list )})" );
                return false;
            }

            try
            {
                File.Delete( FilePath( oldName ) );
                var success = list.Save();
                if ( success )
                    Messages.Message( I18n.ModListRenamed( oldName, list.Name ), MessageTypeDefOf.TaskCompletion, false );
                return success;
            }
            catch ( Exception e )
            {
                Log.Error( e.Message );
                return false;
            }
        }

        public static bool TryCreate( ModList list, bool force = false )
        {
            var success = list.Save( force );
            if ( success )
                Messages.Message( I18n.ModListCreated( list.Name ), MessageTypeDefOf.TaskCompletion, false );
            return success;
        }

        private static bool TryDelete(ModList list)
        {
            var path = FilePath( list );
            if ( !File.Exists( path ) )
            {
                Log.Error( $"Tried to delete {path}, but it does not exist" );
                return false;
            }

            try
            {
                File.Delete( path );
                Notify_ModListsChanged();
                Messages.Message(I18n.ModListDeleted( list.Name ), MessageTypeDefOf.TaskCompletion, false);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }
    }
}