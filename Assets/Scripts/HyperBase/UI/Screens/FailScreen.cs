using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Monetization;
using SortPuzzle.Economy;
using SortPuzzle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace HyperBase.UI.Screens
{
    /// <summary>
    /// Fail screen — Retry / Menu / Watch ad for 1 Undo boost.
    /// </summary>
    public class FailScreen : UIScreen
    {
        [Header("Buttons")]
        [SerializeField] private Button _retryBtn;
        [SerializeField] private Button _menuBtn;
        [SerializeField] private Button _watchAdUndoBtn;   // watch ad → grant 1 Undo, return to gameplay

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI _undoCountLabel;   // shows current undo count

        private GameManager  _game;
        private UIManager    _ui;
        private AudioManager _audio;
        private AdManager    _ads;
        private BoostManager _boosts;
        private EventBus     _events;

        [Inject]
        public void Construct(GameManager game, UIManager ui, AudioManager audio,
                              AdManager ads, BoostManager boosts, EventBus events)
        { _game = game; _ui = ui; _audio = audio; _ads = ads; _boosts = boosts; _events = events; }

        protected override void Awake()
        {
            base.Awake();
            if (_retryBtn)       _retryBtn.onClick.AddListener(OnRetry);
            if (_menuBtn)        _menuBtn.onClick.AddListener(OnMenu);
            if (_watchAdUndoBtn) _watchAdUndoBtn.onClick.AddListener(OnWatchAdForUndo);
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                _audio.PlayLevelFail();

                bool adReady = _ads.IsRewardedReady();
                if (_watchAdUndoBtn) _watchAdUndoBtn.gameObject.SetActive(adReady);

                int undoCount = _boosts?.GetCount(BoostType.Undo) ?? 0;
                if (_undoCountLabel) _undoCountLabel.text = $"Undos: {undoCount}";
            }
            await UniTask.CompletedTask;
        }

        private void OnRetry()
        {
            _audio.PlayButtonClick();
            _game.TransitionTo(GameState.Gameplay);
        }

        private void OnMenu()
        {
            _audio.PlayButtonClick();
            _game.TransitionTo(GameState.MainMenu);
        }

        private void OnWatchAdForUndo()
        {
            _audio.PlayButtonClick();
            _ads.ShowRewarded(success =>
            {
                if (!success) return;
                _boosts.Grant(BoostType.Undo, 1);
                _events.Publish(new OnAdCompleted(AdType.Rewarded, true));
                // Return to gameplay with the new undo available
                _game.TransitionTo(GameState.Gameplay);
            }, "undo_from_fail");
        }
    }
}
