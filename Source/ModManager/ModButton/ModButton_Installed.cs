// ModButton.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using ColourPicker;
using HarmonyLib;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;
using static ModManager.Constants;
using static ModManager.Resources;
using static ModManager.Utilities;

namespace ModManager
{
    public class ModButton_Installed : ModButton
    {
        private ModMetaData _selected;
        private Vector2 _previewScrollPosition = Vector2.zero;
        private Vector2 _descriptionScrollPosition = Vector2.zero;
        public override int SortOrder => Selected.Compatibility();
        public List<ModList> Lists => ModListManager.ListsFor( this );

        public ModButton_Installed( ModMetaData mod )
        {
            if (mod == null )
                throw new ArgumentNullException( nameof(mod) );

            Versions.Add( mod );
        }

        public ModButton_Installed( IEnumerable<ModMetaData> mods )
        {
            if ( mods == null || !mods.Any() )
                throw new ArgumentNullException( nameof( mods ) );

            Versions = mods.ToList();
        }

        public static ModButton_Installed For( ModMetaData mod )
        {
            var button = ModButtonManager.AllButtons.OfType<ModButton_Installed>()
                .FirstOrDefault( mb => mb.Name == mod.Name || ModManager.Settings.TrimTags && mb.TrimmedName == TrimModName( mod.Name ) );
            if ( button == null )
                return new ModButton_Installed( mod );
            if ( !button.Versions.Contains( mod ) )
                button.Versions.Add( mod );
            return button;
        }

        public override int LoadOrder => Selected?.LoadOrder() ?? base.LoadOrder;

        public override int MatchesFilter( string filter )
        {
            if ( base.MatchesFilter( filter ) > 0 )
                return 1;
            if ( Selected.Author.ToUpperInvariant().Contains( filter.ToUpperInvariant() ) )
                return 2;
            // too many false positives.
            //if ( Selected.Description.ToUpperInvariant().Contains( filter.ToUpperInvariant() ) )
            //    return 3;
            return 0;
        }
        
        public IEnumerable<ModMetaData> VersionsOrdered => Versions
            .OrderByDescending( mod => mod.Compatibility() )
            .ThenBy( mod => mod.Source );

        public override string Name => Selected?.Name;
        public override string Identifier => Selected?.PackageId;
        public override bool SamePackageId( string packageId )
        {
            return Selected?.SamePackageId( packageId ) ?? false;
        }

        public override bool Active
        {
            get => Versions.Any( mod => mod.Active );
            set {
                Selected.Active = value;
                ModButtonManager.Notify_ActiveStatusChanged( this, value );
            }
        }

        public List<ModMetaData> Versions { get; } = new List<ModMetaData>();
        public Manifest Manifest => Manifest.For( Selected );
        
        public ModMetaData Selected
        {
            get
            {
                if ( _selected == null )
                    _selected = Versions.FirstOrDefault( m => m.Active ) ?? VersionsOrdered.FirstOrDefault();
                return _selected;
            }
            set
            {
                if ( value != null )
                    value.Active = Selected?.Active ?? false;
                if ( Selected != null )
                    Selected.Active = false;
                var old = _selected;
                _selected = value;
                _titleLinkOptions = null;
                ModButtonManager.Notify_ModListChanged();
            }
        }

        public override Color Color
        {
            get
            {
                // use version colour if set
                if ( ModManager.UserData[Selected].Color != Color.white )
                    return ModManager.UserData[Selected].Color;

                // then button colour
                if ( ModManager.UserData[this].Color != Color.white )
                    return ModManager.UserData[this].Color;

                // if this mod is included in any lists, use that colour
                if ( !Lists.NullOrEmpty() )
                {
                    var colours = Lists.Select( l => l.Color )
                        .Where( c => c != Color.white );
                    if (colours.Any())
                        return colours.Aggregate((a, b) => a + b) / colours.Count();
                }

                // if nothing stuck, use default
                return Color.white;
            }
            set => ModManager.UserData[this].Color = value;
        }

