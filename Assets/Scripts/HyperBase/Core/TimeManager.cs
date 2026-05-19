using System;
using HyperBase.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Core
{
    /// <summary>
    /// Tracks session time, total playtime, and offline duration.
    /// Persists through SaveManager. Registered as IInitializable + ITickable via VContainer.
    /// </summary>
    public class TimeManager : IInitializable, ITickable
    {
        private readonly SaveManager _save;
        private float _sessionStart;
        private float _tickAccum;
        private const float PersistInterval = 60f;

        public float    SessionSeconds  => Time.unscaledTime - _sessionStart;
        public float    TotalSeconds    => _save.Data.TotalPlayTimeSeconds + SessionSeconds;
        public TimeSpan TotalPlayTime   => TimeSpan.FromSeconds(TotalSeconds);
        public TimeSpan OfflineDuration { get; private set; }

        [Inject]
        public TimeManager(SaveManager save) => _save = save;

        public void Initialize()
        {
            _sessionStart = Time.unscaledTime;
            string last   = _save.Data.LastSaveTime;
            if (!string.IsNullOrEmpty(last) && DateTime.TryParse(last, out var dt))
            {
                var elapsed = DateTime.UtcNow - dt;
                OfflineDuration = elapsed.TotalSeconds > 0 ? elapsed : TimeSpan.Zero;
            }
            else OfflineDuration = TimeSpan.Zero;

            Debug.Log($"[TimeManager] Session started. Offline: {OfflineDuration:hh\\:mm\\:ss}");
        }

        public void Tick()
        {
            _tickAccum += Time.unscaledDeltaTime;
            if (_tickAccum < PersistInterval) return;
            _tickAccum = 0f;
            _save.Data.TotalPlayTimeSeconds += PersistInterval;
            _save.SaveAsync().Forget();
        }

        public bool WasOfflineFor(float seconds) => OfflineDuration.TotalSeconds >= seconds;

        public string FormattedTotal()
        {
            var t = TotalPlayTime;
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
        }
    }
}
