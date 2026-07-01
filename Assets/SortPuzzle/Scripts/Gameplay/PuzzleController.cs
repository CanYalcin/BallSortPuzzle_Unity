using System;
using System.Collections.Generic;
using HyperBase.Core;
using SortPuzzle.Data;
using VContainer;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Pure C# puzzle logic engine. No MonoBehaviour, no Unity dependencies.
    /// Owns tube state, validates pours, executes moves, detects win, manages undo stack.
    /// </summary>
    public class PuzzleController
    {
        /// Events — subscribe in TubeView / LevelController for visual and audio feedback.
        /// Parameters: OnPoured(from, to, color, ballCount), OnTubeCompleted(tubeIndex, color),
        /// OnWon(pours, par, stars), OnUndone(), OnRestarted().
        public event Action<int, int, int, int> OnPoured;
        public event Action<int, int>           OnTubeCompleted;
        public event Action<int, int, int>      OnWon;
        public event Action                     OnUndone;
        public event Action                     OnRestarted;

        private TubeData[]                 _tubes;
        private TubeData[]                 _initialTubes;
        private readonly Stack<MoveRecord> _history = new();
        private readonly EventBus          _events;
        private LevelData                  _level;
        private int                        _totalPours;
        private bool                       _solved;
        private int                        _extraTubesAdded;

        public int  TubeCount   => _tubes?.Length ?? 0;
        public int  TotalPours  => _totalPours;
        public bool IsSolved    => _solved;
        public bool CanUndoMove => _history.Count > 0;

        [Inject]
        public PuzzleController(EventBus events) => _events = events;

        /// <summary>
        /// Initialises the puzzle from a LevelData asset and snapshots the initial state for Restart().
        /// Must be called before any Pour(), Undo(), or other mutations.
        /// </summary>
        public void Initialize(LevelData levelData)
        {
            _level           = levelData;
            _tubes           = levelData.CreateRuntimeTubes();
            _initialTubes    = new TubeData[_tubes.Length];
            for (int i = 0; i < _tubes.Length; i++) _initialTubes[i] = _tubes[i].Clone();
            _history.Clear();
            _totalPours      = 0;
            _solved          = false;
            _extraTubesAdded = 0;
        }

        /// <summary>Returns the live tube state at <paramref name="index"/>. Used by TubeView for rendering.</summary>
        public TubeData GetTube(int index) => _tubes[index];

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if a pour from <paramref name="from"/> to <paramref name="to"/> is legal.
        /// Rejects: same index, out-of-range, empty/complete source, full destination, color mismatch,
        /// and no-op moves (single-color source into an empty destination).
        /// </summary>
        public bool CanPour(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= TubeCount || to >= TubeCount)
                return false;

            TubeData s = _tubes[from];
            TubeData d = _tubes[to];

            if (s.IsEmpty || s.IsComplete || d.IsFull) return false;
            if (!d.IsEmpty && d.TopColor != s.TopColor) return false;

            // Prevent no-op: single-color source into empty dest is meaningless.
            if (d.IsEmpty)
            {
                bool singleColor = true;
                int  c0          = s.Balls[0];
                for (int i = 1; i <= s.TopIndex; i++)
                    if (s.Balls[i] != c0) { singleColor = false; break; }
                if (singleColor) return false;
            }

            return true;
        }

        // ── Execute ───────────────────────────────────────────────────────────

        /// <summary>
        /// Executes a pour from <paramref name="from"/> to <paramref name="to"/>.
        /// Transfers as many matching top-run balls as the destination has free capacity.
        /// Pushes a MoveRecord for undo, fires events, and checks for win condition.
        /// </summary>
        /// <returns>Number of balls moved, or 0 if the move is illegal.</returns>
        public int Pour(int from, int to)
        {
            if (!CanPour(from, to)) return 0;

            TubeData s         = _tubes[from];
            TubeData d         = _tubes[to];
            int      color     = s.TopColor;
            int      runLen    = s.TopRunLength;
            int      freeSlots = d.Capacity - (d.TopIndex + 1);
            int      moveCount = Math.Min(runLen, freeSlots);

            for (int i = 0; i < moveCount; i++)
            {
                int si                  = s.TopIndex;
                d.Balls[d.TopIndex + 1] = s.Balls[si];
                s.Balls[si]             = 0;
            }

            _history.Push(new MoveRecord(from, to, color, moveCount));
            _events.Publish(new SortPuzzle.OnPourMade(from, to, moveCount, color));
            OnPoured?.Invoke(from, to, color, moveCount);

            if (d.IsComplete)
            {
                _events.Publish(new SortPuzzle.OnTubeCompleted(to, color));
                OnTubeCompleted?.Invoke(to, color);
            }

            if (!_solved)
            {
                bool allDone = true;
                foreach (var t in _tubes)
                    if (!t.IsEmpty && !t.IsComplete) { allDone = false; break; }

                if (allDone)
                {
                    _solved = true;
                    int gold = _level?.GoldReward ?? 20;
                    _events.Publish(new SortPuzzle.OnPuzzleWon(_level?.LevelIndex ?? 0, gold));
                    OnWon?.Invoke(0, 0, 0);
                }
            }

            return moveCount;
        }

        // ── Undo ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reverses the most recent pour. Restores both tube states from the undo stack.
        /// Clears the solved flag — win can only be achieved by forward moves.
        /// </summary>
        /// <returns>True if a move was reversed; false if the undo stack is empty.</returns>
        public bool Undo()
        {
            if (_history.Count == 0) return false;
            MoveRecord rec = _history.Pop();
            TubeData   s   = _tubes[rec.FromTube];
            TubeData   d   = _tubes[rec.ToTube];
            for (int i = 0; i < rec.BallCount; i++)
            {
                int di                  = d.TopIndex;
                s.Balls[s.TopIndex + 1] = d.Balls[di];
                d.Balls[di]             = 0;
            }
            _solved = false;
            OnUndone?.Invoke();
            return true;
        }

        // ── Restart ───────────────────────────────────────────────────────────

        /// <summary>Resets all tubes to the initial state snapshotted on Initialize(). Clears the undo stack.</summary>
        public void Restart()
        {
            _tubes = new TubeData[_initialTubes.Length];
            for (int i = 0; i < _initialTubes.Length; i++) _tubes[i] = _initialTubes[i].Clone();
            _history.Clear();
            _totalPours      = 0;
            _solved          = false;
            _extraTubesAdded = 0;
            OnRestarted?.Invoke();
            _events.Publish(new SortPuzzle.OnPuzzleRestarted(_level?.LevelIndex ?? 0));
        }

        // ── Boost: Extra Empty Tube ───────────────────────────────────────────

        /// <summary>
        /// Appends a new empty tube to the runtime array (ExtraEmptyTube boost).
        /// Effect is runtime-only — Restart() rolls back to the original tube count.
        /// </summary>
        /// <returns>Index of the newly added tube.</returns>
        public int AddExtraEmptyTube()
        {
            int        cap      = _level?.TubeCapacity ?? 4;
            TubeData[] extended = new TubeData[_tubes.Length + 1];
            for (int i = 0; i < _tubes.Length; i++) extended[i] = _tubes[i];
            extended[_tubes.Length] = TubeData.Create(cap);
            _tubes = extended;
            _extraTubesAdded++;
            return _tubes.Length - 1;
        }
    }
}
