using System.Collections.Generic;
using HyperBase.Core;
using HyperBase.Gameplay;
using HyperBase.VFX;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Scene MonoBehaviour — owns one puzzle session.
    /// Tubes positioned via RectTransform.anchoredPosition (pixel-based, Screen Space Overlay).
    /// Balls are children of their TubeView. Pour uses temp balls on canvas root.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private TubeView       _tubePrefab;
        [SerializeField] private PourAnimator   _pourAnimator;
        [SerializeField] private RectTransform  _tubeContainer;   // parent for tube RectTransforms
        [SerializeField] private RectTransform  _canvasRoot;      // temp anim balls parent
        [SerializeField] private float          _tubeSpacingPx = 120f;
        [SerializeField] private float          _rowSpacingPx  = 340f;
        [SerializeField] private int            _maxPerRow     = 5;
        [SerializeField] private int            _extraPool     = 3;
        [SerializeField] private float          _ballSize      = 50f;

        private PuzzleController _puzzle;
        private LevelManager     _levelManager;
        private BoostManager     _boostManager;
        private GoldManager      _goldManager;
        private BoostSystem      _boostSystem;
        private VFXManager       _vfx;
        private EventBus          _events;
        private HyperBase.UI.Screens.GameplayScreen _gameplayScreen;

        [Inject]
        public void Construct(PuzzleController puzzle, LevelManager levelManager,
                              BoostManager boostManager, GoldManager goldManager,
                              BoostSystem boostSystem, VFXManager vfx, EventBus events)
        {
            _puzzle       = puzzle;
            _levelManager = levelManager;
            _boostManager = boostManager;
            _goldManager  = goldManager;
            _boostSystem  = boostSystem;
            _vfx          = vfx;
            _events       = events;
        }

        /// <summary>
        /// Publishes OnLevelFailed if the player made at least one move but is leaving
        /// without winning. This project has no other lose-condition, so voluntary
        /// mid-level abandonment is treated as the closest equivalent for analytics —
        /// a direct signal for level-funnel/difficulty tracking. Call before navigating
        /// away from the level (not called on Restart — that's normal retry behavior,
        /// not abandonment).
        /// </summary>
        public void ReportAbandonIfNeeded()
        {
            if (_abandonReported) return;
            if (_puzzle != null && _ld != null && _puzzle.TotalPours > 0 && !_puzzle.IsSolved)
            {
                _abandonReported = true;
                _events.Publish(new OnLevelFailed(_ld.LevelIndex, _puzzle.AttemptElapsed));
            }
        }

        // Android's home/app-switcher exit doesn't go through GameplayScreen.OnHome() at all —
        // OnApplicationPause is the reliable cross-platform signal Unity gives for that. Also
        // hooking OnApplicationQuit for parity and for Editor/Standalone testing, where Pause
        // often doesn't fire the same way. Guarded by _abandonReported so a brief background
        // (glance at a notification, then resume) doesn't spam duplicate reports for a puzzle
        // the player is still actively mid-attempt on; the guard resets on the next pour or
        // restart, so a later genuine abandonment on the same level still gets reported.
        private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) ReportAbandonIfNeeded(); }
        private void OnApplicationQuit() => ReportAbandonIfNeeded();

                private RectTransform VfxLayer => _canvasRoot != null ? _canvasRoot : _tubeContainer;