        public override void DoModButton( 
            Rect canvas, 
            bool alternate = false, 
            Action clickAction = null, 
            Action doubleClickAction = null,
            bool deemphasizeFiltered = false,
            string filter = null )
        {
            base.DoModButton( canvas, alternate, clickAction, doubleClickAction, deemphasizeFiltered, filter );

            canvas = canvas.ContractedBy( SmallMargin / 2f ).Rounded();

            /**
             * NAME                    | Versions
             * author                  | Issues
             */
            var nameRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width - (SmallIconSize + SmallMargin) * Versions.Count,
                canvas.height * 3 / 5f);
            var authorRect = new Rect(
                canvas.xMin,
                nameRect.yMax,
                canvas.width - SmallIconSize,
                canvas.height * 2 / 5f);
            var issueRect = new Rect(
                authorRect.xMax,
                nameRect.yMax + (authorRect.height - SmallIconSize) / 2f,
                SmallIconSize,
                SmallIconSize );
            var sourceIconsRect = new Rect(
                nameRect.xMax,
                canvas.yMin,
                ( SmallIconSize + SmallMargin ) * Versions.Count,
                nameRect.height);

            var deemphasized = deemphasizeFiltered && !filter.NullOrEmpty() && MatchesFilter( filter ) <= 0;
            GUI.color = ( deemphasized || !Selected.enabled ) ? Color.Desaturate() : Color;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            Widgets.Label( nameRect, TrimmedName.Truncate( nameRect.width, _modNameTruncationCache ) );
            if ( Mouse.IsOver( nameRect ) && TrimmedName !=
                 TrimmedName.Truncate( nameRect.width, _modNameTruncationCache ) )
                TooltipHandler.TipRegion( nameRect, TrimmedName );

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.grey;
            Widgets.Label(authorRect, Selected.Author);
            GUI.color = Color.white;
            DoSourceButtons(sourceIconsRect);

            DoModIssuesIcon( issueRect );

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // floatmenu
            if ( Event.current.type == EventType.MouseUp &&
                 Event.current.button == 1 &&
                 Mouse.IsOver( canvas ) &&
                 !Mouse.IsOver( issueRect ) &&
                 !Mouse.IsOver( sourceIconsRect ) )
                DoModActionFloatMenu();
        }

        public override bool IsCoreMod => Selected?.IsCoreMod ?? false;

        public override bool IsExpansion => !IsCoreMod && ( Selected?.Official ?? false );

        public override bool IsModManager
        {
            get
            {
                if ( Selected == null )
                    return false;

                return Selected.SamePackageId("Fluffy.ModManager");
            }
        }
        public string GetVersionTip( ModMetaData mod )
        {
            if ( mod.VersionCompatible )
                return I18n.CurrentVersion;
            return I18n.DifferentVersion( mod );
        }

        internal virtual void DoSourceButtons(Rect canvas)
        {
            Rect iconRect = new Rect(
                canvas.xMax - SmallIconSize,
                canvas.yMin,
                SmallIconSize,
                SmallIconSize ).CenteredOnYIn( canvas );

            var singleVersion = VersionsOrdered.Count() == 1;
            foreach ( var mod in VersionsOrdered )
            {
                var icon = mod.Source.GetIcon();
                var color = mod.VersionCompatible ? Color.white : Color.red;
                GUI.color = color;
                if ( singleVersion )
                    GUI.DrawTexture( iconRect, icon );
                else
                {
                    if ( Widgets.ButtonImage( iconRect, icon, mod == Selected ? color : color.Desaturate() ) )
                        Selected = mod;
                }

                TooltipHandler.TipRegion( iconRect, () => GetVersionTip( mod ), mod.GetHashCode() );
                iconRect.x -= SmallIconSize + SmallMargin;
            }
        }

