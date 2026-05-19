using HyperBase.Core;
using HyperBase.Data;
using SortPuzzle.Data;
using UnityEngine;
using VContainer;

namespace SortPuzzle.Economy
{
    /// <summary>
    /// All gold mutations go through here — never write GoldBalance directly.
    /// Publishes OnGoldChanged and auto-saves on every change.
    ///
    /// Gold Sources:
    ///   Level complete  10–50g  (varies by difficulty)
    ///   Daily challenge 100g
    ///   Daily login      50g
    ///   Rewarded ad     300g   (capped at 5 per day)
    ///   IAP purchase    varies
    /// </summary>
    public class GoldManager
    {
        private readonly SaveManager _save;
        private readonly EventBus    _events;
        private readonly BoostConfig _config;

        public int Balance => _save.Data.GoldBalance;

        /// <summary>Fires after every Add or TrySpend with the new balance. Subscribe in UI widgets.</summary>
        public event System.Action<int> OnGoldChanged;

        [Inject]
        public GoldManager(SaveManager save, EventBus events, BoostConfig config)
        {
            _save   = save;
            _events = events;
            _config = config;
        }

        /// <summary>Adds gold from any source. Source string is for analytics.</summary>
public void Add(int amount, string source = "unknown")
        {
            if (amount <= 0) return;
            int prev = _save.Data.GoldBalance;
            _save.Data.GoldBalance += amount;
            Debug.Log($"[GoldManager] +{amount} gold from '{source}'. Balance: {_save.Data.GoldBalance}");
            _events.Publish(new OnGoldChanged(prev, _save.Data.GoldBalance, amount, source));
            OnGoldChanged?.Invoke(_save.Data.GoldBalance);
            _save.SaveAsync().Forget();
        }

        /// <summary>
        /// Attempts to spend gold. Returns false and does nothing if insufficient balance.
        /// Sink string is for analytics.
        /// </summary>
public bool TrySpend(int amount, string sink = "unknown")
        {
            if (amount <= 0) return false;
            if (_save.Data.GoldBalance < amount)
            {
                Debug.Log($"[GoldManager] Not enough gold. Have {_save.Data.GoldBalance}, need {amount}.");
                _events.Publish(new OnGoldInsufficient(amount, sink));
                return false;
            }
            int prev = _save.Data.GoldBalance;
            _save.Data.GoldBalance -= amount;
            Debug.Log($"[GoldManager] -{amount} gold on '{sink}'. Balance: {_save.Data.GoldBalance}");
            _events.Publish(new OnGoldChanged(prev, _save.Data.GoldBalance, -amount, sink));
            OnGoldChanged?.Invoke(_save.Data.GoldBalance);
            _save.SaveAsync().Forget();
            return true;
        }

        public bool CanAfford(int amount) => _save.Data.GoldBalance >= amount;

        /// <summary>
        /// Claims the daily login bonus. Returns false if already claimed today.
        /// </summary>
        public bool TryClaimDailyLoginBonus()
        {
            string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_save.Data.LastDailyResetDate == today && _save.Data.DailyLoginBonusClaimed)
                return false;

            if (_save.Data.LastDailyResetDate != today)
            {
                _save.Data.LastDailyResetDate       = today;
                _save.Data.DailyRewardedAdsWatched  = 0;
                _save.Data.DailyLoginBonusClaimed   = false;
            }

            _save.Data.DailyLoginBonusClaimed = true;
            Add(_config.DailyLoginBonusGold, "daily_login");
            return true;
        }

        /// <summary>
        /// Claims gold for watching a rewarded ad. Returns false if daily cap reached.
        /// </summary>
        public bool TryClaimRewardedAdGold()
        {
            string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_save.Data.LastDailyResetDate != today)
            {
                _save.Data.LastDailyResetDate      = today;
                _save.Data.DailyRewardedAdsWatched = 0;
                _save.Data.DailyLoginBonusClaimed  = false;
            }

            if (_save.Data.DailyRewardedAdsWatched >= _config.DailyRewardedAdCap)
            {
                Debug.Log("[GoldManager] Daily rewarded ad cap reached.");
                return false;
            }

            _save.Data.DailyRewardedAdsWatched++;
            Add(_config.RewardedAdGoldAmount, "rewarded_ad");
            return true;
        }
    }
}