private Vector2   _lastUndoScreenPos;
        private readonly List<TubeView> _views = new();
        private int       _activeCount;
        private int       _sel    = -1;
        private bool      _locked;
        private LevelData _ld;
        private bool      _abandonReported;

        private void Start()
        {
            if (_levelManager == null)
            {
                var scope = Object.FindFirstObjectByType<VContainer.Unity.LifetimeScope>();
                if (scope != null)
                {
                    _puzzle       = scope.Container.Resolve<PuzzleController>();
                    _levelManager = scope.Container.Resolve<HyperBase.Gameplay.LevelManager>();
                    _boostManager = scope.Container.Resolve<SortPuzzle.Economy.BoostManager>();
                    _goldManager  = scope.Container.Resolve<SortPuzzle.Economy.GoldManager>();
                }
            }

            if (_levelManager == null) { Debug.LogError("[LevelController] _levelManager is null."); return; }

            _gameplayScreen = UnityEngine.Object.FindFirstObjectByType<HyperBase.UI.Screens.GameplayScreen>(FindObjectsInactive.Include);
            if (_gameplayScreen != null) _gameplayScreen.SetLevelController(this);

            _ld = _levelManager.CurrentLevel as LevelData;
            if (_ld == null) { Debug.LogError("[LevelController] CurrentLevel is not LevelData."); return; }

            _puzzle.Initialize(_ld);
            _puzzle.OnWon += (pours, par, stars) =>
            {
                Vector2 center = VfxLayer != null ? (Vector2)VfxLayer.TransformPoint(VfxLayer.rect.center) : Vector2.zero;
                _vfx?.Play(VFXType.Confetti, VfxLayer, center);
                _levelManager.CompleteCurrentLevel(_puzzle.TotalPours);
            };
            _puzzle.OnUndone += OnPuzzleUndone;

            for (int i = 0; i < _ld.TubeCount + _extraPool; i++)
            {
                var v   = Instantiate(_tubePrefab, _tubeContainer);
                bool on = i < _ld.TubeCount;
                v.Setup(i); v.gameObject.SetActive(on);
                if (on) v.OnTapped += OnTap;
                _views.Add(v);
            }
            _activeCount = _ld.TubeCount;

            int n0    = _activeCount;
            int rows0 = Mathf.CeilToInt((float)n0 / _maxPerRow);
            for (int i = 0; i < n0; i++)
            {
                int r   = i / _maxPerRow;
                int c   = i % _maxPerRow;
                int inR = Mathf.Min(_maxPerRow, n0 - r * _maxPerRow);
                var rt  = _views[i].GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2((c - (inR - 1) * 0.5f) * _tubeSpacingPx,
                                                  ((rows0 - 1) * 0.5f - r) * _rowSpacingPx);
            }
            for (int i = 0; i < n0 && i < _puzzle.TubeCount; i++)
                _views[i].Refresh(_puzzle.GetTube(i));
        }

        // ── Boost buttons ─────────────────────────────────────────────────────

        /// <summary>
        /// Called from BoostBarWidget Undo button. Uses one Undo boost if available;
        /// otherwise triggers a rewarded ad via BoostSystem.
        /// </summary>
        public void OnUndoPressed()
        {
            if (_locked) return;
            if (_boostManager.HasBoost(BoostType.Undo))
            {
                if (!_puzzle.CanUndoMove) return;
                if (!_boostManager.TryUseBoost(BoostType.Undo)) return;
                _puzzle.Undo();
                _vfx?.Play(VFXType.BoostUsed, VfxLayer, _lastUndoScreenPos);
                if (_sel >= 0) { _views[_sel].SetSelected(false); _sel = -1; }
                for (int i = 0; i < _activeCount && i < _puzzle.TubeCount; i++)
                    _views[i].Refresh(_puzzle.GetTube(i));
            }
            else
            {
                _boostSystem?.WatchAdForBoost(BoostType.Undo);
            }
        }

        /// <summary>
        /// Called from BoostBarWidget ExtraEmptyTube button. Adds a new tube if boost available;
        /// otherwise triggers a rewarded ad via BoostSystem. Repositions all TubeViews to fit.
        /// </summary>
        public void OnExtraEmptyTubePressed()
        {
            if (_locked) return;
            if (_boostManager.HasBoost(BoostType.ExtraEmptyTube))
            {
                if (_activeCount >= _views.Count) return;
                if (!_boostManager.TryUseBoost(BoostType.ExtraEmptyTube)) return;
                int ni = _puzzle.AddExtraEmptyTube();
                var nv = _views[_activeCount];
                nv.Setup(ni); nv.gameObject.SetActive(true); nv.OnTapped += OnTap;
                _activeCount++;
                int n1 = _activeCount, rows1 = Mathf.CeilToInt((float)n1 / _maxPerRow);
                for (int i = 0; i < n1; i++)
                {
                    int r = i / _maxPerRow, c = i % _maxPerRow;
                    int inR = Mathf.Min(_maxPerRow, n1 - r * _maxPerRow);
                    var rt  = _views[i].GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot     = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2((c-(inR-1)*0.5f)*_tubeSpacingPx, ((rows1-1)*0.5f-r)*_rowSpacingPx);
                }
                for (int i = 0; i < _activeCount && i < _puzzle.TubeCount; i++)
                    _views[i].Refresh(_puzzle.GetTube(i));
                _vfx?.Play(VFXType.BoostUsed, VfxLayer, _views[ni].GetSlotScreenPos(0));
            }
            else
            {
                _boostSystem?.WatchAdForBoost(BoostType.ExtraEmptyTube);
            }
        }

        /// <summary>Resets the puzzle to its initial state and refreshes all TubeViews.</summary>
        public void OnRestartPressed()
        {
            _locked          = false;
            _abandonReported = false;
            _puzzle.Restart();
            if (_sel >= 0) { _views[_sel].SetSelected(false); _sel = -1; }
            for (int i = 0; i < _activeCount && i < _puzzle.TubeCount; i++)
                _views[i].Refresh(_puzzle.GetTube(i));
        }

        private void OnPuzzleUndone(int fromTube, int toTube)
        {
            if (fromTube < 0 || fromTube >= _views.Count) return;
            var tube = _puzzle.GetTube(fromTube);
            int slot = Mathf.Max(0, tube.TopIndex);
            _lastUndoScreenPos = _views[fromTube].GetSlotScreenPos(slot);
            _vfx?.Play(VFXType.Undo, VfxLayer, _lastUndoScreenPos);
        }

        // ── Tap handler ───────────────────────────────────────────────────────

        private void OnTap(TubeView tapped)
        {
            if (_locked) return;
            int idx = tapped.TubeIndex;
            if (_sel == -1) { TapWhenNoneSelected(idx, tapped); return; }
            if (_sel == idx) { tapped.SetSelected(false); _sel = -1; return; }
            if (_puzzle.CanPour(_sel, idx)) TapPour(_sel, idx);
            else TapReselect(idx, tapped);
        }

        private void TapWhenNoneSelected(int idx, TubeView t)
        {
            if (_puzzle.GetTube(idx).IsEmpty) return;
            _sel = idx; t.SetSelected(true);
        }

        private void TapReselect(int idx, TubeView t)
        {
            if (_sel >= 0) _views[_sel].SetSelected(false);
            _sel = -1;
            if (!_puzzle.GetTube(idx).IsEmpty) { _sel = idx; t.SetSelected(true); }
        }

        private void TapPour(int from, int to)
        {
            var srcTube  = _puzzle.GetTube(from);
            var destTube = _puzzle.GetTube(to);
            int freeSlots = destTube.Capacity - (destTube.TopIndex + 1);
            int moveCount = Mathf.Min(srcTube.TopRunLength, freeSlots);
            if (moveCount <= 0) return;

            // PrepareForPour: stops lift coroutine, hides moving balls, returns lifted screen pos
            Vector2 liftedTopPos = _views[from].PrepareForPour(moveCount);

            int topSlot  = srcTube.TopIndex;
            int destBase = destTube.TopIndex + 1;
            var colorIds     = new int[moveCount];
            var colors       = new Color[moveCount];
            var srcPositions = new Vector2[moveCount];
            var dstPositions = new Vector2[moveCount];

            for (int k = 0; k < moveCount; k++)
            {
                // k=0 = bottom of run, k=moveCount-1 = top
                int srcSlot    = topSlot - (moveCount - 1 - k);
                colorIds[k]    = srcTube.Balls[srcSlot];
                colors[k]      = _views[from].GetBallColor(colorIds[k]);
                // Top ball starts from its already-lifted position; lower balls from their slots
                srcPositions[k] = k == moveCount - 1 ? liftedTopPos : _views[from].GetSlotScreenPos(srcSlot);
                dstPositions[k] = _views[to].GetSlotScreenPos(destBase + k);
            }

            _sel = -1; _locked = true;
            RectTransform animRoot = _canvasRoot != null ? _canvasRoot : _tubeContainer;
            _pourAnimator.PlayPour(colorIds, colors, _views[from].BallPrefab,
                animRoot, srcPositions, dstPositions, _ballSize, () =>
                {
                    _puzzle.Pour(from, to);
                    Vector2 landPos = dstPositions[moveCount - 1];
                    _vfx?.Play(VFXType.PourSplash, VfxLayer, landPos);
                    if (_puzzle.GetTube(to).IsComplete)
                        _vfx?.Play(VFXType.TubeComplete, VfxLayer, landPos);
                    _views[from].Refresh(_puzzle.GetTube(from));
                    _views[to].Refresh(_puzzle.GetTube(to));
                    _locked          = false;
                    _abandonReported = false;
                });
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _activeCount && i < _views.Count; i++)
                if (_views[i] != null) _views[i].OnTapped -= OnTap;
            if (_puzzle != null) _puzzle.OnUndone -= OnPuzzleUndone;
        }
    }
}
