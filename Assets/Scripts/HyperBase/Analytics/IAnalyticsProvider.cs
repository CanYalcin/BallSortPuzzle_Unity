using System.Collections.Generic;

namespace HyperBase.Analytics
{
    /// <summary>Abstraction over any analytics SDK. Register implementations with AnalyticsManager.</summary>
    public interface IAnalyticsProvider
    {
        void Initialize();
        void LogLevelStart    (int levelIndex);
        void LogLevelComplete (int levelIndex, float duration, int softEarned);
        void LogLevelFail     (int levelIndex, float duration);
        void LogAdShown       (string adType, string placement);
        void LogAdCompleted   (string adType, string placement, bool rewarded);
        void LogPurchase      (string productId, double price, string currency);
        void LogCurrencyEarned(string currencyType, int amount, string source);
        void LogCurrencySpent (string currencyType, int amount, string sink);
        void LogEvent         (string eventName, Dictionary<string, object> parameters = null);
        void SetUserProperty  (string name, string value);
    }
}
