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

namespace ModManager {
    public class ModList: IExposable {
        private Color _color = Color.white;
        private List<string> _modIds = new List<string>();
        private List<string> _modNames = new List<string>();
        private List<string> _modSteamWorkshopIds = new List<string>();
        private string _name;

        [Obsolete("This constructor should not be used directly.")]
        public ModList() {
            // scribe
            Version = 1;
        }

        public ModList(IEnumerable<ModButton> mods, bool transient = false) {
            _modIds = new List<string>();
            _modNames = new List<string>();
            _modSteamWorkshopIds = new List<string>();
            Debug.Log("Creating modlist...");
            foreach (ModButton button in mods.Where(m => m.Active)) {
                Debug.Log($"\tAdding {button.Name} ({button.Identifier}, {button.SteamWorkshopId})");
                _modIds.Add(button.Identifier);
                _modNames.Add(button.Name);
                _modSteamWorkshopIds.Add(button.SteamWorkshopId.ToString());
            }

            if (!transient) {
                Find.WindowStack.Add(new Dialog_Rename_ModList(this));
            }
        }

        public int Version { get; set; }


        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public string Name {
            get => _name;
            set {
                // _oldName is only cleared upon the _end_ of the task, to prevent callbacks setting _oldName to the new _name.
                string oldName = _name;
                _name = value;

                if (oldName.NullOrEmpty() && _name.NullOrEmpty()) {
                    // going from null to null, likely after an import.
                    return;
                }

                // create or rename
                if (oldName.NullOrEmpty()) {
                    ModListManager.TryCreate(this,
                        () => {
                            _name = oldName;
                            Find.WindowStack.Add(new Dialog_Rename_ModList(this));
                        },
                        () => {
                            Messages.Message(I18n.ModListCreated(_name),
                                MessageTypeDefOf.TaskCompletion, false);
                            ModListManager.Notify_ModListsChanged();
                        });
                } else {
                    ModListManager.TryRename(this, oldName, () => _name = oldName, () => {
                        Messages.Message(I18n.ModListRenamed(oldName, _name), MessageTypeDefOf.TaskCompletion,
                            false);
                        ModListManager.Notify_ModListsChanged();
                    });
                }
            }
        }

        [YamlIgnore]
        public Color Color {
            get => _color;
            set {
                _color = value;
                Save(true);
            }
        }

        public List<ModIdentifier> Mods {
            get {
                List<ModIdentifier> mods = new List<ModIdentifier>();
                for (int i = 0; i < _modIds.Count; i++) {
                    mods.Add(new ModIdentifier(_modIds[i], _modNames[i], _modSteamWorkshopIds[i]));
                }

                return mods;
            }
            set {
                _modIds.Clear();
                _modNames.Clear();
                _modSteamWorkshopIds.Clear();
                foreach (ModIdentifier mod in value) {
                    _modIds.Add(mod.Id);
                    _modNames.Add(mod.Name);
                    _modSteamWorkshopIds.Add(mod.SteamWorkshopId);
                }
            }
        }

        public void ExposeData() {
            Scribe_Values.Look(ref _name, "Name");
            Scribe_Values.Look(ref _color, "Color", Color.white);
            Scribe_Collections.Look(ref _modIds, "modIds");
            Scribe_Collections.Look(ref _modNames, "modNames");
            Scribe_Collections.Look(ref _modSteamWorkshopIds, "modSteamWorkshopIds");
        }

        public static ModList FromFile(string path) {
#pragma warning disable CS0618 // Type or member is obsolete
            ModList list = new ModList();
#pragma warning restore CS0618 // Type or member is obsolete
            Scribe.loader.InitLoading(path);
            Scribe.EnterNode(ModListManager.RootElement);
            list.ExposeData();
            Scribe.loader.FinalizeLoading();
            Debug.Log(list.ToString());

            list._modSteamWorkshopIds ??=
                Enumerable.Repeat("0", list._modIds.Count).ToList(); // provided for backwards compatibility

            if (list._modIds.NullOrEmpty() || list._modNames.NullOrEmpty()) {
                throw new InvalidDataException("ModList contains no mods.");
            }

            if (list._modIds.Count != list._modNames.Count) {
                throw new InvalidDataException("ids and names unbalanced.");
            }

            if (list._modIds.Count != list._modSteamWorkshopIds.Count) {
                throw new InvalidDataException("ids and steam workshop ids unbalanced.");
            }

            return list;
        }

        public static ModList FromSave(string name, string path) {
#pragma warning disable CS0618 // Type or member is obsolete
            ModList list = new ModList();
#pragma warning restore CS0618 // Type or member is obsolete

            try {
                Scribe.loader.InitLoadingMetaHeaderOnly(path);
                ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.None, false);

                list._name = name;
                list._modIds = ScribeMetaHeaderUtility.loadedModIdsList;
                list._modNames = ScribeMetaHeaderUtility.loadedModNamesList;

                // steam ids _should_ exist, but sometimes they don't. 
                list._modSteamWorkshopIds =
                    ScribeMetaHeaderUtility.loadedModSteamIdsList?.Select(x => x.ToString()).ToList() ??
                    Enumerable.Repeat("0", list._modIds.Count).ToList();

            } finally {
                // clean up after ourselves if something bungs up
                Scribe.loader.FinalizeLoading();
                Debug.Log(list.ToString());
            }

