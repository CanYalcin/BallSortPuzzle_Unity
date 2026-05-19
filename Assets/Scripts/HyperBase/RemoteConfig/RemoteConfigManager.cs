using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.RemoteConfig;
using UnityEngine;

namespace HyperBase.RemoteConfig
{
    /// <summary>
    /// Wraps Firebase Remote Config with typed accessors and in-code defaults.
    /// Always call FetchAndActivateAsync at startup.
    /// </summary>
    public class RemoteConfigManager
    {
        public bool IsInitialized { get; private set; }

        private readonly Dictionary<string, object> _defaults = new()
        {
            { RCKeys.InterstitialCooldownSec, 30   },
            { RCKeys.InterstitialMinLevel,    3    },
            { RCKeys.RewardedMultiplier,      2    },
            { RCKeys.StartingSoftCurrency,    100  },
            { RCKeys.BannerEnabled,           true },
        };

        public async UniTask FetchAndActivateAsync(CancellationToken ct = default)
        {
            try
            {
                await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(_defaults).AsUniTask().AttachExternalCancellation(ct);
                var result = await FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().AsUniTask().AttachExternalCancellation(ct);
                IsInitialized = true;
                Debug.Log($"[RemoteConfig] Fetch & activate complete. Updated: {result}");
            }
            catch (OperationCanceledException) { IsInitialized = true; Debug.Log("[RemoteConfig] Fetch cancelled — using defaults."); }
            catch (Exception e)                { IsInitialized = true; Debug.LogWarning($"[RemoteConfig] Fetch failed — using defaults. ({e.Message})"); }
        }

        public int    GetInt   (string key, int    fallback = 0)    => IsInitialized ? (int)FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue          : fallback;
        public float  GetFloat (string key, float  fallback = 0f)   => IsInitialized ? (float)FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue       : fallback;
        public bool   GetBool  (string key, bool   fallback = false) => IsInitialized ? FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue             : fallback;
        public string GetString(string key, string fallback = "")    => IsInitialized ? FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue              : fallback;
    }

    public static class RCKeys
    {
        public const string InterstitialCooldownSec = "interstitial_cooldown_sec";
        public const string InterstitialMinLevel    = "interstitial_min_level";
        public const string RewardedMultiplier      = "rewarded_multiplier";
        public const string StartingSoftCurrency    = "starting_soft_currency";
        public const string BannerEnabled           = "banner_enabled";
    }
}
