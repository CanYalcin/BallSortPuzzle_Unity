using System;
using Cysharp.Threading.Tasks;
using HyperBase.Core;
using UnityEngine;
using VContainer;

namespace HyperBase.Monetization
{
    /// <summary>
    /// AppLovin MAX ad manager — Banner, Interstitial, Rewarded.
    /// Flat architecture: no method calls another method within this class.
    /// </summary>
    public class AdManager
    {

        /// <summary>Exposes the AdConfig asset for read-only access by other systems.</summary>
        public AdConfig Config => _config;

        private readonly AdConfig _config;
        private readonly EventBus _eventBus;
        private readonly HyperBase.RemoteConfig.RemoteConfigManager _remoteConfig;
        private bool   _isNoAds;
        private bool   _bannerCreated;
        private float  _lastInterTime = float.MinValue;
        private int    _currentLevel;
        private int    _intRetry;
        private int    _rwdRetry;
        private const int MaxDelay = 64;
        private Action<bool> _rewardCb;

        [Inject]
        public AdManager(AdConfig config, EventBus eventBus, HyperBase.RemoteConfig.RemoteConfigManager remoteConfig)
        {
            _config       = config;
            _eventBus     = eventBus;
            _remoteConfig = remoteConfig;
        }

        // Remote-Config-driven values, falling back to the local AdConfig asset. Reads only
        // from _config/_remoteConfig — kept self-contained per this class's flat-architecture rule.
        private int   EffectiveMinLevel      => _remoteConfig.GetInt  (HyperBase.RemoteConfig.RCKeys.InterstitialMinLevel,    _config.InterstitialMinLevel);
        private float EffectiveCooldown      => _remoteConfig.GetFloat(HyperBase.RemoteConfig.RCKeys.InterstitialCooldownSec, _config.InterstitialCooldownSeconds);
        private bool  EffectiveBannerEnabled => _remoteConfig.GetBool (HyperBase.RemoteConfig.RCKeys.BannerEnabled,           _config.EnableBanner);

        // ── Init ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises MAX SDK, registers all ad lifecycle callbacks, and loads the first ad units.
        /// Must be called once after construction (in BootstrapEntryPoint).
        /// </summary>
        public void Initialize()
        {
            if (!_config.EnableAds)
            {
                Debug.Log("[AdManager] Ads disabled via AdConfig. Skipping MAX init.");
                return;
            }
            MaxSdk.SetSdkKey(_config.SdkKey);
            MaxSdk.InitializeSdk();
            _eventBus.Subscribe<OnNoAdsActivated>(_ =>
            {
                _isNoAds = true;
                if (_bannerCreated) { MaxSdk.DestroyBanner(_config.BannerAdUnitId); _bannerCreated = false; }
                Debug.Log("[AdManager] No-ads activated.");
            });

            MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
            {
                Debug.Log("[AdManager] MAX ready.");

                if (_config.EnableInterstitial)
                {
                    MaxSdkCallbacks.Interstitial.OnAdLoadedEvent     += (id, info) => { _intRetry = 0; };
                    MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (id, err)  =>
                    {
                        _intRetry++;
                        int d = Mathf.Min((int)Mathf.Pow(2, _intRetry), MaxDelay);
                        WaitLoad(MaxSdk.LoadInterstitial, _config.InterstitialAdUnitId, d).Forget();
                    };
                    MaxSdkCallbacks.Interstitial.OnAdHiddenEvent        += (id, info)      => MaxSdk.LoadInterstitial(_config.InterstitialAdUnitId);
                    MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (id, info, err)  => MaxSdk.LoadInterstitial(_config.InterstitialAdUnitId);
                    MaxSdk.LoadInterstitial(_config.InterstitialAdUnitId);
                }

                if (_config.EnableRewarded)
                {
                    MaxSdkCallbacks.Rewarded.OnAdLoadedEvent     += (id, info) => { _rwdRetry = 0; };
                    MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (id, err)  =>
                    {
                        _rwdRetry++;
                        int d = Mathf.Min((int)Mathf.Pow(2, _rwdRetry), MaxDelay);
                        WaitLoad(MaxSdk.LoadRewardedAd, _config.RewardedAdUnitId, d).Forget();
                    };
                    MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (id, reward, info) =>
                    {
                        _eventBus.Publish(new OnAdCompleted(AdType.Rewarded, true));
                        _rewardCb?.Invoke(true);
                        _rewardCb = null;
                    };
                    MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (id, info) =>
                    {
                        if (_rewardCb != null)
                        {
                            _eventBus.Publish(new OnAdCompleted(AdType.Rewarded, false));
                            _rewardCb.Invoke(false);
                            _rewardCb = null;
                        }
                        MaxSdk.LoadRewardedAd(_config.RewardedAdUnitId);
                    };
                    MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (id, info, err) =>
                    {
                        _rewardCb?.Invoke(false);
                        _rewardCb = null;
                        MaxSdk.LoadRewardedAd(_config.RewardedAdUnitId);
                    };
                    MaxSdk.LoadRewardedAd(_config.RewardedAdUnitId);
                }

                if (EffectiveBannerEnabled && _config.ShowBannerOnStart && !_isNoAds)
                {
                    MaxSdk.CreateBanner(_config.BannerAdUnitId, _config.BannerPosition);
                    MaxSdk.SetBannerBackgroundColor(_config.BannerAdUnitId, Color.black);
                    MaxSdk.ShowBanner(_config.BannerAdUnitId);
                    _bannerCreated = true;
                }
            };
        }

