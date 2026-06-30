using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HyperBase.Data;
using UnityEngine;
using VContainer;

namespace HyperBase.Audio
{
    /// <summary>
    /// Manages all game audio: SFX pool with auto-grow, dual-source crossfading music,
    /// and per-channel volume controls. Respects SoundEnabled, MusicEnabled, SfxVolume,
    /// and MasterVolume from PlayerData.
    /// Call <see cref="Initialize"/> once on startup before any other method.
    /// </summary>
    public class AudioManager
    {
        private readonly AudioConfig        _config;
        private readonly SaveManager        _saveManager;
        private readonly List<AudioSource>  _sfxPool = new();
        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool        _usingA = true;
        private GameObject  _root;

        public AudioConfig Config => _config;

        [Inject]
        public AudioManager(AudioConfig config, SaveManager saveManager)
        {
            _config      = config;
            _saveManager = saveManager;
        }

        /// <summary>
        /// Creates the runtime AudioSource pool and music sources on a DontDestroyOnLoad GameObject.
        /// Must be called once after construction (in BootstrapEntryPoint) before any audio plays.
        /// </summary>
        public void Initialize()
        {
            // Single GameObject — all AudioSources added as components directly.
            _root = new GameObject("[AudioManager]");
            Object.DontDestroyOnLoad(_root);

            for (int i = 0; i < _config.SfxPoolSize; i++)
            {
                var src         = _root.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop        = false;
                _sfxPool.Add(src);
            }

            _musicA             = _root.AddComponent<AudioSource>();
            _musicA.playOnAwake = false;
            _musicA.loop        = true;

            _musicB             = _root.AddComponent<AudioSource>();
            _musicB.playOnAwake = false;
            _musicB.loop        = true;

            Debug.Log("[AudioManager] Initialized.");
        }

        /// <summary>
        /// Plays a one-shot SFX clip. Auto-grows the pool if all sources are busy.
        /// Respects SoundEnabled and SfxVolume. No-op if clip is null or sound is disabled.
        /// </summary>
        /// <param name="vol">Per-call volume multiplier applied on top of SfxVolume and MasterVolume.</param>
        public void PlaySfx(AudioClip clip, float vol = 1f)
        {
            if (clip == null || !_saveManager.Data.SoundEnabled) return;
            AudioSource src = null;
            foreach (var s in _sfxPool) { if (!s.isPlaying) { src = s; break; } }
            if (src == null)
            {
                src             = _root.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
            src.clip   = clip;
            src.volume = _saveManager.Data.SfxVolume * _saveManager.Data.MasterVolume * vol;
            src.Play();
        }

        /// Named SFX shortcuts — each calls PlaySfx with the matching clip from AudioConfig.
        public void PlayButtonClick()   => PlaySfx(_config.ButtonClick);
        public void PlayCoinCollect()   => PlaySfx(_config.CoinCollect);
        public void PlayLevelComplete() => PlaySfx(_config.LevelComplete);
        public void PlayLevelFail()     => PlaySfx(_config.LevelFail);
        public void PlayPurchase()      => PlaySfx(_config.PurchaseSuccess);
        public void PlayRewardEarned()  => PlaySfx(_config.RewardEarned);

        /// <summary>
        /// Crossfades to a new music track using dual-AudioSource ping-pong.
        /// Fade duration is set in AudioConfig. No-op if clip is null.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            var cur = _usingA ? _musicA : _musicB;
            var nxt = _usingA ? _musicB : _musicA;
            nxt.clip   = clip;
            nxt.loop   = loop;
            nxt.volume = 0f;
            nxt.Play();
            CrossfadeAsync(cur, nxt).Forget();
            _usingA = !_usingA;
        }

        /// <summary>Fades out the currently active music track.</summary>
        public void StopMusic()
        {
            var act = _usingA ? _musicA : _musicB;
            FadeOutAsync(act).Forget();
        }

        /// Volume/toggle setters write directly to PlayerData — caller must call SaveAsync() to persist.
        public void SetSoundEnabled(bool v)   => _saveManager.Data.SoundEnabled   = v;
        public void SetHapticsEnabled(bool v)  => _saveManager.Data.HapticsEnabled = v;
        public void SetSfxVolume(float v)      => _saveManager.Data.SfxVolume      = Mathf.Clamp01(v);

        public void SetMusicEnabled(bool v)
        {
            _saveManager.Data.MusicEnabled = v;
            var act = _usingA ? _musicA : _musicB;
            if (act != null)
                act.volume = v ? _saveManager.Data.MusicVolume * _saveManager.Data.MasterVolume : 0f;
        }

        public void SetMasterVolume(float v)
        {
            _saveManager.Data.MasterVolume = Mathf.Clamp01(v);
            var act = _usingA ? _musicA : _musicB;
            if (act != null)
                act.volume = _saveManager.Data.MusicEnabled
                    ? _saveManager.Data.MusicVolume * _saveManager.Data.MasterVolume : 0f;
        }

        public void SetMusicVolume(float v)
        {
            _saveManager.Data.MusicVolume = Mathf.Clamp01(v);
            var act = _usingA ? _musicA : _musicB;
            if (act != null)
                act.volume = _saveManager.Data.MusicEnabled
                    ? _saveManager.Data.MusicVolume * _saveManager.Data.MasterVolume : 0f;
        }

        private async UniTaskVoid CrossfadeAsync(AudioSource from, AudioSource to)
        {
            float tgt = _saveManager.Data.MusicEnabled
                ? _saveManager.Data.MusicVolume * _saveManager.Data.MasterVolume : 0f;
            float dur = _config.MusicFadeDuration;
            float e   = 0f;
            while (e < dur)
            {
                e          += Time.unscaledDeltaTime;
                float p     = Mathf.Clamp01(e / dur);
                from.volume = Mathf.Lerp(tgt, 0f, p);
                to.volume   = Mathf.Lerp(0f, tgt, p);
                await UniTask.Yield();
            }
            from.Stop();
            from.volume = 0f;
            to.volume   = tgt;
        }

        private async UniTaskVoid FadeOutAsync(AudioSource src)
        {
            float start = src.volume;
            float dur   = _config.MusicFadeDuration;
            float e     = 0f;
            while (e < dur)
            {
                e          += Time.unscaledDeltaTime;
                src.volume  = Mathf.Lerp(start, 0f, Mathf.Clamp01(e / dur));
                await UniTask.Yield();
            }
            src.Stop();
            src.volume = 0f;
        }
    }
}
