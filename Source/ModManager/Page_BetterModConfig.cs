// Window_ModSelection.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ModManager.Constants;
using static ModManager.Resources;

namespace ModManager {
    public class Page_BetterModConfig: Page_ModsConfig {
        private Vector2 _availableScrollPosition = Vector2.zero;
        private Vector2 _activeScrollPosition = Vector2.zero;
        private int _scrollViewHeight = 0;
        private ModButton _selected;
        private string _availableFilter;
        private string _activeFilter;
        private bool _availableFilterVisible;
        private bool _activeFilterVisible = true;
        private int _activeModsHash;

        public enum FocusArea {
            AvailableFilter,
            Available,
            ActiveFilter,
            Active
        }
        private FocusArea _focusArea = FocusArea.Available;
        private int _focusElement = 1;

        public Page_BetterModConfig() {
            Instance = this;
            closeOnAccept = false;
            closeOnCancel = true;
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = false;
            resizeable = true;
            AccessTools.Field(typeof(Window), "resizer")
                .SetValue(this, new WindowResizer { minWindowSize = MinimumSize });
        }

        public bool FilterAvailable => !_availableFilterVisible && !_availableFilter.NullOrEmpty();
        public bool FilterActive => !_activeFilterVisible && !_activeFilter.NullOrEmpty();

        public static Page_BetterModConfig Instance { get; protected set; }

        public override Vector2 InitialSize => StandardSize;

        public static Vector2 MinimumSize => StandardSize * 2 / 3f;

        public ModButton Selected {
            get => _selected;
            set {
                if (_selected == value) {
                    return;
                }

                _selected = value;
                CrossPromotionManager.Notify_UpdateRelevantMods();

                // cop-out if null
                if (value == null) {
                    return;
                }

                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

                // clear text field focus
                GUIUtility.keyboardControl = 0;

                // set correct focus
                if (!value.Active) {
                    _focusArea = FocusArea.Available;
                    if (!FilteredAvailableButtons.Contains(value)) {
                        _availableFilter = null;
                    }

                    _focusElement = FilteredAvailableButtons.FirstIndexOf(mod => mod == value);
                    EnsureVisible(ref _availableScrollPosition, _focusElement);
                } else {
                    _focusArea = FocusArea.Active;
                    if (!FilteredActiveButtons.Contains(value)) {
                        _activeFilter = null;
                    }

                    _focusElement = FilteredActiveButtons.FirstIndexOf(mod => mod == value);
                    EnsureVisible(ref _activeScrollPosition, _focusElement);
                }
            }
        }

        public bool SelectedHasFocus =>
            (_focusArea == FocusArea.Active && ModButtonManager.ActiveButtons.Contains(Selected)) ||
            (_focusArea == FocusArea.Available && ModButtonManager.AvailableButtons.Contains(Selected));

        public override void
            ExtraOnGUI() {
            base.ExtraOnGUI();
            DraggingManager.OnGUI();

#if DEBUG
            DebugOnGUI();
#endif
        }

        private static void DebugOnGUI() {
            string msg = "";
            List<ModMetaData> mods = ModsConfig.ActiveModsInLoadOrder.ToList();
            for (int i = 0; i < mods.Count; i++) {
                ModMetaData mod = mods[i];
                msg += $"{i}: {mod?.Name ?? "NULL"}\t({mod?.PackageId ?? "NULL"})\n";
            }
            Widgets.Label(new Rect(0, 0, Screen.width, Screen.height), msg);
        }

        public override void WindowUpdate() {
            base.WindowUpdate();
            DraggingManager.Update();
            CrossPromotionManager.Update();
        }

        public override void DoWindowContents(Rect canvas) {

            CheckResized();
            HandleKeyboardNavigation();

            int iconBarHeight = IconSize + SmallMargin;
            int colWidth = Mathf.FloorToInt( canvas.width / 5 );

            Rect availableRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                colWidth,
                canvas.height - SmallMargin - iconBarHeight );
            Rect moreModButtonsRect = new Rect(
                canvas.xMin,
                availableRect.yMax + SmallMargin,
                colWidth,
                iconBarHeight );
            Rect activeRect = new Rect(
                availableRect.xMax + SmallMargin,
                canvas.yMin,
                colWidth,
                canvas.height - SmallMargin - iconBarHeight);
            Rect modSetButtonsRect = new Rect(
                activeRect.xMin,
                activeRect.yMax + SmallMargin,
                colWidth,
                iconBarHeight );
            Rect detailRect = new Rect(
                activeRect.xMax + SmallMargin,
                canvas.yMin,
                canvas.width - (( colWidth + SmallMargin ) * 2),
                canvas.height - SmallMargin - iconBarHeight);
            Rect modButtonsRect = new Rect(
                detailRect.xMin,
                detailRect.yMax + SmallMargin,
                detailRect.width,
                iconBarHeight );

            // if ( !DraggingManager.Dragging && !( Mouse.IsOver( availableRect ) || Mouse.IsOver( activeRect ) ) )
            //     GUI.DragWindow();
            DoAvailableMods(availableRect);
            DoActiveMods(activeRect);
            DoDetails(detailRect);

            DoAvailableModButtons(moreModButtonsRect);
            DoActiveModButtons(modSetButtonsRect);
            Selected?.DoModActionButtons(modButtonsRect);
        }

        private float _width;
        private void CheckResized() {
            if (windowRect.width != _width) {
                ModButton.Notify_ModButtonSizeChanged();
                _width = windowRect.width;
            }
        }

        private void DoAvailableModButtons(Rect canvas) {
            Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
            canvas = canvas.ContractedBy(SmallMargin / 2f);

            Rect iconRect = new Rect(
                canvas.xMax - IconSize,
                canvas.yMin,
                IconSize,
                IconSize );
            if (Utilities.ButtonIcon(ref iconRect, Steam, I18n.GetMoreMods_SteamWorkshop, Status_Plus, Direction8Way.NorthWest)) {
                SteamUtility.OpenSteamWorkshopPage();
            }

            if (Utilities.ButtonIcon(ref iconRect, Ludeon, I18n.GetMoreMods_LudeonForums, Status_Plus, Direction8Way.NorthWest)) {
                Application.OpenURL("http://rimworldgame.com/getmods");
            }

            iconRect.x = canvas.xMin;
            if (ModButtonManager.AllMods.Any(m => m.Source == ContentSource.SteamWorkshop) &&
                Utilities.ButtonIcon(ref iconRect, Steam, I18n.MassUnSubscribe, Status_Cross, Direction8Way.NorthWest, Color.red, direction: UIDirection.RightThenDown)) {
                Workshop.MassUnsubscribeFloatMenu();
            }

            if (ModButtonManager.AllMods.Any(m => m.Source == ContentSource.ModsFolder && !m.IsCoreMod) &&
                 Utilities.ButtonIcon(ref iconRect, Folder, I18n.MassRemoveLocal, Status_Cross, mouseOverColor: Color.red, direction: UIDirection.RightThenDown)) {
                IO.MassRemoveLocalFloatMenu();
            }
        }

