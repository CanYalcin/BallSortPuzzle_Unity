using SortPuzzle.Economy;
using TMPro;
using UnityEngine;
using VContainer;

namespace SortPuzzle.UI.Widgets
{
    /// <summary>
    /// Displays current gold balance.
    ///
    /// Subscribes in Start() — NOT OnEnable() — because VContainer injection
    /// (LifetimeScope.Awake) is not guaranteed to complete before OnEnable fires.
    /// By Start(), all [Inject] calls are done and _gold is valid.
    /// </summary>
    public class GoldCounterWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private GoldManager _gold;

        [Inject]
        public void Construct(GoldManager gold) => _gold = gold;

        private void Start()
        {
            if (_gold == null) return;
            _gold.OnGoldChanged += Refresh;
            Refresh(_gold.Balance);
        }

        private void OnDestroy()
        {
            if (_gold != null) _gold.OnGoldChanged -= Refresh;
        }

        private void Refresh(int newBalance)
        {
            if (_label) _label.text = newBalance.ToString("N0");
        }
    }
}
