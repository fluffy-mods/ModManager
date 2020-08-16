// CrossPromotion.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.IO;
using Steamworks;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class CrossPromotion
    {
        private SteamUGCDetails_t _details;
        private CallResult<RemoteStorageDownloadUGCResult_t> _callResult;


        private string PreviewPath => Path.Combine( CrossPromotionManager.CachePath, ( Name + "_" + FileId ).SanitizeFileName() + ".jpg" );

        public bool Ready => Preview != null;
        public PublishedFileId_t FileId => _details.m_nPublishedFileId;
        public string Name => _details.m_rgchTitle;
        public string Description => _details.m_rgchDescription;
        public Texture2D Preview { get; private set; }
        public IntVec2 Size => Ready ? new IntVec2( Preview.width, Preview.height ) : IntVec2.Zero;
        public bool ShouldShow => Ready && ( ModManager.Settings.ShowPromotions_NotSubscribed && !Installed ||
                                             ModManager.Settings.ShowPromotions_NotActive && !Active );
        public bool Installed => ModButtonManager.AllButtons.Any( b => b.Name == Name );
        public bool Active => ModButtonManager.ActiveButtons.Any( b => b.Name == Name );

        public int NormalizedWidth( int height )
        {
            if ( !Ready )
                return 0;
            return Preview.width * height / Preview.height;
        }

        public CrossPromotion( SteamUGCDetails_t details )
        {
            _details = details;
            _callResult = CallResult<RemoteStorageDownloadUGCResult_t>.Create( OnPreviewDownloaded );

            if ( File.Exists( PreviewPath ) && new FileInfo( PreviewPath ).Length == details.m_nPreviewFileSize )
                LoadPreview();
            else
                FetchPreview( details.m_hPreviewFile );
        }

        private void LoadPreview()
        {
            try
            {
                Preview = new Texture2D( 1, 1, TextureFormat.ARGB32, false );
                Preview.LoadImage( File.ReadAllBytes( PreviewPath ) );
            }
            catch ( Exception ex )
            {
                Log.Error( ex.ToString() );
                Preview = null;
            }
        }

        private void OnPreviewDownloaded( RemoteStorageDownloadUGCResult_t result, bool failure )
        {
            Debug.Log($"Received preview image for {_details.m_rgchTitle}: failure: {failure}, result: {result.m_eResult}");
            if (result.m_eResult != EResult.k_EResultOK)
                return;
            LoadPreview();
            CrossPromotionManager.Notify_UpdateRelevantMods();
        }
        private void FetchPreview( UGCHandle_t previewHandle )
        {
            Debug.Log( $"Fetching preview image for {_details.m_rgchTitle}..."  );
            var request = SteamRemoteStorage.UGCDownloadToLocation( previewHandle, PreviewPath, 0 );
            _callResult.Set( request );
        }
    }
}