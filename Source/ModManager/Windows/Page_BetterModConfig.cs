// Window_ModSelection.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ModManager.Constants;
using static ModManager.Resources;

namespace ModManager
{
    public class Page_BetterModConfig: Page_ModsConfig
    {
        private static Page_BetterModConfig _instance;
        private Vector2 _availableScrollPosition = Vector2.zero;
        private Vector2 _activeScrollPosition = Vector2.zero;
        private int _scrollViewHeight = 0;
        private ModButton _selected;
        private string _availableFilter;
        private string _activeFilter;
        private bool _availableFilterVisible;
        private bool _activeFilterVisible = true;
        private int _activeModsHash;

        public enum FocusArea
        {
            AvailableFilter,
            Available,
            ActiveFilter,
            Active
        }
        private FocusArea _focusArea = FocusArea.Available;
        private int _focusElement = 1;

        public Page_BetterModConfig()
        {
            _instance = this;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = false;
            resizeable = true;
            AccessTools.Field( typeof( Window ), "resizer" )
                .SetValue( this, new WindowResizer {minWindowSize = MinimumSize} );
        }

        public bool FilterAvailable => !_availableFilterVisible && !_availableFilter.NullOrEmpty();
        public bool FilterActive => !_activeFilterVisible && !_activeFilter.NullOrEmpty();

        public static Page_BetterModConfig Instance => _instance;

        public override Vector2 InitialSize => StandardSize;
        public static Vector2 MinimumSize => StandardSize * 2 / 3f;

        public ModButton Selected
        {
            get => _selected;
            set
            {
                _selected = value;

                // cop-out if null
                if ( value == null )
                    return;

                SoundDefOf.RadioButtonClicked.PlayOneShotOnCamera();

                // clear text field focus
                GUIUtility.keyboardControl = 0;

                // set correct focus
                if ( !value.Active )
                {
                    _focusArea = FocusArea.Available;
                    if ( FilteredAvailableButtons.Contains( value ) )
                    {
                        _focusElement = FilteredAvailableButtons.FirstIndexOf( mod => mod == value );
                        EnsureVisible(ref _availableScrollPosition, _focusElement);
                    }
                    else
                    {
                        _selected = null;
                    }
                }
                else
                {
                    _focusArea = FocusArea.Active;
                    if ( FilteredActiveButtons.Contains( value ) )
                    {
                        _focusElement = FilteredActiveButtons.FirstIndexOf( mod => mod == value );
                        EnsureVisible( ref _activeScrollPosition, _focusElement );
                    }
                    else
                    {
                        _selected = null;
                    }
                }
            }
        }

        public bool SelectedHasFocus =>
            _focusArea == FocusArea.Active && ModButtonManager.ActiveButtons.Contains( Selected ) ||
            _focusArea == FocusArea.Available && ModButtonManager.AvailableButtons.Contains( Selected );
        
        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            DraggingManager.OnGUI();

#if DEBUG
            DebugOnGUI();
#endif
        }

        private static void DebugOnGUI()
        {
            var msg = "";
            var mods = ModsConfig.ActiveModsInLoadOrder.ToList();
            for( int i = 0; i < mods.Count; i++ )
            {
                var mod = mods[i];
                msg += $"{i}: {mod?.Name ?? "NULL"}\t({mod?.RootDir.Name ?? "NULL"})\n";
            }
            Widgets.Label( new Rect( 0, 0, Screen.width, Screen.height ), msg );
        }

