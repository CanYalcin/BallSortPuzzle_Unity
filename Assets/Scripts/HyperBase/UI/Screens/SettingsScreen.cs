using Cysharp.Threading.Tasks;
using HyperBase.Audio;
using HyperBase.Core;
using HyperBase.Data;
using HyperBase.Haptics;
using HyperBase.Monetization;
using HyperBase.UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace HyperBase.UI.Screens
{
    public class SettingsScreen : UIScreen
    {
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Button _closeBtn;
        [SerializeField] private Button _homeBtn;
        [SerializeField] private Button _restoreBtn;

        private AudioManager   _audio;
        private HapticsManager _haptics;
        private UIManager      _ui;
        private SaveManager    _save;
        private GameManager    _game;
        private IAPManager     _iap;

        [Inject]
        public void Construct(AudioManager audio, HapticsManager haptics,
                              UIManager ui, SaveManager save, GameManager game, IAPManager iap)
        { _audio = audio; _haptics = haptics; _ui = ui; _save = save; _game = game; _iap = iap; }

        protected override void Awake()
        {
            base.Awake();
            if (_closeBtn)   _closeBtn.onClick.AddListener(OnClose);
            if (_homeBtn)    _homeBtn.onClick.AddListener(OnHome);
            if (_restoreBtn) _restoreBtn.onClick.AddListener(OnRestore);
            if (_soundToggle)   _soundToggle.onValueChanged.AddListener(v   => { _audio.SetSoundEnabled(v); _haptics.MediumImpact(); });
            if (_musicToggle)   _musicToggle.onValueChanged.AddListener(v   => _audio.SetMusicEnabled(v));
            if (_hapticsToggle) _hapticsToggle.onValueChanged.AddListener(v => _haptics.SetEnabled(v));
            if (_masterSlider)  _masterSlider.onValueChanged.AddListener(v  => _audio.SetMasterVolume(v));
            if (_sfxSlider)     _sfxSlider.onValueChanged.AddListener(v     => _audio.SetSfxVolume(v));
            if (_musicSlider)   _musicSlider.onValueChanged.AddListener(v   => _audio.SetMusicVolume(v));
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                var d = _save.Data;
                if (_soundToggle)   _soundToggle.isOn   = d.SoundEnabled;
                if (_musicToggle)   _musicToggle.isOn   = d.MusicEnabled;
                if (_hapticsToggle) _hapticsToggle.isOn = d.HapticsEnabled;
                if (_masterSlider)  _masterSlider.value = d.MasterVolume;
                if (_sfxSlider)     _sfxSlider.value    = d.SfxVolume;
                if (_musicSlider)   _musicSlider.value  = d.MusicVolume;
                if (_homeBtn)       _homeBtn.gameObject.SetActive(_game.CurrentState == GameState.Paused);
            }
            await UniTask.CompletedTask;
        }

        public override void OnBackPressed() => OnClose();

        private void OnClose()
        {
            _audio.PlayButtonClick();
            _save.SaveAsync().Forget();
            if (_game != null && _game.CurrentState == GameState.Paused)
                _game.Resume();
            _ui.GoBackAsync().Forget();
        }

        private void OnHome()
        {
            _audio.PlayButtonClick();
            _save.SaveAsync().Forget();
            _game.TransitionTo(GameState.MainMenu);
        }

        private void OnRestore()
        {
            _audio.PlayButtonClick();
            // Contacts Apple/Google to restore non-consumable purchases (e.g. No Ads).
            // Required by Apple App Store guidelines for non-consumable IAPs.
            _iap?.RestorePurchasesAsync().Forget();
        }
    }
}
