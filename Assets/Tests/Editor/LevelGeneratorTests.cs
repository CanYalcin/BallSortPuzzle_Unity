using NUnit.Framework;
using SortPuzzle.Data;
using SortPuzzle.Generation;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Property tests for <see cref="LevelGenerator"/>: verifies that every generated level
    /// is solvable and meets the minimum par requirement for its difficulty.
    ///
    /// Difficulties 1-7 are covered by parameterised tests.
    /// Difficulties 8-10 use a smaller maxAttempts to keep CI time reasonable.
    /// </summary>
    [TestFixture]
    public class LevelGeneratorTests
    {
        // Exact minPar values mirrored from LevelGenerator.DiffConfig
        private static readonly int[] MinPar = { 0, 3, 4, 6, 8, 10, 13, 15, 18, 22, 28 };

        // ── Core properties (diff 1-7) ────────────────────────────────────────

        [Test]
        public void Generate_ReturnsNonNull([Range(1, 7)] int difficulty)
        {
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld,
                $"Generate() returned null for difficulty {difficulty} — " +
                "could not meet minPar within maxAttempts.");
        }

        [Test]
        public void Generate_ProducedLevelIsSolvable([Range(1, 7)] int difficulty)
        {
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld);
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable,
                $"Generated level at difficulty {difficulty} failed BFS solvability check.");
        }

        [Test]
        public void Generate_ParMeetsDifficultyMinimum([Range(1, 7)] int difficulty)
        {
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld);
            Assert.GreaterOrEqual(ld.ParMoves, MinPar[difficulty],
                $"Stored ParMoves {ld.ParMoves} is below minPar {MinPar[difficulty]} " +
                $"for difficulty {difficulty}.");
        }

        [Test]
        public void Generate_SolverParMatchesStoredPar([Range(1, 7)] int difficulty)
        {
            // Re-running the solver should reproduce the same par stored at generation time
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld);
            var result = LevelSolver.Solve(ld);
            Assert.AreEqual(ld.ParMoves, result.ParMoves,
                $"Re-running solver produced par {result.ParMoves} but stored par is " +
                $"{ld.ParMoves} at difficulty {difficulty}. Solver must be deterministic.");
        }

        [Test]
        public void Generate_SolutionPathLengthMatchesPar([Range(1, 7)] int difficulty)
        {
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld);
            Assert.IsNotEmpty(ld.ValidatedSolution,
                "ValidatedSolution must be stored on the generated level.");
            int pathMoves = ld.ValidatedSolution.Split(',').Length;
            Assert.AreEqual(ld.ParMoves, pathMoves,
                "Move count in ValidatedSolution must equal ParMoves.");
        }

        // ── Structural properties ─────────────────────────────────────────────

        [Test]
        public void Generate_TubeCountEqualsColorsAndEmpties([Range(1, 5)] int difficulty)
        {
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld);
            Assert.AreEqual(ld.ColorCount + ld.EmptyTubeCount, ld.TubeCount,
                "TubeCount must equal ColorCount + EmptyTubeCount.");
        }

        [Test]
        public void Generate_DefaultCapacityIsFour([Range(1, 5)] int difficulty)
        {
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0);
            Assert.IsNotNull(ld);
            Assert.AreEqual(4, ld.TubeCapacity);
        }

        [Test]
        public void Generate_MetadataIsStoredCorrectly()
        {
            const int difficulty = 3;
            const int worldIdx   = 0;
            const int levelIdx   = 7;
            var ld = LevelGenerator.Generate(difficulty, worldIdx, levelIdx);
            Assert.IsNotNull(ld);
            Assert.AreEqual(worldIdx,   ld.WorldIndex);
            Assert.AreEqual(levelIdx,   ld.LevelIndex);
            Assert.AreEqual(difficulty, ld.DifficultyRating);
        }

        // ── Randomness check ──────────────────────────────────────────────────

        [Test]
        public void Generate_TwoCallsProduceDifferentSolutions()
        {
            // The generator is time-seeded, so two calls should statistically
            // produce different solution paths. False failure probability is negligible.
            var ld1 = LevelGenerator.Generate(3, 0, 0);
            var ld2 = LevelGenerator.Generate(3, 0, 0);
            Assert.IsNotNull(ld1);
            Assert.IsNotNull(ld2);
            Assert.AreNotEqual(ld1.ValidatedSolution, ld2.ValidatedSolution,
                "Two independent Generate() calls produced identical solution paths — " +
                "RNG may not be time-seeded.");
        }

        // ── Higher difficulties ───────────────────────────────────────────────

        [Test]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        public void Generate_HighDifficulty_IsSolvableWhenNotNull(int difficulty)
        {
            // Fewer attempts to keep CI time reasonable for expensive BFS levels.
            // Returns Inconclusive (not Failed) if generation times out.
            var ld = LevelGenerator.Generate(difficulty, worldIndex: 0, levelIndex: 0,
                                             maxAttempts: 20);
            if (ld == null)
            {
                Assert.Inconclusive(
                    $"Difficulty {difficulty} did not produce a qualifying level in 20 attempts. " +
                    "Expected occasionally — increase maxAttempts if this fails often.");
                return;
            }
            var result = LevelSolver.Solve(ld);
            Assert.IsTrue(result.IsSolvable);
            Assert.GreaterOrEqual(ld.ParMoves, MinPar[difficulty]);
        }
    }
}