        public override void DoWindowContents( Rect canvas )
        {
            CheckResized();
            HandleKeyboardNavigation();
            DraggingManager.Update();
            
            var iconBarHeight = IconSize + SmallMargin;

            var availableRect = new Rect( 
                canvas.xMin, 
                canvas.yMin, 
                canvas.width * 1/5f, 
                canvas.height - SmallMargin - iconBarHeight );
            var moreModButtonsRect = new Rect(
                canvas.xMin,
                availableRect.yMax + SmallMargin,
                canvas.width * 1/5f,
                iconBarHeight );
            var activeRect = new Rect( 
                availableRect.xMax + SmallMargin,
                canvas.yMin,
                canvas.width * 1 / 5f,
                canvas.height - SmallMargin - iconBarHeight);
            var modSetButtonsRect = new Rect(
                activeRect.xMin,
                activeRect.yMax + SmallMargin,
                canvas.width * 1 / 5f,
                iconBarHeight );
            var detailRect = new Rect( 
                activeRect.xMax + SmallMargin, 
                canvas.yMin, 
                canvas.width * 3 / 5f - SmallMargin * 2,
                canvas.height - SmallMargin - iconBarHeight);
            var modButtonsRect = new Rect(
                detailRect.xMin,
                detailRect.yMax + SmallMargin,
                detailRect.width,
                iconBarHeight );

            DoAvailableMods(availableRect);
            DoActiveMods(activeRect);
            DoDetails( detailRect );

            DoMoreModButtons( moreModButtonsRect );
            DoModSetButtons( modSetButtonsRect );
            Selected?.DoModActionButtons( modButtonsRect );
        }

        private float _width;
        private void CheckResized()
        {
            if ( windowRect.width != _width )
            {
                ModButton.Notify_ModButtonSizeChanged();
                _width = windowRect.width;
            }
        }

        private void DoMoreModButtons( Rect canvas )
        {
            Widgets.DrawBoxSolid( canvas, SlightlyDarkBackground );
            canvas = canvas.ContractedBy( SmallMargin / 2f );

            var iconRect = new Rect(
                canvas.xMax - IconSize,
                canvas.yMin,
                IconSize,
                IconSize );
            if ( Utilities.ButtonIcon( ref iconRect, Steam, I18n.GetMoreMods_SteamWorkshop, Status_Plus, Direction8Way.NorthWest ) )
                SteamUtility.OpenSteamWorkshopPage();
            if ( Utilities.ButtonIcon( ref iconRect, Ludeon, I18n.GetMoreMods_LudeonForums, Status_Plus, Direction8Way.NorthWest ) )
                Application.OpenURL("http://rimworldgame.com/getmods");
        }

        private int _issueIndex = 0;
        private void DoModSetButtons( Rect canvas )
        {
            Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
            canvas = canvas.ContractedBy(SmallMargin / 2f);

            var iconRect = new Rect(
                canvas.xMax - IconSize,
                canvas.yMin,
                IconSize,
                IconSize);
            if ( Utilities.ButtonIcon( ref iconRect, File, I18n.LoadModList, Status_Up ) )
                DoLoadModListFloatMenu();
            if ( Utilities.ButtonIcon( ref iconRect, File, I18n.SaveModList, Status_Down ) )
                new ModList( ModButtonManager.ActiveButtons );
            if ( ModListManager.ModLists.Any() )
                if ( Utilities.ButtonIcon( ref iconRect, File, I18n.DeleteModList, Status_Cross, mouseOverColor: Color.red ) )
                    ModListManager.DoDeleteModListFloatMenu();
            if ( ModButtonManager.ActiveMods.Any( mod => mod.Source == ContentSource.SteamWorkshop ) )
                if ( Utilities.ButtonIcon( ref iconRect, Folder, I18n.CreateLocalCopies, Status_Plus ) )
                    IO.CreateLocalCopies( ModButtonManager.ActiveMods );
            if ( ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
                .Any( b => b.Identifier.IsSteamWorkshopIdentifier() ) )
                if ( Utilities.ButtonIcon( ref iconRect, Steam, I18n.SubscribeAllMissing, Status_Plus, Direction8Way.NorthWest ) )
                    Workshop.Subscribe( ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
                        .Where( b => b.Identifier.IsSteamWorkshopIdentifier() ).Select( b => b.Identifier ) );

            if ( ModButtonManager.AnyIssue )
            {
                var issueRect = new Rect( canvas.xMin + SmallMargin, canvas.yMin, IconSize, IconSize );
                var groupedIssues = ModButtonManager.Issues
                    .Where( i => i.severity > Severity.Notice )
                    .GroupBy( i => i.button )
                    .OrderByDescending( bi => bi.Max( i => i.severity ) )
                    .ThenBy( bi => bi.Key.Name );
                foreach ( var buttonIssues in groupedIssues)
                {
                    var tip = $"<b>{buttonIssues.Key.Name}</b>";
                    tip += buttonIssues.Select( i => $"\n<color={ColorUtility.ToHtmlStringRGBA(i.Color)}>{i.tip}</color>" ).StringJoin( "" );
                    TooltipHandler.TipRegion(issueRect, tip);
                }
                var color = ModButtonManager.Issues.MaxBy( i => i.severity ).Color;

                if ( Widgets.ButtonImage( issueRect, Warning, color ) )
                {
                    _issueIndex = Utilities.Modulo( _issueIndex, groupedIssues.Count() );
                    Selected = groupedIssues.ElementAt( _issueIndex++ ).Key;
                }
            }
        }

