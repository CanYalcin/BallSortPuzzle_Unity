using System;
using UnityEngine;

namespace SortPuzzle.Data
{
    public enum BoostType { Undo, ExtraEmptyTube }

    /// <summary>
    /// Runtime state of a single tube during gameplay.
    /// Pure C# — no MonoBehaviour, no Unity dependencies.
    /// Bottom of tube = index 0. Top = highest filled index.
    /// </summary>
    [Serializable]
    public class TubeData
    {
        public int   Capacity;       // max balls the tube holds (default 4)
        public int[] Balls;          // colorId per slot; 0 = empty slot

        // ── Computed properties ───────────────────────────────────────────────

        /// <summary>Index of the topmost filled slot. -1 if empty.</summary>
        public int TopIndex
        {
            get
            {
                for (int i = Capacity - 1; i >= 0; i--)
                    if (Balls[i] != 0) return i;
                return -1;
            }
        }

        public bool IsEmpty    => TopIndex == -1;
        public bool IsFull     => TopIndex == Capacity - 1;

        /// <summary>Color of the topmost ball. 0 if empty.</summary>
        public int TopColor    => IsEmpty ? 0 : Balls[TopIndex];

        /// <summary>
        /// How many consecutive same-color balls sit at the top.
        /// Used to determine how many balls pour at once.
        /// </summary>
        public int TopRunLength
        {
            get
            {
                int top = TopIndex;
                if (top < 0) return 0;
                int color = Balls[top];
                int count = 0;
                for (int i = top; i >= 0; i--)
                {
                    if (Balls[i] == color) count++;
                    else break;
                }
                return count;
            }
        }

        /// <summary>
        /// True when tube is completely filled with a single color.
        /// A completed tube cannot be poured from or into.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                if (!IsFull) return false;
                int color = Balls[0];
                for (int i = 1; i < Capacity; i++)
                    if (Balls[i] != color) return false;
                return true;
            }
        }

        // ── Factory ───────────────────────────────────────────────────────────

        public static TubeData Create(int capacity)
        {
            return new TubeData
            {
                Capacity = capacity,
                Balls    = new int[capacity]  // all 0 = empty
            };
        }

        public static TubeData CreateWithBalls(int capacity, int[] balls)
        {
            var t = Create(capacity);
            int copy = Mathf.Min(balls.Length, capacity);
            for (int i = 0; i < copy; i++) t.Balls[i] = balls[i];
            return t;
        }

        // ── Deep copy ─────────────────────────────────────────────────────────

        public TubeData Clone()
        {
            var c = new TubeData { Capacity = Capacity, Balls = new int[Capacity] };
            Array.Copy(Balls, c.Balls, Capacity);
            return c;
        }
    }
}
