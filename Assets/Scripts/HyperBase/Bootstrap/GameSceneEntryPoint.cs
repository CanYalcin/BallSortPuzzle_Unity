using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Gameplay;
using HyperBase.Monetization;
using HyperBase.StoreReview;
using HyperBase.Utilities;
using SortPuzzle.DailyChallenge;
using SortPuzzle.Economy;
using SortPuzzle.Gameplay;
using HyperBase.UI;
using HyperBase.UI.Screens;
using SortPuzzle.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Bootstrap
{
    public class GameSceneEntryPoint : IStartable, System.IDisposable
    {
        private const string SceneMainMenu = "MainMenu";
        private const string SceneGame     = "GameScene";

        private readonly UIManager        _ui;
        private readonly EventBus         _events;
        private readonly GameManager      _game;
        private readonly LevelManager     _levels;
        private readonly AdManager        _ads;
        private readonly AudioManager     _audio;
        private readonly RateUsManager    _rateUs;
        private readonly GoldManager      _gold;
        private readonly BoostManager     _boosts;
        private readonly DailyManager     _daily;
        private readonly SceneLoader      _loader;
        private readonly HyperBase.Data.SaveManager _save;

        private readonly MainMenuScreen  _mainMenu;
        private readonly GameplayScreen  _gameplay;
        private readonly WinScreen       _win;
        private readonly FailScreen      _fail;
        private readonly LoadingScreen   _loading;
        private readonly SettingsScreen  _settings;

        private DailyChallengeScreen _dailyChallenge;
        private ShopScreen           _shop;
        private int                  _levelsSinceAd;
        private const int            InterstitialEveryN = 3;private System.Action<OnNoAdsActivated> _onNoAds;

        [Inject]
        public GameSceneEntryPoint(
            UIManager        ui,
            EventBus         events,
            GameManager      game,
            LevelManager     levels,
            AdManager        ads,
            AudioManager     audio,
            RateUsManager    rateUs,
            GoldManager      gold,
            BoostManager     boosts,
            DailyManager     daily,
            SceneLoader      loader,
            HyperBase.Data.SaveManager save,
            MainMenuScreen   mainMenu,
            GameplayScreen   gameplay,
            WinScreen        win,
            FailScreen       fail,
            LoadingScreen    loading,
            SettingsScreen   settings)
        {
            _ui        = ui;
            _events    = events;
            _game      = game;
            _levels    = levels;
            _ads       = ads;
            _audio     = audio;
            _rateUs    = rateUs;
            _gold      = gold;
            _boosts    = boosts;
            _daily     = daily;
            _loader    = loader;
            _save      = save;
            _mainMenu  = mainMenu;
            _gameplay  = gameplay;
            _win       = win;
            _fail      = fail;
            _loading   = loading;
            _settings  = settings;
        }

        public void Start()
        {
            _ui.RegisterScreen(_mainMenu);
            _ui.RegisterScreen(_gameplay);
            _ui.RegisterScreen(_win);
            _ui.RegisterScreen(_fail);
            _ui.RegisterScreen(_loading);
            _ui.RegisterScreen(_settings);

            var dc = Object.FindFirstObjectByType<DailyChallengeScreen>(FindObjectsInactive.Include);
            var sh = Object.FindFirstObjectByType<ShopScreen>(FindObjectsInactive.Include);
            if (dc) { _dailyChallenge = dc; _ui.RegisterScreen(dc); }
            if (sh) { _shop = sh;           _ui.RegisterScreen(sh); }

            _onNoAds = _ => _ads.ActivateNoAds();
            _events.Subscribe<OnGameStateChanged>    (OnStateChanged);
            _events.Subscribe<OnLevelCompleted>      (OnLevelCompleted);            _events.Subscribe<OnNoAdsActivated>      (_onNoAds);
            _events.Subscribe<OnPurchaseCompleted>   (OnPurchased);
            _events.Subscribe<SortPuzzle.OnPuzzleWon>(OnPuzzleWon);

            _gold.TryClaimDailyLoginBonus();

            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == SceneGame)
            {
                var lc = Object.FindFirstObjectByType<LevelController>();
                _gameplay.SetLevelController(lc);
                _ui.ShowScreenAsync<GameplayScreen>().Forget();
                _audio.PlayMusic(_audio.Config?.GameplayMusic);

                // If daily mode was set before scene load, DON'T call StartCurrentLevel
                // (which resets _dailyMode). LevelController.Start() reads IsDailyMode itself.
                if (!_levels.IsDailyMode)
                    _levels.StartCurrentLevel();
                else
                    _events.Publish(new OnLevelStarted(-1)); // daily — just fire HUD event
            }
            else
            {
                _ui.ShowScreenAsync<MainMenuScreen>().Forget();
                _audio.PlayMusic(_audio.Config?.MainMenuMusic);
            }
        }

        private void OnStateChanged(OnGameStateChanged e)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            switch (e.Current)
            {
                case GameState.MainMenu:
                    _levels.ResetDailyMode(); // always clear daily flag on menu return
                    if (currentScene != SceneMainMenu)
                        _loader.LoadSceneAsync(SceneMainMenu).Forget();
                    else
                        _ui.ShowScreenAsync<MainMenuScreen>().Forget();
                    _audio.PlayMusic(_audio.Config?.MainMenuMusic);
                    break;

                case GameState.Gameplay:
                    _loader.LoadSceneAsync(SceneGame).Forget();
                    _audio.PlayMusic(_audio.Config?.GameplayMusic);
                    break;

                case GameState.Win:                    _ui.ShowScreenAsync<WinScreen>().Forget();
                    _audio.PlayMusic(_audio.Config?.WinMusic);
                    _rateUs.TryPromptAsync().Forget();
                    break;

                case GameState.Fail:
                    _ui.ShowScreenAsync<FailScreen>().Forget();
                    break;
            }
        }

