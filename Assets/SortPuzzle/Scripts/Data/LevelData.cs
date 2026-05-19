using System;
using HyperBase.Gameplay;
using UnityEngine;

namespace SortPuzzle.Data
{
    /// <summary>
    /// Serializable wrapper for one tube's ball array.
    /// Unity cannot serialize int[][] directly — this is the fix.
    /// </summary>
    [Serializable]
    public class TubeRow
    {
        [Tooltip("Ball colorIds, index 0 = bottom. 0 = empty slot.")]
        public int[] Balls;

        public TubeRow(int capacity)
        {
            Balls = new int[capacity];
        }

        public TubeRow(int[] balls)
        {
            Balls = (int[])balls.Clone();
        }
    }

    /// <summary>
    /// Defines a single water-sort puzzle level.
    /// Extends LevelConfig so it lives inside HyperBase LevelDatabase
    /// and is retrieved via LevelManager.CurrentLevel.
    ///
    /// Create via: Assets -> Create -> SortPuzzle -> Level Data
    ///
    /// Tubes[i].Balls[j]:
    ///   j = 0 is the bottom of the tube, j = TubeCapacity-1 is the top.
    ///   value 0 = empty slot, value 1..N = colorId.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "SortPuzzle/Level Data")]
    public class LevelData : LevelConfig
    {
        [Header("World")]
        public int WorldIndex;

        [Header("Tube Configuration")]
        public int TubeCount      = 6;
        public int EmptyTubeCount = 2;
        public int TubeCapacity   = 4;
        public int ColorCount     = 4;

        [Header("Initial State")]
        [Tooltip("One TubeRow per tube. Each TubeRow.Balls[j] = colorId (0=empty).")]
        public TubeRow[] Tubes;

        [Header("Solver")]
        public int    ParMoves          = 0;
        public string ValidatedSolution = "";

        [Header("Rewards")]
        public int  GoldReward        = 20;
        public bool ContainsUndoBoost           = false;
        public bool ContainsExtraEmptyTubeBoost = false;

        [Header("Daily Challenge")]
        public bool IsDailyLevel = false;

#if UNITY_EDITOR
        public void SetValidationResult(int parMoves, string solution)
        {
            ParMoves          = parMoves;
            ValidatedSolution = solution;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Converts old jagged-array InitialState into the new TubeRow format.
        /// Call from the Level Editor Window when saving assets.
        /// </summary>
        public void SetInitialState(int[][] state)
        {
            Tubes = new TubeRow[TubeCount];
            for (int i = 0; i < TubeCount; i++)
            {
                if (state != null && i < state.Length && state[i] != null)
                    Tubes[i] = new TubeRow(state[i]);
                else
                    Tubes[i] = new TubeRow(TubeCapacity);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>
        /// Returns a fresh TubeData[] array populated from the saved Tubes rows.
        /// Called by PuzzleController.Initialize().
        /// </summary>
        public TubeData[] CreateRuntimeTubes()
        {
            var result = new TubeData[TubeCount];
            for (int i = 0; i < TubeCount; i++)
            {
                if (Tubes != null && i < Tubes.Length && Tubes[i]?.Balls != null)
                    result[i] = TubeData.CreateWithBalls(TubeCapacity, Tubes[i].Balls);
                else
                    result[i] = TubeData.Create(TubeCapacity);
            }
            return result;
        }
    }
}
