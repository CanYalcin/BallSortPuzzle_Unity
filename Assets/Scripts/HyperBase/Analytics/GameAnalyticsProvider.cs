using System.Collections.Generic;
using GameAnalyticsSDK;
using UnityEngine;

namespace HyperBase.Analytics
{
    /// <summary>GameAnalytics provider. Requires GameAnalytics Unity SDK.</summary>
    public class GameAnalyticsProvider : IAnalyticsProvider
    {
        public void Initialize()
        {
            GameAnalytics.Initialize();
            Debug.Log("[GameAnalytics] Provider initialized.");
        }

        public void LogLevelStart(int idx)
            => GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, $"Level_{idx:000}");

        public void LogLevelComplete(int idx, float dur, int soft)
            => GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, $"Level_{idx:000}", (int)dur);

        public void LogLevelFail(int idx, float dur)
            => GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, $"Level_{idx:000}", (int)dur);

        public void LogAdShown(string adType, string placement)
        {
            GAAdType t = adType.ToLower() == "rewarded" ? GAAdType.RewardedVideo :
                         adType.ToLower() == "banner"   ? GAAdType.Banner        :
                         adType.ToLower() == "interstitial" ? GAAdType.Interstitial : GAAdType.Undefined;
            GameAnalytics.NewAdEvent(GAAdAction.Show, t, "applovin_max", placement);
        }

        public void LogAdCompleted(string adType, string placement, bool rewarded)
        {
            GAAdType t = adType.ToLower() == "rewarded" ? GAAdType.RewardedVideo :
                         adType.ToLower() == "banner"   ? GAAdType.Banner        :
                         adType.ToLower() == "interstitial" ? GAAdType.Interstitial : GAAdType.Undefined;
            GameAnalytics.NewAdEvent(rewarded ? GAAdAction.RewardReceived : GAAdAction.Show, t, "applovin_max", placement);
        }

        public void LogPurchase(string productId, double price, string currency)
            => GameAnalytics.NewDesignEvent($"IAP:Purchase:{productId}", (float)price);

        public void LogCurrencyEarned(string type, int amount, string source)
            => GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, type, amount, "Currency", source);

        public void LogCurrencySpent(string type, int amount, string sink)
            => GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, type, amount, "Currency", sink);

        public void LogEvent(string name, Dictionary<string, object> parms)
        {
            float v = 0f;
            if (parms != null)
                foreach (var kv in parms)
                    if      (kv.Value is int   i) { v = i; break; }
                    else if (kv.Value is float  f) { v = f; break; }
            GameAnalytics.NewDesignEvent(name.Replace(" ", "_"), v);
        }

        public void SetUserProperty(string name, string value)
        {
            if      (name.ToLower() == "user_segment") GameAnalytics.SetCustomDimension01(value);
            else if (name.ToLower() == "ab_group")     GameAnalytics.SetCustomDimension02(value);
            else                                       GameAnalytics.SetCustomDimension03(value);
        }
    }
}