        internal override void DoModActionButtons( Rect canvas )
        {
            Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
            canvas = canvas.ContractedBy(SmallMargin / 2f);

            if (IsCoreMod)
                return;

            var iconRect = new Rect(
                canvas.xMax - IconSize,
                canvas.yMin,
                IconSize,
                IconSize);

            if ( ModListManager.ListsFor( this ).Count < ModListManager.ModLists.Count )
            {
                if ( ButtonIcon( ref iconRect, File, I18n.AddToModList, Status_Plus ) )
                    ModListManager.DoAddToModListFloatMenu( this );
            }

            if ( ModListManager.ListsFor( this ).Any() )
            {
                if ( ButtonIcon( ref iconRect, File, I18n.RemoveFromModList, Status_Cross,
                    mouseOverColor: Color.red ) )
                    ModListManager.DoRemoveFromModListFloatMenu( this );
            }
            
            if (Selected.Source == ContentSource.SteamWorkshop)
            {
                if ( ButtonIcon( ref iconRect, Steam, I18n.UnSubscribe, Status_Cross, Direction8Way.NorthWest,
                    Color.red ) )
                    Workshop.Unsubscribe( Selected );
                if ( ButtonIcon( ref iconRect, Folder, I18n.CreateLocalCopy( Selected.Name ), Status_Plus ) )
                    IO.CreateLocalCopy( Selected );
            }
            if (Selected.Source == ContentSource.ModsFolder && !Selected.IsCoreMod)
            {
                if ( ButtonIcon( ref iconRect, Folder, I18n.DeleteLocalCopy( Selected.Name ),
                    Status_Cross, Direction8Way.NorthEast, Color.red ) )
                    IO.DeleteLocal( Selected );
            }
            if (Prefs.DevMode && SteamManager.Initialized && Selected.CanToUploadToWorkshop())
            {
                if (ButtonIcon(ref iconRect, Steam, Verse.Steam.Workshop.UploadButtonLabel( Selected.GetPublishedFileId() ), Status_Up, Direction8Way.NorthWest))
                    Workshop.Upload( Selected );
            }
            if ( ButtonIcon( ref iconRect, Palette, I18n.ChangeColour ) )
            {
                var options = NewOptionsList;
                options.Add( new FloatMenuOption( I18n.ChangeModColour( Name ), () => Find.WindowStack.Add(
                    new Dialog_ColourPicker( Color, color =>
                                                 ModManager.UserData[Selected].Color = color
                     ) ) ) );
                options.Add( new FloatMenuOption( I18n.ChangeButtonColour( Name ), () => Find.WindowStack.Add(
                    new Dialog_ColourPicker( Color, color =>
                                                 ModManager.UserData[this].Color = color
                     ) ) ) );
                FloatMenu( options );
            }
            if ( Selected.HasSettings() && ButtonIcon( ref iconRect, Gear, I18n.ModSettings ) )
                OpenSettingsFor( Selected );
        }

        public void DoModActionFloatMenu()
        {
            var options = NewOptionsList;
            if (ModListManager.ListsFor(this).Count < ModListManager.ModLists.Count)
            {
                options.Add( new FloatMenuOption( I18n.AddToModList,
                    () => ModListManager.DoAddToModListFloatMenu( this ) ) );
            }

            if (ModListManager.ListsFor(this).Any())
            {
                options.Add( new FloatMenuOption( I18n.RemoveFromModList,
                    () => ModListManager.DoRemoveFromModListFloatMenu( this ) ) );
            }

            if (Selected.Source == ContentSource.SteamWorkshop)
            {
                options.Add( new FloatMenuOption( I18n.UnSubscribe, () => Workshop.Unsubscribe( Selected ) ) );
                options.Add( new FloatMenuOption( I18n.CreateLocalCopy( Selected.Name ),
                    () => IO.CreateLocalCopy( Selected ) ) );
            }
            if (Selected.Source == ContentSource.ModsFolder && !Selected.IsCoreMod)
            {
                options.Add( new FloatMenuOption( I18n.DeleteLocalCopy( Selected.Name ),
                    () => IO.DeleteLocal( Selected ) ) );
            }
            if (Prefs.DevMode && SteamManager.Initialized && Selected.CanToUploadToWorkshop())
            {
                options.Add( new FloatMenuOption(
                    Verse.Steam.Workshop.UploadButtonLabel( Selected.GetPublishedFileId() ),
                    () => Workshop.Upload( Selected ) ) );
            }
            options.Add( new FloatMenuOption( I18n.ChangeColour, () =>
            {
                var options2 = NewOptionsList;
                options2.Add( new FloatMenuOption( I18n.ChangeModColour( Name ), () => Find.WindowStack.Add(
                    new Dialog_ColourPicker( Color,
                                             color =>
                        
                                                 ModManager.UserData[Selected].Color = color
                         ) ) ) );
                options2.Add( new FloatMenuOption( I18n.ChangeButtonColour( Name ), () => Find.WindowStack.Add(
                    new Dialog_ColourPicker( Color,
                                             color => ModManager.UserData[this].Color = color
                         ) ) ) );
                FloatMenu(options2);
            } ) );
            if ( Selected.HasSettings() )
                options.Add( new FloatMenuOption( I18n.ModSettings, () => OpenSettingsFor( Selected ) ) );
            if ( Prefs.DevMode )
            {
                options.Add( new FloatMenuOption( "Open mod directory",
                                                  () => Application.OpenURL( Selected.RootDir.FullName ) ) );
            }
            FloatMenu( options );
        }
        
