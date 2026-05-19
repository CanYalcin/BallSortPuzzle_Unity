using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Monetization;
using SortPuzzle.Economy;
using SortPuzzle.UI.Screens;
using SortPuzzle.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace HyperBase.UI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        [Header("Core Buttons")]
        [SerializeField] private Button _playBtn;
        [SerializeField] private Button _settingsBtn;
        [SerializeField] private Button _noAdsBtn;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _dailyBtn;
        [SerializeField] private Button _shopBtn;
        [SerializeField] private Button _worldMapBtn;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private TextMeshProUGUI _goldLabel;      // direct reference — widget injection unreliable in MainMenu scene
        [SerializeField] private TextMeshProUGUI _streakLabel;    // direct reference

        private GameManager  _game;
        private UIManager    _ui;
        private AudioManager _audio;
        private IAPManager   _iap;
        private AdManager    _ads;
        private EventBus     _events;
        private HyperBase.Data.SaveManager _save;
        private GoldManager  _gold;
        private SortPuzzle.DailyChallenge.DailyManager _daily;

        [Inject]
        public void Construct(GameManager game, UIManager ui, AudioManager audio,
                              IAPManager iap, AdManager ads, EventBus events,
                              HyperBase.Data.SaveManager save, GoldManager gold,
                              SortPuzzle.DailyChallenge.DailyManager daily)
        {
            _game  = game; _ui = ui; _audio = audio;
            _iap   = iap;  _ads = ads; _events = events;
            _save  = save; _gold = gold; _daily = daily;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_playBtn)     _playBtn.onClick.AddListener(OnPlay);
            if (_settingsBtn) _settingsBtn.onClick.AddListener(OnSettings);
            if (_noAdsBtn)    _noAdsBtn.onClick.AddListener(OnNoAds);
            if (_dailyBtn)    _dailyBtn.onClick.AddListener(OnDaily);
            if (_shopBtn)     _shopBtn.onClick.AddListener(OnShop);
            if (_worldMapBtn) _worldMapBtn.onClick.AddListener(OnWorldMap);
        }

        private void Start()
        {
            // Subscribe here — injection guaranteed complete by Start()
            if (_gold != null) _gold.OnGoldChanged += OnGoldChanged;
        }

        private void OnDestroy()
        {
            if (_gold != null) _gold.OnGoldChanged -= OnGoldChanged;
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                if (_levelLabel) _levelLabel.text = $"LEVEL {_save.Data.CurrentLevelIndex + 1}";
                if (_goldLabel)  _goldLabel.text  = _gold?.Balance.ToString("N0") ?? "0";

                int streak = _daily?.CurrentStreak ?? 0;
                if (_streakLabel) _streakLabel.text = streak > 0 ? $"🔥 {streak}" : "";

                if (_noAdsBtn) _noAdsBtn.gameObject.SetActive(_ads.Config.EnableIAP && !_save.Data.IsNoAds);
                _events.Subscribe<OnNoAdsActivated>(OnNoAdsActivated);
            }
            else if (evt == LifecycleEvent.AfterHide)
            {
                _events.Unsubscribe<OnNoAdsActivated>(OnNoAdsActivated);
            }
            await UniTask.CompletedTask;
        }

        private void OnGoldChanged(int newBalance)
        {
            if (_goldLabel) _goldLabel.text = newBalance.ToString("N0");
        }

        private void OnPlay()     { _audio.PlayButtonClick(); _game.TransitionTo(GameState.Gameplay); }
        private void OnSettings() { _audio.PlayButtonClick(); _ui.ShowScreenAsync<SettingsScreen>().Forget(); }
        private void OnNoAds()    { _audio.PlayButtonClick(); _iap.PurchaseAsync(IAPManager.ProductIds.NoAds).Forget(); }
        private void OnDaily()    { _audio.PlayButtonClick(); _ui.ShowScreenAsync<DailyChallengeScreen>().Forget(); }
        private void OnShop()     { _audio.PlayButtonClick(); _ui.ShowScreenAsync<ShopScreen>().Forget(); }
        private void OnWorldMap() { _audio.PlayButtonClick(); _ui.ShowScreenAsync<WorldMapScreen>().Forget(); }
        private void OnNoAdsActivated(OnNoAdsActivated _) { if (_noAdsBtn) _noAdsBtn.gameObject.SetActive(false); }
    }
}