        private int _issueIndex = 0;
        private void DoActiveModButtons(Rect canvas) {
            Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
            canvas = canvas.ContractedBy(SmallMargin / 2f);

            Rect iconRect = new Rect(
                canvas.xMax - IconSize,
                canvas.yMin,
                IconSize,
                IconSize);
            if (Utilities.ButtonIcon(ref iconRect, File, I18n.ModListsTip)) {
                DoModListFloatMenu();
            }

            if (ModButtonManager.ActiveMods.Any(mod => mod.Source == ContentSource.SteamWorkshop)) {
                if (Utilities.ButtonIcon(ref iconRect, Folder, I18n.CreateLocalCopies, Status_Plus)) {
                    IO.CreateLocalCopies(ModButtonManager.ActiveMods);
                }
            }

            if (ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
                .Any(b => b.Identifier.IsSteamWorkshopIdentifier())) {
                if (Utilities.ButtonIcon(ref iconRect, Steam, I18n.SubscribeAllMissing, Status_Plus, Direction8Way.NorthWest)) {
                    Workshop.Subscribe(ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
                        .Where(b => b.Identifier.IsSteamWorkshopIdentifier()).Select(b => b.Identifier));
                }
            }

            iconRect.x = canvas.xMin + SmallMargin;
            int severityThreshold = 2;
            IEnumerable<Dependency> relevantIssues = ModButtonManager.Issues.Where( i => i.Severity >= severityThreshold );
            if (relevantIssues.Any()) {
                IOrderedEnumerable<IGrouping<Manifest, Dependency>> groupedIssues = relevantIssues
                    .GroupBy( i => i.parent )
                    .OrderByDescending( bi => bi.Max( i => i.Severity ) )
                    .ThenBy( bi => bi.Key.Mod.Name );
                foreach (IGrouping<Manifest, Dependency> buttonIssues in groupedIssues) {
                    string tip = $"<b>{buttonIssues.Key.Mod.Name}</b>\n";
                    tip += buttonIssues.Select(i => i.Tooltip.Colorize(i.Color)).StringJoin("\n");
                    TooltipHandler.TipRegion(iconRect, tip);
                }

                Dependency worstIssue = relevantIssues.MaxBy(i => i.Severity);
                Color color = worstIssue.Color;

                if (Widgets.ButtonImage(iconRect, worstIssue.StatusIcon, color)) {
                    _issueIndex = Utilities.Modulo(_issueIndex, groupedIssues.Count());
                    Selected = ModButton_Installed.For(groupedIssues.ElementAt(_issueIndex++).Key.Mod);
                }
                iconRect.x += IconSize + SmallMargin;
            }
            if (ModButtonManager.ActiveButtons.Count > 0) {
                if (Utilities.ButtonIcon(ref iconRect, Spinner[0], I18n.ResetMods, mouseOverColor: Color.red, direction: UIDirection.RightThenDown)) {
                    Find.WindowStack.Add(new Dialog_MessageBox(I18n.ConfirmResetMods, I18n.Yes,
                                                                 () => ModButtonManager.Reset(), I18n.Cancel,
                                                                 buttonADestructive: true));
                }
            }

            if (ModButtonManager.ActiveButtons.Count > 1 && ModButtonManager.AnyIssue) {
                if (Utilities.ButtonIcon(ref iconRect, Wand, I18n.SortMods)) {
                    ModButtonManager.Sort();
                }
            }
        }

