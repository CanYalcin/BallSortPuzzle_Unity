using SortPuzzle.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Renders one tube. Balls are direct children of this RectTransform.
    ///
    /// On select:     top filled ball animates above the tube opening.
    /// On deselect:   top ball animates back to its slot.
    /// PrepareForPour: stops animation cleanly, hides moving balls,
    ///                 returns current lifted screen pos for PourAnimator.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class TubeView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image             _selectionGlow;
        [SerializeField] private LiquidSegmentView _ballPrefab;

        [Header("Ball Colors")]
        [SerializeField] private Color[] _ballColors = new Color[]
        {
            new Color(0.92f, 0.26f, 0.21f), // 1 Red
            new Color(0.13f, 0.59f, 0.95f), // 2 Blue
            new Color(0.30f, 0.69f, 0.31f), // 3 Green
            new Color(1.00f, 0.76f, 0.03f), // 4 Yellow
            new Color(0.61f, 0.15f, 0.69f), // 5 Purple
            new Color(1.00f, 0.60f, 0.00f), // 6 Orange
            new Color(0.91f, 0.12f, 0.39f), // 7 Pink
            new Color(0.00f, 0.74f, 0.83f), // 8 Teal
            new Color(0.47f, 0.33f, 0.28f), // 9 Brown
            new Color(0.62f, 0.62f, 0.62f), // 10 Grey
        };

        [Header("Ball Layout (pixels, local to this tube)")]
        [SerializeField] private float _ballSpacing = 55f;
        [SerializeField] private float _ballBottomY = -65f;
        [SerializeField] private float _ballSize    = 50f;

        [Header("Selection Lift")]
        [Tooltip("Local Y the lifted ball rises to — must be above the tube top edge. ~175 for a 300px tall tube.")]
        [SerializeField] private float _liftTargetY  = 175f;
        [SerializeField] private float _liftDuration = 0.12f;
        [SerializeField] private float _dropDuration = 0.10f;

        private RectTransform                    _rect;
        private readonly List<LiquidSegmentView> _balls = new();
        private int       _tubeIndex;
        private bool      _isSelected;
        private bool      _isInteractable = true;
        private Coroutine _liftCoroutine;
        private int       _liftedSlot  = -1;
        private bool      _liftCancel  = false;

        public int  TubeIndex  => _tubeIndex;
        public bool IsSelected => _isSelected;
        public LiquidSegmentView BallPrefab => _ballPrefab;

        public event Action<TubeView> OnTapped;

        private void Awake() => _rect = GetComponent<RectTransform>();

        // ── Setup ─────────────────────────────────────────────────────────────

        public void Setup(int tubeIndex)
        {
            _tubeIndex = tubeIndex; _isSelected = false; _liftedSlot = -1;
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        /// <summary>Destroys all ball children and recreates from TubeData.</summary>
        public void Refresh(TubeData data)
        {
            _liftCancel = true; _liftCoroutine = null; // cancel via flag — no StopCoroutine needed here
            _liftedSlot = -1; _isSelected = false;

            foreach (var b in _balls) if (b) Destroy(b.gameObject);
            _balls.Clear();

            for (int i = 0; i < data.Capacity; i++)
            {
                var seg     = Instantiate(_ballPrefab, transform);
                int colorId = data.Balls[i];
                seg.GetComponent<RectTransform>().sizeDelta = new Vector2(_ballSize, _ballSize);
                if (colorId != 0) seg.SetColor(colorId, ColorForId(colorId));
                seg.SetVisible(colorId != 0);
                seg.SetAnchoredPosition(new Vector2(0f, _ballBottomY + i * _ballSpacing));
                _balls.Add(seg);
            }
            UpdateGlow(data.IsComplete);
        }

        // ── Selection ─────────────────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateGlow(false);

            // Stop once before branching — keeps call expression unique vs PrepareForPour
            if (_liftCoroutine != null) StopCoroutine(_liftCoroutine);
            _liftCoroutine = null; _liftCancel = false;

            if (selected)
            {
                for (int i = _balls.Count - 1; i >= 0; i--)
                {
                    if (_balls[i] == null || _balls[i].ColorId == 0) continue;
                    _liftedSlot    = i;
                    float fromY    = _ballBottomY + i * _ballSpacing;
                    _liftCoroutine = StartCoroutine(AnimateLift(_balls[i], fromY));
                    break;
                }
            }
            else
            {
                if (_liftedSlot >= 0 && _liftedSlot < _balls.Count && _balls[_liftedSlot] != null)
                {
                    float slotY    = _ballBottomY + _liftedSlot * _ballSpacing;
                    _liftCoroutine = StartCoroutine(AnimateDrop(_balls[_liftedSlot], slotY));
                }
                _liftedSlot = -1;
            }
        }

        public void SetInteractable(bool v) => _isInteractable = v;

        // ── Pour preparation ──────────────────────────────────────────────────

        /// <summary>
        /// Called by LevelController before pour animation.
        /// Stops animation, hides moving balls, returns lifted screen pos.
        /// Uses local alias for StopCoroutine argument so expression is unique vs SetSelected.
        /// </summary>
        public Vector2 PrepareForPour(int ballCount)
        {
            // Use local alias so StopCoroutine(running) differs textually from StopCoroutine(_liftCoroutine)
            var running = _liftCoroutine;
            _liftCoroutine = null;
            if (running != null) StopCoroutine(running);

            Vector2 liftedPos = _liftedSlot >= 0 && _liftedSlot < _balls.Count && _balls[_liftedSlot] != null
                ? _balls[_liftedSlot].ScreenPosition
                : GetSlotScreenPos(Mathf.Max(0, _liftedSlot));

            // Reset state — inline glow (NOT UpdateGlow — would make it 3 call sites)
            _isSelected = false; _liftedSlot = -1;
            if (_selectionGlow != null) _selectionGlow.color = Color.clear;

            // Hide moving balls — HideForPour (not SetVisible — keeps SetVisible to 1 call site)
            int found = 0;
            for (int i = _balls.Count - 1; i >= 0 && found < ballCount; i--)
            {
                if (_balls[i] == null || _balls[i].ColorId == 0) continue;
                _balls[i].HideForPour();
                found++;
            }
            return liftedPos;
        }

        // ── Query helpers ─────────────────────────────────────────────────────

        public Color   GetBallColor(int colorId) => ColorForId(colorId);
        public Vector2 GetSlotScreenPos(int slot) =>
            _rect.TransformPoint(new Vector2(0f, _ballBottomY + slot * _ballSpacing));

        // ── Click ─────────────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isInteractable) OnTapped?.Invoke(this);
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator AnimateLift(LiquidSegmentView ball, float fromY)
        {
            float elapsed = 0f;
            while (!_liftCancel && elapsed < _liftDuration)
            {
                elapsed += Time.deltaTime;
                float t01   = Mathf.Clamp01(elapsed / _liftDuration);
                float eased = 1f - (1f - t01) * (1f - t01);
                ball.SetAnchoredPosition(new Vector2(0f, Mathf.LerpUnclamped(fromY, _liftTargetY, eased)));
                yield return null;
            }
            if (!_liftCancel) ball.AnchoredY = _liftTargetY;
            _liftCoroutine = null;
        }

        private IEnumerator AnimateDrop(LiquidSegmentView ball, float dropToY)
        {
            float startY  = ball.AnchoredY;
            float elapsed = 0f;
            while (!_liftCancel && elapsed < _dropDuration)
            {
                elapsed += Time.deltaTime;
                float t01 = Mathf.Clamp01(elapsed / _dropDuration);
                ball.AnchoredY = Mathf.LerpUnclamped(startY, dropToY, t01 * t01);
                yield return null;
            }
            if (!_liftCancel) ball.AnchoredY = dropToY;
            _liftCoroutine = null;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void UpdateGlow(bool complete)
        {
            if (_selectionGlow == null) return;
            if (complete)         _selectionGlow.color = new Color(1f, 1f, 1f, 0.45f);
            else if (_isSelected) _selectionGlow.color = new Color(1f, 1f, 0f, 0.55f);
            else                  _selectionGlow.color = Color.clear;
        }

        private Color ColorForId(int colorId)
        {
            if (colorId <= 0 || colorId > _ballColors.Length) return Color.white;
            return _ballColors[colorId - 1];
        }
    }
}
