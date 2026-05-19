using UnityEngine;

namespace SortPuzzle.Data
{
    /// <summary>
    /// Ordered pool of 30 daily challenge levels.
    /// Day index = (DayOfYear - 1) % 30 — same puzzle globally each day.
    /// Create via: Assets -> Create -> SortPuzzle -> Daily Level Database
    /// </summary>
    [CreateAssetMenu(fileName = "DailyLevelDatabase", menuName = "SortPuzzle/Daily Level Database")]
    public class DailyLevelDatabase : ScriptableObject
    {
        [Tooltip("Exactly 30 entries. Index 0 = Day 1, Index 29 = Day 30. Loops forever.")]
        public LevelData[] DailyLevels = new LevelData[30];

        public int Count => DailyLevels?.Length ?? 0;

        /// <summary>Returns the level for today based on UTC day of year.</summary>
        public LevelData GetTodaysLevel()
        {
            int index = (System.DateTime.UtcNow.DayOfYear - 1) % 30;
            return GetLevel(index);
        }

        /// <summary>Returns the level for a specific 0-based day index (0–29).</summary>
        public LevelData GetLevel(int dayIndex)
        {
            if (DailyLevels == null || DailyLevels.Length == 0)
            {
                Debug.LogError("[DailyLevelDatabase] No daily levels assigned.");
                return null;
            }
            int clamped = dayIndex % 30;
            if (DailyLevels[clamped] == null)
            {
                Debug.LogWarning($"[DailyLevelDatabase] Day {clamped + 1} has no level assigned.");
                return null;
            }
            return DailyLevels[clamped];
        }

        /// <summary>Returns the 0-based day index for today (0–29).</summary>
        public static int TodayIndex => (System.DateTime.UtcNow.DayOfYear - 1) % 30;
    }
}