        private void DoLoadModListFloatMenu()
        {
            var options = new List<FloatMenuOption>();
            options.Add( new FloatMenuOption( I18n.LoadModListFromSave, DoLoadModListFromSaveFloatMenu ) );
            options.AddRange( ModListManager.SavedModListOptions );
            Find.WindowStack.Add( new FloatMenu( options ) );
        }

        private void DoLoadModListFromSaveFloatMenu()
        {
            Find.WindowStack.Add( new FloatMenu( ModListManager.SaveFileOptions ) );
        }


        private int _lastControlID = 0;
        public void HandleKeyboardNavigation()
        {
            if ( !Find.WindowStack.CurrentWindowGetsInput )
                return;

            var id = GUIUtility.keyboardControl;
            if ( _lastControlID != id )
            {
                _lastControlID = id;
                Debug.Log($"Focus: {_focusArea}, {_focusElement}, {id}, {GUI.GetNameOfFocusedControl()}, {Event.current.type}");
            }

            // unity tries to select the next textField on Tab (which is _not_ documented!)
            if ( _focusArea.ToString() != GUI.GetNameOfFocusedControl() )
            {
                if ( _focusArea == FocusArea.ActiveFilter || _focusArea == FocusArea.AvailableFilter )
                {
                    // focus the control we want.
                    GUI.FocusControl( _focusArea.ToString() );
                }
                else
                {
                    // clear focus.
                    GUIUtility.keyboardControl = 0;
                }
            }


            // handle keyboard events
            var key = Event.current.keyCode;
            var shift = Event.current.shift;
            if ( Event.current.type == EventType.KeyDown )
            {
                // move controlled area on (shift) tab
                if ( key == KeyCode.Tab )
                {
                    // determine new focus
                    var focusInt = Utilities.Modulo( (int) _focusArea + ( shift ? -1 : 1 ), 4 );
                    var focus = (FocusArea) focusInt;
                    
                    Debug.Log( $"current focus: {_focusArea}, new focus: {focus}" );

                    // apply new focus
                    _focusArea = focus;
                    switch ( focus )
                    {
                        case FocusArea.AvailableFilter:
                        case FocusArea.ActiveFilter:
                            GUI.FocusControl( focus.ToString() );
                            break;
                        case FocusArea.Available:
                            SelectAt( FilteredAvailableButtons, _availableScrollPosition );
                            break;
                        case FocusArea.Active:
                            SelectAt( FilteredActiveButtons, _activeScrollPosition );
                            break;
                    }
                    return;
                }

                if (_focusArea == FocusArea.Available)
                {
                    switch (key)
                    {
                        case KeyCode.UpArrow:
                            SelectPrevious( FilteredAvailableButtons );
                            break;
                        case KeyCode.DownArrow:
                            SelectNext( FilteredAvailableButtons );
                            break;
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                            Log.Message( $"{Selected.Name} active?: {Selected.Active}"  );
                            Selected.Active = true;
                            Log.Message($"{Selected.Name} active?: {Selected.Active}");

                            if ( FilteredAvailableButtons.Any() )
                            {
                                var index = Math.Min( _focusElement, FilteredAvailableButtons.Count - 1 );
                                Selected = FilteredAvailableButtons.ElementAt( index );
                            }
                            else
                            {
                                Selected = Selected;
                            }
                            
                            break;
                        case KeyCode.RightArrow:
                            if ( shift )
                            {
                                Selected.Active = true;
                                Selected = Selected; // sets _focusElement, _focusArea, plays sound.
                            }
                            else
                            {
                                SelectAt( FilteredActiveButtons, _availableScrollPosition, _activeScrollPosition );
                            }
                            break;
                    }
                    return;
                }


                if (_focusArea == FocusArea.Active)
                {
                    switch (key)
                    {
                        case KeyCode.UpArrow:
                            if ( Event.current.shift )
                                MoveUp();
                            else
                                SelectPrevious( FilteredActiveButtons );
                            break;
                        case KeyCode.DownArrow:
                            if ( shift )
                                MoveDown();
                            else
                                SelectNext( FilteredActiveButtons );
                            break;
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                        case KeyCode.Delete:
                            Selected.Active = false;
                            if (FilteredActiveButtons.Any() )
                            {
                                var index = Math.Min( _focusElement, FilteredActiveButtons.Count - 1 );
                                Selected = FilteredActiveButtons.ElementAt( index );
                            }
                            else
                            {
                                Selected = Selected;
                            }

                            break;
                        case KeyCode.LeftArrow:
                            if ( shift )
                            {
                                Selected.Active = false;
                                Selected = Selected; // sets _focusArea, _focusElement, plays sound.
                            }
                            else
                            {
                                SelectAt( FilteredAvailableButtons, _activeScrollPosition, _availableScrollPosition );
                            }
                            break;
                    }
                }
            }
        }

