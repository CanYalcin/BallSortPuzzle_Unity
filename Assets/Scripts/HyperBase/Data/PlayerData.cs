using System;
using System.Collections.Generic;

namespace HyperBase.Data
{
    /// <summary>
    /// The single source of truth for all persistent player data.
    /// Serialised to JSON and AES-256 encrypted on disk.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public int    SchemaVersion          = 2;
        public string PlayerId               = Guid.NewGuid().ToString();
        public string LastSaveTime;

        // ── Core Progression ──────────────────────────────────────────────────
        public int CurrentLevelIndex         = 0;
        public int HighestUnlockedLevel      = 0;
        public int TotalLevelsCompleted      = 0;

        // ── HyperBase Currency (unused in SortPuzzle — kept for base compat) ──
        public int SoftCurrency              = 0;
        public int HardCurrency              = 0;

        // ── SortPuzzle Economy ────────────────────────────────────────────────
        /// <summary>Primary in-game currency. Earned from levels, daily challenge, login, rewarded ads.</summary>
        public int GoldBalance               = 0;

        // ── Boosts ────────────────────────────────────────────────────────────
        /// <summary>Undo last pour. 1 use = 1 move undone. Chainable.</summary>
        public int UndoCount                 = 3;   // 3 free on install


        /// <summary>Add one temporary empty tube to the current level.</summary>
        public int ExtraEmptyTubeCount       = 0;

        // ── Daily Challenge & Streak ──────────────────────────────────────────
        /// <summary>UTC date string "yyyy-MM-dd" of last completed daily challenge.</summary>
        public string LastDailyChallengeDate = "";

        /// <summary>Whether today's daily challenge has been completed.</summary>
        public bool TodaysChallengeCompleted = false;

        /// <summary>Current consecutive daily challenge streak.</summary>
        public int CurrentStreakDays         = 0;

        /// <summary>All-time best streak.</summary>
        public int LongestStreakDays         = 0;

        /// <summary>Tracks which of the 30 cycle days have been completed (for calendar display).</summary>
        public bool[] DailyCompletedFlags    = new bool[30];

        // ── World Progression ─────────────────────────────────────────────────
        /// <summary>Highest world index unlocked (0 = World1 only, 1 = World2, 2 = World3).</summary>
        public int HighestWorldUnlocked      = 0;

        /// <summary>Stars earned per level per world. Key = "worldIndex_levelIndex".</summary>
        public SerializableDictionary<string, int> LevelStars = new();

        // ── Monetisation ─────────────────────────────────────────────────────
        public bool IsNoAds                  = false;
        public bool StarterPackPurchased     = false;
        public int  TotalInterstitialsShown  = 0;
        public int  TotalRewardedShown       = 0;

        // ── Settings ──────────────────────────────────────────────────────────
        public bool  SoundEnabled            = true;
        public bool  MusicEnabled            = true;
        public bool  HapticsEnabled          = true;
        public float MasterVolume            = 1f;
        public float SfxVolume               = 1f;
        public float MusicVolume             = 0.6f;
        public bool  ColorblindMode          = false;

        // ── Session & Lifetime Stats ──────────────────────────────────────────
        public int   TotalSessionCount       = 0;
        public float TotalPlayTimeSeconds    = 0f;
        public int   TotalPoursMade          = 0;
        public int   TotalUndosUsed          = 0;
        public int   TotalBoostsUsed         = 0;

        // ── Daily Economy (resets each UTC day) ───────────────────────────────
        /// <summary>UTC date of last daily reset. Used to reset daily earn limits.</summary>
        public string LastDailyResetDate     = "";
        public int    DailyRewardedAdsWatched = 0;  // cap: 5 per day
        public bool   DailyLoginBonusClaimed = false;

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>Returns the stars earned for a specific level (0 if not played).</summary>
        public int GetLevelStars(int worldIndex, int levelIndex)
        {
            string key = worldIndex + "_" + levelIndex;
            return LevelStars.TryGetValue(key, out int stars) ? stars : 0;
        }

        /// <summary>Sets stars for a level, only if higher than existing value.</summary>
        public void SetLevelStars(int worldIndex, int levelIndex, int stars)
        {
            string key = worldIndex + "_" + levelIndex;
            if (!LevelStars.ContainsKey(key) || LevelStars[key] < stars)
                LevelStars[key] = stars;
        }
    }

    /// <summary>
    /// JSON-serialisable dictionary wrapper.
    /// Unity's JsonUtility does not support Dictionary — use this instead.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey>   Keys   = new();
        public List<TValue> Values = new();

        public bool TryGetValue(TKey key, out TValue value)
        {
            int idx = Keys.IndexOf(key);
            if (idx < 0) { value = default; return false; }
            value = Values[idx];
            return true;
        }

        public bool ContainsKey(TKey key) => Keys.Contains(key);

        public TValue this[TKey key]
        {
            get { int idx = Keys.IndexOf(key); return idx >= 0 ? Values[idx] : default; }
            set
            {
                int idx = Keys.IndexOf(key);
                if (idx >= 0) Values[idx] = value;
                else { Keys.Add(key); Values.Add(value); }
            }
        }
    }
}
