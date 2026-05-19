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

public AdConfig Config => _config;

        private readonly AdConfig _config;
        private readonly EventBus _eventBus;
        private bool   _isNoAds;
        private bool   _bannerCreated;
        private float  _lastInterTime = float.MinValue;
        private int    _currentLevel;
        private int    _intRetry;
        private int    _rwdRetry;
        private const int MaxDelay = 64;
        private Action<bool> _rewardCb;

        [Inject]
        public AdManager(AdConfig config, EventBus eventBus)
        {
            _config   = config;
            _eventBus = eventBus;
        }

        // ── Init ─────────────────────────────────────────────────────────────────

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

                if (_config.EnableBanner && _config.ShowBannerOnStart && !_isNoAds)
                {
                    MaxSdk.CreateBanner(_config.BannerAdUnitId, _config.BannerPosition);
                    MaxSdk.SetBannerBackgroundColor(_config.BannerAdUnitId, Color.black);
                    MaxSdk.ShowBanner(_config.BannerAdUnitId);
                    _bannerCreated = true;
                }
            };
        }

        // ── Banner ────────────────────────────────────────────────────────────────

public void ShowBanner()
        {
            if (!_config.EnableAds || !_config.EnableBanner || _isNoAds) return;
            if (!_bannerCreated)
            {
                MaxSdk.CreateBanner(_config.BannerAdUnitId, _config.BannerPosition);
                MaxSdk.SetBannerBackgroundColor(_config.BannerAdUnitId, Color.black);
                _bannerCreated = true;
            }
            MaxSdk.ShowBanner(_config.BannerAdUnitId);
        }

        public void HideBanner()
        {
            if (_bannerCreated) MaxSdk.HideBanner(_config.BannerAdUnitId);
        }

        public void DestroyBanner()
        {
            if (!_bannerCreated) return;
            MaxSdk.DestroyBanner(_config.BannerAdUnitId);
            _bannerCreated = false;
        }

        // ── Interstitial ──────────────────────────────────────────────────────────

public bool CanShowInterstitial()
        {
            if (!_config.EnableAds || !_config.EnableInterstitial || _isNoAds)           return false;
            if (!MaxSdk.IsInterstitialReady(_config.InterstitialAdUnitId))               return false;
            if (_currentLevel < _config.InterstitialMinLevel)                            return false;
            if (Time.unscaledTime - _lastInterTime < _config.InterstitialCooldownSeconds) return false;
            return true;
        }

public void TryShowInterstitial(string placement = "default")
        {
            if (!_config.EnableAds || !_config.EnableInterstitial || _isNoAds)       return;
            if (!MaxSdk.IsInterstitialReady(_config.InterstitialAdUnitId))            return;
            if (_currentLevel < _config.InterstitialMinLevel)                        return;
            if (Time.unscaledTime - _lastInterTime < _config.InterstitialCooldownSeconds) return;

            MaxSdk.ShowInterstitial(_config.InterstitialAdUnitId, placement);
            _lastInterTime = Time.unscaledTime;
            _eventBus.Publish(new OnAdShown(AdType.Interstitial));
        }

        // ── Rewarded ──────────────────────────────────────────────────────────────

public bool IsRewardedReady()
        {
            if (!_config.EnableAds || !_config.EnableRewarded) return false;
            return MaxSdk.IsRewardedAdReady(_config.RewardedAdUnitId);
        }

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

        public void SetCurrentLevel(int level) => _currentLevel = level;

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
