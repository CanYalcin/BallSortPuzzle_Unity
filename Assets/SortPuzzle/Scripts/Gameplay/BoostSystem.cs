using HyperBase.Monetization;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Handles the "out of boosts" flow: shows a rewarded ad and grants
    /// 1 boost on success. Called from LevelController when a boost button
    /// is pressed and the player has no inventory left.
    /// </summary>
    public class BoostSystem
    {
        private readonly BoostManager _boostManager;
        private readonly AdManager    _ads;

        [Inject]
        public BoostSystem(BoostManager boostManager, AdManager ads)
        {
            _boostManager = boostManager;
            _ads          = ads;
        }

        /// <summary>
        /// Shows a rewarded ad. On success, grants 1 boost of the given type.
        /// Logs a warning and does nothing if no ad is ready.
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
