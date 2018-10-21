// ModButton.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
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
        private VersionStatus? _version;
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
            var button = ModButtonManager.AllButtons.OfType<ModButton_Installed>().FirstOrDefault( mb => mb.Name == mod.Name );
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

        private List<ModIssue> _issues;
        public override void Notify_RecacheIssues()
        {
            _issues = null;
            Manifest?.Notify_RecacheIssues();
        }
        public override IEnumerable<ModIssue> Issues
        {
            get
            {
                if ( _issues == null )
                {
                    _issues = new List<ModIssue>();
                    switch ( Selected.GetVersionStatus().match )
                    {
                        case VersionMatch.DifferentVersion:
                            _issues.Add( ModIssue.DifferentVersion( this ));
                            break;
                        case VersionMatch.DifferentBuild:
                            _issues.Add( ModIssue.DifferentBuild( this )  );
                            break;
                        case VersionMatch.InvalidVersion when !IsCoreMod:
                            _issues.Add( ModIssue.InvalidVersion( this )  );
                            break;
                    }

                    var attributes = ModManager.Attributes[Selected];
                    if ( attributes.Source != null && attributes.SourceHash != attributes.Source.RootDir.GetFolderHash() )
                    {
                        _issues.Add( new ModIssue( Severity.Update, Subject.Other, this, Identifier,
                            I18n.SourceModChanged,
                            () => Resolvers.ResolveUpdateLocalCopy( attributes.Source, Selected ) ) );
                    }

                    if ( Manifest != null )
                        _issues.AddRange( Manifest.Issues );

                    if ( !Active )
                        _issues = _issues
                            .Where( i => i.subject == Subject.Other || i.subject == Subject.Version )
                            .ToList();
                }
                return _issues;
            }
        }
        
        public IEnumerable<ModMetaData> VersionsOrdered => Versions
            .OrderByDescending( mod => mod.Compatibility() )
            .ThenBy( mod => mod.Source );

        public override string Name => Selected?.Name ?? "ERROR :: NOTHING SELECTED";
        public override string Identifier => Selected?.Identifier ?? "ERROR :: NOTHING SELECTED";
        public override bool MatchesIdentifier( string identifier )
        {
            return Selected?.MatchesIdentifier( identifier ) ?? false;
        }

        public override bool Active
        {
            get => Versions.Any( mod => mod.Active );
            set {
                Selected.Active = value;
                ModButtonManager.Notify_Activated( this, value );
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
                _selected = value;
                _version = null;
                _titleLinkOptions = null;
                ModButtonManager.Notify_ModOrderChanged();
            }
        }

        public override Color Color
        {
            get
            {
                // use version colour if set
                if ( ModManager.Attributes[Selected].Color != Color.white )
                    return ModManager.Attributes[Selected].Color;

                // then button colour
                if ( ModManager.Attributes[this].Color != Color.white )
                    return ModManager.Attributes[this].Color;

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
            set => ModManager.Attributes[this].Color = value;
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

            canvas = canvas.ContractedBy(SmallMargin / 2f);

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
            Widgets.Label(nameRect, Selected.Name.Truncate(nameRect.width, _modNameTruncationCache));
            if ( Mouse.IsOver( nameRect ) && Selected.Name != Selected.Name.Truncate( nameRect.width, _modNameTruncationCache ) )
                TooltipHandler.TipRegion( nameRect, Selected.Name );

            if (!Selected.IsCoreMod)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                Widgets.Label(authorRect, Selected.Author);
                GUI.color = Color.white;
                DoSourceButtons(sourceIconsRect);
            }

            DoModIssuesIcon( issueRect );

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        public override bool IsCoreMod => Selected?.IsCoreMod ?? false;

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
                var status = mod.GetVersionStatus();
                GUI.color = status.Color();
                if ( singleVersion )
                    GUI.DrawTexture( iconRect, icon );
                else
                {
                    if ( Widgets.ButtonImage( iconRect, icon,
                        mod == Selected ? status.Color() : status.Color().Desaturate() ) )
                        Selected = mod;
                }

                mod.GetVersionStatus().Tooltip( iconRect );
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
            if (Selected.Source == ContentSource.LocalFolder && !Selected.IsCoreMod)
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
                var options = NewOptions;
                options.Add( new FloatMenuOption( I18n.ChangeModColour( Name ), () => Find.WindowStack.Add(
                    new ColourPicker.Dialog_ColourPicker( Color, color => ModManager.Attributes[Selected].Color = color ) ) ) );
                options.Add( new FloatMenuOption( I18n.ChangeButtonColour( Name ), () => Find.WindowStack.Add(
                    new ColourPicker.Dialog_ColourPicker( Color, color => ModManager.Attributes[this].Color = color ) ) ) );
                FloatMenu( options );
            }
            if ( Selected.HasSettings() && ButtonIcon( ref iconRect, Gear, I18n.ModSettings ) )
                OpenSettingsFor( Selected );
        }

        internal bool Matches(Dependency dep, bool strict = false )
        {
            return base.MatchesFilter( dep.Identifier ) > 0
                   && dep.MatchesVersion( Selected, false );
        }

        private List<FloatMenuOption> _titleLinkOptions;

        private List<FloatMenuOption> TitleLinkOptions
        {
            get
            {
                if ( _titleLinkOptions == null )
                {
                    _titleLinkOptions = NewOptions;
                    if ( !Selected?.Url.NullOrEmpty() ?? false )
                    {
                        _titleLinkOptions.Add( new FloatMenuOption( I18n.ModHomePage( Selected.Url ),
                            () => Application.OpenURL( Selected.Url ) ) );
                    }
                    if ( Selected?.Source == ContentSource.SteamWorkshop )
                    {
                        var publishedFileId = Selected.GetWorkshopItemHook().PublishedFileId;
                        _titleLinkOptions.Add(
                            new FloatMenuOption( I18n.WorkshopPage( Selected.Name ),
                            () => SteamUtility.OpenWorkshopPage( publishedFileId ) ) );
                    }
                }
                return _titleLinkOptions;
            }
        }

        internal override void DoModDetails( Rect canvas )
        {
            var mod = Selected;
            if ( mod.previewImage != null )
            {
                DoLabel( ref canvas, I18n.Preview );
                var width = mod.previewImage.width;
                var height = mod.previewImage.height;
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
                GUI.DrawTexture( viewRect, mod.previewImage );
                Widgets.EndScrollView();
                canvas.yMin = outRect.yMax + SmallMargin;
            }
            else
            {
                DoLabel( ref canvas, I18n.Details );
            }

            if (!mod.IsCoreMod)
            {
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
                var titleLabelRect = new Rect(titleRect) { width = labelWidth };
                GUI.color = Color.grey;
                Widgets.Label(titleLabelRect, I18n.Title);
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
                    if ( mod.Source == ContentSource.SteamWorkshop )
                    {
                        var authorId = Traverse.Create( mod.GetWorkshopItemHook() )
                            .Field( "steamAuthor" )
                            .GetValue<CSteamID>();
                        ActionButton( authorRect,
                            () => SteamUtility.OpenUrl( $"https://steamcommunity.com/profiles/{authorId}/myworkshopfiles/" ) );
                    }
                }

                // targetVersion
                labelWidth = MaxWidth(I18n.TargetVersion, I18n.Version);
                labelRect = new Rect(targetVersionRect) { width = labelWidth };
                GUI.color = Color.grey;
                Widgets.Label(labelRect, I18n.TargetVersion);
                targetVersionRect.xMin += labelWidth + SmallMargin;
                mod.GetVersionStatus().Label(targetVersionRect);
                if ( mod.GetVersionStatus().match != VersionMatch.CurrentVersion )
                    ActionButton( targetVersionRect,
                        () => Resolvers.ResolveFindMod( mod.Name.StripSpaces(), this, replace: true ) );

                // version
                labelRect = new Rect(versionRect) { width = labelWidth };
                GUI.color = Color.grey;
                Widgets.Label(labelRect, I18n.Version);
                versionRect.xMin += labelWidth + SmallMargin;
                var versionIconRect = new Rect( versionRect.xMax - SmallIconSize, 0f, SmallIconSize, SmallIconSize )
                    .CenteredOnYIn( versionRect );

                GUI.color = Manifest.Color;
                GUI.DrawTexture( versionIconRect, Manifest.Icon );
                GUI.color = Color.white;
                TooltipHandler.TipRegion(versionRect, Manifest.Tip);
                if (Manifest.Resolver != null)
                {
                    ActionButton(versionRect, Manifest.Resolver);
                }

                if ( Manifest.Version != null )
                {
                    Widgets.Label( versionRect, Manifest.Version.ToString() );
                }
                else
                {
                    GUI.color = Color.grey;
                    Widgets.Label( versionRect, I18n.Unknown );
                    GUI.color = Color.white;
                }
            }

            if ( Active )
            {
                Manifest?.DoDependencyDetails( ref canvas );
                DoOtherIssues( ref canvas );
            }

            CrossPromotionManager.HandleCrossPromotions( ref canvas, Selected );

            Widgets.DrawBoxSolid( canvas, SlightlyDarkBackground);
            var descriptionOutRect = canvas.ContractedBy(SmallMargin);

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



        public override void Notify_ResetSelected()
        {
            Selected = null;
        }

        public void Notify_VersionAdded( ModMetaData version, bool active = false )
        {
            Versions.TryAdd( version );
            if ( active && Selected.Active )
                Selected.Active = false;

            version.Active = active;
            Selected = version;
            ModButtonManager.Notify_ModOrderChanged();
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

        public void Notify_VersionUpdated( ModMetaData local )
        {
            var old = Versions.FirstOrDefault( m => m.Identifier == local.Identifier );
            if ( Versions.TryRemove( old ) )
                Notify_VersionAdded( local, true );
        }
    }
}