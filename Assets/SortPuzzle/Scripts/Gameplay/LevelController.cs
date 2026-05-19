using System.Collections.Generic;
using HyperBase.Core;
using HyperBase.Gameplay;
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
        private HyperBase.UI.Screens.GameplayScreen _gameplayScreen;

public void Construct(PuzzleController puzzle, LevelManager levelManager,
                              BoostManager boostManager, GoldManager goldManager,
                              BoostSystem boostSystem, EventBus events)
        {
            _puzzle       = puzzle;
            _levelManager = levelManager;
            _boostManager = boostManager;
            _goldManager  = goldManager;
            _boostSystem  = boostSystem;
        }

        private readonly List<TubeView> _views = new();
        private int       _activeCount;
        private int       _sel    = -1;
        private bool      _locked;
        private LevelData _ld;

private void Start()
        {
            if (_levelManager == null)
            {
                var scope = GetComponentInParent<VContainer.Unity.LifetimeScope>();
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
            _puzzle.OnWon += (pours, par, stars) => _levelManager.CompleteCurrentLevel();

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

public void OnUndoPressed()
        {
            if (_locked) return;
            if (_boostManager.HasBoost(BoostType.Undo))
            {
                if (!_puzzle.CanUndoMove) return;
                if (!_boostManager.TryUseBoost(BoostType.Undo)) return;
                _puzzle.Undo();
                if (_sel >= 0) { _views[_sel].SetSelected(false); _sel = -1; }
                for (int i = 0; i < _activeCount && i < _puzzle.TubeCount; i++)
                    _views[i].Refresh(_puzzle.GetTube(i));
            }
            else
            {
                _boostSystem?.WatchAdForBoost(BoostType.Undo);
            }
        }



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
            }
            else
            {
                _boostSystem?.WatchAdForBoost(BoostType.ExtraEmptyTube);
            }
        }

public void OnRestartPressed()
        {
            _locked = false;
            _puzzle.Restart();
            if (_sel >= 0) { _views[_sel].SetSelected(false); _sel = -1; }
            for (int i = 0; i < _activeCount && i < _puzzle.TubeCount; i++)
                _views[i].Refresh(_puzzle.GetTube(i));
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
                    _views[from].Refresh(_puzzle.GetTube(from));
                    _views[to].Refresh(_puzzle.GetTube(to));
                    _locked = false;
                });
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _activeCount && i < _views.Count; i++)
                if (_views[i] != null) _views[i].OnTapped -= OnTap;
        }
    }
}
