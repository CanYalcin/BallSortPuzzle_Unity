using HyperBase.Currency;
using HyperBase.Monetization;

namespace HyperBase.Core
{
    public readonly struct OnGameStateChanged
    {
        public readonly GameState Previous;
        public readonly GameState Current;
        public OnGameStateChanged(GameState previous, GameState current) { Previous = previous; Current = current; }
    }
    public readonly struct OnLevelStarted
    {
        public readonly int LevelIndex; // -1 for daily challenge
        public OnLevelStarted(int levelIndex) => LevelIndex = levelIndex;
    }
    public readonly struct OnLevelCompleted
    {
        public readonly int   LevelIndex;
        public readonly float CompletionTime;
        public readonly bool  IsDaily;
        public OnLevelCompleted(int levelIndex, float completionTime, bool isDaily = false)
        { LevelIndex = levelIndex; CompletionTime = completionTime; IsDaily = isDaily; }
    }
    public readonly struct OnWorldComplete
    {
        public readonly int WorldIndex;
        public OnWorldComplete(int worldIndex) => WorldIndex = worldIndex;
    }
    public readonly struct OnLevelFailed
    {
        public readonly int LevelIndex;
        public OnLevelFailed(int levelIndex) => LevelIndex = levelIndex;
    }
    public readonly struct OnCurrencyChanged
    {
        public readonly CurrencyType Type;
        public readonly int OldAmount;
        public readonly int NewAmount;
        public OnCurrencyChanged(CurrencyType type, int oldAmount, int newAmount) { Type = type; OldAmount = oldAmount; NewAmount = newAmount; }
    }
    public readonly struct OnAdShown
    {
        public readonly AdType AdType;
        public OnAdShown(AdType adType) => AdType = adType;
    }
    public readonly struct OnAdCompleted
    {
        public readonly AdType AdType;
        public readonly bool Success;
        public OnAdCompleted(AdType adType, bool success) { AdType = adType; Success = success; }
    }
    public readonly struct OnNoAdsActivated { }
    public readonly struct OnPurchaseCompleted
    {
        public readonly string ProductId;
        public OnPurchaseCompleted(string productId) => ProductId = productId;
    }
    public readonly struct OnPurchaseFailed
    {
        public readonly string ProductId;
        public readonly string Reason;
        public OnPurchaseFailed(string productId, string reason) { ProductId = productId; Reason = reason; }
    }
}
