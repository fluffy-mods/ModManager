// CrossPromotionManager.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Noise;
using static ModManager.Constants;
using static ModManager.Resources;

namespace ModManager
{
    public static class CrossPromotionManager
    {
        private static HashSet<PublishedFileId_t> _currentlyFetchingFiles = new HashSet<PublishedFileId_t>();
        private static Dictionary<AccountID_t, List<CrossPromotion>> _modsForAuthor = new Dictionary<AccountID_t, List<CrossPromotion>>();
        private static Dictionary<PublishedFileId_t, AccountID_t> _authorForMod = new Dictionary<PublishedFileId_t, AccountID_t>();
        private static CallResult<SteamUGCQueryCompleted_t> _userModsCallResult;
        private static CallResult<SteamUGCQueryCompleted_t> _modDetailsCallResult;
        private static AppId_t _appId = AppId_t.Invalid;
        private static Vector2 _scrollPosition = Vector2.zero;
        private static bool _enabled = false;

        public static void Update()
        {
            if (_enabled)
                SteamAPI.RunCallbacks();
        }


        private static  string _cachePath;
        internal static bool   cachePathOverriden;

        internal static string CachePath
        {
            get
            {
                if ( _cachePath != null )
                    return _cachePath;

                string path;
                if ( GenCommandLine.TryGetCommandLineArg( "cross-promotions-path", out path ) )
                {
                    path = path.TrimEnd( '\\', '/' );
                    if ( path == "" )
                        path = Path.DirectorySeparatorChar.ToString() ?? "";
                    Log.Message( "CrossPromotion preview images location overriden: " + path );
                }
                else if ( ModManager.Settings.UseTempFolderForCrossPromotions )
                {
                    path = Path.Combine( Path.GetTempPath(), "CrossPromotions" );
                }
                else
                {
                    path = Path.Combine( GenFilePaths.SaveDataFolderPath, "CrossPromotions" );
                }

                var dir = new DirectoryInfo( path );
                if ( !dir.Exists )
                    dir.Create();
                return path;
            }
        }

        public static int? _cacheCount;
        public static int CacheCount
        {
            get
            {
                return _cacheCount ??= new DirectoryInfo( CachePath ).GetFiles().Length;
            }
        }

        public static long? _cacheSize;

        public static long CacheSize
        {
            get
            {
                return _cacheSize ??= new DirectoryInfo( CachePath ).GetFiles().Sum( f => f.Length );
            }
        }

        internal static void DeleteCache()
        {
            Find.WindowStack.Add( new Dialog_MessageBox(
                                      I18n.ConfirmDeletingCrossPromotionCache(
                                          CachePath, CacheCount, CacheSize ),
                                      "Confirm".Translate(),
                                      () =>
                                      {
                                          var dir = new DirectoryInfo( CachePath );
                                          dir.Delete( true );
                                          Notify_CrossPromotionPathChanged();
                                      }, "Cancel".Translate(), buttonADestructive: true ) );
        }

        public static void Notify_CrossPromotionPathChanged()
        {
            _cachePath = null;
            _cacheCount         = null;
            _cacheSize          = null;
        }

