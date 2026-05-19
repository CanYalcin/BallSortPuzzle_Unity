using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using HyperBase.Analytics;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Data;
using HyperBase.Monetization;
using HyperBase.Notifications;
using HyperBase.RemoteConfig;
using HyperBase.StoreReview;
using HyperBase.Utilities;
using HyperBase.VFX;
using SortPuzzle.DailyChallenge;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Bootstrap
{
    /// <summary>
    /// Startup sequencer. Initialises all services in order, then navigates to MainMenu.
    /// Ads/IAP are gated by AdConfig.EnableAds / EnableIAP — toggle in the Inspector.
    /// </summary>
    public class BootstrapEntryPoint : IAsyncStartable
    {
        private readonly SaveManager         _save;
        private readonly TimeManager         _time;
        private readonly RemoteConfigManager _remoteConfig;
        private readonly AnalyticsManager    _analytics;
        private readonly AudioManager        _audio;
        private readonly VFXManager          _vfx;
        private readonly AdManager           _ads;
        private readonly IAPManager          _iap;
        private readonly NotificationManager _notifications;
        private readonly RateUsManager       _rateUs;
        private readonly GameManager         _game;
        private readonly SceneLoader         _loader;
        private readonly DailyManager        _daily;
        private readonly string              _revenueCatApiKey;

        [Inject]
        public BootstrapEntryPoint(
            SaveManager         save,
            TimeManager         time,
            RemoteConfigManager remoteConfig,
            AnalyticsManager    analytics,
            AudioManager        audio,
            VFXManager          vfx,
            AdManager           ads,
            IAPManager          iap,
            NotificationManager notifications,
            RateUsManager       rateUs,
            GameManager         game,
            SceneLoader         loader,
            DailyManager        daily,
            string              revenueCatApiKey)
        {
            _save             = save;
            _time             = time;
            _remoteConfig     = remoteConfig;
            _analytics        = analytics;
            _audio            = audio;
            _vfx              = vfx;
            _ads              = ads;
            _iap              = iap;
            _notifications    = notifications;
            _rateUs           = rateUs;
            _game             = game;
            _loader           = loader;
            _daily            = daily;
            _revenueCatApiKey = revenueCatApiKey;
        }

        public async Awaitable StartAsync(CancellationToken cancellation)
        {
            Debug.Log("[Bootstrap] == Init start ==");
            await RunInitAsync(cancellation);
            Debug.Log("[Bootstrap] == Init complete ==");
        }

        private async UniTask RunInitAsync(CancellationToken ct)
        {
            // 1. Save — must be first
            _save.Load();
            _save.Data.TotalSessionCount++;

            // 2. Daily streak check — immediately after save loads
            _daily.CheckStreakOnOpen();

            // 3. Daily login bonus
            // Injected via GoldManager in DailyManager — no extra call needed here.
            // GoldManager.TryClaimDailyLoginBonus() is called from MainMenuScreen on show.

            // 4. Time
            _time.Initialize();

            // 5. Firebase
            var firebaseTcs = new UniTaskCompletionSource<bool>();
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                bool ok = task.Result == DependencyStatus.Available;
                if (!ok) Debug.LogError("[Bootstrap] Firebase unavailable: " + task.Result);
                else     Debug.Log("[Bootstrap] Firebase ready.");
                firebaseTcs.TrySetResult(ok);
            });
            await firebaseTcs.Task.AttachExternalCancellation(ct);

            // 6. Remote Config
            await _remoteConfig.FetchAndActivateAsync(ct);

            // 7. Analytics
            _analytics.RegisterProvider(new FirebaseAnalyticsProvider());
            _analytics.RegisterProvider(new GameAnalyticsProvider());
            _analytics.SubscribeToEvents();
            _analytics.SetUserProperty("total_sessions", _save.Data.TotalSessionCount.ToString());

            // 8. Audio
            _audio.Initialize();

            // 9. VFX
            _vfx.Initialize();

            // 10. Ads
            if (_ads.Config.EnableAds)
            {
                _ads.Initialize();
                _ads.SetCurrentLevel(_save.Data.CurrentLevelIndex);
                if (_save.Data.IsNoAds) _ads.ActivateNoAds();
                Debug.Log("[Bootstrap] Ads initialised (MAX).");
            }
            else
            {
                Debug.Log("[Bootstrap] Ads disabled via AdConfig.EnableAds.");
            }

            // 11. IAP
            if (_ads.Config.EnableIAP)
            {
                _iap.Initialize(_revenueCatApiKey);
                Debug.Log("[Bootstrap] IAP initialised (RevenueCat).");
            }
            else
            {
                Debug.Log("[Bootstrap] IAP disabled via AdConfig.EnableIAP.");
            }

            // 12. Notifications
            _notifications.Initialize();
            _notifications.CancelAll();
            _notifications.ScheduleDailyReminder("Your Game");

            // 13. Navigate to MainMenu
            Application.targetFrameRate = 60;
            _game.TransitionTo(GameState.MainMenu);
            _loader.LoadSceneAsync("MainMenu").Forget();

            _save.SaveAsync().Forget();
        }
    }
}
