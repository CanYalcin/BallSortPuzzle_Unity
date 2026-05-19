using System;
using UnityEngine;

namespace SortPuzzle.Data
{
    /// <summary>
    /// Reward granted at streak milestone days (7, 14, 21, 30).
    /// Create via: Assets -> Create -> SortPuzzle -> Daily Reward Config
    /// </summary>
    [Serializable]
    public class DailyMilestoneReward
    {
        [Tooltip("Streak day this reward is granted on (e.g. 7, 14, 21, 30).")]
        public int  StreakDay;

        [Tooltip("Gold granted at this milestone.")]
        public int  GoldBonus;

        [Tooltip("Undo boosts granted.")]
        public int  UndoBonus;

        [Tooltip("Shuffle Tube boosts granted.")]
        public int  ShuffleTubeBonus;

        [Tooltip("Extra Empty Tube boosts granted.")]
        public int  ExtraEmptyTubeBonus;

        [Tooltip("Unlocks a cosmetic skin reward.")]
        public bool UnlocksSkin;

        [Tooltip("Skin ID to unlock (matches WorldConfig skin key).")]
        public string SkinId;

        [Tooltip("Badge / title granted (display only, stored in PlayerData future field).")]
        public string BadgeTitle;
    }

    /// <summary>
    /// Full schedule of daily streak milestone rewards.
    /// Edit in the Inspector to change rewards without code changes.
    /// Create via: Assets -> Create -> SortPuzzle -> Daily Reward Config
    /// </summary>
    [CreateAssetMenu(fileName = "DailyRewardConfig", menuName = "SortPuzzle/Daily Reward Config")]
    public class DailyRewardConfig : ScriptableObject
    {
        [Tooltip("Must be in ascending StreakDay order. Days not listed give no special reward.")]
        public DailyMilestoneReward[] Milestones;

        /// <summary>Returns the milestone reward for a given streak day, or null if none.</summary>
        public DailyMilestoneReward GetReward(int streakDay)
        {
            if (Milestones == null) return null;
            foreach (var m in Milestones)
                if (m.StreakDay == streakDay) return m;
            return null;
        }

        /// <summary>Returns true if the given streak day has a milestone reward.</summary>
        public bool HasReward(int streakDay) => GetReward(streakDay) != null;
    }
}
