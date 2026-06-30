using NUnit.Framework;
using HyperBase.Core;
using SortPuzzle.Data;
using SortPuzzle.Gameplay;
using UnityEngine;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="PuzzleController"/>: pour legality, move execution,
    /// undo, restart, and win detection.
    /// Pure C# — no Play Mode or scene required.
    /// </summary>
    [TestFixture]
    public class PuzzleControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PuzzleController CreateController()
            => new PuzzleController(new EventBus());

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

        // ── CanPour — rejection cases ─────────────────────────────────────────

        [Test]
        public void CanPour_SameTube_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 2, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            Assert.IsFalse(pc.CanPour(0, 0));
        }

        [Test]
        public void CanPour_OutOfRangeIndex_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 0, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            Assert.IsFalse(pc.CanPour(-1, 1));
            Assert.IsFalse(pc.CanPour(0, 99));
        }

        [Test]
        public void CanPour_EmptySource_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 0, 0, 0, 0 }, new[] { 1, 0, 0, 0 } }));
            Assert.IsFalse(pc.CanPour(0, 1));
        }

        [Test]
        public void CanPour_CompleteSource_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 1, 1, 1 }, new[] { 0, 0, 0, 0 } }));
            Assert.IsFalse(pc.CanPour(0, 1));
        }

        [Test]
        public void CanPour_FullDestination_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 0, 0, 0 }, new[] { 1, 1, 1, 1 } }));
            Assert.IsFalse(pc.CanPour(0, 1));
        }

        [Test]
        public void CanPour_ColorMismatch_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 0, 0, 0 }, new[] { 2, 0, 0, 0 } }));
            Assert.IsFalse(pc.CanPour(0, 1));
        }

        [Test]
        public void CanPour_SingleColorSourceIntoEmptyDest_ReturnsFalse_NoOp()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            Assert.IsFalse(pc.CanPour(0, 1));
        }

        // ── CanPour — valid cases ─────────────────────────────────────────────

        [Test]
        public void CanPour_MixedSourceIntoEmptyDest_ReturnsTrue()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            Assert.IsTrue(pc.CanPour(0, 1));
        }

        [Test]
        public void CanPour_MatchingColorDestination_ReturnsTrue()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 1, 0, 0, 0 } }));
            Assert.IsTrue(pc.CanPour(0, 1));
        }

        // ── Pour — execution ──────────────────────────────────────────────────

        [Test]
        public void Pour_ValidMove_ReturnsBallCountMoved()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            Assert.AreEqual(1, pc.Pour(0, 1));
        }

        [Test]
        public void Pour_ValidMove_CorrectlyMutatesSourceAndDest()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            pc.Pour(0, 1);
            Assert.AreEqual(2, pc.GetTube(0).TopColor);
            Assert.AreEqual(1, pc.GetTube(1).TopColor);
        }

        [Test]
        public void Pour_TransfersEntireTopRun()
        {
            // tube[0]=[2,1,1,0] — mixed source, top run of two 1s goes to empty dest
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 1, 0 }, new[] { 0, 0, 0, 0 } }));
            int moved = pc.Pour(0, 1);
            Assert.AreEqual(2, moved, "Should transfer both balls in the top run.");
            Assert.AreEqual(2, pc.GetTube(0).TopColor, "Only color-2 ball remains in source.");
        }

        [Test]
        public void Pour_LimitedByDestinationFreeSlots()
        {
            // tube[0]=[2,1,1,0], tube[1]=[3,3,1,0] — dest has 1 free slot, src top run = 2
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 1, 0 }, new[] { 3, 3, 1, 0 } }));
            Assert.AreEqual(1, pc.Pour(0, 1), "Transfer must be capped by destination free slots.");
        }

        [Test]
        public void Pour_IllegalMove_ReturnsZero()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 0, 0, 0, 0 }, new[] { 1, 0, 0, 0 } }));
            Assert.AreEqual(0, pc.Pour(0, 1));
        }

        // ── Undo ──────────────────────────────────────────────────────────────

        [Test]
        public void Undo_AfterPour_RestoresBothTubes()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            pc.Pour(0, 1);
            Assert.IsTrue(pc.Undo());
            Assert.AreEqual(1, pc.GetTube(0).TopColor, "Source tube must be restored.");
            Assert.IsTrue(pc.GetTube(1).IsEmpty,       "Destination tube must be empty again.");
        }

        [Test]
        public void Undo_EmptyHistory_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 0, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            Assert.IsFalse(pc.Undo());
        }

        [Test]
        public void Undo_OnceHistoryDrained_ReturnsFalse()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            pc.Pour(0, 1);
            Assert.IsTrue(pc.Undo());
            Assert.IsFalse(pc.Undo(), "Second undo on empty history must return false.");
        }

        // ── Restart ───────────────────────────────────────────────────────────

        [Test]
        public void Restart_AfterPour_RestoresInitialState()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            pc.Pour(0, 1);
            pc.Restart();
            Assert.AreEqual(1, pc.GetTube(0).TopColor);
            Assert.IsTrue(pc.GetTube(1).IsEmpty);
        }

        [Test]
        public void Restart_ClearsUndoHistory()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 2, 1, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            pc.Pour(0, 1);
            pc.Restart();
            Assert.IsFalse(pc.CanUndoMove);
        }

        // ── AddExtraEmptyTube ─────────────────────────────────────────────────

        [Test]
        public void AddExtraEmptyTube_IncreasesToubeCountByOne()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 0, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            int before = pc.TubeCount;
            pc.AddExtraEmptyTube();
            Assert.AreEqual(before + 1, pc.TubeCount);
        }

        [Test]
        public void AddExtraEmptyTube_NewTubeIsEmpty()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 0, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            int newIdx = pc.AddExtraEmptyTube();
            Assert.IsTrue(pc.GetTube(newIdx).IsEmpty);
        }

        [Test]
        public void AddExtraEmptyTube_RestartRollsBackExtraTube()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(4, new[] { new[] { 1, 0, 0, 0 }, new[] { 0, 0, 0, 0 } }));
            int before = pc.TubeCount;
            pc.AddExtraEmptyTube();
            pc.Restart();
            Assert.AreEqual(before, pc.TubeCount, "Restart must roll back the extra tube.");
        }

        // ── Win detection ─────────────────────────────────────────────────────

        [Test]
        public void Pour_FinalWinningMove_PublishesOnPuzzleWonEvent()
        {
            bool wonFired = false;
            var bus = new EventBus();
            bus.Subscribe<SortPuzzle.OnPuzzleWon>(_ => wonFired = true);
            var pc = new PuzzleController(bus);
            pc.Initialize(MakeLevel(2, new[]
            {
                new[] { 1, 0 },
                new[] { 1, 0 },
                new[] { 0, 0 }
            }));
            pc.Pour(0, 1);
            Assert.IsTrue(wonFired, "OnPuzzleWon must be published when the puzzle is solved.");
        }

        [Test]
        public void Pour_WinningMove_SetsSolvedFlag()
        {
            var pc = CreateController();
            pc.Initialize(MakeLevel(2, new[]
            {
                new[] { 1, 0 },
                new[] { 1, 0 },
                new[] { 0, 0 }
            }));
            pc.Pour(0, 1);
            Assert.IsTrue(pc.IsSolved);
        }
    }
}
