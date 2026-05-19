using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace HyperBase.UI.Screens
{
    public class WinScreen : UIScreen
    {
        [SerializeField] private TextMeshProUGUI _goldEarnedLabel;
        [SerializeField] private Button          _nextBtn;
        [SerializeField] private Button          _tripleGoldBtn;

        private GameManager  _game;
        private AudioManager _audio;
        private AdManager    _ads;
        private EventBus     _events;

        private int  _baseGold;
        private bool _isDaily;

        [Inject]
        public void Construct(GameManager game, AudioManager audio, AdManager ads, EventBus events)
        { _game = game; _audio = audio; _ads = ads; _events = events; }

        protected override void Awake()
        {
            base.Awake();
            if (_tripleGoldBtn) _tripleGoldBtn.onClick.AddListener(OnTripleGold);
        }

        public void SetWinData(int goldEarned, bool isDaily = false)
        {
            _baseGold = goldEarned;
            _isDaily  = isDaily;
        }

        public void SetReward(int amount)
        {
            if (_baseGold == 0) _baseGold = amount;
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                _audio.PlayLevelComplete();

                if (_goldEarnedLabel) _goldEarnedLabel.text = $"+{_baseGold} gold";

                if (_tripleGoldBtn)
                {
                    bool show = !_isDaily && _ads.IsRewardedReady();
                    _tripleGoldBtn.gameObject.SetActive(show);
                }

                if (_nextBtn)
                {
                    _nextBtn.onClick.RemoveAllListeners();
                    if (_isDaily)
                    {
                        _nextBtn.onClick.AddListener(() => { _audio.PlayButtonClick(); _game.TransitionTo(GameState.MainMenu); });
                        var lbl = _nextBtn.GetComponentInChildren<TextMeshProUGUI>();
                        if (lbl) lbl.text = "BACK TO MENU";
                    }
                    else
                    {
                        _nextBtn.onClick.AddListener(() => { _audio.PlayButtonClick(); _game.TransitionTo(GameState.Gameplay); });
                        var lbl = _nextBtn.GetComponentInChildren<TextMeshProUGUI>();
                        if (lbl) lbl.text = "NEXT LEVEL";
                    }
                }
            }
            await UniTask.CompletedTask;
        }

        private void OnTripleGold()
        {
            _audio.PlayButtonClick();
            _ads.ShowRewarded(success =>
            {
                if (!success) return;
                _events.Publish(new OnAdCompleted(AdType.Rewarded, true));
                int tripled = _baseGold * 3;
                if (_goldEarnedLabel) _goldEarnedLabel.text = $"+{tripled} gold";
                if (_tripleGoldBtn)   _tripleGoldBtn.gameObject.SetActive(false);
            }, "triple_gold_win");
        }
    }
}
