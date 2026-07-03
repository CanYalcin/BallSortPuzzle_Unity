using System.Collections.Generic;
using SortPuzzle.Data;

namespace SortPuzzle.Generation
{
    /// <summary>
    /// BFS-based puzzle solver.
    ///
    /// Usage:
    ///   var result = LevelSolver.Solve(levelData);
    ///   if (result.IsSolvable) Debug.Log($"Par: {result.ParMoves}");
    ///
    /// The solver works on raw TubeData arrays (no MonoBehaviours).
    /// State is hashed as a compact string for the visited set.
    /// Max depth is capped to prevent infinite loops on pathological inputs.
    /// </summary>
    public static class LevelSolver
    {
        private const int    MaxDepth       = 300;
        // Wall-clock bound, not a node-count bound: a maximally-scrambled high-difficulty
        // board (e.g. 12 tubes / 10 colors) can have a reachable state space large enough
        // that even this correctly-memoized BFS takes many minutes on an unlucky seed. That
        // was observed directly in CI: a single Solve() call ran 1105s and blew past even a
        // generous 600s test timeout. Bounding wall-clock time (checked periodically, not
        // every node, to keep Stopwatch overhead negligible) makes worst-case runtime
        // deterministic regardless of board pathology or runner speed. Hitting the cap is
        // treated identically to "proven unsolvable" by every caller — LevelGenerator retries
        // either way, and difficulty 8-10 generation is already documented as best-effort.
        private const double MaxSolveSeconds = 30.0;
        private const int    TimeCheckEvery  = 1024; // nodes between Stopwatch checks

        public struct SolveResult
        {
            public bool   IsSolvable;
            public int    ParMoves;      // optimal solution move count
            public string SolutionPath; // "f0t2,f2t1,..." for each pour
            public bool   SearchBudgetExceeded; // true if aborted by the time cap, not proven unsolvable
        }

        public static SolveResult Solve(LevelData levelData)
        {
            TubeData[] initial = levelData.CreateRuntimeTubes();

            if (IsWon(initial))
                return new SolveResult { IsSolvable = true, ParMoves = 0, SolutionPath = "" };

            var queue   = new Queue<(TubeData[] tubes, string path)>();
            var visited = new HashSet<string>();

            string startHash = Hash(initial);
            queue.Enqueue((initial, ""));
            visited.Add(startHash);

            var sw          = System.Diagnostics.Stopwatch.StartNew();
            int nodesChecked = 0;

            while (queue.Count > 0)
            {
                if ((++nodesChecked % TimeCheckEvery) == 0 && sw.Elapsed.TotalSeconds > MaxSolveSeconds)
                {
                    return new SolveResult
                    {
                        IsSolvable = false, ParMoves = 0, SolutionPath = "",
                        SearchBudgetExceeded = true
                    };
                }

                var (tubes, path) = queue.Dequeue();

                int depth = path.Length == 0 ? 0 : (path.Split(',').Length);
                if (depth >= MaxDepth) continue;

                int n = tubes.Length;
                for (int from = 0; from < n; from++)
                {
                    for (int to = 0; to < n; to++)
                    {
                        if (!CanPour(tubes, from, to)) continue;

                        TubeData[] next = ApplyPour(tubes, from, to, out int moveCount);
                        string     move = "f" + from + "t" + to;
                        string     nextPath = path.Length == 0 ? move : path + "," + move;

                        if (IsWon(next))
                        {
                            int parMoves = nextPath.Split(',').Length;
                            return new SolveResult
                            {
                                IsSolvable   = true,
                                ParMoves     = parMoves,
                                SolutionPath = nextPath
                            };
                        }

                        string h = Hash(next);
                        if (!visited.Contains(h))
                        {
                            visited.Add(h);
                            queue.Enqueue((next, nextPath));
                        }
                    }
                }
            }

            return new SolveResult { IsSolvable = false, ParMoves = 0, SolutionPath = "" };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsWon(TubeData[] tubes)
        {
            foreach (var t in tubes)
                if (!t.IsEmpty && !t.IsComplete) return false;
            return true;
        }

        private static bool CanPour(TubeData[] tubes, int from, int to)
        {
            if (from == to) return false;
            TubeData s = tubes[from];
            TubeData d = tubes[to];
            if (s.IsEmpty || s.IsComplete || d.IsFull) return false;
            if (!d.IsEmpty && d.TopColor != s.TopColor) return false;
            if (d.IsEmpty)
            {
                int c0       = s.Balls[0];
                bool single  = true;
                for (int i = 1; i <= s.TopIndex; i++)
                    if (s.Balls[i] != c0) { single = false; break; }
                if (single) return false;
            }
            return true;
        }

        private static TubeData[] ApplyPour(TubeData[] tubes, int from, int to, out int moved)
        {
            // Deep copy tubes
            TubeData[] next = new TubeData[tubes.Length];
            for (int i = 0; i < tubes.Length; i++) next[i] = tubes[i].Clone();

            TubeData s      = next[from];
            TubeData d      = next[to];
            int      run    = s.TopRunLength;
            int      free   = d.Capacity - (d.TopIndex + 1);
            moved           = run < free ? run : free;

            for (int i = 0; i < moved; i++)
            {
                int si            = s.TopIndex;
                d.Balls[d.TopIndex + 1] = s.Balls[si];
                s.Balls[si]       = 0;
            }
            return next;
        }

        private static string Hash(TubeData[] tubes)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in tubes)
            {
                for (int i = 0; i < t.Capacity; i++)
                    sb.Append(t.Balls[i]).Append(',');
                sb.Append('|');
            }
            return sb.ToString();
        }
    }
}
