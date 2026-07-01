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
        public OnLevelCompleted(int levelIndex, float completionTime, bool isDaily = false)
        { LevelIndex = levelIndex; CompletionTime = completionTime; IsDaily = isDaily; }
    }

    /// <summary>Fired by LevelManager when the player fails a level.</summary>
    public readonly struct OnLevelFailed
    {
        public readonly int LevelIndex;
        public OnLevelFailed(int levelIndex) => LevelIndex = levelIndex;
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
        public OnPurchaseCompleted(string productId) => ProductId = productId;
    }

    /// <summary>Fired by IAPManager when a purchase fails or is cancelled. Reason is the RevenueCat error description.</summary>
    public readonly struct OnPurchaseFailed
    {
        public readonly string ProductId;
        public readonly string Reason;
        public OnPurchaseFailed(string productId, string reason) { ProductId = productId; Reason = reason; }
    }
}
