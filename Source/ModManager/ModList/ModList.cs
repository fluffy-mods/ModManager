// ModList.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using YamlDotNet.Serialization;

namespace ModManager
{
    public class ModList : IExposable
    {
        private Color        _color    = Color.white;
        private List<string> _modIds   = new List<string>();
        private List<string> _modNames = new List<string>();
        private string       _name;

        [Obsolete( "This constructor should not be used directly." )]
        public ModList()
        {
            // scribe
            Version = 1;
        }

        public ModList( IEnumerable<ModButton> mods )
        {
            _modIds   = new List<string>();
            _modNames = new List<string>();
            Debug.Log( "Creating modlist..." );
            foreach ( var button in mods.Where( m => m.Active ) )
            {
                Debug.Log( $"\tAdding {button.Name} ({button.Identifier})" );
                _modIds.Add( button.Identifier );
                _modNames.Add( button.Name );
            }

            Find.WindowStack.Add( new Dialog_Rename_ModList( this ) );
        }

        public int Version { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                // _oldName is only cleared upon the _end_ of the task, to prevent callbacks setting _oldName to the new _name.
                var oldName = _name;
                _name = value;

                // create or rename
                if ( oldName.NullOrEmpty() )
                    ModListManager.TryCreate( this,
                                              () =>
                                              {
                                                  _name = oldName;
                                                  Find.WindowStack.Add( new Dialog_Rename_ModList( this ) );
                                              },
                                              () =>
                                              {
                                                  Messages.Message( I18n.ModListCreated( _name ),
                                                                    MessageTypeDefOf.TaskCompletion, false );
                                                  ModListManager.Notify_ModListsChanged();
                                              } );
                else
                    ModListManager.TryRename( this, oldName, () => _name = oldName, () =>
                    {
                        Messages.Message( I18n.ModListRenamed( oldName, _name ), MessageTypeDefOf.TaskCompletion,
                                          false );
                        ModListManager.Notify_ModListsChanged();
                    } );
            }
        }

