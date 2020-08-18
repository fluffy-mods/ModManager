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

        private string _source = string.Empty;
        private string _sourceHash;
        public ModMetaData Source
        {
            get => ModLister.GetModWithIdentifier( _source );
            set
            {
                _source = value.PackageId;
                _sourceHash = value.RootDir.GetFolderHash();
                Write();
            }
        }

        public string SourceHash
        {
            get => _sourceHash;
        }

        public void ExposeData()
        {
            Scribe_Values.Look( ref _color, "Color", Color.white );
            Scribe_Values.Look( ref _source, "SourceMod", string.Empty );
            Scribe_Values.Look( ref _sourceHash, "SourceHash" );
        }

        public string FilePath => UserData.GetModAttributesPath( Mod );
        public void Write()
        {
            UserData.Write( this );
        }
    }
}