// ModButton.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static ModManager.Constants;

namespace ModManager
{
    public abstract class ModButton
    {
        private ModButton _focus;
        private string _trimmedName;
        public virtual string TrimmedName
        {
            get
            {
                if ( _trimmedName.NullOrEmpty() )
                    _trimmedName = Utilities.TrimModName( Name );
                return _trimmedName;
            }
        }
        public abstract string Name { get; }
        public abstract string Identifier { get; }
        public abstract bool SamePackageId( string packageId );
        public abstract bool Active { get; set; }
        public virtual Color Color { get; set; }
        public virtual void DoModButton( Rect canvas, bool alternate = false, Action clickAction = null,
            Action doubleClickAction = null, bool deemphasizeFiltered = false, string filter = null )
        {

#if DEBUG
            clickAction += () => Debug.Log( "clicked: " + Name );
            doubleClickAction += () => Debug.Log( "doubleClicked: " + Name );
#endif

            if ( alternate )
                Widgets.DrawBoxSolid( canvas, Resources.SlightlyDarkBackground );
            if ( Page_BetterModConfig.Instance.Selected == this )
            {
                if ( Page_BetterModConfig.Instance.SelectedHasFocus )
                    Widgets.DrawHighlightSelected( canvas );
                else
                    Widgets.DrawHighlight( canvas );
            }
            if ( !DraggingManager.Dragging )
                HandleInteractions( canvas, clickAction, doubleClickAction );
        }

        public virtual bool IsCoreMod => false;
        public virtual bool IsExpansion => false;
        public virtual bool IsModManager => false;

        public virtual int MatchesFilter( string filter )
        {
            if ( filter.NullOrEmpty() )
                return 1;
            if ( ModManager.Settings.TrimTags && TrimmedName.ToLower().Contains( filter.ToLower() ) ||
                !ModManager.Settings.TrimTags && Name.ToLower().Contains( filter.ToLower( ) ) )
            {
                return 1;
            }
            return 0;
        }

        internal abstract void DoModActionButtons( Rect canvas );
        internal abstract void DoModDetails( Rect canvas );
        public virtual int LoadOrder => -1;
        public virtual int SortOrder => -1;
        public abstract IEnumerable<Dependency> Requirements { get; }
        internal virtual void HandleInteractions(Rect canvas, Action clickAction, Action doubleClickAction)
        {
            if (Mouse.IsOver(canvas))
            {
                Widgets.DrawHighlight(canvas);
                if (Event.current.type == EventType.MouseDown)
                {
                    _focus = this;
                    if (Event.current.clickCount == 2)
                    {
                        doubleClickAction?.Invoke();
                    }
                }
                if (Event.current.type == EventType.MouseUp && _focus == this)
                {
                    clickAction?.Invoke();
                }
            }
        }

        private List<Dependency> _relevantIssues;
        protected virtual int                     SeverityThreshold => 2;
        protected List<Dependency> RelevantIssues
        {
            get
            {
                return _relevantIssues ??= Requirements.Where( i => i.Severity >= SeverityThreshold ).ToList();
            }
        }

        private string _relevantIssuesString;
        protected string RelevantIssuesString
        {
            get
            {
                return _relevantIssuesString ??= RelevantIssues.OrderBy( i => i.Severity )
                                                               .Select( i => i.Tooltip.Colorize( i.Color ) )
                                                               .StringJoin( "\n" );
            }
        }

        public virtual void Notify_RecacheIssues()
        {
            _relevantIssues       = null;
            _relevantIssuesString = null;
        }

        internal virtual void DoModIssuesIcon( Rect canvas )
        {
            if ( !RelevantIssues.Any() )
                return;

            var worst = Requirements.MaxBy( d => d.Severity );
            GUI.color = worst.Color;
            GUI.DrawTexture( canvas, Resources.Warning );
            GUI.color = Color.white;
            TooltipHandler.TipRegion( canvas, RelevantIssuesString );
        }

        internal virtual void DrawRequirements(ref Rect canvas)
        {
            var severityThreshold = ModManager.Settings.ShowSatisfiedRequirements ? 0 : 1;
            var relevantIssues = Requirements.Where( i => i.Severity >= severityThreshold );
            if ( !relevantIssues.Any() )
                return;

            Utilities.DoLabel(ref canvas, I18n.Dependencies );
            var outRect = new Rect(canvas) { height = relevantIssues.Count() * LineHeight + SmallMargin * 2f };
            Widgets.DrawBoxSolid(outRect, Resources.SlightlyDarkBackground);
            canvas.yMin += outRect.height + SmallMargin;
            outRect = outRect.ContractedBy(SmallMargin);
            var issueRect = new Rect(
                outRect.xMin,
                outRect.yMin,
                outRect.width,
                LineHeight);

            foreach (var issue in relevantIssues )
            {
                var iconRect = new Rect(issueRect.xMin, issueRect.yMin, SmallIconSize, SmallIconSize)
                    .CenteredOnYIn(issueRect);
                var labelRect = new Rect(issueRect);
                labelRect.xMin += SmallIconSize + SmallMargin;
                GUI.color = issue.Color;
                GUI.DrawTexture( iconRect, issue.StatusIcon );
                Widgets.Label( labelRect, issue.Tooltip );
                if ( issue.Resolvers.Any() )
                    Utilities.ActionButton( issueRect, () => issue.OnClicked( null ) ); // todo: reference to window? Why?
                issueRect.y += LineHeight;
            }
            GUI.color = Color.white;
        }

        public static void Notify_ModButtonSizeChanged()
        {
            _modNameTruncationCache.Clear();
        }
        internal static Dictionary<string, string> _modNameTruncationCache = new Dictionary<string, string>();
    }
}