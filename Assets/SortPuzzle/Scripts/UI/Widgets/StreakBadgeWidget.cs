using SortPuzzle.DailyChallenge;
using TMPro;
using UnityEngine;
using VContainer;

namespace SortPuzzle.UI.Widgets
{
    /// <summary>
    /// Shows current streak count. Updates in Start() — injection guaranteed complete by then.
    /// Streak increases on daily challenge WIN only (not on login).
    /// </summary>
    public class StreakBadgeWidget : MonoBehaviour
    {
        [SerializeField] private GameObject      _root;
        [SerializeField] private TextMeshProUGUI _streakLabel;

        private DailyManager _daily;

        [Inject]
        public void Construct(DailyManager daily) => _daily = daily;

        private void Start()
        {
            if (_daily == null) return;
            int streak = _daily.CurrentStreak;
            if (_root)        _root.SetActive(streak > 0);
            if (_streakLabel) _streakLabel.text = streak.ToString();
        }
    }
}
