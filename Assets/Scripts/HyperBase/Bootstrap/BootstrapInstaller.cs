using HyperBase.Analytics;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Currency;
using HyperBase.Data;
using HyperBase.Gameplay;
using HyperBase.Haptics;
using HyperBase.Monetization;
using HyperBase.Notifications;
using HyperBase.ObjectPool;
using HyperBase.RemoteConfig;
using HyperBase.StoreReview;
using HyperBase.UI;
using HyperBase.Utilities;
using HyperBase.VFX;
using SortPuzzle.DailyChallenge;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Bootstrap
{
    public class BootstrapInstaller : LifetimeScope
    {
        [Header("HyperBase Config")]
        [SerializeField] private AdConfig      _adConfig;
        [SerializeField] private AudioConfig   _audioConfig;
        [SerializeField] private LevelDatabase _levelDatabase;
        [SerializeField] private VFXConfig     _vfxConfig;

        [Header("SortPuzzle Config")]
        [SerializeField] private BoostConfig        _boostConfig;
        [SerializeField] private DailyLevelDatabase _dailyLevelDatabase;
        [SerializeField] private DailyRewardConfig  _dailyRewardConfig;

        [Header("Scene MonoBehaviour References")]
        [SerializeField] private ObjectPoolManager           _poolManager;
        [SerializeField] private ApplicationLifecycleHandler _lifecycleHandler;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private HyperBase.DevTools.DebugConsole _debugConsole;
#endif

        [Header("RevenueCat API Key")]
        [SerializeField] private string _revenueCatApiKey = "YOUR_REVENUECAT_API_KEY";

        protected override void Configure(IContainerBuilder builder)
        {
            // Core
            builder.Register<EventBus>   (Lifetime.Singleton);
            builder.Register<GameManager>(Lifetime.Singleton);

            // Data
            builder.Register<SaveManager>(Lifetime.Singleton);

            // Time
            builder.Register<TimeManager>(Lifetime.Singleton)
                   .AsImplementedInterfaces().AsSelf();

            // Remote Config
            builder.Register<RemoteConfigManager>(Lifetime.Singleton);

            // HyperBase Currency (kept for base compat)
            builder.Register<CurrencyManager>(Lifetime.Singleton);

            // Audio
            builder.RegisterInstance(_audioConfig);
            builder.Register<AudioManager>(Lifetime.Singleton);

            // Haptics
            builder.Register<HapticsManager>(Lifetime.Singleton);

            // Monetization
            builder.RegisterInstance(_adConfig);
            builder.Register<AdManager> (Lifetime.Singleton);
            builder.Register<IAPManager>(Lifetime.Singleton);

            // Analytics
            builder.Register<AnalyticsManager>(Lifetime.Singleton);

            // HyperBase Gameplay
            builder.RegisterInstance(_levelDatabase);
            builder.Register<LevelManager>(Lifetime.Singleton);

            // UI
            builder.Register<UIManager>(Lifetime.Singleton);

            // Utilities
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.RegisterInstance(_poolManager);

            // VFX
            builder.RegisterInstance(_vfxConfig);
            builder.Register<VFXManager>(Lifetime.Singleton);

            // Notifications
            builder.Register<NotificationManager>(Lifetime.Singleton);

            // Store Review
            builder.Register<RateUsManager>(Lifetime.Singleton);

            // SortPuzzle Economy
            builder.RegisterInstance(_boostConfig);
            builder.RegisterInstance(_dailyLevelDatabase);
            builder.RegisterInstance(_dailyRewardConfig);
            builder.Register<GoldManager> (Lifetime.Singleton);
            builder.Register<BoostManager>(Lifetime.Singleton);

            // SortPuzzle Daily Challenge
            builder.Register<DailyManager>(Lifetime.Singleton);

            // Lifecycle MonoBehaviour
            builder.RegisterComponent(_lifecycleHandler)
                   .AsImplementedInterfaces().AsSelf();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_debugConsole != null)
                builder.RegisterComponent(_debugConsole);
#endif

            // Bootstrap entry point
            builder.RegisterEntryPoint<BootstrapEntryPoint>()
                   .WithParameter("revenueCatApiKey", _revenueCatApiKey);
        }
    }
}
