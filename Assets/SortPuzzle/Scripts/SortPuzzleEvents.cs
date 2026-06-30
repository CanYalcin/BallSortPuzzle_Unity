using SortPuzzle.Data;

namespace SortPuzzle
{
    // ── Puzzle ────────────────────────────────────────────────────────────────

    /// <summary>Fired by PuzzleController when all tubes are sorted. Carries the gold reward and level indices.</summary>
    public readonly struct OnPuzzleWon
    {
        public readonly int LevelIndex;
        public readonly int WorldIndex;
        public readonly int GoldEarned;
        public OnPuzzleWon(int levelIndex, int worldIndex, int goldEarned)
        { LevelIndex = levelIndex; WorldIndex = worldIndex; GoldEarned = goldEarned; }
    }

    /// <summary>Fired by PuzzleController after each successful pour. Carries source, destination, ball count and color.</summary>
    public readonly struct OnPourMade
    {
        public readonly int FromTube;
        public readonly int ToTube;
        public readonly int BallCount;
        public readonly int Color;
        public OnPourMade(int fromTube, int toTube, int ballCount, int color)
        { FromTube = fromTube; ToTube = toTube; BallCount = ballCount; Color = color; }
    }

    /// <summary>Fired by PuzzleController when a tube is fully sorted to a single color.</summary>
    public readonly struct OnTubeCompleted
    {
        public readonly int TubeIndex;
        public readonly int Color;
        public OnTubeCompleted(int tubeIndex, int color) { TubeIndex = tubeIndex; Color = color; }
    }

    /// <summary>Fired by PuzzleController when the puzzle is reset to its initial state.</summary>
    public readonly struct OnPuzzleRestarted
    {
        public readonly int LevelIndex;
        public OnPuzzleRestarted(int levelIndex) => LevelIndex = levelIndex;
    }

    // ── Gold ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired by GoldManager after every balance change.
    /// Delta is positive for earnings, negative for spends.
    /// </summary>
    public readonly struct OnGoldChanged
    {
        public readonly int    OldAmount;
        public readonly int    NewAmount;
        public readonly int    Delta;
        public readonly string Source;
        public OnGoldChanged(int oldAmount, int newAmount, int delta, string source)
        { OldAmount = oldAmount; NewAmount = newAmount; Delta = delta; Source = source; }
    }

    /// <summary>Fired by GoldManager when a spend attempt fails due to insufficient balance.</summary>
    public readonly struct OnGoldInsufficient
    {
        public readonly int    Amount;
        public readonly string Sink;
        public OnGoldInsufficient(int amount, string sink) { Amount = amount; Sink = sink; }
    }

    // ── Boosts ────────────────────────────────────────────────────────────────

    /// <summary>Fired by BoostManager when a boost is consumed from inventory.</summary>
    public readonly struct OnBoostUsed
    {
        public readonly BoostType Type;
        public readonly int       Remaining;
        public OnBoostUsed(BoostType type, int remaining) { Type = type; Remaining = remaining; }
    }

    /// <summary>Fired by BoostManager when boosts are added to inventory (ad reward, daily milestone, or starter grant).</summary>
    public readonly struct OnBoostGranted
    {
        public readonly BoostType Type;
        public readonly int       CountGranted;
        public readonly int       NewTotal;
        public OnBoostGranted(BoostType type, int countGranted, int newTotal)
        { Type = type; CountGranted = countGranted; NewTotal = newTotal; }
    }

    /// <summary>Fired by BoostManager when a boost button is pressed but inventory is empty. BoostSystem listens to trigger the ad flow.</summary>
    public readonly struct OnBoostInsufficient
    {
        public readonly BoostType Type;
        public OnBoostInsufficient(BoostType type) => Type = type;
    }

    // ── Daily Challenge ───────────────────────────────────────────────────────

    /// <summary>Fired by LevelManager when entering daily challenge mode.</summary>
    public readonly struct OnDailyChallengeStarted
    {
        public readonly string DateKey;
        public readonly int    StreakBefore;
        public OnDailyChallengeStarted(string dateKey, int streakBefore)
        { DateKey = dateKey; StreakBefore = streakBefore; }
    }

    /// <summary>Fired by DailyManager when the daily challenge is finished. Carries day index, new streak, and gold earned.</summary>
    public readonly struct OnDailyChallengeCompleted
    {
        public readonly int DayIndex;
        public readonly int NewStreakDays;
        public readonly int GoldEarned;
        public OnDailyChallengeCompleted(int dayIndex, int newStreakDays, int goldEarned)
        { DayIndex = dayIndex; NewStreakDays = newStreakDays; GoldEarned = goldEarned; }
    }

    /// <summary>Fired by DailyManager when the player reaches a streak milestone. Carries the milestone day count.</summary>
    public readonly struct OnStreakMilestoneReached
    {
        public readonly int StreakDay;
        public OnStreakMilestoneReached(int streakDay) => StreakDay = streakDay;
    }

    /// <summary>Fired by DailyManager when the player misses a day and their streak resets to zero.</summary>
    public readonly struct OnStreakBroken
    {
        public readonly int PreviousStreak;
        public OnStreakBroken(int previousStreak) => PreviousStreak = previousStreak;
    }
}
