using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using SortPuzzle.Gameplay;
using SortPuzzle.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace HyperBase.UI.Screens
{
    public class GameplayScreen : UIScreen
    {
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private GoldCounterWidget _goldCounter;
        [SerializeField] private BoostBarWidget    _boostBar;
        [SerializeField] private Button            _pauseBtn;
        [SerializeField] private Button            _restartBtn;

        private GameManager     _game;
        private AudioManager    _audio;
        private EventBus        _events;
        private LevelController _levelController;

        [Inject]
        public void Construct(GameManager game, AudioManager audio, EventBus events)
        { _game = game; _audio = audio; _events = events; }

        public void SetLevelController(LevelController lc)
        {
            _levelController = lc;
            _boostBar?.SetController(lc);
        }

        protected override void Awake()
        {
            base.Awake();
            if (_pauseBtn)   _pauseBtn.onClick.AddListener(() => { _audio.PlayButtonClick(); _game.Pause(); });
            if (_restartBtn) _restartBtn.onClick.AddListener(() => { _audio.PlayButtonClick(); _levelController?.OnRestartPressed(); });
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
                _events.Subscribe<OnLevelStarted>(OnLevelStarted);
            else if (evt == LifecycleEvent.AfterHide)
                _events.Unsubscribe<OnLevelStarted>(OnLevelStarted);
            await UniTask.CompletedTask;
        }

        private void OnLevelStarted(OnLevelStarted e)
        {
            if (_levelLabel) _levelLabel.text = e.LevelIndex >= 0
                ? $"LEVEL {e.LevelIndex + 1}"
                : "DAILY";
        }
    }
}
