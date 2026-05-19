using UnityEngine;

namespace HyperBase.Monetization
{
    /// <summary>
    /// Project-level monetization configuration.
    /// Create via: Assets -> Create -> HyperBase -> Ad Config
    ///
    /// ── QUICK SETUP ──────────────────────────────────────────────────────────
    ///  Ads only    → EnableAds = true,  EnableIAP = false
    ///  IAP only    → EnableAds = false, EnableIAP = true
    ///  Both        → EnableAds = true,  EnableIAP = true  (default)
    ///  Neither     → EnableAds = false, EnableIAP = false
    ///
    ///  Within Ads you can independently toggle Banner / Interstitial / Rewarded.
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "HyperBase/Ad Config")]
    public class AdConfig : ScriptableObject
    {
        // ── Monetization Mode ─────────────────────────────────────────────────
        [Header("Monetization Mode")]
        [Tooltip("Master switch for AppLovin MAX ads. Disabling skips all ad SDK init.")]
        public bool EnableAds  = true;

        [Tooltip("Master switch for in-app purchases via RevenueCat. Disabling skips IAP SDK init and hides the No-Ads button.")]
        public bool EnableIAP  = true;

        // ── Ad Type Toggles ───────────────────────────────────────────────────
        [Header("Ad Types  (only relevant when EnableAds = true)")]
        [Tooltip("Show a persistent banner ad.")]
        public bool EnableBanner        = true;

        [Tooltip("Show interstitial ads between levels.")]
        public bool EnableInterstitial  = true;

        [Tooltip("Show rewarded ads (double reward, continue after fail, etc).")]
        public bool EnableRewarded      = true;

        // ── AppLovin MAX SDK ──────────────────────────────────────────────────
        [Header("AppLovin MAX SDK Key  (only relevant when EnableAds = true)")]
        public string SdkKey = "YOUR_APPLOVIN_SDK_KEY";

        // ── Android Ad Unit IDs ───────────────────────────────────────────────
        [Header("Android Ad Unit IDs")]
        public string Android_BannerAdUnitId       = "YOUR_ANDROID_BANNER_ID";
        public string Android_InterstitialAdUnitId = "YOUR_ANDROID_INTERSTITIAL_ID";
        public string Android_RewardedAdUnitId     = "YOUR_ANDROID_REWARDED_ID";

        // ── iOS Ad Unit IDs ───────────────────────────────────────────────────
        [Header("iOS Ad Unit IDs")]
        public string iOS_BannerAdUnitId           = "YOUR_IOS_BANNER_ID";
        public string iOS_InterstitialAdUnitId     = "YOUR_IOS_INTERSTITIAL_ID";
        public string iOS_RewardedAdUnitId         = "YOUR_IOS_REWARDED_ID";

        // ── Interstitial Capping ──────────────────────────────────────────────
        [Header("Interstitial Capping")]
        public float InterstitialCooldownSeconds   = 30f;
        public int   InterstitialMinLevel          = 3;

        // ── Banner Settings ───────────────────────────────────────────────────
        [Header("Banner Settings")]
        public bool                        ShowBannerOnStart = true;
        public MaxSdkBase.BannerPosition   BannerPosition    = MaxSdkBase.BannerPosition.BottomCenter;

        // ── Platform-resolved helpers ─────────────────────────────────────────
#if UNITY_ANDROID
        public string BannerAdUnitId       => Android_BannerAdUnitId;
        public string InterstitialAdUnitId => Android_InterstitialAdUnitId;
        public string RewardedAdUnitId     => Android_RewardedAdUnitId;
#else
        public string BannerAdUnitId       => iOS_BannerAdUnitId;
        public string InterstitialAdUnitId => iOS_InterstitialAdUnitId;
        public string RewardedAdUnitId     => iOS_RewardedAdUnitId;
#endif
    }
}
