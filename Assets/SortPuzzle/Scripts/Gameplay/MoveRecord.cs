using System;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// Records a single pour action for undo purposes.
    /// Stores enough information to fully reverse the move.
    /// </summary>
    [Serializable]
    public class MoveRecord
    {
        public int FromTube;      // source tube index
        public int ToTube;        // destination tube index
        public int ColorId;       // color that was poured
        public int BallCount;     // how many balls moved in this pour

        public MoveRecord(int from, int to, int colorId, int ballCount)
        {
            FromTube  = from;
            ToTube    = to;
            ColorId   = colorId;
            BallCount = ballCount;
        }
    }
}
