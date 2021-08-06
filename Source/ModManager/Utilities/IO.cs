// Copyright Karel Kroeze, 2020-2021.
// ModManager/ModManager/IO.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ModManager {
    public static class IO {
        public const string LocalCopyPostfix = "_copy";

        private static readonly Regex _postfixRegex =
            new Regex($@"(?:{ModMetaData.SteamModPostfix}|{LocalCopyPostfix}(?:_\d+)?)$");

        private static readonly List<Pair<ModButton_Installed, ModMetaData>> _batchCreatedCopies =
            new List<Pair<ModButton_Installed, ModMetaData>>();

        private static readonly Dictionary<string, string> _hashCache = new Dictionary<string, string>();


        public static readonly char[] invalidChars =
            Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();

        public static readonly Regex invalidFileNameCharsRegex =
            new Regex(string.Format("[{0}]", Regex.Escape(new string(invalidChars))),
                      RegexOptions.Compiled & RegexOptions.CultureInvariant);

        public static readonly string[] reservedWords =
        {
            "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string DateStamp => $"-{DateTime.Now.Day}-{DateTime.Now.Month}";
        public static string LocalCopyPrefix => "__LocalCopy";
        public static string ModsDir => GenFilePaths.ModsFolderPath;


        private static void Copy(this DirectoryInfo source, string destination, bool recursive) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo[] dirs = source.GetDirectories();

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destination)) {
                Directory.CreateDirectory(destination);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destination, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (recursive) {
                foreach (DirectoryInfo subdir in dirs) {
                    string temppath = Path.Combine(destination, subdir.Name);
                    subdir.Copy(temppath, true);
                }
            }
        }

        internal static void CreateLocalCopies(IEnumerable<ModMetaData> mods, bool force = false) {
            IEnumerable<ModMetaData> steamMods = mods.Where(m => m.Source == ContentSource.SteamWorkshop);
            if (!force && steamMods.Count() > 5) {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                         I18n.CreateLocalCopiesConfirmation(steamMods.Count()),
                                         () => CreateLocalCopies(steamMods, true)));
                return;
            }

            foreach (ModMetaData mod in steamMods) {
                CreateLocalCopy(mod, true);
            }

            FinishBatchCreate();
        }

        internal static void CreateLocalCopy(ModMetaData mod, bool batch = false) {
            LongEventHandler.QueueLongEvent(() => {
                LongEventHandler.SetCurrentEventText(I18n.CreatingLocal(mod.Name));
                if (TryCreateLocalCopy(mod, out ModMetaData copy)) {
                    ModButton_Installed button = ModButton_Installed.For(copy);
                    if (batch) {
                        _batchCreatedCopies.Add(new Pair<ModButton_Installed, ModMetaData>(button, copy));
                    } else {
                        Messages.Message(I18n.CreateLocalSucceeded(mod.Name), MessageTypeDefOf.NeutralEvent, false);
                        LongEventHandler.QueueLongEvent(() => button.Notify_VersionAdded(copy, true), "", true, null);
                    }
                } else {
                    Messages.Message(I18n.CreateLocalFailed(mod.Name), MessageTypeDefOf.RejectInput, false);
                }
            }, null, true, null);
        }

        internal static void DeleteLocal(ModMetaData mod, bool force = false) {
            if (force) {
                LongEventHandler.QueueLongEvent(() => {
                    LongEventHandler.SetCurrentEventText(I18n.RemovingLocal(mod.Name));
                    if (TryRemoveLocalCopy(mod)) {
                        Messages.Message(I18n.RemoveLocalSucceeded(mod.Name),
                                         MessageTypeDefOf.NeutralEvent, false);
                    } else {
                        Messages.Message(I18n.RemoveLocalFailed(mod.Name),
                                         MessageTypeDefOf.RejectInput, false);
                    }

                    // remove this version either way, as it's likely to be borked.
                    ModButton_Installed.For(mod).Notify_VersionRemoved(mod);
                }, null, true, null);
                return;
            }

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                     I18n.ConfirmRemoveLocal(mod.Name), () => DeleteLocal(mod, true), true));
        }

        internal static void DeleteLocalCopies(IEnumerable<ModMetaData> mods) {
            string modList = mods
                         .Select(
                              m =>
                                  $"{m.Name} ({m.SupportedVersionsReadOnly.Select(v => v.ToString()).StringJoin(", ")})")
                         .ToLineList();
            Dialog_MessageBox dialog = Dialog_MessageBox.CreateConfirmation(
                I18n.MassUnSubscribeConfirm(mods.Count(), modList),
                () =>
                {
                    foreach (ModMetaData mod in new List<ModMetaData>(mods))
                    {
                        DeleteLocal(mod, true);
                    }
                },
                true);
            Find.WindowStack.Add(dialog);
        }

        internal static void FinishBatchCreate() {
            LongEventHandler.QueueLongEvent(() => {
                foreach (Pair<ModButton_Installed, ModMetaData> localCopy in _batchCreatedCopies) {
                    ModButton_Installed button = localCopy.First;
                    ModMetaData copy   = localCopy.Second;
                    button.Notify_VersionAdded(copy, true);
                }

                _batchCreatedCopies.Clear();
            }, string.Empty, true, null);
        }

        internal static string GetFolderHash(this DirectoryInfo folder) {
            if (_hashCache.TryGetValue(folder.FullName, out string hash)) {
                return hash;
            }

            IOrderedEnumerable<FileInfo> files = folder.GetFiles("*", SearchOption.AllDirectories)
                              .OrderBy(p => p.FullName);

            using MD5 md5 = MD5.Create();
            foreach (FileInfo file in files) {
                // hash path
                byte[] pathBytes = Encoding.UTF8.GetBytes(file.FullName.Substring(folder.FullName.Length));
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                byte[] contentBytes = File.ReadAllBytes(file.FullName);

                md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            //Handles empty filePaths case
            md5.TransformFinalBlock(new byte[0], 0, 0);

            hash = BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            _hashCache.Add(folder.FullName, hash);
            return hash;
        }

        public static string GetLocalCopyFolder(this ModMetaData mod) {
            return Path.Combine(ModsDir, $"{LocalCopyPrefix}_{mod.Name}_{DateStamp}".SanitizeFileName());
        }

        private static string GetUniquePackageId(ModMetaData mod) {
            string baseId = mod.PackageIdPlayerFacing + LocalCopyPostfix;
            string id     = baseId;
            int i      = 1;
            while (ModLister.GetModWithIdentifier(id) != null) {
                id = baseId + "_" + i++;
            }

            return id;
        }

        public static T ItemFromXmlString<T>(string xml, bool resolveCrossRefs = true) where T : new() {
            if (resolveCrossRefs && DirectXmlCrossRefLoader.LoadingInProgress) {
                Log.Error(
                    "Cannot call ItemFromXmlFile with resolveCrossRefs=true while loading is already in progress.");
            }

            if (xml.NullOrEmpty()) {
                return Activator.CreateInstance<T>();
            }

            T result;
            try {
                XmlDocument xmlDocument = new XmlDocument();
                // Trim should theoretically also get rid of BOMs
                xmlDocument.LoadXml(xml.Trim());
                T t = DirectXmlToObject.ObjectFromXml<T>(xmlDocument.DocumentElement, false);
                if (resolveCrossRefs) {
                    DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
                }

                result = t;
            } catch (Exception ex) {
                Log.Error(
                    $"Exception loading item from string. Loading defaults instead. \nXML: {xml}\n\nException: {ex}");
                result = Activator.CreateInstance<T>();
            }

            return result;
        }

        public static void MassRemoveLocalFloatMenu() {
            List<FloatMenuOption> options     = Utilities.NewOptionsList;
            IEnumerable<ModMetaData> localCopies = ModButtonManager.AllMods.Where(m => m.IsLocalCopy());
            IEnumerable<ModMetaData> outdated    = localCopies.Where(m => !m.VersionCompatible && !m.MadeForNewerVersion);
            IEnumerable<ModMetaData> inactive    = ModButtonManager.AvailableMods.Where(m => m.IsLocalCopy());
            options.Add(new FloatMenuOption(I18n.MassRemoveLocalAll, () => DeleteLocalCopies(localCopies)));
            if (outdated.Any()) {
                options.Add(new FloatMenuOption(I18n.MassRemoveLocalOutdated, () => DeleteLocalCopies(outdated)));
            }

            if (inactive.Any()) {
                options.Add(new FloatMenuOption(I18n.MassRemoveLocalInactive, () => DeleteLocalCopies(inactive)));
            }

            Utilities.FloatMenu(options);
        }

        private static string NewSettingsFilePath(string source, Regex mask, string targetIdentifier) {
            Match match = mask.Match(source);
            return Path.Combine(GenFilePaths.ConfigFolderPath,
                                GenText.SanitizeFilename($"Mod_{targetIdentifier}_{match.Groups[1].Value}.xml"));
        }

        public static string SanitizeFileName(this string str) {
            string fileSystemSanitized = invalidFileNameCharsRegex.Replace(str, "_");
            return reservedWords
                  .Select(reservedWord => $@"^{reservedWord}\.")
                  .Aggregate(fileSystemSanitized,
                             (current, reservedWordPattern) =>
                                 Regex.Replace(current, reservedWordPattern, "_", RegexOptions.IgnoreCase));
        }

        private static Regex SettingsMask(string identifier) {
            return new Regex($@"Mod_{GenText.SanitizeFilename(identifier)}_(.*)\.xml");
        }

        private static void SetUniquePackageId(ModMetaData mod) {
            string id = GetUniquePackageId(mod);
            UpdatePackageId_ModMetaData(mod, id);
            UpdatePackageId_Xml(mod, id);
        }

        public static string StripPostfixes(this string packageId) {
            return _postfixRegex.Replace(packageId.Trim(), "");
        }

        private static bool TryCopyMod(ModMetaData mod,
                                       out ModMetaData copy,
                                       string targetDir,
                                       bool copySettings = true,
                                       bool copyUserData = true) {
            try {
                // copy mod
                mod.RootDir.Copy(targetDir, true);
                copy = new ModMetaData(targetDir);
                SetUniquePackageId(copy);
                ModManager.UserData[copy].Source = mod;
                Manifest manifest = copy.GetManifest();
                manifest.sourceSync = new SourceSync(manifest, mod.PackageId);

                (ModLister.AllInstalledMods as List<ModMetaData>)?.Add(copy);

                // copy settings and color attribute
                if (copySettings) {
                    TryCopySettings(mod, copy);
                }

                if (copyUserData) {
                    TryCopyUserData(mod, copy);
                }

                // set source attribute
                ModManager.UserData[copy].Source = mod;

                return true;
            } catch (Exception e) {
                Log.Error($"Creating local copy failed: {e.Message} \n\n{e.StackTrace}");
                copy = null;
                return false;
            }
        }

        private static void TryCopySettings(ModMetaData source, ModMetaData target, bool deleteOld = false) {
            // find any settings files that belong to the source mod
            Regex mask = SettingsMask(source.FolderName);
            var settings = Directory.GetFiles(GenFilePaths.ConfigFolderPath)
                                    .Where(f => mask.IsMatch(f))
                                    .Select(f => new
                                     {
                                        source = f,
                                        target = NewSettingsFilePath(f, mask, target.FolderName)
                                    });

            // copy settings files, overwriting existing - if any.
            foreach (var setting in settings) {
                Debug.Log($"Copying settings :: {setting.source} => {setting.target}");
                if (deleteOld) {
                    File.Move(setting.source, setting.target);
                } else {
                    File.Copy(setting.source, setting.target, true);
                }
            }
        }

        private static bool TryCopyUserData(ModMetaData source, ModMetaData target, bool deleteOld = false) {
            try {
                string sourcePath = UserData.GetModAttributesPath(source);
                if (!File.Exists(sourcePath)) {
                    return true;
                }

                File.Copy(sourcePath, UserData.GetModAttributesPath(target), true);
                if (deleteOld) {
                    File.Delete(sourcePath);
                }

                return true;
            } catch (Exception err) {
                Debug.Error("Error copying user settings: " +
                            $"\n\tsource: {source.Name}" +
                            $"\n\ttarget: {target.Name}" +
                            $"\n\terror: {err}");
            }

            return false;
        }

        public static bool TryCreateLocalCopy(ModMetaData mod, out ModMetaData copy) {
            if (mod.Source != ContentSource.SteamWorkshop) {
                Log.Error("Can only create local copies of steam workshop mods.");
                copy = null;
                return false;
            }

            string baseTargetDir = mod.GetLocalCopyFolder();
            string targetDir     = baseTargetDir;
            int i             = 2;
            while (Directory.Exists(targetDir)) {
                targetDir = $"{baseTargetDir} ({i++})";
            }

            return TryCopyMod(mod, out copy, targetDir);
        }

        public static bool TryRemoveLocalCopy(ModMetaData mod) {
            if (mod.Source != ContentSource.ModsFolder) {
                Log.Error("Can only delete locally installed non-steam workshop mods.");
                return false;
            }

            try {
                mod.RootDir.Delete(true);
                TryRemoveUserData(mod);
                (ModLister.AllInstalledMods as List<ModMetaData>)?.TryRemove(mod);
                return true;
            } catch (Exception e) {
                Log.Error(e.Message);
                Log.Warning("Deleting failed. Retrying may help.");
                return false;
            }
        }

        private static void TryRemoveUserData(ModMetaData mod) {
            string path = mod.UserData()?.FilePath;
            if (path == null) {
                return;
            }

            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            } catch (Exception err) {
                Debug.Error($"failed to delete {path}:\n{err}");
            }
        }

        public static bool TryUpdateLocalCopy(ModMetaData source, ModMetaData local) {
            // delete and re-copy mod.
            bool removedResult = TryRemoveLocalCopy(local);
            if (!removedResult) {
                return false;
            }

            bool updateResult = TryCopyMod(source, out ModMetaData updated, local.RootDir.FullName, false);
            if (!updateResult) {
                return false;
            }

            // update version 
            ModButton_Installed button = ModButton_Installed.For(updated);
            button.Notify_VersionRemoved(local);
            button.Notify_VersionAdded(updated, true);


            return true;
        }

        private static void UpdatePackageId_ModMetaData(ModMetaData mod, string id) {
            Traverse traverse = Traverse.Create(mod);
            traverse.Field("meta").Field("traverse").SetValue(id);
            traverse.Field("packageIdLowerCase").SetValue(id.ToLower());
        }

        private static void UpdatePackageId_Xml(ModMetaData mod, string id) {
            string filePath = Path.Combine(mod.AboutDir(), "About.xml");
            XmlDocument meta     = new XmlDocument();
            meta.Load(filePath);
            XmlNode node = meta.SelectSingleNode("ModMetaData/packageId");
            if (node == null) {
                Debug.Error($"packageId node not found for {mod.Name}!");
            } else {
                node.InnerText = id;
                meta.Save(filePath);
            }
        }
    }
}
