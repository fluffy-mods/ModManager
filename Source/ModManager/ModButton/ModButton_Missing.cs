// ModButton_Missing.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static ModManager.Constants;
using static ModManager.Resources;

namespace ModManager
{
    public class ModButton_Missing: ModButton
    {
        private string _name;
        public override string Name => _name;
        private string _identifier;

        public ModButton_Missing(string id, string name)
        {
            _identifier = id;
            _name = name;
        }

        public override string Identifier => _identifier;


        public override bool MatchesIdentifier( string identifier )
        {
            return false;
        }

        public override bool Active
        {
            get => true;
            set
            {
                if ( value == false )
                    ModButtonManager.TryRemove( this );
            }
        }

        public override void DoModButton( Rect canvas, bool alternate = false, Action clickAction = null, Action doubleClickAction = null,
            bool deemphasizeFiltered = false, string filter = null )
        {
            base.DoModButton(canvas, alternate, clickAction, doubleClickAction, deemphasizeFiltered, filter);
            canvas = canvas.ContractedBy( SmallMargin / 2f);

            /**
             * NAME                   
             */
            var nameRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width - SmallIconSize * 2 - SmallMargin,
                canvas.height * 2 / 3f);

            var deemphasized = deemphasizeFiltered && !filter.NullOrEmpty() && !MatchesFilter(filter);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            GUI.color = deemphasized ? Color.grey.Desaturate() : Color.grey;
            Widgets.Label(nameRect, Name.Truncate(nameRect.width, _modNameTruncationCache));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Mouse.IsOver(nameRect) && Name != Name.Truncate(nameRect.width, _modNameTruncationCache))
                TooltipHandler.TipRegion(nameRect, Name);
        }

        public override bool IsCoreMod => false;

        internal override void DoModActionButtons( Rect canvas ){}

        internal override void DoModDetails( Rect canvas )
        {
            DoOtherIssues( ref canvas );
        }

        public List<ModIssue> _issues;

        public override IEnumerable<ModIssue> Issues
        {
            get
            {
                if ( _issues == null )
                {
                    _issues = new List<ModIssue>();
                    _issues.Add( ModIssue.MissingMod( this ) );
                }
                return _issues;
            }
        }

        public override void Notify_ResetSelected(){}
    }
}