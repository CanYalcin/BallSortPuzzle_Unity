using HyperBase.Core;
using HyperBase.Data;
using UnityEngine;
using VContainer;

namespace HyperBase.Currency
{
    /// <summary>
    /// All currency mutations go through here — never write to PlayerData directly.
    /// Publishes OnCurrencyChanged and auto-saves on every change.
    /// </summary>
    public class CurrencyManager
    {
        private readonly SaveManager _save;
        private readonly EventBus    _events;

        public SaveManager SaveManager => _save;

        [Inject]
        public CurrencyManager(SaveManager save, EventBus events) { _save = save; _events = events; }

        public int  GetBalance(CurrencyType type) =>
            type == CurrencyType.Soft ? _save.Data.SoftCurrency : _save.Data.HardCurrency;

        public bool HasEnough(CurrencyType type, int amount) =>
            (type == CurrencyType.Soft ? _save.Data.SoftCurrency : _save.Data.HardCurrency) >= amount;

        public void Add(CurrencyType type, int amount)
        {
            if (amount <= 0) return;
            if (type == CurrencyType.Soft)
            {
                int prev = _save.Data.SoftCurrency;
                _save.Data.SoftCurrency = prev + amount;
                _events.Publish(new OnCurrencyChanged(type, prev, _save.Data.SoftCurrency));
            }
            else
            {
                int prev = _save.Data.HardCurrency;
                _save.Data.HardCurrency = prev + amount;
                _events.Publish(new OnCurrencyChanged(type, prev, _save.Data.HardCurrency));
            }
            _save.SaveAsync().Forget();
        }

        public bool TrySpend(CurrencyType type, int amount)
        {
            if (amount <= 0) return false;
            if (type == CurrencyType.Soft)
            {
                if (_save.Data.SoftCurrency < amount) { Debug.Log($"[Currency] Not enough Soft. Have {_save.Data.SoftCurrency}, need {amount}."); return false; }
                int prev = _save.Data.SoftCurrency;
                _save.Data.SoftCurrency = prev - amount;
                _events.Publish(new OnCurrencyChanged(type, prev, _save.Data.SoftCurrency));
            }
            else
            {
                if (_save.Data.HardCurrency < amount) { Debug.Log($"[Currency] Not enough Hard. Have {_save.Data.HardCurrency}, need {amount}."); return false; }
                int prev = _save.Data.HardCurrency;
                _save.Data.HardCurrency = prev - amount;
                _events.Publish(new OnCurrencyChanged(type, prev, _save.Data.HardCurrency));
            }
            _save.SaveAsync().Forget();
            return true;
        }

        public void SetAmount(CurrencyType type, int amount)
        {
            int clamped = Mathf.Max(0, amount);
            if (type == CurrencyType.Soft)
            {
                int prev = _save.Data.SoftCurrency;
                _save.Data.SoftCurrency = clamped;
                _events.Publish(new OnCurrencyChanged(type, prev, clamped));
            }
            else
            {
                int prev = _save.Data.HardCurrency;
                _save.Data.HardCurrency = clamped;
                _events.Publish(new OnCurrencyChanged(type, prev, clamped));
            }
            _save.SaveAsync().Forget();
        }
    }
}