        private List<FloatMenuOption> _titleLinkOptions;

        private List<FloatMenuOption> TitleLinkOptions
        {
            get
            {
                if ( _titleLinkOptions == null )
                {
                    _titleLinkOptions = NewOptionsList;
                    if ( !Selected?.Url.NullOrEmpty() ?? false )
                    {
                        _titleLinkOptions.Add( new FloatMenuOption( I18n.ModHomePage( Selected.Url ),
                            () => Application.OpenURL( Selected.Url ) ) );
                    }
                    if ( Selected?.Source == ContentSource.SteamWorkshop )
                    {
                        var publishedFileId = Selected.GetPublishedFileId();
                        _titleLinkOptions.Add(
                            new FloatMenuOption( I18n.WorkshopPage( Selected.Name ),
                            () => SteamUtility.OpenWorkshopPage( publishedFileId ) ) );
                    }

                    var source = Selected?.UserData()?.Source;
                    if ( Selected?.Source == ContentSource.ModsFolder && source != null )
                    {
                        var publishedFileId = source.GetPublishedFileId();
                        _titleLinkOptions.Add(
                            new FloatMenuOption( I18n.WorkshopPage( source.Name ),
                                                 () => SteamUtility.OpenWorkshopPage( publishedFileId ) ) );
                    }
                }
                return _titleLinkOptions;
            }
        }

        internal override void DoModDetails( Rect canvas )
        {
            var mod = Selected;
            if ( !mod.PreviewImage.NullOrBad() )
            {
                DoLabel( ref canvas, I18n.Preview );
                var width = mod.PreviewImage.width;
                var height = mod.PreviewImage.height;
                var scale = canvas.width / width;
                var viewRect = new Rect(
                    canvas.xMin,
                    canvas.yMin,
                    width * scale,
                    height * scale );
                var outRect = new Rect(
                    canvas.xMin,
                    canvas.yMin,
                    canvas.width,
                    Mathf.Min( viewRect.height, canvas.width / GoldenRatio, Page.StandardSize.x * 3/5f / GoldenRatio ) );
                if ( viewRect.height > outRect.height )
                    viewRect.xMax -= 18f;

                Widgets.BeginScrollView( outRect, ref _previewScrollPosition, viewRect );
                GUI.DrawTexture( viewRect, mod.PreviewImage );
                Widgets.EndScrollView();
                canvas.yMin = outRect.yMax + SmallMargin;
            }
            else
            {
                DoLabel( ref canvas, I18n.Details );
            }

            var detailRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                LineHeight * 2 + SmallMargin * 2);

            Widgets.DrawBoxSolid(detailRect, SlightlyDarkBackground);
            canvas.yMin = detailRect.yMax + SmallMargin;
            detailRect = detailRect.ContractedBy(SmallMargin);

            var titleRect = new Rect(
                detailRect.xMin,
                detailRect.yMin,
                (detailRect.width - SmallMargin) / 2f,
                LineHeight);
            var authorRect = new Rect(
                detailRect.xMin,
                titleRect.yMax,
                (detailRect.width - SmallMargin) / 2f,
                LineHeight);
            var targetVersionRect = new Rect(
                titleRect.xMax,
                detailRect.yMin,
                (detailRect.width - SmallMargin) / 2f,
                LineHeight);
            var versionRect = new Rect(
                authorRect.xMax,
                targetVersionRect.yMax,
                (detailRect.width - SmallMargin) / 2f,
                LineHeight);
            Rect labelRect;

