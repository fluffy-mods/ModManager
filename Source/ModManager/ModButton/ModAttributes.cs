// ModAttributes.cs
// Copyright Karel Kroeze, 2018-2018

using System.IO;
using Steamworks;
using UnityEngine;
using Verse;

// note to self: don't write code while drunk
namespace ModManager
{
    public class ModAttributes: IUserData
    {
        public ModMetaData Mod { get;  set; }        
        public ModAttributes()
        {
            // scribe
        }
        public ModAttributes( ModMetaData mod )
        {
            Mod = mod;
        }

        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Write();
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
                    Write();
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
            Scribe_Values.Look( ref _color, "Color", Color.white );
            Scribe_Values.Look( ref _source, "SourceMod", PublishedFileId_t.Invalid );
            Scribe_Values.Look( ref _sourceHash, "SourceHash" );
        }
        public string FilePath => UserData.GetModAttributesPath( Mod );
        public void Write()
        {
            UserData.Write( this );
        }
    }
}