private void OnPuzzleWon(SortPuzzle.OnPuzzleWon e)
        {
            bool isDaily           = _levels.IsDailyMode;
            int  goldActuallyAdded = isDaily ? 100 : e.GoldEarned; // e.GoldEarned is LevelData.GoldReward, computed once in PuzzleController.Pour()
            _win.SetWinData(goldActuallyAdded, isDaily);
        }

        private void OnLevelCompleted(OnLevelCompleted e)
        {
            if (e.IsDaily)
            {
                _daily.CompleteChallenge();
                return;
            }

            _levelsSinceAd++;
            if (_levelsSinceAd >= InterstitialEveryN)
            {
                _ads.TryShowInterstitial("level_complete_" + e.LevelIndex);
                _levelsSinceAd = 0;
            }
            _ads.SetCurrentLevel(_levels.CurrentIndex);
            if (e.LevelIndex == 2 && !_save.Data.StarterPackPurchased && _shop != null)
                _ui.ShowScreenAsync<ShopScreen>().Forget();
        }



        public void Dispose()
        {
            _events.Unsubscribe<OnGameStateChanged>    (OnStateChanged);
            _events.Unsubscribe<OnLevelCompleted>      (OnLevelCompleted);            _events.Unsubscribe<OnNoAdsActivated>      (_onNoAds);
            _events.Unsubscribe<OnPurchaseCompleted>   (OnPurchased);
            _events.Unsubscribe<SortPuzzle.OnPuzzleWon>(OnPuzzleWon);
        }

        private void OnPurchased(OnPurchaseCompleted e)
        {
            string id = e.ProductId;
            if (id == IAPManager.ProductIds.NoAds)
            {
                _save.Data.IsNoAds = true;
                _events.Publish(new OnNoAdsActivated());
                _save.SaveAsync().Forget();
                return;
            }
            if (id == IAPManager.ProductIds.Gold1000)  { _gold.Add(1000,  "iap_gold"); return; }
            if (id == IAPManager.ProductIds.Gold5000)  { _gold.Add(5000,  "iap_gold"); return; }
            if (id == IAPManager.ProductIds.Gold10000) { _gold.Add(10000, "iap_gold"); return; }
            if (id == IAPManager.ProductIds.Gold25000) { _gold.Add(25000, "iap_gold"); return; }

            if (id == IAPManager.ProductIds.PackStarter)
            {
                _save.Data.StarterPackPurchased = true;
                _gold.Add(4500, "iap_pack");
                _boosts.Grant(SortPuzzle.Data.BoostType.Undo,           1);
                _boosts.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 1);
                _save.SaveAsync().Forget();
                return;
            }
            if (id == IAPManager.ProductIds.PackSmall)
            {
                _gold.Add(4500, "iap_pack");
                _boosts.Grant(SortPuzzle.Data.BoostType.Undo,           1);
                _boosts.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 1);
                return;
            }
            if (id == IAPManager.ProductIds.PackMid)
            {
                _gold.Add(7500, "iap_pack");
                _boosts.Grant(SortPuzzle.Data.BoostType.Undo,           2);
                _boosts.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 2);
                _save.Data.IsNoAds = true;
                _events.Publish(new OnNoAdsActivated());
                _save.SaveAsync().Forget();
                return;
            }
            if (id == IAPManager.ProductIds.PackBig)
            {
                _gold.Add(20000, "iap_pack");
                _boosts.Grant(SortPuzzle.Data.BoostType.Undo,           4);
                _boosts.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 4);
                _save.Data.IsNoAds = true;
                _events.Publish(new OnNoAdsActivated());
                _save.SaveAsync().Forget();
                return;
            }
            if (id == IAPManager.ProductIds.PackMega)
            {
                _gold.Add(85000, "iap_pack");
                _boosts.Grant(SortPuzzle.Data.BoostType.Undo,           8);
                _boosts.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 8);
                _save.Data.IsNoAds = true;
                _events.Publish(new OnNoAdsActivated());
                _save.SaveAsync().Forget();
                return;
            }
            if (id == IAPManager.ProductIds.PackPremium)
            {
                _gold.Add(200000, "iap_pack");
                _boosts.Grant(SortPuzzle.Data.BoostType.Undo,           12);
                _boosts.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 12);
                _save.Data.IsNoAds = true;
                _events.Publish(new OnNoAdsActivated());
                _save.SaveAsync().Forget();
                return;
            }
            Debug.LogWarning($"[GameSceneEntryPoint] Unhandled product: {id}");
        }
    }
}
