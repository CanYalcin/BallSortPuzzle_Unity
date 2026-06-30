using NUnit.Framework;
using SortPuzzle.Data;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="TubeData"/> computed properties and factory methods.
    /// Pure C# — no Play Mode or scene required.
    /// </summary>
    [TestFixture]
    public class TubeDataTests
    {
        // ── TopIndex ─────────────────────────────────────────────────────────

        [Test]
        public void TopIndex_EmptyTube_ReturnsMinusOne()
        {
            var tube = TubeData.Create(4);
            Assert.AreEqual(-1, tube.TopIndex);
        }

        [Test]
        public void TopIndex_OneFilledSlot_ReturnsZero()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 0, 0, 0 });
            Assert.AreEqual(0, tube.TopIndex);
        }

        [Test]
        public void TopIndex_FullTube_ReturnsCapacityMinusOne()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 2, 3, 4 });
            Assert.AreEqual(3, tube.TopIndex);
        }

        // ── IsEmpty / IsFull ─────────────────────────────────────────────────

        [Test]
        public void IsEmpty_FreshTube_ReturnsTrue()
        {
            Assert.IsTrue(TubeData.Create(4).IsEmpty);
        }

        [Test]
        public void IsEmpty_TubeWithBall_ReturnsFalse()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 0, 0, 0 });
            Assert.IsFalse(tube.IsEmpty);
        }

        [Test]
        public void IsFull_AllSlotsOccupied_ReturnsTrue()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 2, 3, 4 });
            Assert.IsTrue(tube.IsFull);
        }

        [Test]
        public void IsFull_PartialTube_ReturnsFalse()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 2, 0, 0 });
            Assert.IsFalse(tube.IsFull);
        }

        // ── TopColor ─────────────────────────────────────────────────────────

        [Test]
        public void TopColor_EmptyTube_ReturnsZero()
        {
            Assert.AreEqual(0, TubeData.Create(4).TopColor);
        }

        [Test]
        public void TopColor_ReturnsColorOfTopmostBall()
        {
            // Balls = [1, 2, 0, 0] → top is index 1 → color 2
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 2, 0, 0 });
            Assert.AreEqual(2, tube.TopColor);
        }

        // ── TopRunLength ─────────────────────────────────────────────────────

        [Test]
        public void TopRunLength_EmptyTube_ReturnsZero()
        {
            Assert.AreEqual(0, TubeData.Create(4).TopRunLength);
        }

        [Test]
        public void TopRunLength_SingleBall_ReturnsOne()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 2, 0, 0, 0 });
            Assert.AreEqual(1, tube.TopRunLength);
        }

        [Test]
        public void TopRunLength_TwoSameColorAtTop_ReturnsTwo()
        {
            // [2, 1, 1, 0] → top run of 1s has length 2
            var tube = TubeData.CreateWithBalls(4, new[] { 2, 1, 1, 0 });
            Assert.AreEqual(2, tube.TopRunLength);
        }

        [Test]
        public void TopRunLength_AllSameColor_ReturnsFullCapacity()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 3, 3, 3, 3 });
            Assert.AreEqual(4, tube.TopRunLength);
        }

        [Test]
        public void TopRunLength_ColorBreakInMiddle_ReturnsOnlyTopRun()
        {
            // [1, 2, 1, 0] → top is color 1 at index 2, but index 1 is color 2 → run length = 1
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 2, 1, 0 });
            Assert.AreEqual(1, tube.TopRunLength);
        }

        // ── IsComplete ───────────────────────────────────────────────────────

        [Test]
        public void IsComplete_FullSingleColor_ReturnsTrue()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 2, 2, 2, 2 });
            Assert.IsTrue(tube.IsComplete);
        }

        [Test]
        public void IsComplete_FullMixedColors_ReturnsFalse()
        {
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 2, 1, 2 });
            Assert.IsFalse(tube.IsComplete);
        }

        [Test]
        public void IsComplete_NotFull_ReturnsFalse()
        {
            // Three balls of same color but one slot empty — not complete
            var tube = TubeData.CreateWithBalls(4, new[] { 1, 1, 1, 0 });
            Assert.IsFalse(tube.IsComplete);
        }

        // ── Clone ─────────────────────────────────────────────────────────────

        [Test]
        public void Clone_ProducesDeepCopy_ChangingCloneDoesNotAffectOriginal()
        {
            var original = TubeData.CreateWithBalls(4, new[] { 1, 2, 0, 0 });
            var clone    = original.Clone();
            clone.Balls[0] = 99;
            Assert.AreEqual(1, original.Balls[0], "Original should be unchanged after mutating the clone.");
        }

        [Test]
        public void Clone_HasSameCapacityAndBalls()
        {
            var original = TubeData.CreateWithBalls(4, new[] { 3, 1, 2, 0 });
            var clone    = original.Clone();
            Assert.AreEqual(original.Capacity, clone.Capacity);
            Assert.AreEqual(original.TopColor,  clone.TopColor);
            Assert.AreEqual(original.TopIndex,  clone.TopIndex);
        }
    }
}
