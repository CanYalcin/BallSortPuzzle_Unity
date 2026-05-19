using HyperBase.Core;
using HyperBase.Monetization;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Bridges UI boost buttons → BoostManager → PuzzleController.
    /// Handles the "not enough boosts" flow: offer gold purchase or rewarded ad.
    ///
    /// Call TryActivateBoost() from boost bar button clicks.
    /// Subscribe to OnShowGetBoostPopup to display the purchase popup.
    /// </summary>
    public class BoostSystem
    {
        public event System.Action<BoostType, int> OnShowGetBoostPopup; // (type, goldCost)

        private readonly BoostManager _boostManager;
        private readonly GoldManager  _goldManager;
        private readonly AdManager    _ads;
        private readonly EventBus     _events;
        private readonly BoostConfig  _config;

        [Inject]
        public BoostSystem(BoostManager boostManager, GoldManager goldManager,
                           AdManager ads, EventBus events, BoostConfig config)
        {
            _boostManager = boostManager;
            _goldManager  = goldManager;
            _ads          = ads;
            _events       = events;
            _config       = config;
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public int  GetCount(BoostType type)   => _boostManager.GetCount(type);
        public bool HasBoost(BoostType type)   => _boostManager.HasBoost(type);
        public int  GetCost(BoostType type)    => _config.GetCost(type);
        public bool CanAfford(BoostType type)  => _goldManager.CanAfford(_config.GetCost(type));

        // ── Activate ──────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to activate a boost from inventory.
        /// If inventory is empty, fires OnShowGetBoostPopup so the UI can
        /// present the "Buy with Gold / Watch Ad" popup.
        /// Returns true if the boost was consumed from inventory.
        /// </summary>
        public bool TryActivateBoost(BoostType type)
        {
            if (_boostManager.HasBoost(type))
                return _boostManager.TryUseBoost(type);

            int cost = _config.GetCost(type);
            OnShowGetBoostPopup?.Invoke(type, cost);
            return false;
        }

        // ── Purchase paths (called from popup buttons) ────────────────────────

        /// <summary>
        /// Player chose "Buy with Gold" in the popup.
        /// Returns true if purchase succeeded (gold deducted, boost granted, ready to use).
        /// </summary>
        public bool BuyWithGold(BoostType type)
        {
            bool bought = _boostManager.TryBuyWithGold(type);
            if (bought)
                Debug.Log($"[BoostSystem] Bought {type} with gold.");
            return bought;
        }

        /// <summary>
        /// Player chose "Watch Ad" in the popup.
        /// Shows a rewarded ad; on success grants 1 boost of the given type.
        /// </summary>
        public void WatchAdForBoost(BoostType type)
        {
            if (!_ads.IsRewardedReady())
            {
                Debug.LogWarning("[BoostSystem] Rewarded ad not ready.");
                return;
            }
            _ads.ShowRewarded(success =>
            {
                if (success)
                {
                    _boostManager.Grant(type, 1);
                    Debug.Log($"[BoostSystem] Granted 1x {type} via rewarded ad.");
                }
            }, "boost_" + type.ToString().ToLower());
        }
    }
}
