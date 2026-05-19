using HyperBase.Core;
using HyperBase.Currency;
using UnityEngine;
using VContainer;

namespace HyperBase.UI
{
    /// <summary>
    /// Self-updating currency HUD widget. Subscribes to EventBus
    /// and animates via AnimatedCounter whenever the balance changes.
    /// </summary>
    public class CurrencyDisplayWidget : MonoBehaviour
    {
        [SerializeField] private AnimatedCounter _counter;
        [SerializeField] private CurrencyType    _type = CurrencyType.Soft;

        private CurrencyManager _currency;
        private EventBus        _events;

        [Inject]
        public void Construct(CurrencyManager currency, EventBus events)
        {
            _currency = currency;
            _events   = events;
        }

        private void OnEnable()
        {
            _events?.Subscribe<OnCurrencyChanged>(OnChanged);
            if (_counter != null && _currency != null)
                _counter.SetImmediate(_currency.GetBalance(_type));
        }

        private void OnDisable()
        {
            _events?.Unsubscribe<OnCurrencyChanged>(OnChanged);
        }

        private void OnChanged(OnCurrencyChanged e)
        {
            if (e.Type != _type) return;
            _counter?.AnimateTo(e.NewAmount);
        }
    }
}
