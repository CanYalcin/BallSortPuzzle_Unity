using UnityEngine;

namespace HyperBase.Gameplay
{
    /// <summary>Per-level configuration asset. Create via Assets > Create > HyperBase > Level Config</summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "HyperBase/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        public int    LevelIndex;
        public string DisplayName;
        public string SceneName;

        [Header("Rewards")]
        public int    SoftCurrencyReward = 100;
        public int    HardCurrencyReward = 0;

        [Header("Difficulty")]
        [Range(1, 10)]
        public int    DifficultyRating   = 1;
        public float  TimeLimit          = 0f;

        [Header("Tuning")]
        public float  GameSpeedMultiplier = 1f;
    }
}
