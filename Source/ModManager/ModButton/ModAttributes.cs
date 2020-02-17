// ModAttributes.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Specialized;
using Steamworks;
using UnityEngine;
using Verse;

// note to self: don't write code while drunk
namespace ModManager
{
    public class ModAttributes: IExposable
    {
        private string _identifier;
        public ModMetaData Mod { get; private set; }
        public bool TryResolve()
        {
            Mod = ModLister.GetModWithIdentifier( _identifier );
            return Mod != null;
        }
        public ModAttributes()
        {
            // scribe
        }
        public ModAttributes( ModMetaData mod )
        {
            Mod = mod;
            _identifier = mod.PackageId;
        }

        public string Identifier => _identifier;
        public bool IsDefault => _color == Color.white && _source == PublishedFileId_t.Invalid;
        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                ModManager.Instance.WriteSettings();
            }
        }

        private PublishedFileId_t _source = PublishedFileId_t.Invalid;
        private string _sourceHash;
        public ModMetaData Source
        {
            get
            {
                if ( _source == PublishedFileId_t.Invalid )
                    return null;
                return ModLister.GetModWithIdentifier( _source.ToString() );
            }
            set
            {
                if ( ulong.TryParse( value.PackageId, out ulong id ) )
                {
                    _source = new PublishedFileId_t( id );
                    _sourceHash = value.RootDir.GetFolderHash();
                    Debug.Log( $"Source: {_source}, hash: {_sourceHash}" );

                    ModManager.Instance.WriteSettings();
                }
                else
                {
                    Log.Warning( $"Could not parse {value.PackageId} as PublishedFileID" );
                }
            }
        }

        public string SourceHash
        {
            get => _sourceHash;
        }

        public void ExposeData()
        {
            Debug.Log( $"{_identifier} {Scribe.mode} {IsDefault}" +
                       $"\nIdentifier: {_identifier}" +
                       $"\nColor: {_color}" +
                       $"\nSource: {_source} ({_sourceHash})" );

            Scribe_Values.Look( ref _identifier, "Identifier" );
            Scribe_Values.Look( ref _color, "Color", Color.white );
            Scribe_Values.Look( ref _source, "SourceMod", PublishedFileId_t.Invalid );
            Scribe_Values.Look( ref _sourceHash, "SourceHash" );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
                TryResolve();
        }
    }
}