using HyperBase.Core;
using HyperBase.Data;
using HyperBase.Utilities;
using SortPuzzle.Data;
using SortPuzzle.Economy;
using UnityEngine;
using VContainer;

namespace HyperBase.Gameplay
{
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

        public void StartCurrentLevel()
        {
            _dailyMode = false;
            var cfg = CurrentLevel;
            if (cfg == null) { Debug.LogError("[LevelManager] CurrentLevel is null."); return; }
            _startTime = Time.unscaledTime;
            _events.Publish(new OnLevelStarted(_save.Data.CurrentLevelIndex));
            Debug.Log($"[LevelManager] Level started: {_save.Data.CurrentLevelIndex} — {cfg.DisplayName}");
        }

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

            _gold.Add(cfg.SoftCurrencyReward, "level_complete");

            var ld = cfg as SortPuzzle.Data.LevelData;
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

            _events.Publish(new OnLevelCompleted(completedIdx, dur, false));
            if (wasLast) _events.Publish(new OnWorldComplete(0));
            _game.TransitionTo(GameState.Win);
            _save.SaveAsync().Forget();
        }

        public void FailCurrentLevel()
        {
            _events.Publish(new OnLevelFailed(_save.Data.CurrentLevelIndex));
            _game.TransitionTo(GameState.Fail);
            _save.SaveAsync().Forget();
        }

        public void RetryCurrentLevel() => StartCurrentLevel();

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

        public void ResetDailyMode() { _dailyMode = false; _dailyLevel = null; }

        public void JumpToLevel(int levelIndex)
        {
            if (!_db.IsValid(levelIndex)) { Debug.LogWarning($"[LevelManager] JumpToLevel: index {levelIndex} out of range."); return; }
            _save.Data.CurrentLevelIndex = levelIndex;
            _dailyMode  = false;
            _dailyLevel = null;
        }

        public bool IsUnlocked(int index) => _db.IsValid(index) && index <= _save.Data.HighestUnlockedLevel;
    }
}
