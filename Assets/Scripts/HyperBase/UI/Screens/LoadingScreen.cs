using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HyperBase.Core;
using HyperBase.Audio;
using HyperBase.Currency;
using VContainer;

namespace HyperBase.UI.Screens
{
    public class LoadingScreen : UIScreen
    {
        [SerializeField] private Slider          _bar;
        [SerializeField] private TextMeshProUGUI _pct;
        [SerializeField] private TextMeshProUGUI _tip;

        private static readonly string[] Tips =
        {
            "Complete levels to unlock harder challenges!",
            "Watch ads to double your coin rewards!",
            "Gems are awarded for perfect completions!"
        };

        protected override async UniTask HandleLifecycle(LifecycleEvent evt)
        {
            if (evt == LifecycleEvent.BeforeShow)
            {
                SetProgress(0f);
                if (_tip) _tip.text = Tips[Random.Range(0, Tips.Length)];
            }
            await UniTask.CompletedTask;
        }

        public void SetProgress(float t)
        {
            float v = Mathf.Clamp01(t);
            if (_bar) _bar.value = v;
            if (_pct) _pct.text  = $"{Mathf.RoundToInt(v * 100)}%";
        }
    }
}
