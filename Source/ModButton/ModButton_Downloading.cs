// ModButton_Downloading.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using Verse;
using static ModManager.Constants;

namespace ModManager {
    public class ModButton_Downloading: ModButton {
        public ModButton_Downloading() { }

        public ModButton_Downloading(PublishedFileId_t pfid) {
            Debug.Log($"ModButton_Downloading({pfid})");
            _identifier = pfid;
        }

        private PublishedFileId_t _identifier;
        public override string Name => $"Workshop mod {Identifier}";
        public override string Identifier => _identifier.ToString();
        public override ulong SteamWorkshopId => 0;
		public override int SortOrder => 9;

        public override bool SamePackageId(string packageId) {
            return Identifier == packageId;
        }

        public override bool Active { get; set; }
        public override Color Color => Color.white;

        public override void DoModButton(Rect canvas, bool alternate = false, Action clickAction = null, Action doubleClickAction = null,
            bool deemphasizeFiltered = false, string filter = null) {

            base.DoModButton(canvas, alternate, clickAction, doubleClickAction, deemphasizeFiltered, filter);
            canvas = canvas.ContractedBy(SmallMargin / 2f);

            /**
             * NAME                   
             * progress
             */

            Rect nameRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                canvas.height * 3 / 5f);
            Rect progressRect = new Rect(
                canvas.xMin,
                nameRect.yMax,
                canvas.width,
                canvas.height * 2 / 5f);

            Widgets.Label(nameRect, Name.Truncate(nameRect.width, _modNameTruncationCache));

            if (Mouse.IsOver(nameRect) && Name != Name.Truncate(nameRect.width, _modNameTruncationCache)) {
                TooltipHandler.TipRegion(nameRect, Name);
            }

            bool downloading = SteamUGC.GetItemDownloadInfo( _identifier, out ulong done, out ulong total );
            if (downloading && total > 0) {
                Widgets.FillableBar(progressRect.ContractedBy(SmallMargin / 2f), (float) ((double) done / total));
            } else {
                GUI.color = Color.grey;
                Text.Font = GameFont.Tiny;
                Widgets.Label(progressRect, I18n.DownloadPending);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

            }
        }

        internal override void DoModActionButtons(Rect canvas) { }

        internal override void DoModDetails(Rect canvas) { }

        public override IEnumerable<Dependency> Requirements => Manifest.EmptyRequirementList;
    }
}
