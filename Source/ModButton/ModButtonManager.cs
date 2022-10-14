// ModButtonManager.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.Linq;
using FluffyUI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ModManager {
    public static class ModButtonManager {
        private static List<ModButton> _allButtons;
        private static List<ModButton> _activeButtons;
        private static List<ModButton> _availableButtons;

        public static List<ModButton> AllButtons {
            get {
                if (_allButtons == null) {
                    InitializeModButtons();
                }

                return _allButtons;
            }
        }

        public static List<ModButton> ActiveButtons {
            get {
                if (_activeButtons == null) {
                    InitializeModButtons();
                }

                return _activeButtons;
            }
        }

        public static List<ModButton> AvailableButtons {
            get {
                if (_availableButtons == null) {
                    InitializeModButtons();
                }

                return _availableButtons;
            }
        }

        public static IEnumerable<ModMetaData> AllMods => AllButtons.OfType<ModButton_Installed>()
                                                                    .SelectMany(b => b.Versions);

        private static List<ModMetaData> _activeMods;

        public static List<ModMetaData> ActiveMods => _activeMods ??= ActiveButtons.OfType<ModButton_Installed>().Select(b => b.Selected).ToList();

        public static IEnumerable<ModMetaData> AvailableMods => AllMods.Where(m => !m.Active);
        public static bool AnyIssue => Issues.Any(i => i.Severity > 1);

        public static void TryAdd(ModButton button, bool notifyModListChanged = true) {
            _allButtons.TryAdd(button);
            if (button.Active) {
                _activeButtons.TryAdd(button);
                _availableButtons.TryRemove(button);
                if (notifyModListChanged && button is ModButton_Installed) {
                    Notify_ModListChanged();
                }
            } else {
                _availableButtons.TryAdd(button);
                _activeButtons.TryRemove(button);
                SortAvailable();
            }
        }

        public static void TryRemove(ModButton mod) {
            _allButtons.TryRemove(mod);

            if (_activeButtons.TryRemove(mod) && mod is ModButton_Installed) {
                Notify_ModListChanged();
            }

            _availableButtons.TryRemove(mod);
        }

        internal static void InitializeModButtons() {
            _allButtons = new List<ModButton>();
            _activeButtons = new List<ModButton>();
            _availableButtons = new List<ModButton>();

            // create all the buttons
            foreach (IGrouping<string, ModMetaData> mods in ModLister.AllInstalledMods.GroupBy(m => Utilities.TrimModName(m.Name))) {
                TryAdd(new ModButton_Installed(mods), false);
            }

            SortActive();
            SortAvailable();
        }

        private static void SortAvailable() {
            _availableButtons = _availableButtons
                               .OrderByDescending(b => b.SortOrder)
                               .ThenBy(b => b.TrimmedName)
                               .ToList();
        }

        private static void SortActive() {
            _activeButtons = _activeButtons
                            .OrderBy(b => b.LoadOrder)
                            .ToList();
        }

        private static void Notify_RecacheAllManifests() {
            foreach (ModMetaData mod in ActiveMods) {
                mod.GetManifest().Notify_Recache();
            }
        }

        private static void Notify_RecacheAllModButtons() {
            foreach (ModButton button in AllButtons) {
                button.Notify_RecacheIssues();
            }
        }

        public static void Notify_RecacheModMetaData() {
            _activeMods = null;
        }

        public static void Notify_ModListChanged() {
            Notify_RecacheModMetaData();
            ModsConfig.SetActiveToList(ActiveMods.Select(m => m.PackageId).ToList());
            Notify_RecacheIssues();
        }

        public static void Notify_RecacheIssues() {
            Notify_RecacheIssuesList();
            Notify_RecacheAllManifests();
            Notify_RecacheAllModButtons();
        }

        public static ModButton_Installed CoreMod => AllButtons.First(b => b.IsCoreMod) as ModButton_Installed;
        public static ModButton_Installed ModManagerMod => AllButtons.First(b => b.IsModManager) as ModButton_Installed;
        public static IEnumerable<ModButton_Installed> Expansions => AllButtons.Where(b => b.IsExpansion).Cast<ModButton_Installed>();
        public static void RecacheIssues() {
            _issues = ActiveButtons.SelectMany(b => b.Requirements).ToList();
        }

        public static void Reorder(int from, int to) {
            if (to == from) {
                return;
            }

            Insert(ActiveButtons[from], to);
        }

        public static void MoveBefore(ModButton from, ModButton to) {
            Insert(from, ActiveButtons.IndexOf(to));
        }

        public static void MoveAfter(ModButton from, ModButton to) {
            Insert(from, ActiveButtons.IndexOf(to) + 1);
        }

        public static void Insert(ModButton button, int to) {
            AllButtons.TryAdd(button);
            AvailableButtons.TryRemove(button);
            if (ActiveButtons.Contains(button)) {
                if (ActiveButtons.IndexOf(button) < to) {
                    to--;
                }

                ActiveButtons.Remove(button);
            }
            ActiveButtons.Insert(Mathf.Clamp(to, 0, ActiveButtons.Count), button);

            if (button is ModButton_Installed) {
                Notify_ModListChanged();
            }
        }

        public static void Notify_ActiveStatusChanged(ModButton_Installed mod, bool active) {
            if (active) {
                _availableButtons.TryRemove(mod);
                _activeButtons.TryAdd(mod);

                Notify_ModListChanged();
            } else {
                _activeButtons.TryRemove(mod);
                _availableButtons.TryAdd(mod);

                Notify_ModListChanged();
                SortAvailable();
            }
        }

        public static void Notify_Unsubscribed(string publishedFileId) {
            ModButton_Installed button = AllButtons.OfType<ModButton_Installed>()
                .FirstOrDefault( b => b.Versions.Any( m => m.Source == ContentSource.SteamWorkshop &&
                                                           m.PackageId == publishedFileId ) );
            ModMetaData mod = button?.Versions.First( m => m.Source == ContentSource.SteamWorkshop &&
                                                  m.PackageId == publishedFileId );
            button?.Notify_VersionRemoved(mod);
        }

        public static void Notify_DownloadCompleted(ModMetaData mod) {
            ModButton_Downloading downloading = AllButtons.OfType<ModButton_Downloading>()
                .FirstOrDefault( b => b.Identifier == mod.PackageId );

            ModButton_Missing missing = AllButtons.OfType<ModButton_Missing>()
                .FirstOrDefault( b => b.Identifier == mod.PackageId );

            // add installed item to MBM
            ModButton_Installed installed = ModButton_Installed.For( mod );
            if (missing != null && missing.Active) {
                Insert(installed, ActiveButtons.IndexOf(missing));
            } else {
                TryAdd(installed);
            }

            Page_BetterModConfig.Instance.Selected = installed;
            TryRemove(downloading);
            TryRemove(missing);

            // Page_BetterModConfig.Instance.Notify_ModsListChanged();
        }

        public static void Notify_RecacheIssuesList() {
            _issues = null;
        }

        private static List<Dependency> _issues;
        public static List<Dependency> Issues {
            get {
                if (_issues == null) {
                    RecacheIssues();
                }

                return _issues;
            }
        }

        public static void Sort() {
            // ReSharper disable once InvalidXmlDocComment
            /**
             * Topological sort.
             * Depth first, because it's the easiest to understand.
             * https://en.wikipedia.org/wiki/Topological_sorting
             *
             *  L ← Empty list that will contain the sorted nodes       // we'll use done.
             *  while exists nodes without a permanent mark do
             *      select an unmarked node n
             *      visit(n)
             *
             *  function visit(node n)
             *      if n has a permanent mark then                      // is in done list
             *          return
             *      if n has a temporary mark then                      // is in progress list
             *          stop   (not a DAG)
             *
             *      mark n with a temporary mark                        // add to progress list
             *
             *      for each node m with an edge from n to m do         // visit each dependency
             *          visit(m)
             *
             *      remove temporary mark from n                        // remove from progress list
             *      mark n with a permanent mark                        // add to done list
             *      add n to head of L
             */

            Dictionary<ModButton, HashSet<ModButton>> graph    = new Dictionary<ModButton, HashSet<ModButton>>();
            HashSet<ModButton> done     = new HashSet<ModButton>();
            HashSet<ModButton> progress = new HashSet<ModButton>();

            // create a directed acyclic graph.
            foreach (ModButton activeButton in ActiveButtons) {
                if (!graph.ContainsKey(activeButton)) {
                    graph[activeButton] = new HashSet<ModButton>();
                }

                if (!(activeButton is ModButton_Installed installedActiveButton)) {
                    continue;
                }

                foreach (ModMetaData target in installedActiveButton.Manifest.LoadBefore
                                                             .Select(d => d.Target)
                                                             .Where(t => t != null)) {
                    ModButton_Installed targetButton = ModButton_Installed.For( target );
                    if (!graph.ContainsKey(targetButton)) {
                        graph[targetButton] = new HashSet<ModButton>();
                    }

                    graph[targetButton].Add(activeButton);
                }


                foreach (ModMetaData target in installedActiveButton.Manifest.LoadAfter
                                                             .Concat(installedActiveButton.Manifest.Dependencies)
                                                             .Select(d => d.Target)
                                                             .Where(t => t != null)) {
                    ModButton_Installed targetButton = ModButton_Installed.For( target );
                    graph[activeButton].Add(targetButton);
                    if (!graph.ContainsKey(targetButton)) {
                        graph[targetButton] = new HashSet<ModButton>();
                    }
                }
            }

            // do that sort
            foreach (ModButton activeButton in ActiveButtons) {
                FailReason success = Sort_Visit( activeButton, graph, ref done, ref progress );
                if (!success) {
                    // we have a cyclic dependency.
                    Messages.Message(I18n.SortFailed_Cyclic(activeButton.Name, success.Reason), MessageTypeDefOf.CautionInput, false);
                    return;
                }
            }

            // reset mod list, then add mods back in order.
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            Reset(false);
            foreach (ModButton mod in done) {
                // try to avoid re-caching too many times.
                if (mod is ModButton_Installed installed) {
                    installed.Selected.Active = true;
                } else {
                    mod.Active = true;
                }

                TryAdd(mod, false);
            }

            Notify_ModListChanged();
        }

        private static FailReason Sort_Visit(ModButton node, Dictionary<ModButton, HashSet<ModButton>> graph, ref HashSet<
                                                  ModButton> done,
                                        ref HashSet<ModButton> progress) {
            if (done.Contains(node)) {
                return true;
            }

            if (progress.Contains(node)) {
                return node.Name;
            }

            progress.Add(node);
            foreach (ModButton dep in graph[node]) {
                FailReason success = Sort_Visit( dep, graph, ref done, ref progress );
                if (!success) {
                    return success.Reason ?? dep.Name;
                }
            }

            progress.Remove(node);
            Debug.Log(node.Name);
            done.Add(node);
            return true;
        }

        public static void Reset(bool addDefaultMods = true) {
            foreach (ModButton button in new List<ModButton>(ActiveButtons)) {
                button.Active = false;
            }

            if (addDefaultMods) {
                CoreMod.Active = true;

                if (ModManager.Settings.AddExpansionsToNewModLists) {
                    foreach (ModButton_Installed expansion in Expansions) {
                        expansion.Active = true;
                    }
                }

                if (ModManager.Settings.AddHugsLibToNewModLists) {
                    ModMetaData hugslib = ModLister.GetModWithIdentifier( "unlimitedhugs.hugslib" );
                    if (hugslib != null) {
                        ModButton_Installed hugslibButton = ModButton_Installed.For( hugslib );
                        hugslibButton.Active = true;
                    }
                }

                if (ModManager.Settings.AddModManagerToNewModLists && ModManagerMod != null) {
                    ModManagerMod.Active = true;
                }

                if (ModManager.Settings.AddHugsLibToNewModLists || ModManager.Settings.AddModManagerToNewModLists) {
                    // also try to activate harmony
                    ModMetaData harmony = ModLister.GetModWithIdentifier( "brrainz.harmony" );
                    if (harmony != null) {
                        ModButton_Installed harmonyButton = ModButton_Installed.For( harmony );
                        harmonyButton.Active = true;
                        Insert(harmonyButton, 0);
                    }
                }
            }

            Notify_ModListChanged();
        }
    }
}
