// ModButtonManager.cs
// Copyright Karel Kroeze, 2018-2018

using System;
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
        private static List<ModButton> _availableButtons;
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

        public static List<ModButton> AvailableButtons
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
        public static bool AnyIssue => Issues.Any( i => i.severity > Severity.Notice );

        public static void TryAdd( ModButton button, bool notify_orderChanged = true )
        {
            _allButtons.TryAdd( button );
            if ( button.Active )
            {
                _activeButtons.TryAdd( button );
                _availableButtons.TryRemove( button );
                if (notify_orderChanged)
                    Notify_ModOrderChanged();
            }
            else
            {
                _availableButtons.TryAdd( button );
                _activeButtons.TryRemove( button );
                SortAvailable();
            }
        }

        public static void TryRemove( ModButton mod )
        {
            _allButtons.TryRemove( mod );
            _activeButtons.TryRemove( mod );
            _availableButtons.TryRemove( mod );
            Notify_ModOrderChanged();
        }

        public static ModAttributes AttributesFor( ModButton button )
        {
            if ( button is ModButton_Installed installed )
                return AttributesFor( installed.Selected );
            return null;
        }

        public static ModAttributes AttributesFor( ModMetaData mod )
        {
            return ModManager.Settings[mod];
        }

        internal static void RecacheModButtons()
        {
            Debug.Log( "Recaching ModButtons" );
            _allButtons = new List<ModButton>();
            _activeButtons = new List<ModButton>();
            _availableButtons = new List<ModButton>();

            // create all the buttons
            foreach ( var mods in ModLister.AllInstalledMods.GroupBy( m => Utilities.TrimModName( m.Name ) ) )
                TryAdd( new ModButton_Installed( mods ), false );
            
            SortActive();
            SortAvailable();
        }

        private static void SortAvailable()
        {
            _availableButtons = _availableButtons
                .OrderByDescending( b => b.SortOrder )
                .ThenBy( b => b.TrimmedName)
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
            ModsConfig.SetActiveToList(ActiveMods.Select(m => m.PackageId).ToList());
            Notify_RecacheIssues();
        }

        public static ModButton_Installed CoreMod => AllButtons.First( b => b.IsCoreMod ) as ModButton_Installed;
        public static ModButton_Installed ModManagerMod => AllButtons.First( b => b.IsModManager ) as ModButton_Installed;

        public static void Notify_RecacheIssues()
        {
            foreach (var button in AllButtons)
                button.Notify_RecacheIssues();
            _issues = ActiveButtons.SelectMany( b => b.Issues ).ToList();

            if ( !CoreMod.Active )
                // core not loaded
                _issues.Add( ModIssue.CoreNotFirst( CoreMod ) );
        }

        public static void Reorder( int from, int to )
        {
            if ( to == from )
                return;
            Insert( ActiveButtons[from], to );
        }

        public static void Insert( ModButton button, int to )
        {
            AllButtons.TryAdd( button );
            AvailableButtons.TryRemove( button );
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
                _availableButtons.TryRemove( mod );
                _activeButtons.TryAdd( mod );
                Notify_ModOrderChanged();
            }
            else
            {
                _activeButtons.TryRemove( mod );
                _availableButtons.TryAdd( mod );
                Notify_ModOrderChanged();
                SortAvailable();
            }
        }

        public static void Notify_Unsubscribed( string publishedFileId )
        {
            var button = AllButtons.OfType<ModButton_Installed>()
                .FirstOrDefault( b => b.Versions.Any( m => m.Source == ContentSource.SteamWorkshop &&
                                                           m.PackageId == publishedFileId ) );
            var mod = button?.Versions.First( m => m.Source == ContentSource.SteamWorkshop &&
                                                  m.PackageId == publishedFileId );
            button?.Notify_VersionRemoved( mod );
        }

        public static void Notify_DownloadCompleted( ModMetaData mod )
        {
            var downloading = AllButtons.OfType<ModButton_Downloading>()
                .FirstOrDefault( b => b.Identifier == mod.PackageId );

            var missing = AllButtons.OfType<ModButton_Missing>()
                .FirstOrDefault( b => b.Identifier == mod.PackageId );

            // add installed item to MBM
            var installed = ModButton_Installed.For( mod );
            if ( missing != null && missing.Active )
                Insert( installed, ActiveButtons.IndexOf( missing ) );
            else
                TryAdd( installed );

            Page_BetterModConfig.Instance.Selected = installed;
            TryRemove(downloading);
            TryRemove(missing);

            Page_BetterModConfig.Instance.Notify_ModsListChanged();
        }

        private static List<ModIssue> _issues;
        public static List<ModIssue> Issues
        {
            get
            {
                if ( _issues == null )
                    Notify_RecacheIssues();
                return _issues;
            }
        }

        public static void Reset( bool addDefaultMods = true )
        {
            foreach ( var button in new List<ModButton>( ActiveButtons ) )
                button.Active = false;

            if ( addDefaultMods )
            {
                CoreMod.Active = true;

                if ( ModManager.Settings.AddModManagerToNewModLists )
                    ModManagerMod.Active = true;
            }
            
            Notify_ModOrderChanged();
        }
    }
}