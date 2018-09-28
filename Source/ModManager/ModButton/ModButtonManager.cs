﻿// ModButtonManager.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using Verse;
using System.Linq;
using UnityEngine;

namespace ModManager
{
    public static class ModButtonManager
    {
        private static List<ModButton> _allButtons;
        private static List<ModButton> _activeButtons;
        private static List<ModButton_Installed> _availableButtons;
        public static List<ModButton> AllButtons 
        {
            get
            {
                if ( _allButtons == null )
                    RecacheModButtons();
                return _allButtons;
            }
        }

        public static List<ModButton> ActiveButtons
        {
            get
            {
                if(_activeButtons == null)
                    RecacheModButtons();
                return _activeButtons;
            }
        }

        public static List<ModButton_Installed> AvailableButtons
        {
            get
            {
                if (_availableButtons == null)
                    RecacheModButtons();
                return _availableButtons;
            }
        }
        public static IEnumerable<ModMetaData> AllMods => AllButtons.OfType<ModButton_Installed>()
            .SelectMany( b => b.Versions );

        public static IEnumerable<ModMetaData> ActiveMods => ActiveButtons.OfType<ModButton_Installed>()
            .Select( b => b.Selected );

        public static IEnumerable<ModMetaData> AvailableMods => AllMods.Where( m => !m.Active );
        public static bool AnyIssue => ActiveButtons.Any( b => b.Issues.Any( i => i.severity > Severity.Notice ) );

        public static void Add( ModButton button )
        {
            _allButtons.TryAdd( button );
            if ( button.Active )
            {
                _activeButtons.TryAdd( button );
                _availableButtons.TryRemove( button as ModButton_Installed );
            }
            else
            {
                _availableButtons.TryAdd( button as ModButton_Installed );
                _activeButtons.TryRemove( button );
                SortAvailable();
            }
        }

        public static void Remove( ModButton mod )
        {
            _allButtons.TryRemove( mod );
            _activeButtons.TryRemove( mod );
            _availableButtons.TryRemove( mod as ModButton_Installed );
            Notify_ModOrderChanged();
        }

        internal static void RecacheModButtons()
        {
            Debug.Log( "Recaching ModButtons" );
            _allButtons = new List<ModButton>();
            _activeButtons = new List<ModButton>();
            _availableButtons = new List<ModButton_Installed>();

            // create all the buttons
            foreach ( var mods in ModLister.AllInstalledMods.GroupBy( m => m.Name ) )
                Add( new ModButton_Installed( mods ) );
            
            SortActive();
            SortAvailable();
        }

        private static void SortAvailable()
        {
            _availableButtons = _availableButtons
                .OrderByDescending( b => b.Selected.Compatibility() )
                .ThenBy( b => b.Name )
                .ToList();
        }

        private static void SortActive()
        {
            _activeButtons = _activeButtons
                .OrderBy(b => b.LoadOrder)
                .ToList();
        }

        public static void Notify_ModOrderChanged()
        {
            ModsConfig.SetActiveToList( ActiveMods.Select( m => m.Identifier ).ToList() );
            foreach ( var button in AllButtons )
                button.Notify_RecacheIssues();
        }

        public static void Reorder( int from, int to )
        {
            if ( to == from )
                return;
            Insert( ActiveButtons[from], to );
        }

        public static void Insert( ModButton button, int to )
        {
            AvailableButtons.TryRemove( button as ModButton_Installed );
            if ( ActiveButtons.Contains( button ) )
            {
                if ( ActiveButtons.IndexOf( button ) < to )
                    to--;
                ActiveButtons.Remove( button );
            }
            ActiveButtons.Insert( Mathf.Clamp( to, 0, ActiveButtons.Count ), button );
            Notify_ModOrderChanged();
        }

        public static void Notify_Activated( ModButton mod, bool active )
        {
            if ( active )
            {
                _availableButtons.TryRemove( mod as ModButton_Installed );
                _activeButtons.TryAdd( mod );
                Notify_ModOrderChanged();
            }
            else
            {
                _activeButtons.TryRemove( mod );
                _availableButtons.TryAdd( mod as ModButton_Installed );
                Notify_ModOrderChanged();
                SortAvailable();
            }
        }

        public static void Notify_ModListApplied()
        {
            ActiveButtons.ForEach( b => b.Notify_ResetSelected() );
            Notify_ModOrderChanged();
        }

        public static void Notify_Unsubscribed( string publishedFileId )
        {
            var mod = ModLister.GetModWithIdentifier( publishedFileId );
            var button = ModButton_Installed.For( mod );
            button.Notify_VersionRemoved( mod );
        }

        public static void DeactivateAll()
        {
            foreach ( var button in new List<ModButton>( ActiveButtons ) )
                button.Active = false;
        }
    }
}