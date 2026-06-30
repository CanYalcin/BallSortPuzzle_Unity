using HyperBase.Core;
using HyperBase.Data;
using SortPuzzle.Data;
using UnityEngine;
using VContainer;

namespace SortPuzzle.Economy
{
    /// <summary>
    /// Manages boost inventory (Undo and ExtraEmptyTube) persisted in PlayerData.
    /// All mutations publish events via EventBus and trigger auto-save.
    /// Subscribe to <see cref="OnBoostChanged"/> for UI refresh.
    /// Use <see cref="TryBuyWithGold"/> for Shop purchases;
    /// use <see cref="Grant"/> for ad rewards and daily milestone grants.
    /// </summary>
    public class BoostManager
    {
        /// <summary>Fires after any inventory change (use, grant, or buy). Subscribe in UI widgets.</summary>
        public event System.Action OnBoostChanged;

        /// <summary>Manually fires <see cref="OnBoostChanged"/> without mutating inventory. Used for initial UI sync.</summary>
        public void ForceNotify() => OnBoostChanged?.Invoke();

        private readonly SaveManager _save;
        private readonly EventBus    _events;
        private readonly GoldManager _gold;
        private readonly BoostConfig _config;

        [Inject]
        public BoostManager(SaveManager save, EventBus events, GoldManager gold, BoostConfig config)
        { _save = save; _events = events; _gold = gold; _config = config; }

        /// <summary>Returns current inventory count for the given boost type.</summary>
        public int GetCount(BoostType type) => type switch
        {
            BoostType.Undo           => _save.Data.UndoCount,
            BoostType.ExtraEmptyTube => _save.Data.ExtraEmptyTubeCount,
            _                        => 0
        };

        /// <summary>Returns true if the player has at least one boost of the given type.</summary>
        public bool HasBoost(BoostType type) => GetCount(type) > 0;

        /// <summary>
        /// Deducts one boost, publishes <see cref="OnBoostUsed"/>, and auto-saves.
        /// Returns false and publishes <see cref="OnBoostInsufficient"/> if inventory is empty.
        /// </summary>
        public bool TryUseBoost(BoostType type)
        {
            if (GetCount(type) <= 0) { _events.Publish(new OnBoostInsufficient(type)); return false; }
            Deduct(type, 1);
            _save.Data.TotalBoostsUsed++;
            if (type == BoostType.Undo) _save.Data.TotalUndosUsed++;
            _events.Publish(new OnBoostUsed(type, GetCount(type)));
            OnBoostChanged?.Invoke();
            _save.SaveAsync().Forget();
            return true;
        }

        /// <summary>
        /// Adds boosts to inventory. Used for ad rewards, daily milestone grants, and starter grants.
        /// Publishes <see cref="OnBoostGranted"/> and auto-saves.
        /// </summary>
        public void Grant(BoostType type, int count)
        {
            if (count <= 0) return;
            Add(type, count);
            _events.Publish(new OnBoostGranted(type, count, GetCount(type)));
            OnBoostChanged?.Invoke();
            _save.SaveAsync().Forget();
        }

        /// <summary>
        /// Spends gold via GoldManager to purchase one boost. Returns false if the player can't afford it.
        /// Used by ShopScreen — not used for ad-reward grants (use <see cref="Grant"/> for those).
        /// </summary>
        public bool TryBuyWithGold(BoostType type)
        {
            int cost = _config.GetCost(type);
            if (!_gold.TrySpend(cost, "boost_" + type.ToString().ToLower())) return false;
            Grant(type, 1);
            return true;
        }

        private void Add(BoostType type, int count)
        {
            switch (type)
            {
                case BoostType.Undo:           _save.Data.UndoCount           += count; break;
                case BoostType.ExtraEmptyTube: _save.Data.ExtraEmptyTubeCount += count; break;
            }
        }

        private void Deduct(BoostType type, int count)
        {
            switch (type)
            {
                case BoostType.Undo:           _save.Data.UndoCount           = Mathf.Max(0, _save.Data.UndoCount           - count); break;
                case BoostType.ExtraEmptyTube: _save.Data.ExtraEmptyTubeCount = Mathf.Max(0, _save.Data.ExtraEmptyTubeCount - count); break;
            }
        }
    }
}
