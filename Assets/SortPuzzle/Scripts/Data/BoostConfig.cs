using UnityEngine;

namespace SortPuzzle.Data
{
    [CreateAssetMenu(fileName = "BoostConfig", menuName = "SortPuzzle/Boost Config")]
    public class BoostConfig : ScriptableObject
    {
        [Header("Boost Gold Costs")]
        public int UndoGoldCost           = 500;
        public int ExtraEmptyTubeGoldCost = 1200;

        [Header("Rewarded Ad Gold")]
        public int RewardedAdGoldAmount = 300;
        public int DailyRewardedAdCap   = 5;

        [Header("Daily Economy")]
        public int DailyLoginBonusGold = 50;

        [Header("Level Gold Rewards")]
        public int GoldPerLevelMin    = 10;
        public int GoldPerLevelMax    = 50;
        public int DailyChallengeGold = 100;

        [Header("Free Boosts on Install")]
        public int StartingUndoCount      = 3;
        public int StartingExtraTubeCount = 0;

        public int GetCost(BoostType type) => type switch
        {
            BoostType.Undo           => UndoGoldCost,
            BoostType.ExtraEmptyTube => ExtraEmptyTubeGoldCost,
            _                        => 0
        };
    }
}
