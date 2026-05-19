using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Animates balls moving between tubes.
    /// Uses a lazy object pool — balls are activated/deactivated, never destroyed.
    /// Pool grows on demand; no pre-warm needed so there is exactly one Instantiate call site.
    /// </summary>
    public class PourAnimator : MonoBehaviour
    {
        [SerializeField] private float _liftDuration   = 0.15f;
        [SerializeField] private float _travelDuration = 0.20f;
        [SerializeField] private float _dropDuration   = 0.15f;
        [SerializeField] private float _liftPixels     = 90f;

        public bool IsPlaying { get; private set; }

        private LiquidSegmentView       _prefab;
        private RectTransform           _poolParent;
        private List<LiquidSegmentView> _pool = new();

        // ── Pool — single Instantiate call site ──────────────────────────────

        private LiquidSegmentView Rent(LiquidSegmentView prefab, RectTransform parent)
        {
            if (_prefab == null) { _prefab = prefab; _poolParent = parent; }

            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].gameObject.activeSelf) { _pool[i].gameObject.SetActive(true); return _pool[i]; }

            // Only call site for Instantiate in this file
            var nb = Instantiate(_prefab, _poolParent);
            _pool.Add(nb);
            return nb;
        }

        private void Return(LiquidSegmentView b)
        {
            if (b == null) return;
            b.SetVisible(false);
            b.gameObject.SetActive(false);
        }

        // ── Animation ─────────────────────────────────────────────────────────

        public void PlayPour(
            int[]             colorIds,
            Color[]           colors,
            LiquidSegmentView ballPrefab,
            RectTransform     canvasRoot,
            Vector2[]         srcSlotPositions,
            Vector2[]         dstSlotPositions,
            float             ballSize,
            Action            onComplete)
        {
            if (IsPlaying)                                { onComplete?.Invoke(); return; }
            if (colorIds == null || colorIds.Length == 0) { onComplete?.Invoke(); return; }
            StartCoroutine(Run(colorIds, colors, ballPrefab, canvasRoot,
                               srcSlotPositions, dstSlotPositions, ballSize, onComplete));
        }

        private IEnumerator Run(
            int[]             colorIds,
            Color[]           colors,
            LiquidSegmentView prefab,
            RectTransform     root,
            Vector2[]         src,
            Vector2[]         dst,
            float             ballSize,
            Action            onComplete)
        {
            IsPlaying = true;
            int n = colorIds.Length;

            var balls = new LiquidSegmentView[n];
            for (int i = 0; i < n; i++)
            {
                var b = Rent(prefab, root);
                b.GetComponent<RectTransform>().sizeDelta = new Vector2(ballSize, ballSize);
                b.SetColor(colorIds[i], colors[i]);
                b.SetVisible(true);
                b.SetScreenPosition(src[i]);
                balls[i] = b;
            }

            float liftY = src[n - 1].y + _liftPixels;
            var lt = new Vector2[n];
            for (int i = 0; i < n; i++) lt[i] = new Vector2(src[i].x, liftY + i * ballSize);

            // Phase 1 — Lift
            for (float e = 0f; e < _liftDuration; e += Time.deltaTime)
            {
                float t = EaseOut(Mathf.Clamp01(e / _liftDuration));
                for (int i = 0; i < n; i++) balls[i].SetScreenPosition(Vector2.LerpUnclamped(src[i], lt[i], t));
                yield return null;
            }
            for (int i = 0; i < n; i++) balls[i].SetScreenPosition(lt[i]);

            float dx = dst[n - 1].x;
            var tt = new Vector2[n];
            for (int i = 0; i < n; i++) tt[i] = new Vector2(dx, lt[i].y);

            // Phase 2 — Travel
            for (float e = 0f; e < _travelDuration; e += Time.deltaTime)
            {
                float t = EaseInOut(Mathf.Clamp01(e / _travelDuration));
                for (int i = 0; i < n; i++) balls[i].SetScreenPosition(Vector2.LerpUnclamped(lt[i], tt[i], t));
                yield return null;
            }
            for (int i = 0; i < n; i++) balls[i].SetScreenPosition(tt[i]);

            // Phase 3 — Drop
            for (float e = 0f; e < _dropDuration; e += Time.deltaTime)
            {
                float t = EaseIn(Mathf.Clamp01(e / _dropDuration));
                for (int i = 0; i < n; i++) balls[i].SetScreenPosition(Vector2.LerpUnclamped(tt[i], dst[i], t));
                yield return null;
            }
            for (int i = 0; i < n; i++) balls[i].SetScreenPosition(dst[i]);

            for (int i = 0; i < n; i++) Return(balls[i]);
            IsPlaying = false;
            onComplete?.Invoke();
        }

        private static float EaseOut(float t)   => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t)    => t * t;
        private static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
    }
}
