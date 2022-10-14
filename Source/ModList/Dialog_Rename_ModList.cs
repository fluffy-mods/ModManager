using System.IO;
using System.Linq;
using Verse;

namespace ModManager {
    public class Dialog_Rename_ModList: Dialog_Rename {
        private readonly ModList list;

        public Dialog_Rename_ModList(ModList list) {
            this.list = list;
            curName = list.Name;
            absorbInputAroundWindow = true;
        }

        protected override void SetName(string name) {
            list.Name = name;
        }

        protected override AcceptanceReport NameIsValid(string name) {
            // any name given?
            if (name.Length < 1) {
                return I18n.NameTooShort;
            }

            // check invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars) {
                if (name.Contains(invalidChar)) {
                    return I18n.InvalidName(name, new string(invalidChars));
                }
            }

            return true;
        }
    }
}