        // ── Banner ────────────────────────────────────────────────────────────────

        /// <summary>Shows the banner, creating it first if needed. No-op if banner/ads disabled or No-Ads active.</summary>
        public void ShowBanner()
        {
            if (!_config.EnableAds || !EffectiveBannerEnabled || _isNoAds) return;
            if (!_bannerCreated)
            {
                MaxSdk.CreateBanner(_config.BannerAdUnitId, _config.BannerPosition);
                MaxSdk.SetBannerBackgroundColor(_config.BannerAdUnitId, Color.black);
                _bannerCreated = true;
            }
            MaxSdk.ShowBanner(_config.BannerAdUnitId);
        }

        /// <summary>Hides the banner without destroying it. Preserves the loaded ad for quick re-show.</summary>
        
public void HideBanner()
        {
            if (_bannerCreated) MaxSdk.HideBanner(_config.BannerAdUnitId);
        }

        /// <summary>Destroys the banner and releases its resources. ShowBanner() will recreate it.</summary>
        
public void DestroyBanner()
        {
            if (!_bannerCreated) return;
            MaxSdk.DestroyBanner(_config.BannerAdUnitId);
            _bannerCreated = false;
        }

        // ── Interstitial ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if an interstitial can be shown right now.
        /// Checks: ads enabled, not No-Ads, ad loaded, min level reached, cooldown elapsed.
        /// </summary>
        public bool CanShowInterstitial()
        {
            if (!_config.EnableAds || !_config.EnableInterstitial || _isNoAds)           return false;
            if (!MaxSdk.IsInterstitialReady(_config.InterstitialAdUnitId))               return false;
            if (_currentLevel < EffectiveMinLevel)                                       return false;
            if (Time.unscaledTime - _lastInterTime < EffectiveCooldown)                  return false;
            return true;
        }

        /// <summary>
        /// Shows an interstitial if all conditions pass. Resets the cooldown timer on success.
        /// Publishes <see cref="HyperBase.Core.OnAdShown"/>.
        /// </summary>
        public void TryShowInterstitial(string placement = "default")
        {
            if (!_config.EnableAds || !_config.EnableInterstitial || _isNoAds)       return;
            if (!MaxSdk.IsInterstitialReady(_config.InterstitialAdUnitId))            return;
            if (_currentLevel < EffectiveMinLevel)                                    return;
            if (Time.unscaledTime - _lastInterTime < EffectiveCooldown)               return;

            MaxSdk.ShowInterstitial(_config.InterstitialAdUnitId, placement);
            _lastInterTime = Time.unscaledTime;
            _eventBus.Publish(new OnAdShown(AdType.Interstitial));
        }

        // ── Rewarded ──────────────────────────────────────────────────────────────

        /// <summary>Returns true if a rewarded ad is loaded and ready to display.</summary>
        public bool IsRewardedReady()
        {
            if (!_config.EnableAds || !_config.EnableRewarded) return false;
            return MaxSdk.IsRewardedAdReady(_config.RewardedAdUnitId);
        }

        /// <summary>
        /// Shows a rewarded ad. Invokes <paramref name="onComplete"/>(true) if the reward is granted,
        /// or (false) on failure, skip, or if rewarded ads are disabled.
        /// Only one pending callback is held at a time; a new call overwrites the previous one.
        /// </summary>
        public void ShowRewarded(Action<bool> onComplete, string placement = "default")
        {
            if (!_config.EnableAds || !_config.EnableRewarded)
            {
                Debug.LogWarning("[AdManager] Rewarded ads are disabled.");
                onComplete?.Invoke(false);
                return;
            }
            if (!MaxSdk.IsRewardedAdReady(_config.RewardedAdUnitId))
            {
                Debug.LogWarning("[AdManager] Rewarded not ready.");
                onComplete?.Invoke(false);
                return;
            }
            _rewardCb = onComplete;
            MaxSdk.ShowRewardedAd(_config.RewardedAdUnitId, placement);
            _eventBus.Publish(new OnAdShown(AdType.Rewarded));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Updates the current level index used for interstitial min-level gating.</summary>
        
public void SetCurrentLevel(int level) => _currentLevel = level;

        /// <summary>Activates No-Ads mode: hides and destroys the banner, suppresses all future ad calls.</summary>
        
public void ActivateNoAds()
        {
            _isNoAds = true;
            if (_bannerCreated) { MaxSdk.DestroyBanner(_config.BannerAdUnitId); _bannerCreated = false; }
            Debug.Log("[AdManager] No-ads activated.");
        }

        private static async UniTaskVoid WaitLoad(Action<string> loader, string id, int sec)
        {
            await UniTask.WaitForSeconds(sec);
            loader?.Invoke(id);
        }
    }
}