        private void DoModListFloatMenu() {
            List<FloatMenuOption> options = Utilities.NewOptionsList;
            options.Add(new FloatMenuOption(I18n.SaveModList, () => new ModList(ModButtonManager.ActiveButtons)));
            options.Add(new FloatMenuOption(I18n.LoadModListFromSave, DoLoadModListFromSaveFloatMenu));
            options.Add(new FloatMenuOption(I18n.ImportModList, () => ModList.FromYaml(GUIUtility.systemCopyBuffer)));
            options.AddRange(ModListManager.SavedModListsOptions);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DoLoadModListFromSaveFloatMenu() {
            Find.WindowStack.Add(new FloatMenu(ModListManager.SaveFileOptions));
        }

        private int _lastControlID = 0;
        public void HandleKeyboardNavigation() {
            if (!Find.WindowStack.CurrentWindowGetsInput) {
                return;
            }

            int id = GUIUtility.keyboardControl;
            if (_lastControlID != id) {
                _lastControlID = id;
                Debug.Log($"Focus: {_focusArea}, {_focusElement}, {id}, {GUI.GetNameOfFocusedControl()}, {Event.current.type}");
            }

            // unity tries to select the next textField on Tab (which is _not_ documented!)
            if (_focusArea.ToString() != GUI.GetNameOfFocusedControl()) {
                if (_focusArea == FocusArea.ActiveFilter || _focusArea == FocusArea.AvailableFilter) {
                    // focus the control we want.
                    GUI.FocusControl(_focusArea.ToString());
                } else {
                    // clear focus.
                    GUIUtility.keyboardControl = 0;
                }
            }


            // handle keyboard events
            KeyCode key = Event.current.keyCode;
            bool shift = Event.current.shift;
            if (Event.current.type == EventType.KeyDown) {
                // move controlled area on (shift) tab
                if (key == KeyCode.Tab) {
                    // determine new focus
                    int focusInt = Utilities.Modulo( (int) _focusArea + ( shift ? -1 : 1 ), 4 );
                    FocusArea focus = (FocusArea) focusInt;

                    Debug.Log($"current focus: {_focusArea}, new focus: {focus}");

                    // apply new focus
                    _focusArea = focus;
                    switch (focus) {
                        case FocusArea.AvailableFilter:
                        case FocusArea.ActiveFilter:
                            GUI.FocusControl(focus.ToString());
                            break;
                        case FocusArea.Available:
                            SelectAt(FilteredAvailableButtons, _availableScrollPosition);
                            break;
                        case FocusArea.Active:
                            SelectAt(FilteredActiveButtons, _activeScrollPosition);
                            break;
                        default:
                            break;
                    }
                    return;
                }

                if (_focusArea == FocusArea.Available) {
                    switch (key) {
                        case KeyCode.UpArrow:
                            SelectPrevious(FilteredAvailableButtons);
                            break;
                        case KeyCode.DownArrow:
                            SelectNext(FilteredAvailableButtons);
                            break;
                        case KeyCode.PageUp:
                            SelectFirst(FilteredAvailableButtons);
                            break;
                        case KeyCode.PageDown:
                            SelectLast(FilteredAvailableButtons);
                            break;
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                            Log.Message($"{Selected.Name} active?: {Selected.Active}");
                            Selected.Active = true;
                            Log.Message($"{Selected.Name} active?: {Selected.Active}");

                            if (FilteredAvailableButtons.Any()) {
                                int index = Math.Min( _focusElement, FilteredAvailableButtons.Count - 1 );
                                Selected = FilteredAvailableButtons.ElementAt(index);
                            } else {
                                Selected = Selected;
                            }

                            break;
                        case KeyCode.RightArrow:
                            if (shift) {
                                Selected.Active = true;
                                Selected = Selected; // sets _focusElement, _focusArea, plays sound.
                            } else {
                                SelectAt(FilteredActiveButtons, _availableScrollPosition, _activeScrollPosition);
                            }
                            break;
                        case KeyCode.None:
                            break;
                        case KeyCode.Backspace:
                            break;
                        case KeyCode.Delete:
                            break;
                        case KeyCode.Tab:
                            break;
                        case KeyCode.Clear:
                            break;
                        case KeyCode.Pause:
                            break;
                        case KeyCode.Escape:
                            break;
                        case KeyCode.Space:
                            break;
                        case KeyCode.Keypad0:
                            break;
                        case KeyCode.Keypad1:
                            break;
                        case KeyCode.Keypad2:
                            break;
                        case KeyCode.Keypad3:
                            break;
                        case KeyCode.Keypad4:
                            break;
                        case KeyCode.Keypad5:
                            break;
                        case KeyCode.Keypad6:
                            break;
                        case KeyCode.Keypad7:
                            break;
                        case KeyCode.Keypad8:
                            break;
                        case KeyCode.Keypad9:
                            break;
                        case KeyCode.KeypadPeriod:
                            break;
                        case KeyCode.KeypadDivide:
                            break;
                        case KeyCode.KeypadMultiply:
                            break;
                        case KeyCode.KeypadMinus:
                            break;
                        case KeyCode.KeypadPlus:
                            break;
                        case KeyCode.KeypadEquals:
                            break;
                        case KeyCode.LeftArrow:
                            break;
                        case KeyCode.Insert:
                            break;
                        case KeyCode.Home:
                            break;
                        case KeyCode.End:
                            break;
                        case KeyCode.F1:
                            break;
                        case KeyCode.F2:
                            break;
                        case KeyCode.F3:
                            break;
                        case KeyCode.F4:
                            break;
                        case KeyCode.F5:
                            break;
                        case KeyCode.F6:
                            break;
                        case KeyCode.F7:
                            break;
                        case KeyCode.F8:
                            break;
                        case KeyCode.F9:
                            break;
                        case KeyCode.F10:
                            break;
                        case KeyCode.F11:
                            break;
                        case KeyCode.F12:
                            break;
                        case KeyCode.F13:
                            break;
                        case KeyCode.F14:
                            break;
                        case KeyCode.F15:
                            break;
                        case KeyCode.Alpha0:
                            break;
                        case KeyCode.Alpha1:
                            break;
                        case KeyCode.Alpha2:
                            break;
                        case KeyCode.Alpha3:
                            break;
                        case KeyCode.Alpha4:
                            break;
                        case KeyCode.Alpha5:
                            break;
                        case KeyCode.Alpha6:
                            break;
                        case KeyCode.Alpha7:
                            break;
                        case KeyCode.Alpha8:
                            break;
                        case KeyCode.Alpha9:
                            break;
                        case KeyCode.Exclaim:
                            break;
                        case KeyCode.DoubleQuote:
                            break;
                        case KeyCode.Hash:
                            break;
                        case KeyCode.Dollar:
                            break;
                        case KeyCode.Percent:
                            break;
                        case KeyCode.Ampersand:
                            break;
                        case KeyCode.Quote:
                            break;
                        case KeyCode.LeftParen:
                            break;
                        case KeyCode.RightParen:
                            break;
                        case KeyCode.Asterisk:
                            break;
                        case KeyCode.Plus:
                            break;
                        case KeyCode.Comma:
                            break;
                        case KeyCode.Minus:
                            break;
                        case KeyCode.Period:
                            break;
                        case KeyCode.Slash:
                            break;
                        case KeyCode.Colon:
                            break;
                        case KeyCode.Semicolon:
                            break;
                        case KeyCode.Less:
                            break;
                        case KeyCode.Equals:
                            break;
                        case KeyCode.Greater:
                            break;
                        case KeyCode.Question:
                            break;
                        case KeyCode.At:
                            break;
                        case KeyCode.LeftBracket:
                            break;
                        case KeyCode.Backslash:
                            break;
                        case KeyCode.RightBracket:
                            break;
                        case KeyCode.Caret:
                            break;
                        case KeyCode.Underscore:
                            break;
                        case KeyCode.BackQuote:
                            break;
                        case KeyCode.A:
                            break;
                        case KeyCode.B:
                            break;
                        case KeyCode.C:
                            break;
                        case KeyCode.D:
                            break;
                        case KeyCode.E:
                            break;
                        case KeyCode.F:
                            break;
                        case KeyCode.G:
                            break;
                        case KeyCode.H:
                            break;
                        case KeyCode.I:
                            break;
                        case KeyCode.J:
                            break;
                        case KeyCode.K:
                            break;
                        case KeyCode.L:
                            break;
                        case KeyCode.M:
                            break;
                        case KeyCode.N:
                            break;
                        case KeyCode.O:
                            break;
                        case KeyCode.P:
                            break;
                        case KeyCode.Q:
                            break;
                        case KeyCode.R:
                            break;
                        case KeyCode.S:
                            break;
                        case KeyCode.T:
                            break;
                        case KeyCode.U:
                            break;
                        case KeyCode.V:
                            break;
                        case KeyCode.W:
                            break;
                        case KeyCode.X:
                            break;
                        case KeyCode.Y:
                            break;
                        case KeyCode.Z:
                            break;
                        case KeyCode.LeftCurlyBracket:
                            break;
                        case KeyCode.Pipe:
                            break;
                        case KeyCode.RightCurlyBracket:
                            break;
                        case KeyCode.Tilde:
                            break;
                        case KeyCode.Numlock:
                            break;
                        case KeyCode.CapsLock:
                            break;
                        case KeyCode.ScrollLock:
                            break;
                        case KeyCode.RightShift:
                            break;
                        case KeyCode.LeftShift:
                            break;
                        case KeyCode.RightControl:
                            break;
                        case KeyCode.LeftControl:
                            break;
                        case KeyCode.RightAlt:
                            break;
                        case KeyCode.LeftAlt:
                            break;
                        case KeyCode.LeftCommand:
                            break;
                        case KeyCode.LeftWindows:
                            break;
                        case KeyCode.RightCommand:
                            break;
                        case KeyCode.RightWindows:
                            break;
                        case KeyCode.AltGr:
                            break;
                        case KeyCode.Help:
                            break;
                        case KeyCode.Print:
                            break;
                        case KeyCode.SysReq:
                            break;
                        case KeyCode.Break:
                            break;
                        case KeyCode.Menu:
                            break;
                        case KeyCode.Mouse0:
                            break;
                        case KeyCode.Mouse1:
                            break;
                        case KeyCode.Mouse2:
                            break;
                        case KeyCode.Mouse3:
                            break;
                        case KeyCode.Mouse4:
                            break;
                        case KeyCode.Mouse5:
                            break;
                        case KeyCode.Mouse6:
                            break;
                        case KeyCode.JoystickButton0:
                            break;
                        case KeyCode.JoystickButton1:
                            break;
                        case KeyCode.JoystickButton2:
                            break;
                        case KeyCode.JoystickButton3:
                            break;
                        case KeyCode.JoystickButton4:
                            break;
                        case KeyCode.JoystickButton5:
                            break;
                        case KeyCode.JoystickButton6:
                            break;
                        case KeyCode.JoystickButton7:
                            break;
                        case KeyCode.JoystickButton8:
                            break;
                        case KeyCode.JoystickButton9:
                            break;
                        case KeyCode.JoystickButton10:
                            break;
                        case KeyCode.JoystickButton11:
                            break;
                        case KeyCode.JoystickButton12:
                            break;
                        case KeyCode.JoystickButton13:
                            break;
                        case KeyCode.JoystickButton14:
                            break;
                        case KeyCode.JoystickButton15:
                            break;
                        case KeyCode.JoystickButton16:
                            break;
                        case KeyCode.JoystickButton17:
                            break;
                        case KeyCode.JoystickButton18:
                            break;
                        case KeyCode.JoystickButton19:
                            break;
                        case KeyCode.Joystick1Button0:
                            break;
                        case KeyCode.Joystick1Button1:
                            break;
                        case KeyCode.Joystick1Button2:
                            break;
                        case KeyCode.Joystick1Button3:
                            break;
                        case KeyCode.Joystick1Button4:
                            break;
                        case KeyCode.Joystick1Button5:
                            break;
                        case KeyCode.Joystick1Button6:
                            break;
                        case KeyCode.Joystick1Button7:
                            break;
                        case KeyCode.Joystick1Button8:
                            break;
                        case KeyCode.Joystick1Button9:
                            break;
                        case KeyCode.Joystick1Button10:
                            break;
                        case KeyCode.Joystick1Button11:
                            break;
                        case KeyCode.Joystick1Button12:
                            break;
                        case KeyCode.Joystick1Button13:
                            break;
                        case KeyCode.Joystick1Button14:
                            break;
                        case KeyCode.Joystick1Button15:
                            break;
                        case KeyCode.Joystick1Button16:
                            break;
                        case KeyCode.Joystick1Button17:
                            break;
                        case KeyCode.Joystick1Button18:
                            break;
                        case KeyCode.Joystick1Button19:
                            break;
                        case KeyCode.Joystick2Button0:
                            break;
                        case KeyCode.Joystick2Button1:
                            break;
                        case KeyCode.Joystick2Button2:
                            break;
                        case KeyCode.Joystick2Button3:
                            break;
                        case KeyCode.Joystick2Button4:
                            break;
                        case KeyCode.Joystick2Button5:
                            break;
                        case KeyCode.Joystick2Button6:
                            break;
                        case KeyCode.Joystick2Button7:
                            break;
                        case KeyCode.Joystick2Button8:
                            break;
                        case KeyCode.Joystick2Button9:
                            break;
                        case KeyCode.Joystick2Button10:
                            break;
                        case KeyCode.Joystick2Button11:
                            break;
                        case KeyCode.Joystick2Button12:
                            break;
                        case KeyCode.Joystick2Button13:
                            break;
                        case KeyCode.Joystick2Button14:
                            break;
                        case KeyCode.Joystick2Button15:
                            break;
                        case KeyCode.Joystick2Button16:
                            break;
                        case KeyCode.Joystick2Button17:
                            break;
                        case KeyCode.Joystick2Button18:
                            break;
                        case KeyCode.Joystick2Button19:
                            break;
                        case KeyCode.Joystick3Button0:
                            break;
                        case KeyCode.Joystick3Button1:
                            break;
                        case KeyCode.Joystick3Button2:
                            break;
                        case KeyCode.Joystick3Button3:
                            break;
                        case KeyCode.Joystick3Button4:
                            break;
                        case KeyCode.Joystick3Button5:
                            break;
                        case KeyCode.Joystick3Button6:
                            break;
                        case KeyCode.Joystick3Button7:
                            break;
                        case KeyCode.Joystick3Button8:
                            break;
                        case KeyCode.Joystick3Button9:
                            break;
                        case KeyCode.Joystick3Button10:
                            break;
                        case KeyCode.Joystick3Button11:
                            break;
                        case KeyCode.Joystick3Button12:
                            break;
                        case KeyCode.Joystick3Button13:
                            break;
                        case KeyCode.Joystick3Button14:
                            break;
                        case KeyCode.Joystick3Button15:
                            break;
                        case KeyCode.Joystick3Button16:
                            break;
                        case KeyCode.Joystick3Button17:
                            break;
                        case KeyCode.Joystick3Button18:
                            break;
                        case KeyCode.Joystick3Button19:
                            break;
                        case KeyCode.Joystick4Button0:
                            break;
                        case KeyCode.Joystick4Button1:
                            break;
                        case KeyCode.Joystick4Button2:
                            break;
                        case KeyCode.Joystick4Button3:
                            break;
                        case KeyCode.Joystick4Button4:
                            break;
                        case KeyCode.Joystick4Button5:
                            break;
                        case KeyCode.Joystick4Button6:
                            break;
                        case KeyCode.Joystick4Button7:
                            break;
                        case KeyCode.Joystick4Button8:
                            break;
                        case KeyCode.Joystick4Button9:
                            break;
                        case KeyCode.Joystick4Button10:
                            break;
                        case KeyCode.Joystick4Button11:
                            break;
                        case KeyCode.Joystick4Button12:
                            break;
                        case KeyCode.Joystick4Button13:
                            break;
                        case KeyCode.Joystick4Button14:
                            break;
                        case KeyCode.Joystick4Button15:
                            break;
                        case KeyCode.Joystick4Button16:
                            break;
                        case KeyCode.Joystick4Button17:
                            break;
                        case KeyCode.Joystick4Button18:
                            break;
                        case KeyCode.Joystick4Button19:
                            break;
                        case KeyCode.Joystick5Button0:
                            break;
                        case KeyCode.Joystick5Button1:
                            break;
                        case KeyCode.Joystick5Button2:
                            break;
                        case KeyCode.Joystick5Button3:
                            break;
                        case KeyCode.Joystick5Button4:
                            break;
                        case KeyCode.Joystick5Button5:
                            break;
                        case KeyCode.Joystick5Button6:
                            break;
                        case KeyCode.Joystick5Button7:
                            break;
                        case KeyCode.Joystick5Button8:
                            break;
                        case KeyCode.Joystick5Button9:
                            break;
                        case KeyCode.Joystick5Button10:
                            break;
                        case KeyCode.Joystick5Button11:
                            break;
                        case KeyCode.Joystick5Button12:
                            break;
                        case KeyCode.Joystick5Button13:
                            break;
                        case KeyCode.Joystick5Button14:
                            break;
                        case KeyCode.Joystick5Button15:
                            break;
                        case KeyCode.Joystick5Button16:
                            break;
                        case KeyCode.Joystick5Button17:
                            break;
                        case KeyCode.Joystick5Button18:
                            break;
                        case KeyCode.Joystick5Button19:
                            break;
                        case KeyCode.Joystick6Button0:
                            break;
                        case KeyCode.Joystick6Button1:
                            break;
                        case KeyCode.Joystick6Button2:
                            break;
                        case KeyCode.Joystick6Button3:
                            break;
                        case KeyCode.Joystick6Button4:
                            break;
                        case KeyCode.Joystick6Button5:
                            break;
                        case KeyCode.Joystick6Button6:
                            break;
                        case KeyCode.Joystick6Button7:
                            break;
                        case KeyCode.Joystick6Button8:
                            break;
                        case KeyCode.Joystick6Button9:
                            break;
                        case KeyCode.Joystick6Button10:
                            break;
                        case KeyCode.Joystick6Button11:
                            break;
                        case KeyCode.Joystick6Button12:
                            break;
                        case KeyCode.Joystick6Button13:
                            break;
                        case KeyCode.Joystick6Button14:
                            break;
                        case KeyCode.Joystick6Button15:
                            break;
                        case KeyCode.Joystick6Button16:
                            break;
                        case KeyCode.Joystick6Button17:
                            break;
                        case KeyCode.Joystick6Button18:
                            break;
                        case KeyCode.Joystick6Button19:
                            break;
                        case KeyCode.Joystick7Button0:
                            break;
                        case KeyCode.Joystick7Button1:
                            break;
                        case KeyCode.Joystick7Button2:
                            break;
                        case KeyCode.Joystick7Button3:
                            break;
                        case KeyCode.Joystick7Button4:
                            break;
                        case KeyCode.Joystick7Button5:
                            break;
                        case KeyCode.Joystick7Button6:
                            break;
                        case KeyCode.Joystick7Button7:
                            break;
                        case KeyCode.Joystick7Button8:
                            break;
                        case KeyCode.Joystick7Button9:
                            break;
                        case KeyCode.Joystick7Button10:
                            break;
                        case KeyCode.Joystick7Button11:
                            break;
                        case KeyCode.Joystick7Button12:
                            break;
                        case KeyCode.Joystick7Button13:
                            break;
                        case KeyCode.Joystick7Button14:
                            break;
                        case KeyCode.Joystick7Button15:
                            break;
                        case KeyCode.Joystick7Button16:
                            break;
                        case KeyCode.Joystick7Button17:
                            break;
                        case KeyCode.Joystick7Button18:
                            break;
                        case KeyCode.Joystick7Button19:
                            break;
                        case KeyCode.Joystick8Button0:
                            break;
                        case KeyCode.Joystick8Button1:
                            break;
                        case KeyCode.Joystick8Button2:
                            break;
                        case KeyCode.Joystick8Button3:
                            break;
                        case KeyCode.Joystick8Button4:
                            break;
                        case KeyCode.Joystick8Button5:
                            break;
                        case KeyCode.Joystick8Button6:
                            break;
                        case KeyCode.Joystick8Button7:
                            break;
                        case KeyCode.Joystick8Button8:
                            break;
                        case KeyCode.Joystick8Button9:
                            break;
                        case KeyCode.Joystick8Button10:
                            break;
                        case KeyCode.Joystick8Button11:
                            break;
                        case KeyCode.Joystick8Button12:
                            break;
                        case KeyCode.Joystick8Button13:
                            break;
                        case KeyCode.Joystick8Button14:
                            break;
                        case KeyCode.Joystick8Button15:
                            break;
                        case KeyCode.Joystick8Button16:
                            break;
                        case KeyCode.Joystick8Button17:
                            break;
                        case KeyCode.Joystick8Button18:
                            break;
                        case KeyCode.Joystick8Button19:
                            break;
                        default:
                            break;
                    }
                    return;
                }


                if (_focusArea == FocusArea.Active) {
                    switch (key) {
                        case KeyCode.UpArrow:
                            if (Event.current.shift) {
                                MoveUp();
                            } else {
                                SelectPrevious(FilteredActiveButtons);
                            }

                            break;
                        case KeyCode.DownArrow:
                            if (shift) {
                                MoveDown();
                            } else {
                                SelectNext(FilteredActiveButtons);
                            }

                            break;
                        case KeyCode.PageUp:
                            if (shift) {
                                MoveTop();
                            } else {
                                SelectFirst(FilteredActiveButtons);
                            }

                            break;
                        case KeyCode.PageDown:
                            if (shift) {
                                MoveBottom();
                            } else {
                                SelectLast(FilteredActiveButtons);
                            }

                            break;
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                        case KeyCode.Delete:
                            Selected.Active = false;
                            if (FilteredActiveButtons.Any()) {
                                int index = Math.Min( _focusElement, FilteredActiveButtons.Count - 1 );
                                Selected = FilteredActiveButtons.ElementAt(index);
                            } else {
                                Selected = Selected;
                            }

                            break;
                        case KeyCode.LeftArrow:
                            if (shift) {
                                Selected.Active = false;
                                Selected = Selected; // sets _focusArea, _focusElement, plays sound.
                            } else {
                                SelectAt(FilteredAvailableButtons, _activeScrollPosition, _availableScrollPosition);
                            }
                            break;
                        case KeyCode.None:
                            break;
                        case KeyCode.Backspace:
                            break;
                        case KeyCode.Tab:
                            break;
                        case KeyCode.Clear:
                            break;
                        case KeyCode.Pause:
                            break;
                        case KeyCode.Escape:
                            break;
                        case KeyCode.Space:
                            break;
                        case KeyCode.Keypad0:
                            break;
                        case KeyCode.Keypad1:
                            break;
                        case KeyCode.Keypad2:
                            break;
                        case KeyCode.Keypad3:
                            break;
                        case KeyCode.Keypad4:
                            break;
                        case KeyCode.Keypad5:
                            break;
                        case KeyCode.Keypad6:
                            break;
                        case KeyCode.Keypad7:
                            break;
                        case KeyCode.Keypad8:
                            break;
                        case KeyCode.Keypad9:
                            break;
                        case KeyCode.KeypadPeriod:
                            break;
                        case KeyCode.KeypadDivide:
                            break;
                        case KeyCode.KeypadMultiply:
                            break;
                        case KeyCode.KeypadMinus:
                            break;
                        case KeyCode.KeypadPlus:
                            break;
                        case KeyCode.KeypadEquals:
                            break;
                        case KeyCode.RightArrow:
                            break;
                        case KeyCode.Insert:
                            break;
                        case KeyCode.Home:
                            break;
                        case KeyCode.End:
                            break;
                        case KeyCode.F1:
                            break;
                        case KeyCode.F2:
                            break;
                        case KeyCode.F3:
                            break;
                        case KeyCode.F4:
                            break;
                        case KeyCode.F5:
                            break;
                        case KeyCode.F6:
                            break;
                        case KeyCode.F7:
                            break;
                        case KeyCode.F8:
                            break;
                        case KeyCode.F9:
                            break;
                        case KeyCode.F10:
                            break;
                        case KeyCode.F11:
                            break;
                        case KeyCode.F12:
                            break;
                        case KeyCode.F13:
                            break;
                        case KeyCode.F14:
                            break;
                        case KeyCode.F15:
                            break;
                        case KeyCode.Alpha0:
                            break;
                        case KeyCode.Alpha1:
                            break;
                        case KeyCode.Alpha2:
                            break;
                        case KeyCode.Alpha3:
                            break;
                        case KeyCode.Alpha4:
                            break;
                        case KeyCode.Alpha5:
                            break;
                        case KeyCode.Alpha6:
                            break;
                        case KeyCode.Alpha7:
                            break;
                        case KeyCode.Alpha8:
                            break;
                        case KeyCode.Alpha9:
                            break;
                        case KeyCode.Exclaim:
                            break;
                        case KeyCode.DoubleQuote:
                            break;
                        case KeyCode.Hash:
                            break;
                        case KeyCode.Dollar:
                            break;
                        case KeyCode.Percent:
                            break;
                        case KeyCode.Ampersand:
                            break;
                        case KeyCode.Quote:
                            break;
                        case KeyCode.LeftParen:
                            break;
                        case KeyCode.RightParen:
                            break;
                        case KeyCode.Asterisk:
                            break;
                        case KeyCode.Plus:
                            break;
                        case KeyCode.Comma:
                            break;
                        case KeyCode.Minus:
                            break;
                        case KeyCode.Period:
                            break;
                        case KeyCode.Slash:
                            break;
                        case KeyCode.Colon:
                            break;
                        case KeyCode.Semicolon:
                            break;
                        case KeyCode.Less:
                            break;
                        case KeyCode.Equals:
                            break;
                        case KeyCode.Greater:
                            break;
                        case KeyCode.Question:
                            break;
                        case KeyCode.At:
                            break;
                        case KeyCode.LeftBracket:
                            break;
                        case KeyCode.Backslash:
                            break;
                        case KeyCode.RightBracket:
                            break;
                        case KeyCode.Caret:
                            break;
                        case KeyCode.Underscore:
                            break;
                        case KeyCode.BackQuote:
                            break;
                        case KeyCode.A:
                            break;
                        case KeyCode.B:
                            break;
                        case KeyCode.C:
                            break;
                        case KeyCode.D:
                            break;
                        case KeyCode.E:
                            break;
                        case KeyCode.F:
                            break;
                        case KeyCode.G:
                            break;
                        case KeyCode.H:
                            break;
                        case KeyCode.I:
                            break;
                        case KeyCode.J:
                            break;
                        case KeyCode.K:
                            break;
                        case KeyCode.L:
                            break;
                        case KeyCode.M:
                            break;
                        case KeyCode.N:
                            break;
                        case KeyCode.O:
                            break;
                        case KeyCode.P:
                            break;
                        case KeyCode.Q:
                            break;
                        case KeyCode.R:
                            break;
                        case KeyCode.S:
                            break;
                        case KeyCode.T:
                            break;
                        case KeyCode.U:
                            break;
                        case KeyCode.V:
                            break;
                        case KeyCode.W:
                            break;
                        case KeyCode.X:
                            break;
                        case KeyCode.Y:
                            break;
                        case KeyCode.Z:
                            break;
                        case KeyCode.LeftCurlyBracket:
                            break;
                        case KeyCode.Pipe:
                            break;
                        case KeyCode.RightCurlyBracket:
                            break;
                        case KeyCode.Tilde:
                            break;
                        case KeyCode.Numlock:
                            break;
                        case KeyCode.CapsLock:
                            break;
                        case KeyCode.ScrollLock:
                            break;
                        case KeyCode.RightShift:
                            break;
                        case KeyCode.LeftShift:
                            break;
                        case KeyCode.RightControl:
                            break;
                        case KeyCode.LeftControl:
                            break;
                        case KeyCode.RightAlt:
                            break;
                        case KeyCode.LeftAlt:
                            break;
                        case KeyCode.LeftCommand:
                            break;
                        case KeyCode.LeftWindows:
                            break;
                        case KeyCode.RightCommand:
                            break;
                        case KeyCode.RightWindows:
                            break;
                        case KeyCode.AltGr:
                            break;
                        case KeyCode.Help:
                            break;
                        case KeyCode.Print:
                            break;
                        case KeyCode.SysReq:
                            break;
                        case KeyCode.Break:
                            break;
                        case KeyCode.Menu:
                            break;
                        case KeyCode.Mouse0:
                            break;
                        case KeyCode.Mouse1:
                            break;
                        case KeyCode.Mouse2:
                            break;
                        case KeyCode.Mouse3:
                            break;
                        case KeyCode.Mouse4:
                            break;
                        case KeyCode.Mouse5:
                            break;
                        case KeyCode.Mouse6:
                            break;
                        case KeyCode.JoystickButton0:
                            break;
                        case KeyCode.JoystickButton1:
                            break;
                        case KeyCode.JoystickButton2:
                            break;
                        case KeyCode.JoystickButton3:
                            break;
                        case KeyCode.JoystickButton4:
                            break;
                        case KeyCode.JoystickButton5:
                            break;
                        case KeyCode.JoystickButton6:
                            break;
                        case KeyCode.JoystickButton7:
                            break;
                        case KeyCode.JoystickButton8:
                            break;
                        case KeyCode.JoystickButton9:
                            break;
                        case KeyCode.JoystickButton10:
                            break;
                        case KeyCode.JoystickButton11:
                            break;
                        case KeyCode.JoystickButton12:
                            break;
                        case KeyCode.JoystickButton13:
                            break;
                        case KeyCode.JoystickButton14:
                            break;
                        case KeyCode.JoystickButton15:
                            break;
                        case KeyCode.JoystickButton16:
                            break;
                        case KeyCode.JoystickButton17:
                            break;
                        case KeyCode.JoystickButton18:
                            break;
                        case KeyCode.JoystickButton19:
                            break;
                        case KeyCode.Joystick1Button0:
                            break;
                        case KeyCode.Joystick1Button1:
                            break;
                        case KeyCode.Joystick1Button2:
                            break;
                        case KeyCode.Joystick1Button3:
                            break;
                        case KeyCode.Joystick1Button4:
                            break;
                        case KeyCode.Joystick1Button5:
                            break;
                        case KeyCode.Joystick1Button6:
                            break;
                        case KeyCode.Joystick1Button7:
                            break;
                        case KeyCode.Joystick1Button8:
                            break;
                        case KeyCode.Joystick1Button9:
                            break;
                        case KeyCode.Joystick1Button10:
                            break;
                        case KeyCode.Joystick1Button11:
                            break;
                        case KeyCode.Joystick1Button12:
                            break;
                        case KeyCode.Joystick1Button13:
                            break;
                        case KeyCode.Joystick1Button14:
                            break;
                        case KeyCode.Joystick1Button15:
                            break;
                        case KeyCode.Joystick1Button16:
                            break;
                        case KeyCode.Joystick1Button17:
                            break;
                        case KeyCode.Joystick1Button18:
                            break;
                        case KeyCode.Joystick1Button19:
                            break;
                        case KeyCode.Joystick2Button0:
                            break;
                        case KeyCode.Joystick2Button1:
                            break;
                        case KeyCode.Joystick2Button2:
                            break;
                        case KeyCode.Joystick2Button3:
                            break;
                        case KeyCode.Joystick2Button4:
                            break;
                        case KeyCode.Joystick2Button5:
                            break;
                        case KeyCode.Joystick2Button6:
                            break;
                        case KeyCode.Joystick2Button7:
                            break;
                        case KeyCode.Joystick2Button8:
                            break;
                        case KeyCode.Joystick2Button9:
                            break;
                        case KeyCode.Joystick2Button10:
                            break;
                        case KeyCode.Joystick2Button11:
                            break;
                        case KeyCode.Joystick2Button12:
                            break;
                        case KeyCode.Joystick2Button13:
                            break;
                        case KeyCode.Joystick2Button14:
                            break;
                        case KeyCode.Joystick2Button15:
                            break;
                        case KeyCode.Joystick2Button16:
                            break;
                        case KeyCode.Joystick2Button17:
                            break;
                        case KeyCode.Joystick2Button18:
                            break;
                        case KeyCode.Joystick2Button19:
                            break;
                        case KeyCode.Joystick3Button0:
                            break;
                        case KeyCode.Joystick3Button1:
                            break;
                        case KeyCode.Joystick3Button2:
                            break;
                        case KeyCode.Joystick3Button3:
                            break;
                        case KeyCode.Joystick3Button4:
                            break;
                        case KeyCode.Joystick3Button5:
                            break;
                        case KeyCode.Joystick3Button6:
                            break;
                        case KeyCode.Joystick3Button7:
                            break;
                        case KeyCode.Joystick3Button8:
                            break;
                        case KeyCode.Joystick3Button9:
                            break;
                        case KeyCode.Joystick3Button10:
                            break;
                        case KeyCode.Joystick3Button11:
                            break;
                        case KeyCode.Joystick3Button12:
                            break;
                        case KeyCode.Joystick3Button13:
                            break;
                        case KeyCode.Joystick3Button14:
                            break;
                        case KeyCode.Joystick3Button15:
                            break;
                        case KeyCode.Joystick3Button16:
                            break;
                        case KeyCode.Joystick3Button17:
                            break;
                        case KeyCode.Joystick3Button18:
                            break;
                        case KeyCode.Joystick3Button19:
                            break;
                        case KeyCode.Joystick4Button0:
                            break;
                        case KeyCode.Joystick4Button1:
                            break;
                        case KeyCode.Joystick4Button2:
                            break;
                        case KeyCode.Joystick4Button3:
                            break;
                        case KeyCode.Joystick4Button4:
                            break;
                        case KeyCode.Joystick4Button5:
                            break;
                        case KeyCode.Joystick4Button6:
                            break;
                        case KeyCode.Joystick4Button7:
                            break;
                        case KeyCode.Joystick4Button8:
                            break;
                        case KeyCode.Joystick4Button9:
                            break;
                        case KeyCode.Joystick4Button10:
                            break;
                        case KeyCode.Joystick4Button11:
                            break;
                        case KeyCode.Joystick4Button12:
                            break;
                        case KeyCode.Joystick4Button13:
                            break;
                        case KeyCode.Joystick4Button14:
                            break;
                        case KeyCode.Joystick4Button15:
                            break;
                        case KeyCode.Joystick4Button16:
                            break;
                        case KeyCode.Joystick4Button17:
                            break;
                        case KeyCode.Joystick4Button18:
                            break;
                        case KeyCode.Joystick4Button19:
                            break;
                        case KeyCode.Joystick5Button0:
                            break;
                        case KeyCode.Joystick5Button1:
                            break;
                        case KeyCode.Joystick5Button2:
                            break;
                        case KeyCode.Joystick5Button3:
                            break;
                        case KeyCode.Joystick5Button4:
                            break;
                        case KeyCode.Joystick5Button5:
                            break;
                        case KeyCode.Joystick5Button6:
                            break;
                        case KeyCode.Joystick5Button7:
                            break;
                        case KeyCode.Joystick5Button8:
                            break;
                        case KeyCode.Joystick5Button9:
                            break;
                        case KeyCode.Joystick5Button10:
                            break;
                        case KeyCode.Joystick5Button11:
                            break;
                        case KeyCode.Joystick5Button12:
                            break;
                        case KeyCode.Joystick5Button13:
                            break;
                        case KeyCode.Joystick5Button14:
                            break;
                        case KeyCode.Joystick5Button15:
                            break;
                        case KeyCode.Joystick5Button16:
                            break;
                        case KeyCode.Joystick5Button17:
                            break;
                        case KeyCode.Joystick5Button18:
                            break;
                        case KeyCode.Joystick5Button19:
                            break;
                        case KeyCode.Joystick6Button0:
                            break;
                        case KeyCode.Joystick6Button1:
                            break;
                        case KeyCode.Joystick6Button2:
                            break;
                        case KeyCode.Joystick6Button3:
                            break;
                        case KeyCode.Joystick6Button4:
                            break;
                        case KeyCode.Joystick6Button5:
                            break;
                        case KeyCode.Joystick6Button6:
                            break;
                        case KeyCode.Joystick6Button7:
                            break;
                        case KeyCode.Joystick6Button8:
                            break;
                        case KeyCode.Joystick6Button9:
                            break;
                        case KeyCode.Joystick6Button10:
                            break;
                        case KeyCode.Joystick6Button11:
                            break;
                        case KeyCode.Joystick6Button12:
                            break;
                        case KeyCode.Joystick6Button13:
                            break;
                        case KeyCode.Joystick6Button14:
                            break;
                        case KeyCode.Joystick6Button15:
                            break;
                        case KeyCode.Joystick6Button16:
                            break;
                        case KeyCode.Joystick6Button17:
                            break;
                        case KeyCode.Joystick6Button18:
                            break;
                        case KeyCode.Joystick6Button19:
                            break;
                        case KeyCode.Joystick7Button0:
                            break;
                        case KeyCode.Joystick7Button1:
                            break;
                        case KeyCode.Joystick7Button2:
                            break;
                        case KeyCode.Joystick7Button3:
                            break;
                        case KeyCode.Joystick7Button4:
                            break;
                        case KeyCode.Joystick7Button5:
                            break;
                        case KeyCode.Joystick7Button6:
                            break;
                        case KeyCode.Joystick7Button7:
                            break;
                        case KeyCode.Joystick7Button8:
                            break;
                        case KeyCode.Joystick7Button9:
                            break;
                        case KeyCode.Joystick7Button10:
                            break;
                        case KeyCode.Joystick7Button11:
                            break;
                        case KeyCode.Joystick7Button12:
                            break;
                        case KeyCode.Joystick7Button13:
                            break;
                        case KeyCode.Joystick7Button14:
                            break;
                        case KeyCode.Joystick7Button15:
                            break;
                        case KeyCode.Joystick7Button16:
                            break;
                        case KeyCode.Joystick7Button17:
                            break;
                        case KeyCode.Joystick7Button18:
                            break;
                        case KeyCode.Joystick7Button19:
                            break;
                        case KeyCode.Joystick8Button0:
                            break;
                        case KeyCode.Joystick8Button1:
                            break;
                        case KeyCode.Joystick8Button2:
                            break;
                        case KeyCode.Joystick8Button3:
                            break;
                        case KeyCode.Joystick8Button4:
                            break;
                        case KeyCode.Joystick8Button5:
                            break;
                        case KeyCode.Joystick8Button6:
                            break;
                        case KeyCode.Joystick8Button7:
                            break;
                        case KeyCode.Joystick8Button8:
                            break;
                        case KeyCode.Joystick8Button9:
                            break;
                        case KeyCode.Joystick8Button10:
                            break;
                        case KeyCode.Joystick8Button11:
                            break;
                        case KeyCode.Joystick8Button12:
                            break;
                        case KeyCode.Joystick8Button13:
                            break;
                        case KeyCode.Joystick8Button14:
                            break;
                        case KeyCode.Joystick8Button15:
                            break;
                        case KeyCode.Joystick8Button16:
                            break;
                        case KeyCode.Joystick8Button17:
                            break;
                        case KeyCode.Joystick8Button18:
                            break;
                        case KeyCode.Joystick8Button19:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void MoveUp() {
            if (_focusElement <= 0) {
                return;
            }

            // is only called from active.
            ModButtonManager.Insert(Selected, ModButtonManager.ActiveButtons.IndexOf(FilteredActiveButtons.ElementAt(_focusElement - 1)));
            Selected = Selected; // sets _focusElement, plays sound.
        }

        private void MoveTop() {
            ModButtonManager.Insert(Selected, 0);
            Selected = Selected;
        }

        private void MoveBottom() {
            ModButtonManager.Insert(Selected, ModButtonManager.ActiveButtons.Count);
            Selected = Selected;
        }

        private void MoveDown() {
            if (_focusElement >= FilteredActiveButtons.Count - 1) {
                return;
            }

            // is only called from active.
            ModButtonManager.Insert(Selected, ModButtonManager.ActiveButtons.IndexOf(FilteredActiveButtons.ElementAt(_focusElement + 1)) + 1);
            Selected = Selected; // sets _focusElement, plays sound.
        }

        private void SelectAt<T>(
            IEnumerable<T> targetMods,
            Vector2 sourceScrollposition,
            Vector2 targetScrollposition) where T : ModButton {
            float offset = (_focusElement * ModButtonHeight) - sourceScrollposition.y;
            SelectAt(targetMods, targetScrollposition, offset);
        }

        private void SelectAt<T>(IEnumerable<T> mods, Vector2 scrollposition, float offset = 0f) where T : ModButton {
            SelectAt(mods, scrollposition.y + offset);
        }

        private void SelectAt<T>(IEnumerable<T> mods, float position) where T : ModButton {
            Selected = mods.Any() ? mods.ElementAt(IndexAt(mods, position)) : null;
        }

        private int IndexAt<T>(IEnumerable<T> mods, Vector2 scrollposition, float offset = 0f) where T : ModButton {
            return IndexAt(mods, scrollposition.y + offset);
        }

        private int IndexAt<T>(IEnumerable<T> mods, float position) where T : ModButton {
            if (!mods.Any()) {
                return -1;
            }

            return Mathf.Clamp(Mathf.CeilToInt(position / ModButtonHeight), 0, mods.Count() - 1);
        }

        private void SelectNext<T>(IEnumerable<T> mods) where T : ModButton {
            if (!mods.Any()) {
                return;
            }

            int index = Utilities.Modulo( _focusElement + 1, mods.Count() );
            Selected = mods.ElementAt(index);
        }

        private void SelectFirst<T>(IEnumerable<T> mods) where T : ModButton {
            if (!mods.Any()) {
                return;
            }

            Selected = mods.ElementAt(0);
        }

        private void SelectPrevious<T>(IEnumerable<T> mods) where T : ModButton {
            if (!mods.Any()) {
                return;
            }

            int index = Utilities.Modulo( _focusElement - 1, mods.Count() );
            Selected = mods.ElementAt(index);
        }

        private void SelectLast<T>(IEnumerable<T> mods) where T : ModButton {
            if (!mods.Any()) {
                return;
            }

            int index = mods.Count() - 1;
            Selected = mods.ElementAt(index);
        }

        private void EnsureVisible(ref Vector2 scrollPosition, int index) {
            int min = index * ModButtonHeight;
            int max = ( index + 1 ) * ModButtonHeight;

            if (min < scrollPosition.y) {
                scrollPosition.y = min;
            }
            if (max > scrollPosition.y + _scrollViewHeight) {
                scrollPosition.y = max - _scrollViewHeight + ModButtonHeight;
            }
        }

        public List<ModButton> FilteredAvailableButtons {
            get {
                if (FilterAvailable) {
                    return ModButtonManager.AvailableButtons
                        .Where(b => b.MatchesFilter(_availableFilter) > 0)
                        .OrderBy(b => b.MatchesFilter(_availableFilter))
                        .ToList();
                }

                return ModButtonManager.AvailableButtons;
            }
        }

        public void DoAvailableMods(Rect canvas) {
            Utilities.DoLabel(ref canvas, I18n.AvailableMods);
            Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);

            List<ModButton> buttons = new List<ModButton>( FilteredAvailableButtons );
            Rect filterRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                FilterHeight );
            Rect outRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                canvas.height - FilterHeight - SmallMargin );
            Rect viewRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                Mathf.Max( ModButtonHeight * buttons.Count, outRect.height ) );
            if (viewRect.height > outRect.height) {
                viewRect.width -= 18f;
            }

            Rect modRect = new Rect(
                viewRect.xMin,
                viewRect.yMin,
                viewRect.width,
                ModButtonHeight );
            _scrollViewHeight = (int) outRect.height;

            DoFilterField(filterRect, ref _availableFilter, ref _availableFilterVisible, FocusArea.AvailableFilter);

            bool alternate = false;

            Widgets.BeginScrollView(outRect, ref _availableScrollPosition, viewRect);
            foreach (ModButton button in buttons) {
                button.DoModButton(modRect, alternate, () => Selected = button, () => button.Active = true, _availableFilterVisible, _availableFilter);
                alternate = !alternate;
                modRect.y += ModButtonHeight;
            }

            // handle drag & drop
            bool dropped = DraggingManager.ContainerUpdate( buttons, viewRect, out int hoverIndex );
            bool draggingOverAvailable = hoverIndex >= 0;
            if (draggingOverAvailable != _draggingOverAvailable) {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                _draggingOverAvailable = draggingOverAvailable;
            }
            if (dropped) {
                DraggingManager.Dragged.Active = false;
            }
            Widgets.EndScrollView();

            if (draggingOverAvailable) {
                GUI.color = Color.grey;
                Widgets.DrawBox(outRect);
                GUI.color = Color.white;
            }
        }

        private bool _draggingOverAvailable = false;
        private int _lastHoverIndex;
        public List<ModButton> FilteredActiveButtons => ModButtonManager.ActiveButtons
                        .Where(b => !FilterActive || b.MatchesFilter(_activeFilter) > 0)
                        .ToList();

        public void DoActiveMods(Rect canvas) {
            Utilities.DoLabel(ref canvas, I18n.ActiveMods);
            Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);

            List<ModButton> buttons = FilteredActiveButtons;
            Rect filterRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                FilterHeight );
            Rect outRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                canvas.height - FilterHeight - SmallMargin );
            Rect viewRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                Mathf.Max( ModButtonHeight * buttons.Count(), outRect.height ) );
            if (viewRect.height > outRect.height) {
                viewRect.width -= 18f;
            }

            Rect modRect = new Rect(
                viewRect.xMin,
                viewRect.yMin,
                viewRect.width,
                ModButtonHeight);

            DoFilterField(filterRect, ref _activeFilter, ref _activeFilterVisible, FocusArea.ActiveFilter);

            bool alternate = false;

            Widgets.BeginScrollView(outRect, ref _activeScrollPosition, viewRect);
            if (DraggingManager.ContainerUpdate(buttons, viewRect, out int hoverIndex)) {
                int dropIndex = hoverIndex;
                // if filtering the active list, figure out the desired index in the source list
                if (FilterActive && hoverIndex > 0) {
                    ModButton insertBefore = buttons.ElementAtOrDefault( hoverIndex );
                    dropIndex = insertBefore == null ? ModButtonManager.ActiveButtons.Count : ModButtonManager.ActiveButtons.IndexOf(insertBefore);

                }
                ModButtonManager.Insert(DraggingManager.Dragged, dropIndex);
            }
            if (hoverIndex != _lastHoverIndex) {
                _lastHoverIndex = hoverIndex;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            for (int i = 0; i < buttons.Count; i++) {
                ModButton mod = buttons.ElementAt(i);

                mod.DoModButton(modRect, alternate, () => Selected = mod, () => mod.Active = false, _activeFilterVisible, _activeFilter);
                alternate = !alternate;

                if (hoverIndex == i) {
                    GUI.color = Color.grey;
                    Widgets.DrawLineHorizontal(modRect.xMin, modRect.yMin, modRect.width);
                    GUI.color = Color.white;
                }

                modRect.y += ModButtonHeight;
            }
            if (hoverIndex == buttons.Count()) {
                GUI.color = Color.grey;
                Widgets.DrawLineHorizontal(modRect.xMin, modRect.yMin, modRect.width);
                GUI.color = Color.white;
            }
            Widgets.EndScrollView();
        }

        public void DoDetails(Rect canvas) {
            if (Selected == null) {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;
                Widgets.Label(canvas, I18n.NoModSelected);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            } else {
                Selected.DoModDetails(canvas);
            }
        }

        public void DoFilterField(Rect canvas, ref string filter, ref bool visible, FocusArea focus) {
            Rect rect = canvas.ContractedBy( SmallMargin / 2f );
            Rect iconRect = new Rect(
                canvas.xMax - SmallIconSize - SmallMargin,
                canvas.yMin + ((canvas.height - SmallIconSize) / 2f),
                SmallIconSize,
                SmallIconSize
            );

            // intercept focus gain events
            if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseUp) {
                _focusArea = focus;
            }

            // handle button interactions before textfield, because textfield eats click events.
            if (!filter.NullOrEmpty()) {
                if (Widgets.ButtonInvisible(iconRect)) {
                    filter = string.Empty;
                }
                iconRect.x -= SmallIconSize + SmallMargin;
                if (Widgets.ButtonInvisible(iconRect)) {
                    visible = !visible;
                }
                iconRect.x += SmallIconSize + SmallMargin;
            } else {
                // Unity gets confused if the number of controls changes, and the textfield will
                // loose focus. To avoid this, spawn some dummy controls.
                Widgets.ButtonInvisible(Rect.zero);
                Widgets.ButtonInvisible(Rect.zero);
            }

            // handle textfield
            GUI.SetNextControlName(focus.ToString());
            string newFilter = Widgets.TextField( rect, filter );
            if (newFilter != filter) {
                filter = newFilter;
                Notify_FilterChanged();
            }

            // draw buttons over textfield.
            // Note that these buttons _cannot_ be clicked, but the ButtonImage
            // code is a handy shortcut for mouseOver interactions.
            if (!filter.NullOrEmpty()) {
                if (Widgets.ButtonImage(iconRect, Status_Cross)) {
                    filter = string.Empty;
                    Notify_FilterChanged();
                }
                iconRect.x -= SmallIconSize + SmallMargin;
                if (Widgets.ButtonImage(iconRect, visible ? EyeClosed : EyeOpen)) {
                    visible = !visible;
                    Notify_FilterChanged();
                }
            } else {
                // search icon, plus more dummy controls (filter is called multiple times).
                GUI.DrawTexture(iconRect, Search);
                Widgets.ButtonInvisible(Rect.zero);
                Widgets.ButtonInvisible(Rect.zero);
            }
        }

        private void Notify_FilterChanged() {
            // filter was changed, which means Selected may now be invisible, and index may have changed.
            if (ModButtonManager.ActiveButtons.Contains(Selected)) {
                if (!FilteredActiveButtons.Contains(Selected)) {
                    _selected = FilteredActiveButtons.FirstOrDefault();
                    _focusElement = 0;
                } else {
                    _focusElement = FilteredActiveButtons.FirstIndexOf(b => b == _selected);
                }
            }

            ModButton available = Selected;
            if (available == null) {
                return;
            }

            if (ModButtonManager.AvailableButtons.Contains(available)) {
                if (!FilteredAvailableButtons.Contains(available)) {
                    _selected = FilteredAvailableButtons.FirstOrDefault();
                    _focusElement = 0;
                } else {
                    _focusElement = FilteredAvailableButtons.FirstIndexOf(b => b == _selected);
                }
            }
        }

        public override void PreOpen() {
            base.PreOpen();
            _activeModsHash = ModLister.InstalledModsListHash(true);
            ModButtonManager.InitializeModButtons();
            ModButtonManager.Notify_RecacheModMetaData();
            ModButtonManager.Notify_RecacheIssues();
            Selected = ModButtonManager.AvailableButtons.FirstOrDefault() ?? ModButtonManager.ActiveButtons.FirstOrDefault();
        }

        public override void OnAcceptKeyPressed() {
            // for some reason, even though closeOnAccept = false, the window still closes.
            // So, let's just override this with nothing.
        }

        public override void Close(bool doCloseSound = true) {
            if (ModButtonManager.AnyIssue) {
                ConfirmModIssues();
            } else {
                Find.WindowStack.TryRemove(this, doCloseSound);
            }
        }

        public override void PostClose() {
            ModsConfig.Save();
            CheckModListChanged();
        }

        public void ConfirmModIssues() {
            IEnumerable<Dependency> issues = ModButtonManager.Issues.Where( i => i.Severity > 1 );
            string issueList = "";
            foreach (IGrouping<Manifest, Dependency> buttonIssues in issues.GroupBy(i => i.parent)
                .OrderByDescending(bi => bi.Max(i => i.Severity))
                .ThenBy(bi => bi.Key.Mod.Name)) {
                issueList += $"{buttonIssues.Key.Mod.Name}\n";
                foreach (Dependency issue in buttonIssues.Where(i => i.Severity > 1).OrderByDescending(i => i.Severity)) {
                    issueList += issue.Tooltip.CapitalizeFirst().Colorize(issue.Color) + "\n";
                }

                issueList += "\n";
            }

            void close() {
                Find.WindowStack.TryRemove(this);
            }

            string title = I18n.DialogConfirmIssuesTitle( issues.Count() );
            string text = I18n.DialogConfirmIssues( issues.Any( i => i.Severity >= 3 ) ? I18n.DialogConfirmIssuesCritical : "", issueList );
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, close, true, title));
        }

        public void CheckModListChanged() {
            if (_activeModsHash != ModLister.InstalledModsListHash(true)) {
                static void restart() {
                    GenCommandLine.Restart();
                }

                Find.WindowStack.Add(
                    new Dialog_MessageBox(
                        I18n.ModsChanged,
                        I18n.Later,
                        null,
                        I18n.OK,
                        restart,
                        null,
                        true,
                        restart,
                        restart));
            }
        }
    }
}
