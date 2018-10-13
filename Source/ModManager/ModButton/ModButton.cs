// ModButton.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static ModManager.Constants;

namespace ModManager
{
    public abstract class ModButton
    {
        private ModButton _focus;
        public abstract string Name { get; }
        public abstract string Identifier { get; }
        public abstract bool MatchesIdentifier( string identifier );
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

        public abstract bool IsCoreMod { get; }

        public virtual int MatchesFilter( string filter )
        {
            if ( filter.NullOrEmpty() || Name.ToUpper().Contains( filter.ToUpper() ) )
            {
                return 1;
            }
            return 0;
        }

        internal abstract void DoModActionButtons( Rect canvas );
        internal abstract void DoModDetails( Rect canvas );
        public virtual int LoadOrder => -1;
        public virtual int SortOrder => -1;
        public abstract IEnumerable<ModIssue> Issues { get; }
        internal virtual void HandleInteractions(Rect canvas, Action clickAction, Action doubleClickAction)
        {
            if (Mouse.IsOver(canvas))
            {
                Widgets.DrawHighlight(canvas);
                if (Event.current.type == EventType.mouseDown)
                {
                    _focus = this;
                    if (Event.current.clickCount == 2)
                    {
                        doubleClickAction?.Invoke();
                    }
                }
                if (Event.current.type == EventType.mouseUp && _focus == this)
                {
                    clickAction?.Invoke();
                }
            }
        }

        private int _issueIndex = 0;
        internal virtual void DoModIssuesIcon( Rect canvas )
        {
            if ( !Issues.Any() )
                return;

            TooltipHandler.TipRegion( canvas, string.Join( "\n", Issues.Select( i => i.tip ).ToArray() ) );
            var worstIssue = Issues.MaxBy( i => i.severity );
            GUI.color = worstIssue.Color;
            GUI.DrawTexture( canvas, worstIssue.Icon );
            GUI.color = Color.white;
        }


        internal virtual void DoOtherIssues(ref Rect canvas)
        {
            var issues = Issues.Where(issue => issue.subject == Subject.LoadOrder || issue.subject == Subject.Other);
            if (!issues.Any())
                return;

            Utilities.DoLabel(ref canvas, I18n.Problems);
            var outRect = new Rect(canvas) { height = issues.Count() * LineHeight + SmallMargin * 2f };
            Widgets.DrawBoxSolid(outRect, Resources.SlightlyDarkBackground);
            canvas.yMin += outRect.height + SmallMargin;
            outRect = outRect.ContractedBy(SmallMargin);
            var issueRect = new Rect(
                outRect.xMin,
                outRect.yMin,
                outRect.width,
                LineHeight);

            foreach (var issue in issues)
            {
                var iconRect = new Rect(issueRect.xMin, issueRect.yMin, SmallIconSize, SmallIconSize)
                    .CenteredOnYIn(issueRect);
                var labelRect = new Rect(issueRect);
                labelRect.xMin += SmallIconSize + SmallMargin;
                GUI.color = issue.Color;
                GUI.DrawTexture(iconRect, Resources.Warning);
                Widgets.Label(labelRect, issue.tip);
                if (issue.resolver != null)
                    Utilities.ActionButton(issueRect, issue.resolver);
                issueRect.y += LineHeight;
            }
            GUI.color = Color.white;
        }

        public static void Notify_ModButtonSizeChanged()
        {
            _modNameTruncationCache.Clear();
        }
        internal static Dictionary<string, string> _modNameTruncationCache = new Dictionary<string, string>();
        public virtual void Notify_ResetSelected(){}
        public virtual void Notify_RecacheIssues(){}
    }
}