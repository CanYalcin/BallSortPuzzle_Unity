using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Data;
using HyperBase.Gameplay;
using HyperBase.UI;
using HyperBase.UI.Screens;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SortPuzzle.UI.Screens
{
    /// <summary>
    /// World map screen. Tapping a level cell jumps to that level index and loads GameScene.
    /// </summary>
    public class WorldMapScreen : UIScreen
    {
        [SerializeField] private WorldPanelView[] _worldPanelViews;
        [SerializeField] private Button           _backBtn;
        [SerializeField] private LevelCellWidget  _levelCellPrefab;

        private GameManager  _game;
        private SaveManager  _save;
        private LevelManager _levels;
        private UIManager    _ui;
        private AudioManager _audio;

        [Inject]
        public void Construct(GameManager game, SaveManager save, LevelManager levels,
                              UIManager ui, AudioManager audio)
        { _game = game; _save = save; _levels = levels; _ui = ui; _audio = audio; }

        protected override void Awake()
        {
            base.Awake();
            if (_backBtn) _backBtn.onClick.AddListener(() =>
            {
                _audio.PlayButtonClick();
                _ui.ShowScreenAsync<MainMenuScreen>().Forget();
            });
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow && _worldPanelViews != null)
            {
                for (int w = 0; w < _worldPanelViews.Length; w++)
                {
                    if (_worldPanelViews[w] == null) continue;
                    bool unlocked = w == 0 || _save.Data.GetLevelStars(w - 1, 19) > 0;
                    _worldPanelViews[w].cellPrefab = _levelCellPrefab;
                    int worldIdx = w;
                    _worldPanelViews[w].OnLevelSelected = (wi, levelIdx) =>
                    {
                        _audio.PlayButtonClick();
                        // Convert world+level index to global LevelDatabase index
                        int globalIndex = wi * 30 + levelIdx;
                        _levels.JumpToLevel(globalIndex);
                        _game.TransitionTo(GameState.Gameplay);
                    };
                    _worldPanelViews[w].Refresh(w, unlocked, _save.Data);
                }
            }
            await UniTask.CompletedTask;
        }
    }
}
