using System.Collections.Generic;
using Firebase.Analytics;
using UnityEngine;

namespace HyperBase.Analytics
{
    /// <summary>Firebase Analytics provider. Requires Firebase Unity SDK.</summary>
    public class FirebaseAnalyticsProvider : IAnalyticsProvider
    {
        public void Initialize()
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            Debug.Log("[FirebaseAnalytics] Initialized.");
        }

        public void LogLevelStart(int idx)
            => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart,
               FirebaseAnalytics.ParameterLevelName, idx.ToString());

        public void LogLevelComplete(int idx, float dur, int soft)
            => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
               new Parameter[]
               {
                   new(FirebaseAnalytics.ParameterLevelName, idx.ToString()),
                   new(FirebaseAnalytics.ParameterSuccess,   "true"),
                   new("duration_sec",  (long)dur),
                   new("soft_earned",   (long)soft)
               });

        public void LogLevelFail(int idx, float dur)
            => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelEnd,
               new Parameter[]
               {
                   new(FirebaseAnalytics.ParameterLevelName, idx.ToString()),
                   new(FirebaseAnalytics.ParameterSuccess,   "false"),
                   new("duration_sec",  (long)dur)
               });

        public void LogAdShown(string adType, string placement)
            => FirebaseAnalytics.LogEvent("ad_shown",
               new Parameter[] { new("ad_type", adType), new("placement", placement) });

        public void LogAdCompleted(string adType, string placement, bool rewarded)
            // EventAdImpression is the correct Firebase 4.x event for ad interactions
            => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAdImpression,
               new Parameter[]
               {
                   new(FirebaseAnalytics.ParameterAdFormat,  adType),
                   new("placement",                          placement),
                   new("rewarded",                           rewarded ? 1L : 0L)
               });

        public void LogPurchase(string productId, double price, string currency)
            => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase,
               new Parameter[]
               {
                   // ParameterItemID (capital D) is the correct constant name
                   new(FirebaseAnalytics.ParameterItemID,    productId),
                   new(FirebaseAnalytics.ParameterValue,     price),
                   new(FirebaseAnalytics.ParameterCurrency,  currency)
               });

        public void LogCurrencyEarned(string type, int amount, string source)
            => FirebaseAnalytics.LogEvent("currency_earned",
               new Parameter[]
               {
                   new("currency_type", type),
                   new("amount",        (long)amount),
                   new("source",        source)
               });

        public void LogCurrencySpent(string type, int amount, string sink)
            => FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSpendVirtualCurrency,
               new Parameter[]
               {
                   new(FirebaseAnalytics.ParameterVirtualCurrencyName, type),
                   new(FirebaseAnalytics.ParameterValue,               (long)amount),
                   new("sink",                                          sink)
               });

        public void LogEvent(string eventName, Dictionary<string, object> parms)
        {
            if (parms == null || parms.Count == 0) { FirebaseAnalytics.LogEvent(eventName); return; }
            var list = new List<Parameter>();
            foreach (var kv in parms)
                list.Add(kv.Value switch
                {
                    int    i => new Parameter(kv.Key, (long)i),
                    long   l => new Parameter(kv.Key, l),
                    float  f => new Parameter(kv.Key, (double)f),
                    double d => new Parameter(kv.Key, d),
                    _        => new Parameter(kv.Key, kv.Value?.ToString() ?? "")
                });
            FirebaseAnalytics.LogEvent(eventName, list.ToArray());
        }

        public void SetUserProperty(string name, string value)
            => FirebaseAnalytics.SetUserProperty(name, value);
    }
}
