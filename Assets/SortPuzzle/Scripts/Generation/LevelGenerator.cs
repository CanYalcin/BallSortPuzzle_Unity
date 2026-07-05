using System;
using System.Collections.Generic;
using SortPuzzle.Data;
using UnityEngine;

namespace SortPuzzle.Generation
{
    /// <summary>
    /// Procedural level generator.
    ///
    /// Approach:
    ///   1. Build a fully solved state (each tube filled with one color).
    ///   2. Apply N random UNRESTRICTED reverse-pours (any ball to any non-full tube).
    ///      No color-match constraint — this is what allows real scrambling.
    ///   3. Run BFS solver to confirm solvable and get par.
    ///   4. Reject if par is below the minimum for this difficulty.
    ///
    /// Difficulty → (colorCount, minPar, scrambleMoves):
    ///   3  → (4 colors, minPar 6,  80  scrambles)
    ///   5  → (5 colors, minPar 10, 120 scrambles)
    ///   7  → (6 colors, minPar 15, 160 scrambles)
    ///
    /// emptyTubes is a direct input, not difficulty-derived. Scrambling itself stays fully
    /// unrestricted (protecting tubes during the scramble was tried and broken — see
    /// DrainTubesToEmpty's doc comment for why). Instead, a post-scramble drain step
    /// empties exactly emptyTubes tubes afterward, redistributing their balls into
    /// whatever room exists elsewhere. This is always exactly possible: total slack across
    /// all tubes never changes from pouring, so emptying any N tubes always finds enough
    /// room in the rest.
    /// </summary>
    public static class LevelGenerator
    {
        private struct DiffConfig
        {
            public int ColorCount;
            public int MinPar;
            public int Scrambles;
        }

        private static DiffConfig GetConfig(int diff) => diff switch
        {
            1  => new DiffConfig { ColorCount = 2,  MinPar = 3,  Scrambles = 40  },
            2  => new DiffConfig { ColorCount = 3,  MinPar = 4,  Scrambles = 60  },
            3  => new DiffConfig { ColorCount = 4,  MinPar = 6,  Scrambles = 80  },
            4  => new DiffConfig { ColorCount = 4,  MinPar = 8,  Scrambles = 100 },
            5  => new DiffConfig { ColorCount = 5,  MinPar = 10, Scrambles = 120 },
            6  => new DiffConfig { ColorCount = 5,  MinPar = 13, Scrambles = 140 },
            7  => new DiffConfig { ColorCount = 6,  MinPar = 15, Scrambles = 160 },
            8  => new DiffConfig { ColorCount = 7,  MinPar = 18, Scrambles = 200 },
            9  => new DiffConfig { ColorCount = 8,  MinPar = 22, Scrambles = 240 },
            _  => new DiffConfig { ColorCount = 10, MinPar = 28, Scrambles = 300 },
        };

        public static LevelData Generate(int difficulty, int levelIndex,
                                         int capacity = 4, int maxAttempts = 100, int emptyTubes = 2)
        {
            int clampedDiff = Mathf.Clamp(difficulty, 1, 10);
            var cfg = GetConfig(clampedDiff);
            int reservedEmpty = Mathf.Max(0, emptyTubes);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                TubeData[] tubes = BuildSolvedState(cfg.ColorCount, reservedEmpty, capacity);
                Scramble(tubes, cfg.Scrambles, capacity);
                DrainTubesToEmpty(tubes, reservedEmpty, capacity);

                var levelData = ScriptableObject.CreateInstance<LevelData>();                levelData.LevelIndex       = levelIndex;
                levelData.DisplayName      = $"Level {levelIndex + 1}";
                levelData.TubeCount        = tubes.Length;
                levelData.EmptyTubeCount   = reservedEmpty;
                levelData.TubeCapacity     = capacity;
                levelData.ColorCount       = cfg.ColorCount;
                levelData.DifficultyRating = clampedDiff;
                levelData.GoldReward       = GoldReward(clampedDiff);

                levelData.Tubes = new TubeRow[tubes.Length];
                for (int i = 0; i < tubes.Length; i++)
                    levelData.Tubes[i] = new TubeRow(tubes[i].Balls);

                var result = LevelSolver.Solve(levelData);
                if (result.IsSolvable && result.ParMoves >= cfg.MinPar)
                {
                    levelData.ParMoves          = result.ParMoves;
                    levelData.ValidatedSolution = result.SolutionPath;
                    return levelData;
                }
            }

