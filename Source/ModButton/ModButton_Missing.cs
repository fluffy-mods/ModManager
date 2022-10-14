// ModButton_Missing.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static ModManager.Constants;

namespace ModManager {
    public class ModButton_Missing: ModButton {
        private readonly string _name;
        public override string Name => _name;
        private readonly string _identifier;
        private readonly ulong _steamWorkshopId;

        public ModButton_Missing(string id, string name, ulong steamWorkshopId) {
            _identifier = id;
            _name = name;
            _steamWorkshopId = steamWorkshopId;
        }

        public override string Identifier => _identifier;
        public override ulong SteamWorkshopId => _steamWorkshopId;


		public override bool SamePackageId(string packageId) {
            return false;
        }

        public override bool Active {
            get => true;
            set {
                if (value == false) {
                    ModButtonManager.TryRemove(this);
                }
            }
        }

        public override Color Color => Color.gray;

        public override void DoModButton(Rect canvas, bool alternate = false, Action clickAction = null, Action doubleClickAction = null,
            bool deemphasizeFiltered = false, string filter = null) {
            base.DoModButton(canvas, alternate, clickAction, doubleClickAction, deemphasizeFiltered, filter);
            canvas = canvas.ContractedBy(SmallMargin / 2f);

            /**
             * NAME                   
             */
            Rect nameRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width - (SmallIconSize * 2) - SmallMargin,
                canvas.height * 2 / 3f);

            bool deemphasized = deemphasizeFiltered && !filter.NullOrEmpty() && MatchesFilter(filter) <= 0;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            GUI.color = deemphasized ? Color.Desaturate() : Color;
            Widgets.Label(nameRect, Name.Truncate(nameRect.width, _modNameTruncationCache));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Mouse.IsOver(nameRect) && Name != Name.Truncate(nameRect.width, _modNameTruncationCache)) {
                TooltipHandler.TipRegion(nameRect, Name);
            }
        }

        internal override void DoModActionButtons(Rect canvas) { }

        internal override void DoModDetails(Rect canvas) {
            DrawRequirements(ref canvas);
        }

        public override IEnumerable<Dependency> Requirements => Manifest.EmptyRequirementList;
        //        {
        //            get
        //            {
        //                if ( _issues == null )
        //                {
        //                    _issues = new List<ModRequirement>();
        //                    _issues.Add( ModRequirement.MissingMod( this ) );
        //                }
        //                return _issues;
        //            }
        //        }
    }
}
