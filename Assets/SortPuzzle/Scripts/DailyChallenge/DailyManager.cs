using HyperBase.Core;
using HyperBase.Data;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;

namespace SortPuzzle.DailyChallenge
{
    public class DailyManager
    {
        private readonly SaveManager        _save;
        private readonly EventBus           _events;
        private readonly GoldManager        _gold;
        private readonly BoostManager       _boosts;
        private readonly DailyLevelDatabase _db;
        private readonly DailyRewardConfig  _rewards;

        public int  CurrentStreak  => _save.Data.CurrentStreakDays;
        public int  LongestStreak  => _save.Data.LongestStreakDays;
        public bool CompletedToday => _save.Data.TodaysChallengeCompleted &&
                                      _save.Data.LastDailyChallengeDate == TodayUtc;

        private static string TodayUtc     => System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        private static string YesterdayUtc => System.DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        [Inject]
        public DailyManager(SaveManager save, EventBus events, GoldManager gold,
                            BoostManager boosts, DailyLevelDatabase db, DailyRewardConfig rewards)
        {
            _save    = save;
            _events  = events;
            _gold    = gold;
            _boosts  = boosts;
            _db      = db;
            _rewards = rewards;
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public LevelData GetTodaysLevel()   => _db.GetTodaysLevel();
        public bool      CanPlayToday()     => !CompletedToday;
        public int       TodayIndex        => DailyLevelDatabase.TodayIndex;
        public bool      IsDayCompleted(int daySlot)
        {
            var flags = _save.Data.DailyCompletedFlags;
            if (flags == null || daySlot < 0 || daySlot >= flags.Length) return false;
            return flags[daySlot];
        }

        // ── On App Open ───────────────────────────────────────────────────────

        public void CheckStreakOnOpen()
        {
            string today     = TodayUtc;
            string yesterday = YesterdayUtc;
            string lastDate  = _save.Data.LastDailyChallengeDate;

            if (_save.Data.LastDailyResetDate != today)
            {
                _save.Data.LastDailyResetDate       = today;
                _save.Data.DailyRewardedAdsWatched  = 0;
                _save.Data.DailyLoginBonusClaimed   = false;
                _save.Data.TodaysChallengeCompleted = false;
            }

            if (string.IsNullOrEmpty(lastDate)) return;
            if (lastDate == today)     return;
            if (lastDate == yesterday) return;

            int prev = _save.Data.CurrentStreakDays;
            if (prev > 0)
            {
                _save.Data.CurrentStreakDays = 0;
                _events.Publish(new SortPuzzle.OnStreakBroken(prev));
                Debug.Log($"[DailyManager] Streak broken. Was {prev} days.");
                _save.SaveAsync().Forget();
            }
        }

        // ── Complete Challenge ────────────────────────────────────────────────

public void CompleteChallenge()
        {
            if (CompletedToday) { Debug.LogWarning("[DailyManager] Today already completed."); return; }

            string lastDate  = _save.Data.LastDailyChallengeDate;
            bool consecutive = lastDate == YesterdayUtc;

            _save.Data.CurrentStreakDays = consecutive ? _save.Data.CurrentStreakDays + 1 : 1;
            if (_save.Data.CurrentStreakDays > _save.Data.LongestStreakDays)
                _save.Data.LongestStreakDays = _save.Data.CurrentStreakDays;

            _save.Data.LastDailyChallengeDate   = TodayUtc;
            _save.Data.TodaysChallengeCompleted = true;

            int daySlot = DailyLevelDatabase.TodayIndex;
            if (_save.Data.DailyCompletedFlags != null && daySlot < _save.Data.DailyCompletedFlags.Length)
                _save.Data.DailyCompletedFlags[daySlot] = true;

            _gold.Add(100, "daily_challenge");

            int streak = _save.Data.CurrentStreakDays;
            _events.Publish(new SortPuzzle.OnDailyChallengeCompleted(daySlot, streak, 100));

            var milestone = _rewards?.GetReward(streak);
            if (milestone != null)
            {
                if (milestone.GoldBonus > 0)
                    _gold.Add(milestone.GoldBonus, "streak_milestone_" + streak);
                if (milestone.UndoBonus > 0)
                    _boosts.Grant(BoostType.Undo, milestone.UndoBonus);
                if (milestone.ExtraEmptyTubeBonus > 0)
                    _boosts.Grant(BoostType.ExtraEmptyTube, milestone.ExtraEmptyTubeBonus);
                _events.Publish(new SortPuzzle.OnStreakMilestoneReached(streak));
                Debug.Log($"[DailyManager] Milestone day {streak}: +{milestone.GoldBonus}g +{milestone.UndoBonus}U +{milestone.ExtraEmptyTubeBonus}ET");
            }

            Debug.Log($"[DailyManager] Challenge complete. Streak: {streak}.");
            _save.SaveAsync().Forget();
        }
    }
}