        private void MoveUp()
        {
            if ( _focusElement <= 0 )
                return;

            // is only called from active.
            ModButtonManager.Insert( Selected, ModButtonManager.ActiveButtons.IndexOf( FilteredActiveButtons.ElementAt( _focusElement - 1 ) ) );
            Selected = Selected; // sets _focusElement, plays sound.
        }

        private void MoveDown()
        {
            if (_focusElement >= FilteredActiveButtons.Count - 1 ) 
                return;

            // is only called from active.
            ModButtonManager.Insert( Selected, ModButtonManager.ActiveButtons.IndexOf( FilteredActiveButtons.ElementAt( _focusElement + 1 ) ) + 1 );
            Selected = Selected; // sets _focusElement, plays sound.
        }

        private void SelectAt<T>( 
            IEnumerable<T> targetMods, 
            Vector2 sourceScrollposition,
            Vector2 targetScrollposition) where T : ModButton
        {
            var offset = _focusElement * ModButtonHeight - sourceScrollposition.y;
            SelectAt( targetMods, targetScrollposition, offset );
        }

        private void SelectAt<T>( IEnumerable<T> mods, Vector2 scrollposition, float offset = 0f ) where T : ModButton
        {
            SelectAt( mods, scrollposition.y + offset );
        }

        private void SelectAt<T>( IEnumerable<T> mods, float position ) where T: ModButton
        {
            if ( mods.Any() )
            {
                Selected = mods.ElementAt( IndexAt( mods, position ) );
            }
            else
            {
                Selected = null;
            }
        }

        private int IndexAt<T>(
            IEnumerable<T> targetMods,
            Vector2 sourceScrollposition,
            Vector2 targetScrollposition ) where T : ModButton
        {
            var offset = _focusElement * ModButtonHeight - sourceScrollposition.y;
            return IndexAt(targetMods, targetScrollposition, offset);
        }

        private int IndexAt<T>( IEnumerable<T> mods, Vector2 scrollposition, float offset = 0f ) where T : ModButton
        {
            return IndexAt( mods, scrollposition.y + offset );
        }