        [YamlIgnore]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Save( true );
            }
        }

        public List<ModIdentifier> Mods
        {
            get
            {
                var mods = new List<ModIdentifier>();
                for ( var i = 0; i < _modIds.Count; i++ )
                    mods.Add( new ModIdentifier( _modIds[i], _modNames[i] ) );
                return mods;
            }
            set
            {
                _modIds.Clear();
                _modNames.Clear();
                foreach ( var mod in value )
                {
                    _modIds.Add( mod.Id );
                    _modNames.Add( mod.Name );
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look( ref _name, "Name" );
            Scribe_Values.Look( ref _color, "Color", Color.white );
            Scribe_Collections.Look( ref _modIds, "modIds" );
            Scribe_Collections.Look( ref _modNames, "modNames" );
        }

        public static ModList FromFile( string path )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var list = new ModList();
#pragma warning restore CS0618 // Type or member is obsolete
            Scribe.loader.InitLoading( path );
            Scribe.EnterNode( ModListManager.RootElement );
            list.ExposeData();
            Scribe.loader.FinalizeLoading();
            Debug.Log( list.ToString() );

            if ( list._modIds.NullOrEmpty() || list._modNames.NullOrEmpty() )
                throw new InvalidDataException( "ModList contains no mods." );
            if ( list._modIds.Count != list._modNames.Count )
                throw new InvalidDataException( "ids and names unbalanced." );

            return list;
        }

        public static ModList FromSave( string name, string path )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var list = new ModList();
#pragma warning restore CS0618 // Type or member is obsolete
            Scribe.loader.InitLoadingMetaHeaderOnly( path );
            ScribeMetaHeaderUtility.LoadGameDataHeader( ScribeMetaHeaderUtility.ScribeHeaderMode.None, false );
            list._modIds   = ScribeMetaHeaderUtility.loadedModIdsList;
            list._modNames = ScribeMetaHeaderUtility.loadedModNamesList;
            list._name     = name;
            Scribe.loader.FinalizeLoading();
            Debug.Log( list.ToString() );
            return list;
        }

        public static ModList FromYaml( string source )
        {
            try
            {
                var deserializerBuilder = new DeserializerBuilder();
                var deserializer        = deserializerBuilder.Build();
                var list                = deserializer.Deserialize<ModList>( source );
                Messages.Message( I18n.ModListCreatedFromClipboard( list.Name ), MessageTypeDefOf.PositiveEvent,
                                  false );
                return list;
            }
            catch ( Exception err )
            {
                Messages.Message( I18n.FailedToCreateModListFromClipboard( err.Message ),
                                  MessageTypeDefOf.NegativeEvent, false );
                ModListManager.Notify_ModListsChanged();
                throw err;
            }
        }

        public void Apply( bool add )
        {
            if ( !add )
                ModButtonManager.Reset( false );

            for ( var i = 0; i < _modIds.Count; i++ )
            {
                var id   = _modIds[i];
                var name = _modNames[i];
                var mod = ModLister.GetModWithIdentifier( id.StripPostfixes(), true );
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

            // recache issues and selected versions for all these mods.
            ModButtonManager.Notify_ModListChanged();
        }

        public override string ToString()
        {
            var str = _name + "\n";
            str += "ids:\n";
            if ( _modIds == null )
                str += "\tNULL\n";
            else
                foreach ( var modId in _modIds )
                    str += "\t" + modId + "\n";
            str += "names:\n";
            if ( _modNames == null )
                str += "\tNULL\n";
            else
                foreach ( var name in _modNames )
                    str += "\t" + name + "\n";
            return str;
        }


        private static void LogObject( object obj, int lvl = 0 )
        {
            Debug.Log( new string( '\t', lvl ) + obj );
            if ( obj is Dictionary<object, object> dict )
                foreach ( var entry in dict )
                    LogObject( entry, lvl + 1 );
            if ( obj is List<object> list )
                foreach ( var entry in list )
                    LogObject( entry, lvl + 1 );
            if ( obj is KeyValuePair<object, object> pair )
            {
                Debug.Log( new string( '\t', lvl ) + pair.Key + ": " + pair.Value );
                if ( pair.Value is List<object> list2 )
                    LogObject( list2, lvl );
                if ( pair.Value is Dictionary<object, object> dict2 )
                    LogObject( dict2, lvl );
            }
        }

        public string ToYaml()
        {
            var serializerBuilder = new SerializerBuilder();
            var serializer        = serializerBuilder.Build();
            var yaml              = serializer.Serialize( this );
            return yaml;
        }


        public void Add( ModMetaData mod )
        {
            _modIds.Add( mod.PackageId );
            _modNames.Add( mod.Name );

            Save( true );
            ModListManager.Notify_ModListChanged();
        }

        public void Remove( ModMetaData mod )
        {
            // remove by index because mods might have duplicate names.
            var index = _modIds.IndexOf( mod.PackageId );
            if ( index < 0 )
                return; // not found
            if ( _modNames[index] != mod.Name )
            {
                Log.Warning( $"Tried to remove mod {mod.Name} (id: {mod.PackageId}) from mod list," +
                             $" but the mod with that identifier in the ModList is named {_modNames[index]}!" );
                return;
            }

            _modIds.RemoveAt( index );
            _modNames.RemoveAt( index );

            Save( true );
            ModListManager.Notify_ModListChanged();
        }

        public bool Save( bool force = false, Action failureCallback = null, Action successCallback = null )
        {
            // if not yet given a valid name, don't save the ruddy thing.
            if ( Name.NullOrEmpty() )
            {
                failureCallback?.Invoke();
                return false;
            }

            var path = ModListManager.FilePath( this );
            if ( File.Exists( path ) && !force )
            {
                Action okCallback = () => Save( true, failureCallback, successCallback );
                var confirmation = new Dialog_MessageBox( I18n.ConfirmOverwriteModList( Name ), I18n.OK, okCallback,
                                                          I18n.Cancel, failureCallback, null, true, okCallback,
                                                          failureCallback );
                Find.WindowStack.Add( confirmation );
                return false;
            }

            try
            {
                Scribe.saver.InitSaving( path, ModListManager.RootElement );
                ExposeData();
                Scribe.saver.FinalizeSaving();
                if ( !ModListManager.ModLists.Contains( this ) )
                    ModListManager.Notify_ModListsChanged();
                successCallback?.Invoke();
                return true;
            }
            catch ( Exception e )
            {
                Log.Error( e.Message );
                failureCallback?.Invoke();
                return false;
            }
        }
    }
}