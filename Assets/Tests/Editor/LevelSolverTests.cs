using NUnit.Framework;
using SortPuzzle.Data;
using SortPuzzle.Generation;
using UnityEngine;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="LevelSolver"/>: BFS solvability detection and par accuracy.
    /// Pure C# — no Play Mode or scene required.
    /// </summary>
    [TestFixture]
    public class LevelSolverTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static LevelData MakeLevel(int capacity, int[][] balls)
        {
            var ld          = ScriptableObject.CreateInstance<LevelData>();
            ld.TubeCapacity = capacity;
            ld.TubeCount    = balls.Length;
            ld.Tubes        = new TubeRow[balls.Length];
            for (int i = 0; i < balls.Length; i++)
                ld.Tubes[i] = new TubeRow(balls[i]);
            return ld;
        }

        // ── Already-solved states ─────────────────────────────────────────────

        [Test]
        public void Solve_AlreadySolvedState_IsSolvableWithZeroPar()
        {
            // Two complete tubes + one empty → already solved
            var ld = MakeLevel(2, new[]
            {
                new[] { 1, 1 },
                new[] { 2, 2 },
                new[] { 0, 0 }
            });
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(0, result.ParMoves);
        }

        // ── Solvable puzzles ──────────────────────────────────────────────────

        [Test]
        public void Solve_OneMoveAway_ReturnsParOne()
        {
            // tube[0]=[1,0] tube[1]=[1,0] tube[2]=[0,0]
            // Pour 0→1 → [0,0], [1,1], [0,0] → solved in 1 move
            var ld = MakeLevel(2, new[]
            {
                new[] { 1, 0 },
                new[] { 1, 0 },
                new[] { 0, 0 }
            });
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(1, result.ParMoves);
        }

        [Test]
        public void Solve_GeneratedDifficulty1Level_IsSolvableWithMinPar()
        {
            var ld = LevelGenerator.Generate(difficulty: 1, levelIndex: 0);
            Assert.IsNotNull(ld);
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable);
            Assert.GreaterOrEqual(result.ParMoves, 3);
        }

        [Test]
        public void Solve_SolutionPathLengthMatchesPar()
        {
            var ld = LevelGenerator.Generate(difficulty: 2, levelIndex: 0);
            Assert.IsNotNull(ld);
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable);
            int pathLength = result.SolutionPath.Split(',').Length;
            Assert.AreEqual(result.ParMoves, pathLength,
                "SolutionPath move count must equal ParMoves.");
        }

        // ── Unsolvable puzzles ────────────────────────────────────────────────

        [Test]
        public void Solve_AllTubesFull_NoValidMoves_IsUnsolvable()
        {
            // Two full tubes, no empty tube — no pour is possible
            var ld = MakeLevel(2, new[]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            });
            var result = LevelSolver.Solve(ld);
            Assert.IsFalse(result.IsSolvable);
        }

        [Test]
        public void Solve_DeadlockedState_IsUnsolvable()
        {
            // All four-capacity tubes full, interleaved so no legal pour exists
            var ld = MakeLevel(4, new[]
            {
                new[] { 1, 2, 1, 2 },
                new[] { 2, 1, 2, 1 }
            });
            var result = LevelSolver.Solve(ld);
            Assert.IsFalse(result.IsSolvable);
        }

        // ── Par accuracy ─────────────────────────────────────────────────────

        [Test]
        public void Solve_GeneratedLevelParMatchesStoredPar()
        {
            // Re-running the solver should reproduce exactly the par stored at generation time
            var ld = LevelGenerator.Generate(difficulty: 3, levelIndex: 0);
            Assert.IsNotNull(ld);
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(ld.ParMoves, result.ParMoves,
                "Solver re-run must produce the same optimal par as stored in LevelData.");
        }
    }
}
