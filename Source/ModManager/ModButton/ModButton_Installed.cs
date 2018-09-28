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

namespace ModManager
{
    public class ModButton_Installed : ModButton
    {
        private VersionStatus? _version;
        private ModMetaData _selected;
        private Vector2 _previewScrollPosition = Vector2.zero;
        private Vector2 _descriptionScrollPosition = Vector2.zero;
        public override int SortOrder => Selected.Compatibility();

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
                    switch ( Selected.VersionStatus().match )
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

                    if ( Manifest != null )
                        _issues.AddRange( Manifest.Issues );

                    if ( IsCoreMod && Selected.LoadOrder() != 0 )
                        _issues.Add( ModIssue.CoreNotFirst( this ) );
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

            var deemphasized = deemphasizeFiltered && !filter.NullOrEmpty() && !MatchesFilter(filter);
            GUI.color = ( deemphasized || !Selected.enabled ) ? Color.white.Desaturate() : Color.white;

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
                var status = mod.VersionStatus();
                GUI.color = status.Color();
                if ( singleVersion )
                    GUI.DrawTexture( iconRect, icon );
                else
                {
                    if ( Widgets.ButtonImage( iconRect, icon,
                        mod == Selected ? status.Color() : status.Color().Desaturate() ) )
                        Selected = mod;
                }

                mod.VersionStatus().Tooltip( iconRect );
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

            if (Selected.Source == ContentSource.SteamWorkshop)
            {
                if ( Utilities.ButtonIcon( ref iconRect, Steam, I18n.UnSubscribe, Status_Cross, Direction8Way.NorthWest,
                    Color.red ) )
                    Workshop.Unsubscribe( Selected );
                if ( Utilities.ButtonIcon( ref iconRect, Folder, I18n.CreateLocalCopy( Selected.Name ), Status_Plus ) )
                    IO.CreateLocalCopy( Selected );
            }
            if (Selected.Source == ContentSource.LocalFolder && !Selected.IsCoreMod)
            {
                if ( Utilities.ButtonIcon( ref iconRect, Folder, I18n.DeleteLocalCopy( Selected.Name ),
                    Status_Cross, Direction8Way.NorthEast, Color.red ) )
                    IO.DeleteLocal( Selected );
            }
            if (Prefs.DevMode && SteamManager.Initialized && Selected.CanToUploadToWorkshop())
            {
                if (Utilities.ButtonIcon(ref iconRect, Steam, Verse.Steam.Workshop.UploadButtonLabel( Selected.GetPublishedFileId() ), Status_Up, Direction8Way.NorthWest))
                    Workshop.Upload( Selected );
            }
        }

        internal bool Matches(Dependency dep, bool strict = false )
        {
            return MatchesFilter( dep.Identifier )
                   && dep.MatchesVersion( Selected, false );
        }

        private List<FloatMenuOption> _titleLinkOptions;

        private List<FloatMenuOption> TitleLinkOptions
        {
            get
            {
                if ( _titleLinkOptions == null )
                {
                    _titleLinkOptions = Utilities.NewOptions;
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
                Utilities.DoLabel( ref canvas, I18n.Preview );
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
                    Mathf.Min( viewRect.height, canvas.width / GoldenRatio ) );
                if ( viewRect.height > outRect.height )
                    viewRect.xMax -= 18f;

                Widgets.BeginScrollView( outRect, ref _previewScrollPosition, viewRect );
                GUI.DrawTexture( viewRect, mod.previewImage );
                Widgets.EndScrollView();
                canvas.yMin = outRect.yMax + SmallMargin;
            }
            else
            {
                Utilities.DoLabel( ref canvas, I18n.Details );
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
                var labelWidth = Utilities.MaxWidth( I18n.Title, I18n.Author );
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                var titleLabelRect = new Rect(titleRect) { width = labelWidth };
                GUI.color = Color.grey;
                Widgets.Label(titleLabelRect, I18n.Title);
                GUI.color = Color.white;
                titleRect.xMin += labelWidth + SmallMargin;
                Widgets.Label(titleRect, mod.Name.Truncate(titleRect.width));
                if (TitleLinkOptions.Any())
                    Utilities.ActionButton( titleRect, () => Utilities.FloatMenu( TitleLinkOptions ) );

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
                        Utilities.ActionButton( authorRect,
                            () => SteamUtility.OpenUrl( $"https://steamcommunity.com/profiles/{authorId}/myworkshopfiles/" ) );
                    }
                }

                // targetVersion
                labelWidth = Utilities.MaxWidth(I18n.TargetVersion, I18n.Version);
                labelRect = new Rect(targetVersionRect) { width = labelWidth };
                GUI.color = Color.grey;
                Widgets.Label(labelRect, I18n.TargetVersion);
                targetVersionRect.xMin += labelWidth + SmallMargin;
                mod.VersionStatus().Label(targetVersionRect);
                if ( mod.VersionStatus().match != VersionMatch.CurrentVersion )
                    Utilities.ActionButton( targetVersionRect,
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
                    Utilities.ActionButton(versionRect, Manifest.Resolver);
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
            if ( active && Selected.Active )
                Selected.Active = false;

            version.Active = active;
            Selected = version;
            ModButtonManager.Notify_ModOrderChanged();
        }

        public void Notify_VersionRemoved( ModMetaData version )
        {
            Versions.TryRemove( version );
            if ( Selected == version )
            {
                _selected = null;
                if (!Versions.Any())
                {
                    ModButtonManager.TryRemove(this);
                    Page_BetterModConfig.Instance.Selected = ModButtonManager.AllButtons.First();
                }
            }
            else
                Selected.Active = version.Active;   
        }
    }
}