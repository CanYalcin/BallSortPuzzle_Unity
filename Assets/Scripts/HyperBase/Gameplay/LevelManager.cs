using HyperBase.Core;
using HyperBase.Data;
using HyperBase.Utilities;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;

namespace HyperBase.Gameplay
{
    /// <summary>
    /// Governs level progression: starting, completing, failing, and retrying levels.
    /// Manages both normal progression (index persisted in PlayerData) and daily challenge mode.
    /// All state transitions fire through GameManager and EventBus — never mutate GameState directly.
    /// </summary>
    public class LevelManager
    {
        private readonly LevelDatabase _db;
        private readonly SaveManager   _save;
        private readonly EventBus      _events;
        private readonly GoldManager   _gold;
        private readonly BoostManager  _boostManager;
        private readonly SceneLoader   _loader;
        private readonly GameManager   _game;
        private float _startTime;

        private bool        _dailyMode;
        private LevelConfig _dailyLevel;

        public int         CurrentIndex => _save.Data.CurrentLevelIndex;
        public LevelConfig CurrentLevel => _dailyMode ? _dailyLevel : _db.Get(_save.Data.CurrentLevelIndex);
        public bool        IsDailyMode  => _dailyMode;
        public bool        IsLastLevel  => _save.Data.CurrentLevelIndex >= _db.Count - 1;

        [Inject]
        public LevelManager(LevelDatabase db, SaveManager save, EventBus events,
                            GoldManager gold, BoostManager boostManager,
                            SceneLoader loader, GameManager game)
        {
            _db           = db;
            _save         = save;
            _events       = events;
            _gold         = gold;
            _boostManager = boostManager;
            _loader       = loader;
            _game         = game;
        }

        /// <summary>Exits daily mode and starts the current normal level. Publishes OnLevelStarted.</summary>
        public void StartCurrentLevel()
        {
            _dailyMode = false;
            var cfg = CurrentLevel;
            if (cfg == null) { Debug.LogError("[LevelManager] CurrentLevel is null."); return; }
            _startTime = Time.unscaledTime;
            _events.Publish(new OnLevelStarted(_save.Data.CurrentLevelIndex));
            Debug.Log($"[LevelManager] Level started: {_save.Data.CurrentLevelIndex} — {cfg.DisplayName}");
        }

        /// <summary>
        /// Completes the current level: awards gold, grants any embedded boosts, advances the level
        /// index, and transitions to Win state. In daily mode, skips gold/boost grants.
        /// Publishes OnLevelCompleted.
        /// </summary>
public void CompleteCurrentLevel()
        {
            float dur = Time.unscaledTime - _startTime;
            var   cfg = CurrentLevel;
            var   d   = _save.Data;

            if (_dailyMode)
            {
                d.TotalLevelsCompleted++;
                _events.Publish(new OnLevelCompleted(-1, dur, true));
                _game.TransitionTo(GameState.Win);
                _save.SaveAsync().Forget();
                return;
            }

            var ld = cfg as SortPuzzle.Data.LevelData;
            if (ld == null)
                Debug.LogWarning($"[LevelManager] CurrentLevel (index {d.CurrentLevelIndex}) is not a LevelData — no gold awarded.");

            _gold.Add(ld != null ? ld.GoldReward : 0, "level_complete");

            if (ld != null && ld.ContainsUndoBoost)
            {
                _boostManager.Grant(SortPuzzle.Data.BoostType.Undo, 1);
                Debug.Log($"[LevelManager] Granted 1 Undo boost for level {ld.LevelIndex}.");
            }
            if (ld != null && ld.ContainsExtraEmptyTubeBoost)
            {
                _boostManager.Grant(SortPuzzle.Data.BoostType.ExtraEmptyTube, 1);
                Debug.Log($"[LevelManager] Granted 1 Extra Tube boost for level {ld.LevelIndex}.");
            }

            int completedIdx = d.CurrentLevelIndex;
            d.TotalLevelsCompleted++;

            bool wasLast = IsLastLevel;
            if (!wasLast)
            {
                d.CurrentLevelIndex    = completedIdx + 1;
                d.HighestUnlockedLevel = Mathf.Max(d.HighestUnlockedLevel, d.CurrentLevelIndex);
            }

            _events.Publish(new OnLevelCompleted(completedIdx, dur, false, ld != null ? ld.GoldReward : 0));
            _game.TransitionTo(GameState.Win);
            _save.SaveAsync().Forget();
        }

        /// <summary>Fails the current level. Publishes OnLevelFailed and transitions to Fail state.</summary>
        public void FailCurrentLevel()
        {
            _events.Publish(new OnLevelFailed(_save.Data.CurrentLevelIndex));
            _game.TransitionTo(GameState.Fail);
            _save.SaveAsync().Forget();
        }

        /// <summary>Retries the current level by calling StartCurrentLevel() again.</summary>
        public void RetryCurrentLevel() => StartCurrentLevel();

        /// <summary>
        /// Enters daily challenge mode with the given level config.
        /// Publishes OnDailyChallengeStarted and transitions to Gameplay.
        /// Call ResetDailyMode() on return to normal levels.
        /// </summary>
        public void StartDailyChallenge(LevelConfig dailyLevel)
        {
            _dailyMode  = true;
            _dailyLevel = dailyLevel;
            _startTime  = Time.unscaledTime;
            _game.TransitionTo(GameState.Gameplay);
            int streak = _save.Data.CurrentStreakDays;
            _events.Publish(new OnLevelStarted(-1));
            _events.Publish(new SortPuzzle.OnDailyChallengeStarted(_save.Data.LastDailyResetDate, streak));
            Debug.Log("[LevelManager] Daily challenge started.");
        }

        /// <summary>Exits daily challenge mode and clears the daily level reference.</summary>
        public void ResetDailyMode() { _dailyMode = false; _dailyLevel = null; }

        /// <summary>Debug/editor utility: directly sets CurrentLevelIndex. No events are fired.</summary>
        public void JumpToLevel(int levelIndex)
        {
            if (!_db.IsValid(levelIndex)) { Debug.LogWarning($"[LevelManager] JumpToLevel: index {levelIndex} out of range."); return; }
            _save.Data.CurrentLevelIndex = levelIndex;
            _dailyMode  = false;
            _dailyLevel = null;
        }

        /// <summary>Returns true if the given index is within the unlocked level range.</summary>
        public bool IsUnlocked(int index) => _db.IsValid(index) && index <= _save.Data.HighestUnlockedLevel;
    }
}