        private int IndexAt<T>( IEnumerable<T> mods, float position ) where T : ModButton
        {
            if ( !mods.Any() )
                return -1;
            return Mathf.Clamp(Mathf.CeilToInt(position / ModButtonHeight), 0, mods.Count() - 1);
        }

        private void SelectNext<T>( IEnumerable<T> mods ) where T: ModButton
        {
            if ( !mods.Any() )
                return;
            var index = Utilities.Modulo( _focusElement + 1, mods.Count() );
            Selected = mods.ElementAt( index );
        }

        private void SelectPrevious<T>( IEnumerable<T> mods ) where T: ModButton
        {
            if (!mods.Any())
                return;
            var index = Utilities.Modulo( _focusElement - 1, mods.Count() );
            Selected = mods.ElementAt( index );
        }


        private void EnsureVisible( ref Vector2 scrollPosition, int index )
        {
            var min = index * ModButtonHeight;
            var max = ( index + 1 ) * ModButtonHeight;

            if ( min < scrollPosition.y )
            {
                scrollPosition.y = min;
            }
            if ( max > scrollPosition.y + _scrollViewHeight )
            {
                scrollPosition.y = max - _scrollViewHeight + ModButtonHeight;
            }
        }

        public List<ModButton> FilteredAvailableButtons
        {
            get
            {
                if ( FilterAvailable )
                    return ModButtonManager.AvailableButtons
                        .Where( b => b.MatchesFilter( _availableFilter ) > 0 )
                        .OrderBy( b => b.MatchesFilter( _availableFilter ) )
                        .ToList();
                return ModButtonManager.AvailableButtons;
            }
        }

