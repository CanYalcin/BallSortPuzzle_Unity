using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HyperBase.VFX
{
    /// <summary>
    /// Lightweight "fake particle" burst built entirely from UI Images. This project's
    /// Canvas is Screen Space - Overlay, which always draws on top of camera-rendered
    /// content — real ParticleSystems can never be visible above it. This component
    /// animates its own child RectTransforms (each a small Image "piece") outward from
    /// their authored start position, fading and scaling down over Duration.
    ///
    /// Prefab authoring: build the burst by placing a handful of small Image children
    /// under this component's GameObject at whatever start offsets/colors you want;
    /// Play() takes it from there. No pooling logic lives here — VFXManager owns pooling.
    /// </summary>
    public class UIBurstEffect : MonoBehaviour
    {
        [SerializeField] private float _duration      = 0.5f;
        [SerializeField] private float _spreadPixels   = 60f;   // max outward travel per piece
        [SerializeField] private float _gravityPixels  = 0f;    // downward accel, px/s^2 (0 = none)
        [SerializeField] private float _startScale     = 1f;
        [SerializeField] private float _endScale       = 0.2f;

        public float Duration => _duration;

        private RectTransform[] _pieces;
        private Vector2[]       _startPos;
        private Vector2[]       _dir;
        private Image[]         _images;
        private Color[]         _baseColor;
        private Coroutine       _running;

        private void CacheChildren()
        {
            if (_pieces != null) return;
            int n = transform.childCount;
            _pieces    = new RectTransform[n];
            _startPos  = new Vector2[n];
            _dir       = new Vector2[n];
            _images    = new Image[n];
            _baseColor = new Color[n];
            for (int i = 0; i < n; i++)
            {
                var child = transform.GetChild(i);
                _pieces[i]   = child as RectTransform;
                _images[i]   = child.GetComponent<Image>();
                _baseColor[i] = _images[i] != null ? _images[i].color : Color.white;
            }
        }

        /// <summary>Plays the burst from scratch and invokes onComplete when the animation finishes.</summary>
        public void Play(Action onComplete)
        {
            CacheChildren();
            if (_running != null) StopCoroutine(_running);

            var rng = new System.Random();
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i] == null) continue;
                _startPos[i] = _pieces[i].anchoredPosition;
                float angle  = (float)(rng.NextDouble() * Mathf.PI * 2);
                float dist   = _spreadPixels * (0.5f + (float)rng.NextDouble() * 0.5f);
                _dir[i]      = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            }
            _running = StartCoroutine(Animate(onComplete));
        }

        private IEnumerator Animate(Action onComplete)
        {
            float t = 0f;
            while (t < _duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _duration);
                float fall = 0.5f * _gravityPixels * t * t;

                for (int i = 0; i < _pieces.Length; i++)
                {
                    if (_pieces[i] == null) continue;
                    Vector2 pos = _startPos[i] + _dir[i] * p;
                    pos.y -= fall;
                    _pieces[i].anchoredPosition = pos;
                    _pieces[i].localScale       = Vector3.one * Mathf.Lerp(_startScale, _endScale, p);
                    if (_images[i] != null)
                    {
                        var c = _baseColor[i];
                        c.a *= (1f - p);
                        _images[i].color = c;
                    }
                }
                yield return null;
            }
            _running = null;
            onComplete?.Invoke();
        }
    }
}
