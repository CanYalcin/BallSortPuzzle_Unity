using SortPuzzle.Data;

namespace SortPuzzle
{
    // ── Economy Events ────────────────────────────────────────────────────────

    public readonly struct OnGoldChanged
    {
        public readonly int    OldAmount;
        public readonly int    NewAmount;
        public readonly int    Delta;
        public readonly string Source;
        public OnGoldChanged(int old, int next, int delta, string source)
        { OldAmount = old; NewAmount = next; Delta = delta; Source = source; }
    }

    public readonly struct OnGoldInsufficient
    {
        public readonly int    AmountNeeded;
        public readonly string Sink;
        public OnGoldInsufficient(int needed, string sink) { AmountNeeded = needed; Sink = sink; }
    }

    public readonly struct OnBoostUsed
    {
        public readonly BoostType Type;
        public readonly int       Remaining;
        public OnBoostUsed(BoostType type, int remaining) { Type = type; Remaining = remaining; }
    }

    public readonly struct OnBoostGranted
    {
        public readonly BoostType Type;
        public readonly int       CountGranted;
        public readonly int       NewTotal;
        public OnBoostGranted(BoostType type, int granted, int total)
        { Type = type; CountGranted = granted; NewTotal = total; }
    }

    public readonly struct OnBoostInsufficient
    {
        public readonly BoostType Type;
        public OnBoostInsufficient(BoostType type) { Type = type; }
    }

    // ── Puzzle Events ─────────────────────────────────────────────────────────

    public readonly struct OnPourMade
    {
        public readonly int FromTube;
        public readonly int ToTube;
        public readonly int BallCount;
        public readonly int ColorId;
        public OnPourMade(int from, int to, int count, int color)
        { FromTube = from; ToTube = to; BallCount = count; ColorId = color; }
    }

    public readonly struct OnTubeCompleted
    {
        public readonly int TubeIndex;
        public readonly int ColorId;
        public OnTubeCompleted(int index, int color) { TubeIndex = index; ColorId = color; }
    }

    public readonly struct OnPuzzleWon
    {
        public readonly int LevelIndex;
        public readonly int WorldIndex;
        public readonly int GoldEarned;
        public OnPuzzleWon(int level, int world, int gold)
        { LevelIndex = level; WorldIndex = world; GoldEarned = gold; }
    }

    public readonly struct OnPuzzleRestarted
    {
        public readonly int LevelIndex;
        public OnPuzzleRestarted(int level) { LevelIndex = level; }
    }

    // ── Daily Challenge Events ────────────────────────────────────────────────

    public readonly struct OnDailyChallengeStarted
    {
        public readonly string DateKey;
        public readonly int    StreakBefore;
        public OnDailyChallengeStarted(string dateKey, int streak) { DateKey = dateKey; StreakBefore = streak; }
    }
    
public readonly struct OnDailyChallengeCompleted
    {
        public readonly int DayIndex;
        public readonly int NewStreakDays;
        public readonly int GoldEarned;
        public OnDailyChallengeCompleted(int day, int streak, int gold)
        { DayIndex = day; NewStreakDays = streak; GoldEarned = gold; }
    }

    public readonly struct OnStreakMilestoneReached
    {
        public readonly int StreakDay;
        public OnStreakMilestoneReached(int day) { StreakDay = day; }
    }

    public readonly struct OnStreakBroken
    {
        public readonly int PreviousStreak;
        public OnStreakBroken(int previous) { PreviousStreak = previous; }
    }
}
