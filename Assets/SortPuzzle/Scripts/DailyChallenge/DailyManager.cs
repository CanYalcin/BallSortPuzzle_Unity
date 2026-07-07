using System;
using HyperBase.Core;
using HyperBase.Data;
using HyperBase.Notifications;
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
        private readonly NotificationManager _notifications;
        private readonly BoostConfig _boostConfig;
        private readonly HyperBase.RemoteConfig.RemoteConfigManager _remoteConfig;

        public int  CurrentStreak  => _save.Data.CurrentStreakDays;
        public int  LongestStreak  => _save.Data.LongestStreakDays;
        public bool CompletedToday => _save.Data.TodaysChallengeCompleted &&
                                      _save.Data.LastDailyChallengeDate == TodayLocal;

        private static string TodayLocal     => DateTime.Now.ToString("yyyy-MM-dd");
        private static string YesterdayLocal => DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        [Inject]
        public DailyManager(SaveManager save, EventBus events, GoldManager gold,
                            BoostManager boosts, DailyLevelDatabase db, DailyRewardConfig rewards,
                            NotificationManager notifications, BoostConfig boostConfig,
                            HyperBase.RemoteConfig.RemoteConfigManager remoteConfig)
        {
            _save          = save;
            _events        = events;
            _gold          = gold;
            _boosts        = boosts;
            _db            = db;
            _rewards       = rewards;
            _notifications = notifications;
            _boostConfig   = boostConfig;
            _remoteConfig  = remoteConfig;
        }

        /// <summary>Gold reward for completing today's challenge, Remote-Config-driven with BoostConfig as local fallback.</summary>
        public int ChallengeGoldReward => _remoteConfig.GetInt(HyperBase.RemoteConfig.RCKeys.DailyChallengeGoldReward, _boostConfig.DailyChallengeGold);

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
            string today     = TodayLocal;
            string yesterday = YesterdayLocal;
            string lastDate  = _save.Data.LastDailyChallengeDate;

            if (_save.Data.LastDailyResetDate != today)
            {
                _save.Data.LastDailyResetDate       = today;                _save.Data.DailyLoginBonusClaimed   = false;
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


        // ── Notifications ─────────────────────────────────────────────────────

        /// <summary>
        /// Cancels any pending reminder/streak-warning notifications and schedules fresh
        /// ones. Call once on app open, after CheckStreakOnOpen() so today's completion
        /// state is already up to date.
        /// </summary>
        public void ScheduleReminders()
        {
            _notifications.CancelAll();

            DateTime now           = DateTime.Now;
            DateTime todayAt10     = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0);
            DateTime reminderTime  = now < todayAt10 ? todayAt10 : todayAt10.AddDays(1);
            _notifications.ScheduleAt("daily_reminder", "Ball Sort Puzzle", "Your daily puzzle is ready!", reminderTime);

            if (_save.Data.CurrentStreakDays > 0 && !CompletedToday)
            {
                // Streak deadline is local midnight (CheckStreakOnOpen/CompleteChallenge both
                // key off local dates now) — warn 4h before that.
                DateTime warnLocal = now.Date.AddDays(1).AddHours(-4);
                if (warnLocal > now)
                {
                    int streak = _save.Data.CurrentStreakDays;
                    _notifications.ScheduleAt("streak_warning", "Don't lose your streak!",
                        $"You're on a {streak}-day streak. Play today's puzzle before it resets!", warnLocal);
                }
            }
        }

        // ── Complete Challenge ────────────────────────────────────────

        public void CompleteChallenge()
        {
            if (CompletedToday) { Debug.LogWarning("[DailyManager] Today already completed."); return; }

            string lastDate  = _save.Data.LastDailyChallengeDate;
            bool consecutive = lastDate == YesterdayLocal;

            _save.Data.CurrentStreakDays = consecutive ? _save.Data.CurrentStreakDays + 1 : 1;
            if (_save.Data.CurrentStreakDays > _save.Data.LongestStreakDays)
                _save.Data.LongestStreakDays = _save.Data.CurrentStreakDays;

            _save.Data.LastDailyChallengeDate   = TodayLocal;
            _save.Data.TodaysChallengeCompleted = true;

            int daySlot = DailyLevelDatabase.TodayIndex;
            if (_save.Data.DailyCompletedFlags != null && daySlot < _save.Data.DailyCompletedFlags.Length)
                _save.Data.DailyCompletedFlags[daySlot] = true;

            int reward = ChallengeGoldReward;
            _gold.Add(reward, "daily_challenge");

            int streak = _save.Data.CurrentStreakDays;
            _events.Publish(new SortPuzzle.OnDailyChallengeCompleted(daySlot, streak, reward));

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
