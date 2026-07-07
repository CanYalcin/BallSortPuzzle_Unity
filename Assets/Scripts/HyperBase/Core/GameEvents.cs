using HyperBase.Monetization;

namespace HyperBase.Core
{
    /// <summary>Fired by GameManager on every state transition. Carries both Previous and Current for stateful listeners.</summary>
    public readonly struct OnGameStateChanged
    {
        public readonly GameState Previous;
        public readonly GameState Current;
        public OnGameStateChanged(GameState previous, GameState current) { Previous = previous; Current = current; }
    }

    /// <summary>Fired by LevelManager when a level begins. LevelIndex is -1 for daily challenge.</summary>
    public readonly struct OnLevelStarted
    {
        public readonly int LevelIndex; // -1 for daily challenge
        public OnLevelStarted(int levelIndex) => LevelIndex = levelIndex;
    }

    /// <summary>Fired by LevelManager on successful completion. CompletionTime is wall-clock seconds since level start.</summary>
    public readonly struct OnLevelCompleted
    {
        public readonly int   LevelIndex;
        public readonly float CompletionTime;
        public readonly bool  IsDaily;
        public readonly int   GoldEarned; // normal levels only — daily gold is decided later by DailyManager
        public readonly int   PourCount;
        public readonly int   ParMoves;
        public OnLevelCompleted(int levelIndex, float completionTime, bool isDaily = false, int goldEarned = 0, int pourCount = 0, int parMoves = 0)
        { LevelIndex = levelIndex; CompletionTime = completionTime; IsDaily = isDaily; GoldEarned = goldEarned; PourCount = pourCount; ParMoves = parMoves; }
    }

    /// <summary>Fired by LevelManager when the player fails a level.</summary>
    public readonly struct OnLevelFailed
    {
        public readonly int   LevelIndex;
        public readonly float Duration; // seconds since this attempt began
        public OnLevelFailed(int levelIndex, float duration = 0f) { LevelIndex = levelIndex; Duration = duration; }
    }
    /// <summary>Fired by AdManager when any ad impression is recorded. Carries the ad type and placement string.</summary>
    public readonly struct OnAdShown
    {
        public readonly AdType AdType;
        public OnAdShown(AdType adType) => AdType = adType;
    }

    /// <summary>Fired by AdManager when a rewarded ad completes. Success is false if the player skipped or the ad failed.</summary>
    public readonly struct OnAdCompleted
    {
        public readonly AdType AdType;
        public readonly bool   Success;
        public OnAdCompleted(AdType adType, bool success) { AdType = adType; Success = success; }
    }

    /// <summary>Fired by IAPManager or AdManager when the player activates No-Ads (purchase or promo).</summary>
    public readonly struct OnNoAdsActivated { }

    /// <summary>Fired by IAPManager when a purchase succeeds. ProductId matches IAPManager.ProductIds constants.</summary>
    public readonly struct OnPurchaseCompleted
    {
        public readonly string ProductId;
        public readonly float  Price;
        public readonly string CurrencyCode;
        public OnPurchaseCompleted(string productId, float price = 0f, string currencyCode = "USD")
        { ProductId = productId; Price = price; CurrencyCode = currencyCode; }
    }

    /// <summary>Fired by IAPManager when a purchase fails or is cancelled. Reason is the RevenueCat error description.</summary>
    public readonly struct OnPurchaseFailed
    {
        public readonly string ProductId;
        public readonly string Reason;
        public OnPurchaseFailed(string productId, string reason) { ProductId = productId; Reason = reason; }
    }
}
