// Copyright Karel Kroeze, 2020-2021.
// ModManager/ModManager/CrossPromotionManager.cs

using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;
using static ModManager.Constants;
using static ModManager.Resources;

namespace ModManager {
    public static class CrossPromotionManager {
        private static AppId_t _appId = AppId_t.Invalid;

        private static readonly Dictionary<PublishedFileId_t, AccountID_t> _authorForMod =
            new Dictionary<PublishedFileId_t, AccountID_t>();

        public static int? _cacheCount;


        private static string _cachePath;

        public static  long?                                _cacheSize;
        private static readonly HashSet<PublishedFileId_t>           _currentlyFetchingFiles = new HashSet<PublishedFileId_t>();
        private static readonly bool                                 _enabled;
        private static readonly CallResult<SteamUGCQueryCompleted_t> _modDetailsCallResult;

        private static readonly Dictionary<AccountID_t, List<CrossPromotion>> _modsForAuthor =
            new Dictionary<AccountID_t, List<CrossPromotion>>();

        private static  Vector2                              _scrollPosition = Vector2.zero;
        private static readonly CallResult<SteamUGCQueryCompleted_t> _userModsCallResult;
        internal static bool                                 cachePathOverriden;

        static CrossPromotionManager() {
            if (SteamManager.Initialized) {
                _enabled = true;
                _userModsCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUserModsReceived);
                _modDetailsCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnModDetailsReceived);
            }
        }

        public static AppId_t AppID {
            get {
                if (_enabled && _appId == AppId_t.Invalid) {
                    _appId = SteamUtils.GetAppID();
                }

                return _appId;
            }
        }

        public static int CacheCount => _cacheCount ??= new DirectoryInfo(CachePath).GetFiles().Length;

        internal static string CachePath {
            get {
                if (_cachePath != null) {
                    return _cachePath;
                }

                if (GenCommandLine.TryGetCommandLineArg("cross-promotions-path", out string path)) {
                    path = path.TrimEnd('\\', '/');
                    if (path == "") {
                        path = Path.DirectorySeparatorChar.ToString() ?? "";
                    }

                    cachePathOverriden = true;
                    Log.Message("CrossPromotion preview images location overriden: " + path);
                } else {
                    path = ModManager.Settings.UseTempFolderForCrossPromotions
                        ? Path.Combine(Path.GetTempPath(), "CrossPromotions")
                        : Path.Combine(GenFilePaths.SaveDataFolderPath, "CrossPromotions");
                }
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists) {
                    dir.Create();
                }

                return path;
            }
        }

        public static long CacheSize => _cacheSize ??= new DirectoryInfo(CachePath).GetFiles().Sum(f => f.Length);

        private static List<CrossPromotion> RelevantPromotions { get; set; }

        public static AccountID_t? AuthorForMod(PublishedFileId_t fileId) {
            if (_authorForMod.TryGetValue(fileId, out AccountID_t author)) {
                return author;
            }

            if (!_currentlyFetchingFiles.Contains(fileId)) {
                FetchModDetails(fileId);
            }

            return null;
        }

        internal static void DeleteCache() {
            Find.WindowStack.Add(new Dialog_MessageBox(
                                     I18n.ConfirmDeletingCrossPromotionCache(
                                         CachePath, CacheCount, CacheSize),
                                     "Confirm".Translate(),
                                     () => {
                                         DirectoryInfo dir = new DirectoryInfo(CachePath);
                                         dir.Delete(true);
                                         Notify_CrossPromotionPathChanged();
                                     }, "Cancel".Translate(), buttonADestructive: true));
        }

        private static void DrawCrossPromotions(ref Rect canvas, IEnumerable<CrossPromotion> promotions) {
            Rect backgroundRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                PromotionsHeight);
            Rect outRect = backgroundRect.ContractedBy(SmallMargin / 2);
            int height  = (int) outRect.height;
            int width   = promotions.Sum(p => p.NormalizedWidth(height)) + ((promotions.Count() - 1) * SmallMargin);
            if (width > outRect.width) {
                height -= 16;
                // recalculate total width
                width = promotions.Sum(p => p.NormalizedWidth(height)) + ((promotions.Count() - 1) * SmallMargin);
            }

            Rect viewRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                width,
                height);
            Vector2 pos = viewRect.min;
            canvas.yMin += PromotionsHeight + SmallMargin;

            Widgets.DrawBoxSolid(backgroundRect, SlightlyDarkBackground);
            if (Mouse.IsOver(outRect) && Event.current.type == EventType.ScrollWheel) {
                _scrollPosition.x += Event.current.delta.y * ScrollSpeed;
            }

            Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
            foreach (CrossPromotion promotion in promotions) {
                int normalizedWidth = promotion.NormalizedWidth(height);
                Rect rect            = new Rect(pos.x, pos.y, normalizedWidth, height);
                if (Widgets.ButtonImage(rect, promotion.Preview, new Color(.9f, .9f, .9f), Color.white)) {
                    if (!promotion.Installed) {
                        List<FloatMenuOption> options = Utilities.NewOptionsList;
                        options.Add(new FloatMenuOption(I18n.Subscribe(promotion.Name),
                                                        () => Workshop.Subscribe(promotion.FileId)));
                        options.Add(new FloatMenuOption(I18n.WorkshopPage(promotion.Name),
                                                        () => SteamUtility.OpenWorkshopPage(promotion.FileId)));
                        Utilities.FloatMenu(options);
                    } else {
                        ModButton button = ModButtonManager.AllButtons.FirstOrDefault(b => b.Name == promotion.Name);
                        if (button != null) {
                            Page_BetterModConfig.Instance.Selected = button;
                        }
                    }
                }

                TooltipHandler.TipRegion(rect, promotion.Name + "\n\n" + promotion.Description);
                pos.x += normalizedWidth + SmallMargin;
            }

            Widgets.EndScrollView();
        }

        private static void FetchModDetails(PublishedFileId_t fileId) {
            Debug.TracePromotions($"Fetching details for {fileId}...");
            _currentlyFetchingFiles.Add(fileId);
            UGCQueryHandle_t query   = SteamUGC.CreateQueryUGCDetailsRequest(new[] {fileId}, 1);
            SteamAPICall_t request = SteamUGC.SendQueryUGCRequest(query);
            _modDetailsCallResult.Set(request);
        }

        private static void FetchModsForAuthor(AccountID_t author) {
            Debug.TracePromotions($"Fetching mods for {author}...");
            UGCQueryHandle_t query = SteamUGC.CreateQueryUserUGCRequest(
                author,
                EUserUGCList.k_EUserUGCList_Published,
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_UsableInGame,
                EUserUGCListSortOrder.k_EUserUGCListSortOrder_VoteScoreDesc,
                AppID, AppID, 1);
            SteamUGC.AddRequiredTag(query, VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor);
            SteamAPICall_t request = SteamUGC.SendQueryUGCRequest(query);
            _userModsCallResult.Set(request);
        }

        public static bool HandleCrossPromotions(ref Rect canvas, ModMetaData mod) {
            if (!_enabled) {
                return false;
            }

            if (!ModManager.Settings.ShowPromotions || !Manifest.For(mod).showCrossPromotions) {
                return false;
            }

            if (mod.GetPublishedFileId() == PublishedFileId_t.Invalid) {
                return false;
            }

            AccountID_t? author = AuthorForMod(mod.GetPublishedFileId());
            if (author == null) {
                return false;
            }

            RelevantPromotions ??= PromotionsForAuthor(author.Value)?.Where(p => p.ShouldShow).ToList();
            if (RelevantPromotions.NullOrEmpty()) {
                return false;
            }

            if (Widgets.ButtonImage(
                new Rect(canvas.xMax - SmallIconSize, canvas.yMin, SmallIconSize, SmallIconSize), Gear, Color.grey,
                GenUI.MouseoverColor)) {
                Utilities.OpenSettingsFor(ModManager.Instance);
            }

            Utilities.DoLabel(ref canvas, I18n.PromotionsFor(mod.AuthorsString));
            DrawCrossPromotions(ref canvas, RelevantPromotions);
            return true;
        }

        public static void Notify_CrossPromotionPathChanged() {
            _cachePath = null;
            _cacheCount = null;
            _cacheSize = null;
        }

        public static void Notify_UpdateRelevantMods() {
            RelevantPromotions = null;
        }

        private static void OnModDetailsReceived(SteamUGCQueryCompleted_t result, bool failure) {
            Debug.Log(
                $"Received mod details: failure: {failure}, result: {result.m_eResult}, count: {result.m_unNumResultsReturned}");
            for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
                if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details)) {
                    Debug.TracePromotions($" - {details.m_rgchTitle} ({details.m_ulSteamIDOwner}");
                    AccountID_t author = new CSteamID(details.m_ulSteamIDOwner).GetAccountID();
                    _authorForMod.Add(details.m_nPublishedFileId, author);
                    _currentlyFetchingFiles.Remove(details.m_nPublishedFileId);
                }
            }

            SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
        }

        private static void OnUserModsReceived(SteamUGCQueryCompleted_t result, bool failure) {
            Debug.Log(
                $"Received user mods: failure: {failure}, result: {result.m_eResult}, count: {result.m_unNumResultsReturned}");
            CSteamID author     = CSteamID.Nil;
            List<CrossPromotion> promotions = new List<CrossPromotion>();
            for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
                if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details)) {
                    Debug.TracePromotions($" - {details.m_rgchTitle} ({details.m_ulSteamIDOwner}");
                    author = new CSteamID(details.m_ulSteamIDOwner);
                    promotions.Add(new CrossPromotion(details));
                }
            }

            if (author != CSteamID.Nil) {
                _modsForAuthor[author.GetAccountID()] = promotions;
                Notify_UpdateRelevantMods();
            }

            SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
        }

        public static List<CrossPromotion> PromotionsForAuthor(AccountID_t author) {
            if (_modsForAuthor.TryGetValue(author, out List<CrossPromotion> mods)) {
                return mods;
            }

            mods = new List<CrossPromotion>();
            _modsForAuthor.Add(author, mods);
            FetchModsForAuthor(author);
            return mods;
        }

        public static void Update() {
            if (_enabled) {
                SteamAPI.RunCallbacks();
            }
        }
    }
}
