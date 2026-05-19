using Cysharp.Threading.Tasks;
using HyperBase.Utilities;
using TMPro;
using UnityEngine;

namespace HyperBase.UI
{
    /// <summary>
    /// Smoothly animates a TextMeshPro label from one numeric value to another.
    /// Supports integer display, K/M short notation, prefix/suffix.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AnimatedCounter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float  _duration      = 0.6f;
        [SerializeField] private bool   _shortNotation = false;
        [SerializeField] private string _format        = "N0";
        [SerializeField] private string _prefix        = "";
        [SerializeField] private string _suffix        = "";
        [SerializeField] private bool   _punchOnIncrease = true;

        private TextMeshProUGUI _label;
        private float _current;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        public float CurrentValue => _current;

        public void SetImmediate(float value)
        {
            _current = value;
            UpdateText(value);
        }

        public async UniTask AnimateToAsync(float target)
        {
            float from = _current;
            if (_punchOnIncrease && target > from)
            {
                transform.localScale = Vector3.one * 1.2f;
                float pt = 0f;
                while (pt < 0.12f)
                {
                    pt += Time.unscaledDeltaTime;
                    transform.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, pt / 0.12f);
                    await UniTask.Yield();
                }
                transform.localScale = Vector3.one;
            }

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed  += Time.unscaledDeltaTime;
                float t   = Mathf.Clamp01(elapsed / _duration);
                float eased = t * (2f - t); // EaseOutQuad
                _current  = Mathf.Lerp(from, target, eased);
                UpdateText(_current);
                await UniTask.Yield();
            }
            _current = target;
            UpdateText(target);
        }

        public void AnimateTo(float target) => AnimateToAsync(target).Forget();

        private void UpdateText(float v)
        {
            string s = _shortNotation ? ((int)v).ToShortNumber() : v.ToString(_format);
            if (_label) _label.text = _prefix + s + _suffix;
        }
    }
}
