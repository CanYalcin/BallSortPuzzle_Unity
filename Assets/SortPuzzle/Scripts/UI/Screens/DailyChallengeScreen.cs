using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Gameplay;
using HyperBase.UI;
using HyperBase.UI.Screens;
using SortPuzzle.DailyChallenge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SortPuzzle.UI.Screens
{
    /// <summary>
    /// Daily Challenge screen — 30-day calendar, streak label, Play Today button.
    /// Calls LevelManager.StartDailyChallenge() so the daily level is loaded
    /// and DailyManager.CompleteChallenge() is triggered on win via GameSceneEntryPoint.
    /// </summary>
    public class DailyChallengeScreen : UIScreen
    {
        [SerializeField] private TextMeshProUGUI _streakLabel;
        [SerializeField] private Transform       _calendarGrid;
        [SerializeField] private DayCellWidget   _dayCellPrefab;
        [SerializeField] private Button          _playTodayBtn;
        [SerializeField] private TextMeshProUGUI _playTodayLabel;
        [SerializeField] private GameObject      _alreadyPlayedLabel;
        [SerializeField] private Button          _backBtn;

        private DailyManager _daily;
        private LevelManager _levels;
        private UIManager    _ui;
        private AudioManager _audio;

        [Inject]
        public void Construct(DailyManager daily, LevelManager levels, UIManager ui, AudioManager audio)
        {
            _daily  = daily;
            _levels = levels;
            _ui     = ui;
            _audio  = audio;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_playTodayBtn) _playTodayBtn.onClick.AddListener(OnPlayToday);
            if (_backBtn)      _backBtn.onClick.AddListener(() =>
            {
                _audio.PlayButtonClick();
                _ui.ShowScreenAsync<MainMenuScreen>().Forget();
            });
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                int streak = _daily.CurrentStreak;
                if (_streakLabel)
                    _streakLabel.text = streak > 0 ? $"🔥 {streak} day streak!" : "Start your streak today!";

                if (_calendarGrid != null && _dayCellPrefab != null)
                {
                    for (int i = _calendarGrid.childCount - 1; i >= 0; i--)
                        Destroy(_calendarGrid.GetChild(i).gameObject);

                    int todayIdx = _daily.TodayIndex;
                    for (int d = 0; d < 30; d++)
                    {
                        var cell     = Instantiate(_dayCellPrefab, _calendarGrid);
                        bool done    = _daily.IsDayCompleted(d);
                        bool isToday = d == todayIdx;
                        cell.Setup(d + 1, done, isToday);
                    }
                }

                bool canPlay = _daily.CanPlayToday();
                if (_playTodayBtn)       _playTodayBtn.interactable = canPlay;
                if (_playTodayLabel)     _playTodayLabel.text = canPlay ? "PLAY TODAY" : "✓ COMPLETED";
                if (_alreadyPlayedLabel) _alreadyPlayedLabel.SetActive(!canPlay);
            }
            await UniTask.CompletedTask;
        }

        private void OnPlayToday()
        {
            _audio.PlayButtonClick();
            var dailyLevel = _daily.GetTodaysLevel();
            if (dailyLevel != null)
                _levels.StartDailyChallenge(dailyLevel);
            else
                _ui.ShowScreenAsync<GameplayScreen>().Forget(); // fallback
        }
    }
}
