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
    /// Difficulty → (colorCount, emptyTubes, minPar, scrambleMoves):
    ///   3  → (4 colors, 2 empty, minPar 6,  80  scrambles)
    ///   5  → (5 colors, 2 empty, minPar 10, 120 scrambles)
    ///   7  → (6 colors, 2 empty, minPar 15, 160 scrambles)
    /// </summary>
    public static class LevelGenerator
    {
        private struct DiffConfig
        {
            public int ColorCount;
            public int EmptyTubes;
            public int MinPar;
            public int Scrambles;
        }

        private static DiffConfig GetConfig(int diff) => diff switch
        {
            1  => new DiffConfig { ColorCount = 2,  EmptyTubes = 2, MinPar = 3,  Scrambles = 40  },
            2  => new DiffConfig { ColorCount = 3,  EmptyTubes = 2, MinPar = 4,  Scrambles = 60  },
            3  => new DiffConfig { ColorCount = 4,  EmptyTubes = 2, MinPar = 6,  Scrambles = 80  },
            4  => new DiffConfig { ColorCount = 4,  EmptyTubes = 2, MinPar = 8,  Scrambles = 100 },
            5  => new DiffConfig { ColorCount = 5,  EmptyTubes = 2, MinPar = 10, Scrambles = 120 },
            6  => new DiffConfig { ColorCount = 5,  EmptyTubes = 2, MinPar = 13, Scrambles = 140 },
            7  => new DiffConfig { ColorCount = 6,  EmptyTubes = 2, MinPar = 15, Scrambles = 160 },
            8  => new DiffConfig { ColorCount = 7,  EmptyTubes = 2, MinPar = 18, Scrambles = 200 },
            9  => new DiffConfig { ColorCount = 8,  EmptyTubes = 2, MinPar = 22, Scrambles = 240 },
            _  => new DiffConfig { ColorCount = 10, EmptyTubes = 2, MinPar = 28, Scrambles = 300 },
        };

        public static LevelData Generate(int difficulty, int worldIndex, int levelIndex,
                                         int capacity = 4, int maxAttempts = 100)
        {
            int clampedDiff = Mathf.Clamp(difficulty, 1, 10);
            var cfg = GetConfig(clampedDiff);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                TubeData[] tubes = BuildSolvedState(cfg.ColorCount, cfg.EmptyTubes, capacity);
                Scramble(tubes, cfg.Scrambles, capacity);

                var levelData = ScriptableObject.CreateInstance<LevelData>();
                levelData.WorldIndex       = worldIndex;
                levelData.LevelIndex       = levelIndex;
                levelData.DisplayName      = $"Level {levelIndex + 1}";
                levelData.TubeCount        = tubes.Length;
                levelData.EmptyTubeCount   = cfg.EmptyTubes;
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
        /// UNRESTRICTED scramble — any ball moves to any non-full tube.
        /// No color-match constraint. This is critical for generating
        /// genuinely complex states rather than trivially reversible ones.
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

        private static int GoldReward(int diff)
        {
            if (diff <= 3) return 10;
            if (diff <= 6) return 25;
            return 50;
        }
    }
}