        static CrossPromotionManager()
        {
            if (Verse.Steam.SteamManager.Initialized)
            {
                _enabled = true;
                _userModsCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUserModsReceived);
                _modDetailsCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnModDetailsReceived);
            }
        }

        public static AppId_t AppID
        {
            get
            {
                if ( _enabled && _appId == AppId_t.Invalid )
                    _appId = SteamUtils.GetAppID();
                return _appId;
            }
        }

        public static List<CrossPromotion> PromotionsForAuthor( AccountID_t author )
        {
            if ( _modsForAuthor.TryGetValue( author, out var mods ) )
                return mods;
            mods = new List<CrossPromotion>();
            _modsForAuthor.Add( author, mods );
            FetchModsForAuthor( author );
            return mods;
        }

        public static AccountID_t? AuthorForMod( PublishedFileId_t fileId )
        {
            if ( _authorForMod.TryGetValue( fileId, out var author ) )
                return author;
            if ( !_currentlyFetchingFiles.Contains( fileId ) )
                FetchModDetails( fileId );
            return null;
        }

        private static List<CrossPromotion> RelevantPromotions { get; set; }

        public static void Notify_UpdateRelevantMods()
        {
            RelevantPromotions = null;
        }
        public static bool HandleCrossPromotions( ref Rect canvas, ModMetaData mod )
        {
            if ( !_enabled )
                return false;

            if ( !ModManager.Settings.ShowPromotions || !Manifest.For( mod ).showCrossPromotions )
                return false;

            if ( mod.GetPublishedFileId() == PublishedFileId_t.Invalid )
                return false;

            var author = AuthorForMod( mod.GetPublishedFileId() );
            if ( author == null )
                return false;

            RelevantPromotions ??= PromotionsForAuthor( author.Value )?.Where( p => p.ShouldShow ).ToList();
            if ( RelevantPromotions.NullOrEmpty() )
                return false;

            if ( Widgets.ButtonImage(
                new Rect( canvas.xMax - SmallIconSize, canvas.yMin, SmallIconSize, SmallIconSize ), Gear, Color.grey,
                GenUI.MouseoverColor ) )
            {
                Utilities.OpenSettingsFor( ModManager.Instance );
            }
            Utilities.DoLabel( ref canvas, I18n.PromotionsFor( mod.Author ) );
            DrawCrossPromotions( ref canvas, RelevantPromotions );
            return true;
        }

        private static void DrawCrossPromotions( ref Rect canvas, IEnumerable<CrossPromotion> promotions )
        {
            var backgroundRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                PromotionsHeight );
            var outRect = backgroundRect.ContractedBy( SmallMargin / 2 );
            var height = (int)outRect.height;
            var width = promotions.Sum(p => p.NormalizedWidth(height)) + (promotions.Count() - 1) * SmallMargin;
            if ( width > outRect.width )
            {
                height -= 16;
                // recalculate total width
                width = promotions.Sum(p => p.NormalizedWidth(height)) + (promotions.Count() - 1) * SmallMargin;
            }
            var viewRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                width,
                height );
            var pos = viewRect.min;
            canvas.yMin += PromotionsHeight + SmallMargin;

            Widgets.DrawBoxSolid(backgroundRect, SlightlyDarkBackground);
            if ( Mouse.IsOver( outRect ) && Event.current.type == EventType.ScrollWheel )
                _scrollPosition.x += Event.current.delta.y * Constants.ScrollSpeed;
            Widgets.BeginScrollView( outRect, ref _scrollPosition, viewRect );
            foreach ( var promotion in promotions )
            {
                var normalizedWidth = promotion.NormalizedWidth( height );
                var rect = new Rect( pos.x, pos.y, normalizedWidth, height );
                if (Widgets.ButtonImage( rect, promotion.Preview, new Color( .9f, .9f, .9f ), Color.white))
                {
                    if ( !promotion.Installed )
                    {
                        var options = Utilities.NewOptionsList;
                        options.Add( new FloatMenuOption( I18n.Subscribe( promotion.Name ),
                                                          () => Workshop.Subscribe( promotion.FileId ) ) );
                        options.Add( new FloatMenuOption( I18n.WorkshopPage( promotion.Name ),
                                                          () => SteamUtility.OpenWorkshopPage( promotion.FileId ) ) );
                        Utilities.FloatMenu( options );
                    }
                    else
                    {
                        var button = ModButtonManager.AllButtons.FirstOrDefault( b => b.Name == promotion.Name );
                        if ( button != null )
                            Page_BetterModConfig.Instance.Selected = button;
                    }
                }
                TooltipHandler.TipRegion( rect, promotion.Name + "\n\n" + promotion.Description );
                pos.x += normalizedWidth + SmallMargin;
            }
            Widgets.EndScrollView();
        }

        private static void OnUserModsReceived( SteamUGCQueryCompleted_t result, bool failure )
        {
            Debug.Log($"Received user mods: failure: {failure}, result: {result.m_eResult}, count: {result.m_unNumResultsReturned}");
            CSteamID author = CSteamID.Nil;
            List<CrossPromotion> promotions = new List<CrossPromotion>();
            for (uint i = 0; i < result.m_unNumResultsReturned; i++)
            {
                if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out var details))
                {
                    Debug.TracePromotions($" - {details.m_rgchTitle} ({details.m_ulSteamIDOwner}");
                    author = new CSteamID( details.m_ulSteamIDOwner );
                    promotions.Add(new CrossPromotion(details));
                }
            }
            if ( author != CSteamID.Nil )
            {
                _modsForAuthor[author.GetAccountID()] = promotions;
                Notify_UpdateRelevantMods();
            }
            SteamUGC.ReleaseQueryUGCRequest( result.m_handle );
        }

        private static void OnModDetailsReceived( SteamUGCQueryCompleted_t result, bool failure )
        {
            Debug.Log( $"Received mod details: failure: {failure}, result: {result.m_eResult}, count: {result.m_unNumResultsReturned}" );
            for ( uint i = 0; i < result.m_unNumResultsReturned; i++ )
            {
                if ( SteamUGC.GetQueryUGCResult( result.m_handle, i, out var details ) )
                {
                    Debug.TracePromotions($" - {details.m_rgchTitle} ({details.m_ulSteamIDOwner}");
                    var author = new CSteamID( details.m_ulSteamIDOwner ).GetAccountID();
                    _authorForMod.Add( details.m_nPublishedFileId, author );
                    _currentlyFetchingFiles.Remove( details.m_nPublishedFileId );
                }
            }
            SteamUGC.ReleaseQueryUGCRequest( result.m_handle );
        }

        private static void FetchModDetails( PublishedFileId_t fileId )
        {
            Debug.TracePromotions( $"Fetching details for {fileId}..." );
            _currentlyFetchingFiles.Add( fileId );
            var query = SteamUGC.CreateQueryUGCDetailsRequest( new[] {fileId}, 1 );
            var request = SteamUGC.SendQueryUGCRequest( query );
            _modDetailsCallResult.Set( request );
        }

        private static void FetchModsForAuthor( AccountID_t author )
        {
            Debug.TracePromotions( $"Fetching mods for {author}..." );
            var query = SteamUGC.CreateQueryUserUGCRequest( 
                author, 
                EUserUGCList.k_EUserUGCList_Published,
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_UsableInGame,
                EUserUGCListSortOrder.k_EUserUGCListSortOrder_VoteScoreDesc, 
                AppID, AppID, 1 );
            SteamUGC.AddRequiredTag( query, VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor );
            var request = SteamUGC.SendQueryUGCRequest( query );
            _userModsCallResult.Set( request );
        }
    }
}