        public void DoAvailableMods( Rect canvas )
        {
            Utilities.DoLabel( ref canvas, I18n.AvailableMods );
            Widgets.DrawBoxSolid( canvas, SlightlyDarkBackground );

            var buttons = FilteredAvailableButtons;
            var filterRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                FilterHeight );
            var outRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                canvas.height - FilterHeight - SmallMargin );
            var viewRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                Mathf.Max( ModButtonHeight * buttons.Count(), outRect.height ) );
            if ( viewRect.height > outRect.height )
                viewRect.width -= 18f;
            var modRect = new Rect(
                viewRect.xMin,
                viewRect.yMin,
                viewRect.width,
                ModButtonHeight );
            _scrollViewHeight = (int) outRect.height;

            DoFilterField( filterRect, ref _availableFilter, ref _availableFilterVisible, FocusArea.AvailableFilter );

            var alternate = false;

            Widgets.BeginScrollView( outRect, ref _availableScrollPosition, viewRect );
            foreach ( var button in buttons )
            {
                button.DoModButton( modRect, alternate, () => Selected = button, () => button.Active = true, _availableFilterVisible, _availableFilter );
                alternate = !alternate;
                modRect.y += ModButtonHeight;
            }

            // handle drag & drop
            int hoverIndex;
            var dropped = DraggingManager.ContainerUpdate( buttons, viewRect, out hoverIndex );
            var draggingOverAvailable = hoverIndex >= 0;
            if ( draggingOverAvailable != _draggingOverAvailable )
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                _draggingOverAvailable = draggingOverAvailable;
            }
            if ( dropped )
            {
                DraggingManager.Dragged.Active = false;
            }
            Widgets.EndScrollView();

            if (draggingOverAvailable)
            {
                GUI.color = Color.grey;
                Widgets.DrawBox(outRect);
                GUI.color = Color.white;
            }
        }

        private bool _draggingOverAvailable = false;
        private int _lastHoverIndex;
        public List<ModButton> FilteredActiveButtons => ModButtonManager.ActiveButtons
                        .Where( b => !FilterActive || b.MatchesFilter( _activeFilter ) > 0 )
                        .ToList();

        public void DoActiveMods( Rect canvas )
        {
            Utilities.DoLabel( ref canvas, I18n.ActiveMods );
            Widgets.DrawBoxSolid( canvas, SlightlyDarkBackground );

            var buttons = FilteredActiveButtons;
            var filterRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                FilterHeight );
            var outRect = new Rect( 
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                canvas.height - FilterHeight - SmallMargin );
            var viewRect = new Rect(
                canvas.xMin,
                filterRect.yMax + SmallMargin,
                canvas.width,
                Mathf.Max( ModButtonHeight * buttons.Count(), outRect.height ) );
            if ( viewRect.height > outRect.height )
                viewRect.width -= 18f;
            var modRect = new Rect(
                viewRect.xMin,
                viewRect.yMin,
                viewRect.width,
                ModButtonHeight);

            DoFilterField(filterRect, ref _activeFilter, ref _activeFilterVisible, FocusArea.ActiveFilter);

            var alternate = false;

            Widgets.BeginScrollView( outRect, ref _activeScrollPosition, viewRect );
            int hoverIndex;
            if ( DraggingManager.ContainerUpdate( buttons, viewRect, out hoverIndex ) )
            {
                var dropIndex = hoverIndex;
                // if filtering the active list, figure out the desired index in the source list
                if ( FilterActive && hoverIndex > 0 )
                {
                    var insertBefore = buttons.ElementAtOrDefault( hoverIndex );
                    if ( insertBefore == null )
                    {
                        dropIndex = ModButtonManager.ActiveButtons.Count;
                    }
                    else
                    {
                        dropIndex = ModButtonManager.ActiveButtons.IndexOf( insertBefore );
                    }

                }
                ModButtonManager.Insert( DraggingManager.Dragged, dropIndex );
            }
            if (hoverIndex != _lastHoverIndex)
            {
                _lastHoverIndex = hoverIndex;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            for ( int i = 0; i < buttons.Count(); i++ )
            {
                var mod = buttons.ElementAt(i);

                mod.DoModButton(modRect, alternate, () => Selected = mod, () => mod.Active = false, _activeFilterVisible, _activeFilter);
                alternate = !alternate;

                if (hoverIndex == i )
                {
                    GUI.color = Color.grey;
                    Widgets.DrawLineHorizontal(modRect.xMin, modRect.yMin, modRect.width);
                    GUI.color = Color.white;
                }

                modRect.y += ModButtonHeight;
            }
            if (hoverIndex == buttons.Count())
            {
                GUI.color = Color.grey;
                Widgets.DrawLineHorizontal(modRect.xMin, modRect.yMin, modRect.width);
                GUI.color = Color.white;
            }
            Widgets.EndScrollView();
        }

        public void DoDetails( Rect canvas )
        {
            if ( Selected == null )
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;
                Widgets.Label( canvas, I18n.NoModSelected );
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                Selected.DoModDetails( canvas );
            }
        }

        public void DoFilterField( Rect canvas, ref string filter, ref bool visible, FocusArea focus )
        {
            var rect = canvas.ContractedBy( SmallMargin / 2f );
            var iconRect = new Rect(
                canvas.xMax - SmallIconSize - SmallMargin,
                canvas.yMin + (canvas.height - SmallIconSize) / 2f,
                SmallIconSize,
                SmallIconSize
            );

            // intercept focus gain events
            if ( Mouse.IsOver( rect ) && Event.current.type == EventType.mouseUp )
            {
                _focusArea = focus;
            }

            // handle button interactions before textfield, because textfield eats click events.
            if ( !filter.NullOrEmpty() )
            {
                if ( Widgets.ButtonInvisible( iconRect ) )
                {
                    filter = string.Empty;
                }
                iconRect.x -= SmallIconSize + SmallMargin;
                if ( Widgets.ButtonInvisible( iconRect ) )
                {
                    visible = !visible;
                }
                iconRect.x += SmallIconSize + SmallMargin;
            }
            else
            {
                // Unity gets confused if the number of controls changes, and the textfield will 
                // loose focus. To avoid this, spawn some dummy controls.
                Widgets.ButtonInvisible( Rect.zero );
                Widgets.ButtonInvisible( Rect.zero );
            }

            // handle textfield
            GUI.SetNextControlName( focus.ToString() );
            var newFilter = Widgets.TextField( rect, filter );
            if ( newFilter != filter )
            {
                filter = newFilter;
                Notify_FilterChanged();
            }

            // draw buttons over textfield.
            // Note that these buttons _cannot_ be clicked, but the ButtonImage 
            // code is a handy shortcut for mouseOver interactions.
            if ( !filter.NullOrEmpty() )
            {
                if ( Widgets.ButtonImage( iconRect, Status_Cross ) )
                {
                    filter = string.Empty;
                    Notify_FilterChanged();
                }
                iconRect.x -= SmallIconSize + SmallMargin;
                if ( Widgets.ButtonImage( iconRect, visible ? EyeClosed : EyeOpen ) )
                {
                    visible = !visible;
                    Notify_FilterChanged();
                }
            }
            else
            {
                // search icon, plus more dummy controls (filter is called multiple times).
                GUI.DrawTexture( iconRect, Search );
                Widgets.ButtonInvisible( Rect.zero );
                Widgets.ButtonInvisible( Rect.zero );
            }
        }

        private void Notify_FilterChanged()
        {
            // filter was changed, which means Selected may now be invisible, and index may have changed.
            if ( ModButtonManager.ActiveButtons.Contains( Selected ) )
            {
                if ( !FilteredActiveButtons.Contains( Selected ) )
                {
                    _selected = FilteredActiveButtons.FirstOrDefault();
                    _focusElement = 0;
                }
                else
                {
                    _focusElement = FilteredActiveButtons.FirstIndexOf( b => b == _selected );
                }
            }

            var available = Selected;
            if ( available == null ) return;
            if ( ModButtonManager.AvailableButtons.Contains( available ) )
            {
                if ( !FilteredAvailableButtons.Contains( available ) )
                {
                    _selected = FilteredAvailableButtons.FirstOrDefault();
                    _focusElement = 0;
                }
                else
                {
                    _focusElement = FilteredAvailableButtons.FirstIndexOf( b => b == _selected );
                }
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            _activeModsHash = ModLister.InstalledModsListHash( true );
            ModButtonManager.RecacheModButtons();
            Selected = ModButtonManager.AvailableButtons.FirstOrDefault() ?? ModButtonManager.ActiveButtons.FirstOrDefault();
        }

        public override void OnAcceptKeyPressed()
        {
            // for some reason, even though closeOnAccept = false, the window still closes.
            // So, let's just override this with nothing.
        }

        public override void Close( bool doCloseSound = true )
        {
            if ( ModButtonManager.AnyIssue )
                ConfirmModIssues();
            else
                Find.WindowStack.TryRemove( this, doCloseSound );
        }

        public override void PostClose()
        {
            ModsConfig.Save();
            CheckModListChanged();
        }

        public void ConfirmModIssues()
        {
            var issues = ModButtonManager.Issues.Where( i => i.severity > Severity.Notice );
            string issueList = "";
            foreach ( var buttonIssues in issues.GroupBy( i => i.button )
                .OrderByDescending( bi => bi.Max( i => i.severity ) )
                .ThenBy( bi => bi.Key.Name  ) )
            {
                issueList += $"{buttonIssues.Key.Name}\n";
                foreach ( var issue in buttonIssues.Where( i => i.severity > Severity.Notice ).OrderByDescending( i => i.severity ) )
                    issueList += $"<i><color=#{ColorUtility.ToHtmlStringRGBA(issue.Color)}>{issue.tip}</color></i>\n";
                issueList += "\n";
            }

            Action close = () => Find.WindowStack.TryRemove( this );
            var title = I18n.DialogConfirmIssuesTitle( issues.Count() );
            var text = I18n.DialogConfirmIssues( issueList );
            Find.WindowStack.Add( Dialog_MessageBox.CreateConfirmation( text, close, true, title ) );
        }

        public void CheckModListChanged()
        {
            if (_activeModsHash != ModLister.InstalledModsListHash(true))
            {
                Action restart = () => GenCommandLine.Restart();
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