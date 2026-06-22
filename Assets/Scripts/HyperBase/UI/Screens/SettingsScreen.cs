using Cysharp.Threading.Tasks;
using HyperBase.Audio;
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
        [SerializeField] private Button _closeBtn;
        [SerializeField] private Button _restoreBtn;
        [SerializeField] private Button _contactUsBtn;
        [SerializeField] private Button _shareBtn;
        [SerializeField] private Button _termsOfUseBtn;
        [SerializeField] private Button _privacyPolicyBtn;

        private AudioManager   _audio;
        private HapticsManager _haptics;
        private UIManager      _ui;
        private SaveManager    _save;
                private IAPManager     _iap;

        [Inject]
        public void Construct(AudioManager audio, HapticsManager haptics,
                              UIManager ui, SaveManager save, IAPManager iap)
        { _audio = audio; _haptics = haptics; _ui = ui; _save = save; _iap = iap; }

        protected override void Awake()
        {
            base.Awake();
            if (_closeBtn)   _closeBtn.onClick.AddListener(OnClose);
            if (_restoreBtn) _restoreBtn.onClick.AddListener(OnRestore);
            if (_contactUsBtn)     _contactUsBtn.onClick.AddListener(OnContactUs);
            if (_shareBtn)         _shareBtn.onClick.AddListener(OnShare);
            if (_termsOfUseBtn)    _termsOfUseBtn.onClick.AddListener(OnTermsOfUse);
            if (_privacyPolicyBtn) _privacyPolicyBtn.onClick.AddListener(OnPrivacyPolicy);
            if (_soundToggle)   _soundToggle.onValueChanged.AddListener(v   => { _audio.SetSoundEnabled(v); _haptics.MediumImpact(); });
            if (_musicToggle)   _musicToggle.onValueChanged.AddListener(v   => _audio.SetMusicEnabled(v));
            if (_hapticsToggle) _hapticsToggle.onValueChanged.AddListener(v => _haptics.SetEnabled(v));
        }

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                var d = _save.Data;
                if (_soundToggle)   _soundToggle.isOn   = d.SoundEnabled;
                if (_musicToggle)   _musicToggle.isOn   = d.MusicEnabled;
                if (_hapticsToggle) _hapticsToggle.isOn = d.HapticsEnabled;
            }
            await UniTask.CompletedTask;
        }

        public override void OnBackPressed() => OnClose();

        private void OnClose()
        {
            _audio.PlayButtonClick();
            _save.SaveAsync().Forget();
            _ui.GoBackAsync().Forget();
        }

        private void OnRestore()
        {
            _audio.PlayButtonClick();
            // Contacts Apple/Google to restore non-consumable purchases (e.g. No Ads).
            // Required by Apple App Store guidelines for non-consumable IAPs.
            _iap?.RestorePurchasesAsync().Forget();
        }

        private void OnContactUs()
        {
            _audio.PlayButtonClick();
            // TODO: append UserID / app version / device info before publishing.
            Application.OpenURL("mailto:drkclltmcy@gmail.com");
        }

        private void OnShare()
        {
            _audio.PlayButtonClick();
            // TODO: placeholder store link until the game is published.
            Application.OpenURL("https://play.google.com/store/apps/details?id=com.ShapeOfVoid.BallSortPuzzleUnity");
        }

        private void OnTermsOfUse()
        {
            _audio.PlayButtonClick();
            // TODO: placeholder — replace with hosted Terms of Use page before publishing.
            Application.OpenURL("https://github.com/CanYalcin");
        }

        private void OnPrivacyPolicy()
        {
            _audio.PlayButtonClick();
            // TODO: placeholder — replace with hosted Privacy Policy page before publishing.
            Application.OpenURL("https://github.com/CanYalcin");
        }
    }
}
