using System;

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
    }
}
