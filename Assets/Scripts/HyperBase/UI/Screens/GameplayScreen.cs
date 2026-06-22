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
        [SerializeField] private Button            _homeBtn;
        [SerializeField] private Button            _restartBtn;

        private GameManager     _game;
        private AudioManager    _audio;
        private EventBus        _events;
        private LevelController _levelController;
        private HyperBase.Data.SaveManager _save;

        [Inject]
        public void Construct(GameManager game, AudioManager audio, EventBus events,
                              HyperBase.Data.SaveManager save)
        {
            _game = game; _audio = audio; _events = events; _save = save;
        }

        public void SetLevelController(LevelController lc)
        {
            _levelController = lc;
            _boostBar?.SetController(lc);
        }

        protected override void Awake()
        {
            base.Awake();
            if (_homeBtn)    _homeBtn.onClick.AddListener(OnHome);
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

        private void OnHome()
        {
            _audio.PlayButtonClick();
            _save.SaveAsync().Forget();
            _game.TransitionTo(GameState.MainMenu);
        }

        private void OnLevelStarted(OnLevelStarted e)
        {
            if (_levelLabel) _levelLabel.text = e.LevelIndex >= 0
                ? $"LEVEL {e.LevelIndex + 1}"
                : "DAILY";
        }
    }
}
