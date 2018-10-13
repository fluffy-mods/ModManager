using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class Dialog_Rename_ModList : Dialog_Rename
    {
        private ModList list;
        private bool _focusedRenameField;

        public Dialog_Rename_ModList( ModList list )
        {
            this.list = list;
            this.curName = list.Name;
            absorbInputAroundWindow = true;
        }

        protected override void SetName( string name )
        {
            list.Name = name;
        }

        protected override AcceptanceReport NameIsValid( string name )
        {
            // any name given?
            if ( name.Length < 1 )
                return I18n.NameTooShort;

            // check invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach ( var invalidChar in invalidChars )
                if ( name.Contains( invalidChar ) )
                    return I18n.InvalidName( name, new string( invalidChars ) );

            // check if file exists
            if ( File.Exists( ModListManager.FilePath( name ) ) )
                return I18n.ModListExists( name );

            return true;
        }
    }
}