            return list;
        }

        // TODO: figure out if/how we can ignore unknown fields in the deserializer
        public static IDeserializer Deserializer = new DeserializerBuilder().Build();

        public static ModList FromYaml(string source) {
            try {
                return Deserializer.Deserialize<ModList>(source);
            } catch (Exception err) {
                ModListManager.Notify_ModListsChanged();
                throw err;
            }
        }

        public void Import(bool add) {
            if (!add) {
                ModButtonManager.Reset(false);
            }

            for (int i = 0; i < _modIds.Count; i++) {
                string id = _modIds[i];
                string name = _modNames[i];
                ulong steamWorkshopId = ulong.TryParse(_modSteamWorkshopIds[i], out ulong result) ? result : 0;
                ModMetaData mod = ModLister.GetModWithIdentifier(id.StripPostfixes(), true);
                if (mod != null) {
                    ModButton_Installed button = ModButton_Installed.For(mod);
                    button.Notify_ResetSelected();
                    mod.Active = true;
                    ModButtonManager.TryAdd(button, false);
                } else {
                    ModButtonManager.TryAdd(new ModButton_Missing(id, name, steamWorkshopId));
                }
            }

            // recache issues and selected versions for all these mods.
            ModButtonManager.Notify_ModListChanged();
        }

        public override string ToString() {
            string str = _name + "\n";
            str += "ids:\n";
            if (_modIds == null) {
                str += "\tNULL\n";
            } else {
                foreach (string modId in _modIds) {
                    str += "\t" + modId + "\n";
                }
            }

            str += "names:\n";
            if (_modNames == null) {
                str += "\tNULL\n";
            } else {
                foreach (string name in _modNames) {
                    str += "\t" + name + "\n";
                }
            }

            str += "steamWorkshopIds:\n";
            if (_modSteamWorkshopIds == null) {
                str += "\tNULL\n";
            } else {
                foreach (string steamWorkshopId in _modSteamWorkshopIds) {
                    str += "\t" + steamWorkshopId + "\n";
                }
            }

            return str;
        }


        private static void LogObject(object obj, int lvl = 0) {
            Debug.Log(new string('\t', lvl) + obj);
            if (obj is Dictionary<object, object> dict) {
                foreach (KeyValuePair<object, object> entry in dict) {
                    LogObject(entry, lvl + 1);
                }
            }

            if (obj is List<object> list) {
                foreach (object entry in list) {
                    LogObject(entry, lvl + 1);
                }
            }

            if (obj is KeyValuePair<object, object> pair) {
                Debug.Log(new string('\t', lvl) + pair.Key + ": " + pair.Value);
                if (pair.Value is List<object> list2) {
                    LogObject(list2, lvl);
                }

                if (pair.Value is Dictionary<object, object> dict2) {
                    LogObject(dict2, lvl);
                }
            }
        }

        public string ToYaml() {
            SerializerBuilder serializerBuilder = new SerializerBuilder();
            ISerializer serializer = serializerBuilder.Build();
            string yaml = serializer.Serialize(this);
            return yaml;
        }

        public void Add(ModMetaData mod) {
            _modIds.Add(mod.PackageId);
            _modNames.Add(mod.Name);
            _modSteamWorkshopIds.Add(mod.GetPublishedFileId().m_PublishedFileId.ToString());

            Save(true);
            ModListManager.Notify_ModListChanged();
        }

        public void Remove(ModMetaData mod) {
            // remove by index because mods might have duplicate names.
            int index = _modIds.IndexOf(mod.PackageId);
            if (index < 0) {
                return; // not found
            }

            if (_modNames[index] != mod.Name) {
                Log.Warning($"Tried to remove mod {mod.Name} (id: {mod.PackageId}) from mod list," +
                            $" but the mod with that identifier in the ModList is named {_modNames[index]}!");
                return;
            }

            _modIds.RemoveAt(index);
            _modNames.RemoveAt(index);
            _modSteamWorkshopIds.RemoveAt(index);

            Save(true);
            ModListManager.Notify_ModListChanged();
        }

        public bool Save(bool force = false, Action failureCallback = null, Action successCallback = null) {
            // if not yet given a valid name, don't save the ruddy thing.
            if (Name.NullOrEmpty()) {
                failureCallback?.Invoke();
                return false;
            }

            string path = ModListManager.FilePath(this);
            if (File.Exists(path) && !force) {
                void okCallback() {
                    Save(true, failureCallback, successCallback);
                }

                Dialog_MessageBox confirmation = new Dialog_MessageBox(I18n.ConfirmOverwriteModList(Name), I18n.OK,
                    okCallback,
                    I18n.Cancel, failureCallback, null, true, okCallback,
                    failureCallback);
                Find.WindowStack.Add(confirmation);
                return false;
            }

            try {
                Scribe.saver.InitSaving(path, ModListManager.RootElement);
                ExposeData();
                Scribe.saver.FinalizeSaving();
                if (!ModListManager.ModLists.Contains(this)) {
                    ModListManager.Notify_ModListsChanged();
                }

                successCallback?.Invoke();
                return true;
            } catch (Exception e) {
                Log.Error(e.Message);
                failureCallback?.Invoke();
                return false;
            }
        }
    }
}
