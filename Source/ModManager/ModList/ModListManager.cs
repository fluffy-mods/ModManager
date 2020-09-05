// ModListManager.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColourPicker;
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

        public static List<ModList> ListsFor( ModButton_Installed button )
        {
            return ListsFor( button?.Selected );
        }

        private static Dictionary<ModMetaData, List<ModList>> _modListsCache = new Dictionary<ModMetaData, List<ModList>>();
        private static List<ModList> _emptyModList = new List<ModList>();

        public static List<ModList> ListsFor( ModMetaData mod )
        {
            // garbage in, garbage out.
            if ( mod == null )
                return _emptyModList;

            // get from cache
            List<ModList> lists;
            if ( _modListsCache.TryGetValue( mod, out lists ) )
                return lists;

            // add to cache
            lists = ModLists
                .Where( l => l.Mods.Any( m => m.Id == mod.PackageId ) )
                .ToList();
            _modListsCache.Add( mod, lists );
            return lists;
        }

        public static void DoAddToModListFloatMenu( ModButton_Installed mod )
        {
            var cur = ListsFor( mod );
            var options = ModLists
                .Where( l => !cur.Contains( l ) )
                .Select( l => new FloatMenuOption( I18n.AddToModListX( l.Name ), () => l.Add( mod.Selected ) ) )
                .ToList();
            Utilities.FloatMenu( options );
        }

        public static void DoRemoveFromModListFloatMenu( ModButton_Installed mod )
        {
            var options = ListsFor( mod )
                .Select( l => new FloatMenuOption( I18n.RemoveFromModListX( l.Name ), () => l.Remove( mod.Selected ) ) )
                .ToList();
            Utilities.FloatMenu( options );
        }

        public static void Notify_ModListsChanged()
        {
            _modLists = null;
            _modListsCache.Clear();
        }

        public static void Notify_ModListChanged()
        {
            _modListsCache.Clear();
        }

        private static List<ModList> _modLists;
        public static List<ModList> ModLists
        {
            get
            {
                if ( _modLists != null )
                    return _modLists;

                _modLists = new List<ModList>();
                foreach ( var path in Directory.GetFiles( BasePath, "*.xml" ) )
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
            var options = Utilities.NewOptionsList;
            options.Add( new FloatMenuOption( I18n.ExportModList, () => { GUIUtility.systemCopyBuffer = list.ToYaml(); Messages.Message( I18n.ModListCopiedToClipboard( list.Name ), MessageTypeDefOf.TaskCompletion, false ); } ) );
            options.Add( new FloatMenuOption( I18n.LoadModList, () => list.Apply( false ) ) );
            options.Add( new FloatMenuOption( I18n.AddModList, () => list.Apply( true ) ) );
            options.Add( new FloatMenuOption( I18n.RenameModList, () => Find.WindowStack.Add( new Dialog_Rename_ModList( list ) ) ) );
            options.Add( new FloatMenuOption( I18n.ChangeListColour, () => Find.WindowStack.Add( new Dialog_ColourPicker( list.Color, color => list.Color = color ) ) ) );
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

        public static void TryRename( ModList list, string oldName, Action failureCallback, Action successCallback )
        {

            if ( !File.Exists( FilePath(oldName) ) )
            {
                Log.Warning( $"List not saved: {oldName} ({FilePath(oldName)})" );
                failureCallback?.Invoke();
                return;
            }

            try
            {
                File.Delete( FilePath( oldName ) );
                list.Save( false, failureCallback, successCallback );
            }
            catch ( Exception e )
            {
                Log.Error( e.Message );
                failureCallback?.Invoke();
            }
        }

        public static void TryCreate( ModList list, Action failureCallback, Action successCallback )
        {
            list.Save( false, failureCallback, successCallback );
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