using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortPuzzle.UI.Screens
{
    /// <summary>
    /// One day bubble in the DailyChallengeScreen calendar grid.
    /// Shows day number, a checkmark if completed, and a highlight ring if today.
    /// </summary>
    public class DayCellWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _dayLabel;
        [SerializeField] private GameObject      _completedMark;  // checkmark overlay
        [SerializeField] private GameObject      _todayRing;      // highlight border
        [SerializeField] private Image           _background;
        [SerializeField] private Color           _completedColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color           _todayColor     = new Color(1.0f, 0.8f, 0.0f);
        [SerializeField] private Color           _defaultColor   = new Color(0.25f, 0.25f, 0.3f);

        public void Setup(int dayNumber, bool completed, bool isToday)
        {
            if (_dayLabel)       _dayLabel.text = dayNumber.ToString();
            if (_completedMark)  _completedMark.SetActive(completed);
            if (_todayRing)      _todayRing.SetActive(isToday);
            if (_background)
            {
                _background.color = completed ? _completedColor
                                  : isToday   ? _todayColor
                                              : _defaultColor;
            }
        }
    }
}