            // title
            var labelWidth = MaxWidth( I18n.Title, I18n.Author );
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            labelRect = new Rect(titleRect) { width = labelWidth };
            GUI.color = Color.grey;
            Widgets.Label( labelRect, I18n.Title);
            GUI.color = Color.white;
            titleRect.xMin += labelWidth + SmallMargin;
            Widgets.Label(titleRect, mod.Name.Truncate(titleRect.width));
            if (TitleLinkOptions.Any())
                ActionButton( titleRect, () => FloatMenu( TitleLinkOptions ) );

            // author
            if (!mod.Author.NullOrEmpty())
            {
                labelRect = new Rect(authorRect) { width = labelWidth };
                GUI.color = Color.grey;
                Widgets.Label(labelRect, I18n.Author);
                GUI.color = Color.white;
                authorRect.xMin += labelWidth + SmallMargin;
                Widgets.Label(authorRect, mod.Author.Truncate(authorRect.width));
                ModMetaData steamMod;
                switch ( mod.Source )
                {
                    case ContentSource.ModsFolder:
                        steamMod = mod.UserData()?.Source;
                        break;
                    case ContentSource.SteamWorkshop:
                        steamMod = mod;
                        break;
                    default:
                        steamMod = null;
                        break;
                }
                if (steamMod != null && SteamManager.Initialized )
                {
                    var authorId = Traverse.Create( steamMod.GetWorkshopItemHook() )
                        .Field( "steamAuthor" )
                        .GetValue<CSteamID>();
                    ActionButton( authorRect,
                        () => SteamUtility.OpenUrl( $"https://steamcommunity.com/profiles/{authorId.GetAccountID().m_AccountID}/myworkshopfiles/" ) );
                }
            }

            // target version(s)
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label( targetVersionRect, mod.SupportedVersionsReadOnly.VersionList() );
            TooltipHandler.TipRegion( targetVersionRect, I18n.TargetVersions( mod.SupportedVersionsReadOnly.VersionList() ) );

            // mod version
            if ( Manifest.HasVersion )
            {
                Widgets.Label( versionRect, Manifest.Version.ToString() );
            }

            Text.Anchor = TextAnchor.UpperLeft;
            
            DrawRequirements( ref canvas );

            CrossPromotionManager.HandleCrossPromotions( ref canvas, Selected );

            Widgets.DrawBoxSolid( canvas, SlightlyDarkBackground);
            var descriptionOutRect = canvas.ContractedBy(SmallMargin).Rounded();

            // description
            var height2 = Text.CalcHeight(mod.Description, descriptionOutRect.width);
            var descriptionViewRect = new Rect(
                descriptionOutRect.xMin,
                descriptionOutRect.yMin,
                descriptionOutRect.width,
                height2);
            if (height2 > descriptionOutRect.height)
                descriptionViewRect.xMax -= 18f;
            Widgets.BeginScrollView(descriptionOutRect, ref _descriptionScrollPosition, descriptionViewRect);
            Widgets.Label(descriptionViewRect, mod.Description);
            Widgets.EndScrollView();
        }

        public override IEnumerable<Dependency> Requirements => Manifest?.Requirements ?? Manifest.EmptyRequirementList;

        public void Notify_ResetSelected()
        {
            _selected = null;
        }

        public void Notify_VersionAdded( ModMetaData version, bool active = false )
        {
            Versions.TryAdd( version );
            if ( active && Selected.Active )
                Selected.Active = false;

            version.Active = active;
            Selected = version;
            if ( active )
                ModButtonManager.Notify_ModListChanged();
        }

        public void Notify_VersionRemoved( ModMetaData version )
        {
            Versions.TryRemove( version );
            if ( !Versions.Any() )
            {
                ModButtonManager.TryRemove(this);
                if ( Page_BetterModConfig.Instance.Selected == this )
                    Page_BetterModConfig.Instance.Selected = null;
                return;
            }
            if ( Selected == version )
                _selected = null;
            Selected.Active = version.Active;   
        }
    }
}