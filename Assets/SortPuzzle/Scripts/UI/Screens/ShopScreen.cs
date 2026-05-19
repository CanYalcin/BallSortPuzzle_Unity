using SortPuzzle.Data;
using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Monetization;
using HyperBase.UI;
using HyperBase.UI.Screens;
using SortPuzzle.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SortPuzzle.UI.Screens
{
    public class ShopScreen : UIScreen
    {
        [Header("Balance")]
        [SerializeField] private TextMeshProUGUI _goldBalanceLabel;

        [Header("Tab Buttons")]
        [SerializeField] private Button _boostsTabBtn;
        [SerializeField] private Button _goldTabBtn;
        [SerializeField] private Button _packagesTabBtn;

        [Header("Panels")]
        [SerializeField] private GameObject _boostsPanel;
        [SerializeField] private GameObject _goldPanel;
        [SerializeField] private GameObject _packagesPanel;

        [Header("Boost Rows — Undo")]
        [SerializeField] private Button          _undoBuyBtn;
        [SerializeField] private TextMeshProUGUI _undoPriceLabel;
        [SerializeField] private TextMeshProUGUI _undoCountLabel;

        [Header("Boost Rows — Extra Tube")]
        [SerializeField] private Button          _extraTubeBuyBtn;
        [SerializeField] private TextMeshProUGUI _extraTubePriceLabel;
        [SerializeField] private TextMeshProUGUI _extraTubeCountLabel;

        [Header("Gold Pack Buttons (1000,5000,10000,25000)")]
        [SerializeField] private Button[] _goldPackBtns;

        [Header("Package Buttons (Starter,Small,Mid,Big,Mega,Premium)")]
        [SerializeField] private Button[]   _packageBtns;
        [SerializeField] private GameObject _starterPackBanner;

        [Header("No Ads")]
        [SerializeField] private Button     _noAdsBtn;
        [SerializeField] private GameObject _noAdsBtnRoot;

        [Header("Back")]
        [SerializeField] private Button _backBtn;

        private GoldManager  _gold;
        private BoostManager _boosts;
        private IAPManager   _iap;
        private BoostConfig  _config;
        private UIManager    _ui;
        private AudioManager _audio;
        private HyperBase.Data.SaveManager _save;

        [Inject]
        public void Construct(GoldManager gold, BoostManager boosts, IAPManager iap,
                              BoostConfig config, UIManager ui, AudioManager audio,
                              HyperBase.Data.SaveManager save)
        { _gold = gold; _boosts = boosts; _iap = iap; _config = config; _ui = ui; _audio = audio; _save = save; }

        protected override void Awake()
        {
            base.Awake();

            if (_boostsTabBtn)   _boostsTabBtn.onClick.AddListener(() =>   ShowTab(0));
            if (_goldTabBtn)     _goldTabBtn.onClick.AddListener(() =>     ShowTab(1));
            if (_packagesTabBtn) _packagesTabBtn.onClick.AddListener(() => ShowTab(2));

            if (_undoBuyBtn)      _undoBuyBtn.onClick.AddListener(OnBuyUndo);
            if (_extraTubeBuyBtn) _extraTubeBuyBtn.onClick.AddListener(OnBuyExtraTube);

            string[] goldIds = {
                IAPManager.ProductIds.Gold1000,
                IAPManager.ProductIds.Gold5000,
                IAPManager.ProductIds.Gold10000,
                IAPManager.ProductIds.Gold25000
            };
            for (int i = 0; i < _goldPackBtns.Length && i < goldIds.Length; i++)
            {
                int idx = i;
                if (_goldPackBtns[idx]) _goldPackBtns[idx].onClick.AddListener(() => _iap.PurchaseAsync(goldIds[idx]).Forget());
            }

            string[] packIds = {
                IAPManager.ProductIds.PackStarter,
                IAPManager.ProductIds.PackSmall,
                IAPManager.ProductIds.PackMid,
                IAPManager.ProductIds.PackBig,
                IAPManager.ProductIds.PackMega,
                IAPManager.ProductIds.PackPremium
            };
            for (int i = 0; i < _packageBtns.Length && i < packIds.Length; i++)
            {
                int idx = i;
                if (_packageBtns[idx]) _packageBtns[idx].onClick.AddListener(() => _iap.PurchaseAsync(packIds[idx]).Forget());
            }

            if (_noAdsBtn) _noAdsBtn.onClick.AddListener(() => { _audio.PlayButtonClick(); _iap.PurchaseAsync(IAPManager.ProductIds.NoAds).Forget(); });
            if (_backBtn)  _backBtn.onClick.AddListener(() => { _audio.PlayButtonClick(); _ui.ShowScreenAsync<MainMenuScreen>().Forget(); });
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                if (_goldBalanceLabel) _goldBalanceLabel.text = _gold.Balance.ToString("N0") + " gold";

                if (_undoPriceLabel)      _undoPriceLabel.text      = _config.UndoGoldCost + "g";
                if (_undoCountLabel)      _undoCountLabel.text      = "x" + _boosts.GetCount(BoostType.Undo);
                if (_extraTubePriceLabel) _extraTubePriceLabel.text = _config.ExtraEmptyTubeGoldCost + "g";
                if (_extraTubeCountLabel) _extraTubeCountLabel.text = "x" + _boosts.GetCount(BoostType.ExtraEmptyTube);

                if (_starterPackBanner) _starterPackBanner.SetActive(!_save.Data.StarterPackPurchased);
                if (_noAdsBtnRoot)      _noAdsBtnRoot.SetActive(!_save.Data.IsNoAds);

                ShowTab(0);
            }
            await UniTask.CompletedTask;
        }

        private void ShowTab(int tab)
        {
            if (_boostsPanel)   _boostsPanel.SetActive(tab == 0);
            if (_goldPanel)     _goldPanel.SetActive(tab == 1);
            if (_packagesPanel) _packagesPanel.SetActive(tab == 2);
        }

        private void OnBuyUndo()
        {
            _audio.PlayButtonClick();
            if (_boosts.TryBuyWithGold(BoostType.Undo))
            {
                if (_undoCountLabel)   _undoCountLabel.text   = "x" + _boosts.GetCount(BoostType.Undo);
                if (_goldBalanceLabel) _goldBalanceLabel.text = _gold.Balance.ToString("N0") + " gold";
            }
        }

        private void OnBuyExtraTube()
        {
            _audio.PlayButtonClick();
            if (_boosts.TryBuyWithGold(BoostType.ExtraEmptyTube))
            {
                if (_extraTubeCountLabel) _extraTubeCountLabel.text = "x" + _boosts.GetCount(BoostType.ExtraEmptyTube);
                if (_goldBalanceLabel)    _goldBalanceLabel.text     = _gold.Balance.ToString("N0") + " gold";
            }
        }
    }
}
