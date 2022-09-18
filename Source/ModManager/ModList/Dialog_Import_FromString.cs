using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager {

    public abstract class Dialog_ImExport_String: Window {
        public Dialog_ImExport_String() {
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
        }

        public override Vector2 InitialSize => new Vector2(820, 506); // golden ratio-ish
        protected override float Margin => 8f;

        public virtual string Content { get; set; }

        public virtual ModList ModList { get; set; }

        public override void PostOpen() {
            base.PostOpen();

            GUI.FocusControl("content");
        }
    }

    public class Dialog_Import_FromString: Dialog_ImExport_String {

        public override void DoWindowContents(Rect inRect) {
            var textAreaRect = inRect.TopPartPixels(inRect.height - Margin - 32);
            var importButtonRect = inRect.BottomPartPixels(32).RightPartPixels(200);

            GUI.SetNextControlName("content");
            Content = GUI.TextArea(textAreaRect, Content ?? "");

            if (GUI.changed) {
                Notify_ModListChanged();
            }

            if (Widgets.ButtonText(importButtonRect, I18n.Import)) {
                ModList.Import(Event.current.shift);
                Messages.Message(I18n.XModsImportedFromString(ModList.Mods.Count), MessageTypeDefOf.TaskCompletion, false);
                Close();
            };
        }

        public void Notify_ModListChanged() {
            try {
                ModList = ModList.FromYaml(Content);
            } catch {
                ModList = null;
            }
        }
    }

    public class Dialog_Export_ToString: Dialog_ImExport_String {
        public Dialog_Export_ToString(ModList modlist) {
            _modlist = modlist;
            _content = modlist.ToYaml();
        }

        private readonly string _content;
        private readonly ModList _modlist;

        public override ModList ModList => _modlist;

        public override string Content => _content;

        public override void DoWindowContents(Rect inRect) {
            var textAreaRect = inRect.TopPartPixels(inRect.height - Margin - 32);
            var importButtonRect = inRect.BottomPartPixels(32).RightPartPixels(200);

            GUI.SetNextControlName("content");
            GUI.TextArea(textAreaRect, Content ?? "");

            if (Widgets.ButtonText(importButtonRect, I18n.CopyToClipboard)) {
                GUIUtility.systemCopyBuffer = Content;
                Messages.Message(I18n.XModsExportedToString(ModList.Mods.Count), MessageTypeDefOf.TaskCompletion, false);
                Close();
            }
        }
    }
}
