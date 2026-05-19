using HyperBase.Core;
using HyperBase.Data;
using SortPuzzle.Data;
using UnityEngine;
using VContainer;

namespace SortPuzzle.Economy
{
    public class BoostManager
    {
        public event System.Action OnBoostChanged;
        public void ForceNotify() => OnBoostChanged?.Invoke();

        private readonly SaveManager _save;
        private readonly EventBus    _events;
        private readonly GoldManager _gold;
        private readonly BoostConfig _config;

        [Inject]
        public BoostManager(SaveManager save, EventBus events, GoldManager gold, BoostConfig config)
        { _save = save; _events = events; _gold = gold; _config = config; }

        public int  GetCount(BoostType type) => type switch
        {
            BoostType.Undo           => _save.Data.UndoCount,
            BoostType.ExtraEmptyTube => _save.Data.ExtraEmptyTubeCount,
            _                        => 0
        };

        public bool HasBoost(BoostType type) => GetCount(type) > 0;

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

        public void Grant(BoostType type, int count)
        {
            if (count <= 0) return;
            Add(type, count);
            _events.Publish(new OnBoostGranted(type, count, GetCount(type)));
            OnBoostChanged?.Invoke();
            _save.SaveAsync().Forget();
        }

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
