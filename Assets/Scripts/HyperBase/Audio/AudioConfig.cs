using UnityEngine;

namespace HyperBase.Audio
{
    /// <summary>
    /// Project-wide audio settings and clip references.
    /// Create via: Assets -> Create -> HyperBase -> Audio Config
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "HyperBase/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Pool & Defaults")]
        public int   SfxPoolSize        = 10;
        public float DefaultSfxVolume   = 1f;
        public float DefaultMusicVolume = 0.6f;

        [Header("Music Cross-Fade")]
        public float MusicFadeDuration  = 0.5f;

        [Header("Common SFX Clips")]
        public AudioClip ButtonClick;
        public AudioClip CoinCollect;
        public AudioClip LevelComplete;
        public AudioClip LevelFail;
        public AudioClip PurchaseSuccess;
        public AudioClip RewardEarned;

        [Header("Music Clips")]
        public AudioClip MainMenuMusic;
        public AudioClip GameplayMusic;
        public AudioClip WinMusic;
    }
}
