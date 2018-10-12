// ModList.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace ModManager
{
    public class ModList: IExposable
    {
        public List<string> _modIds;
        public List<string> _modNames;
        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                var oldName = _name;
                _name = value;
                bool success;

                // create or rename
                if ( oldName.NullOrEmpty() )
                    success = ModListManager.TryCreate( this );
                else
                    success = ModListManager.TryRename( this, oldName );

                // recover from failure
                if ( !success )
                    _name = oldName;
            }   
        }

        private ModList()
        {
            // scribe
        }

        public static ModList FromFile( string path )
        {
            var list = new ModList();
            Scribe.loader.InitLoading( path );
            Scribe.EnterNode( ModListManager.RootElement );
            list.ExposeData();
            Scribe.loader.FinalizeLoading();
            Debug.Log(list.ToString());
            return list;
        }

        public static ModList FromSave( string name, string path )
        {
            var list = new ModList();
            Scribe.loader.InitLoadingMetaHeaderOnly( path );
            ScribeMetaHeaderUtility.LoadGameDataHeader( ScribeMetaHeaderUtility.ScribeHeaderMode.None, false );
            list._modIds = ScribeMetaHeaderUtility.loadedModIdsList;
            list._modNames = ScribeMetaHeaderUtility.loadedModNamesList;
            list._name = name;
            Scribe.loader.FinalizeLoading();
            Debug.Log( list.ToString() );
            return list;
        }

        public ModList( IEnumerable<ModButton> mods )
        {
            _modIds = new List<string>();
            _modNames = new List<string>();
            Debug.Log("Creating modlist...");
            foreach ( var button in mods.Where( m => m.Active ) )
            {
                Debug.Log($"\tAdding {button.Name} ({button.Identifier})");
                _modIds.Add( button.Identifier );
                _modNames.Add( button.Name );
            }
            Find.WindowStack.Add(new Dialog_Rename_ModList(this));
        }

        public void ExposeData()
        {
            Scribe_Values.Look( ref _name, "Name" );
            Scribe_Collections.Look( ref _modIds, "modIds" );
            Scribe_Collections.Look( ref _modNames, "modNames" );
        }

        public void Apply( bool add )
        {
            if (!add)
                ModButtonManager.Reset();

            for ( int i = 0; i < _modIds.Count; i++ )
            {
                var id = _modIds[i];
                var name = _modNames[i];
                var mod = ModLister.GetModWithIdentifier( id );
                if ( mod != null )
                {
                    var button = ModButton_Installed.For( mod );
                    button.Notify_ResetSelected();
                    mod.Active = true;
                    ModButtonManager.TryAdd( button, false );
                }
                else
                {
                    ModButtonManager.TryAdd( new ModButton_Missing( id, name ) );
                }
            }

            // reset selected versions for all these mods
            ModButtonManager.Notify_ModOrderChanged();
        }

        public override string ToString()
        {
            var str = _name + "\n";
            str += "ids:\n";
            if (_modIds == null)
                str += "\tNULL\n";
            else
                foreach (var modId in _modIds)
                    str += "\t" + modId + "\n";
            str += "names:\n";
            if (_modNames == null)
                str += "\tNULL\n";
            else
                foreach (var name in _modNames)
                    str += "\t" + name + "\n";
            return str;
        }

        public void Add( ModMetaData mod )
        {
            _modIds.Add( mod.Identifier );
            _modNames.Add( mod.Name );

            Save( true );
            ModListManager.Notify_ModListChanged();
        }

        public void Remove( ModMetaData mod )
        {
            // remove by index because mods might have duplicate names.
            var index = _modIds.IndexOf( mod.Identifier );
            if ( index < 0 )
                return; // not found
            if ( _modNames[index] != mod.Name )
            {
                Log.Warning( $"Tried to remove mod {mod.Name} (id: {mod.Identifier}) from mod list," +
                             $" but the mod with that identifier in the ModList is named {_modNames[index]}!" );
                return;
            }

            _modIds.RemoveAt( index );
            _modNames.RemoveAt( index );

            Save( true );
            ModListManager.Notify_ModListChanged();
        }

        public bool Save( bool force = false )
        {
            var path = ModListManager.FilePath( this );
            if (File.Exists(path) && !force)
            {
                Log.Error("File exists: " + path);
                return false;
            }

            try
            {
                Scribe.saver.InitSaving( path, ModListManager.RootElement );
                ExposeData();
                Scribe.saver.FinalizeSaving();
                if ( !ModListManager.ModLists.Contains( this ) )
                    ModListManager.Notify_ModListsChanged();
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