            Debug.LogWarning($"[LevelGenerator] Could not meet minPar={GetConfig(clampedDiff).MinPar} after {maxAttempts} attempts (diff {clampedDiff}). Returning null.");
            return null;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private static TubeData[] BuildSolvedState(int colorCount, int emptyTubes, int capacity)
        {
            int total = colorCount + emptyTubes;
            var tubes = new TubeData[total];
            for (int c = 0; c < colorCount; c++)
            {
                tubes[c] = TubeData.Create(capacity);
                for (int b = 0; b < capacity; b++)
                    tubes[c].Balls[b] = c + 1;
            }
            for (int e = 0; e < emptyTubes; e++)
                tubes[colorCount + e] = TubeData.Create(capacity);
            return tubes;
        }

        /// <summary>
        /// UNRESTRICTED scramble — any ball moves to any non-full tube, no color-match
        /// constraint. This is critical for generating genuinely complex states rather than
        /// trivially reversible ones.
        ///
        /// Deliberately does NOT try to protect/reserve empty tubes during this step — an
        /// earlier version did, and it broke generation completely: every color tube starts
        /// completely full (that's what "solved" means), so if the only empty tubes are also
        /// off-limits as destinations, there is literally no legal first move. The scramble
        /// silently did nothing, every attempt, every time. Guaranteeing empty tubes has to
        /// happen after scrambling (see DrainTubesToEmpty), not during it.
        /// </summary>
        private static void Scramble(TubeData[] tubes, int moves, int capacity)
        {
            var rng      = new System.Random();
            int n        = tubes.Length;
            int done     = 0;
            int maxTries = moves * 50;

            for (int attempt = 0; attempt < maxTries && done < moves; attempt++)
            {
                int from = rng.Next(n);
                int to   = rng.Next(n);
                if (from == to) continue;

                TubeData s = tubes[from];
                TubeData d = tubes[to];

                int srcTop = s.TopIndex;
                if (srcTop < 0) continue;            // source empty
                if (d.TopIndex >= capacity - 1) continue;  // dest full

                // UNRESTRICTED — move regardless of color match
                int color = s.Balls[srcTop];
                d.Balls[d.TopIndex + 1] = color;
                s.Balls[srcTop]         = 0;
                done++;
            }
        }

        /// <summary>
        /// Empties exactly emptyTubes tubes (picking the ones with the fewest balls, to
        /// minimize redistribution work), pouring their contents into whatever room exists
        /// in the remaining tubes. Always exactly possible: total capacity is fixed and
        /// total balls never changes, so total slack across all tubes is always
        /// emptyTubes * capacity regardless of which tubes currently hold it — emptying any
        /// N tubes always frees up exactly enough of that slack in the rest to receive them.
        /// Unrestricted, same as Scramble — color match doesn't matter here either.
        /// </summary>
        private static void DrainTubesToEmpty(TubeData[] tubes, int emptyTubes, int capacity)
        {
            if (emptyTubes <= 0) return;

            var order = new List<int>();
            for (int i = 0; i < tubes.Length; i++) order.Add(i);
            order.Sort((a, b) => (tubes[a].TopIndex).CompareTo(tubes[b].TopIndex));
            var drainSet = new HashSet<int>(order.GetRange(0, Mathf.Min(emptyTubes, tubes.Length)));

            foreach (int di in drainSet)
            {
                TubeData src = tubes[di];
                while (src.TopIndex >= 0)
                {
                    int color = src.Balls[src.TopIndex];
                    for (int j = 0; j < tubes.Length; j++)
                    {
                        if (drainSet.Contains(j)) continue;
                        TubeData dst = tubes[j];
                        if (dst.TopIndex >= capacity - 1) continue;
                        dst.Balls[dst.TopIndex + 1] = color;
                        src.Balls[src.TopIndex]     = 0;
                        break;
                    }
                }
            }
        }

        private static int GoldReward(int diff)
        {
            if (diff <= 3) return 10;
            if (diff <= 6) return 25;
            return 50;
        }
    }
}
