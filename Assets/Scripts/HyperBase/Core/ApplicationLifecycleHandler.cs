using System.Collections.Generic;
using HyperBase.Analytics;
using HyperBase.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Core
{
    /// <summary>
    /// Handles Unity app lifecycle events.
    /// Saves on pause/focus-loss and on quit. Logs a session_end analytics event exactly
    /// once per session — guarded so OnApplicationPause/OnApplicationQuit both being able
    /// to fire (mobile backgrounding vs. Editor/desktop quit) never double-logs.
    /// MAX SDK handles its own lifecycle internally — no calls needed here.
    /// </summary>
    public class ApplicationLifecycleHandler : MonoBehaviour, IInitializable
    {
        private SaveManager      _save;
        private TimeManager      _time;
        private AnalyticsManager _analytics;
        private bool             _sessionEndLogged;

        [Inject]
        public void Construct(SaveManager save, TimeManager time, AnalyticsManager analytics)
        {
            _save      = save;
            _time      = time;
            _analytics = analytics;
        }

        public void Initialize() => Debug.Log("[LifecycleHandler] Ready.");

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            _save?.Save();
            LogSessionEnd();
        }

        private void OnApplicationFocus(bool focus)
        {
#if UNITY_ANDROID
            if (!focus) _save?.Save();
#endif
        }

        private void OnApplicationQuit()
        {
            _save?.Save();
            LogSessionEnd();
        }

        private void LogSessionEnd()
        {
            if (_sessionEndLogged) return;
            if (_save == null || _time == null || _analytics == null) return;
            _sessionEndLogged = true;

            int levelsThisSession = _save.Data.TotalLevelsCompleted - _time.LevelsCompletedAtSessionStart;
            _analytics.LogEvent("session_end", new Dictionary<string, object>
            {
                { "session_duration_seconds",       _time.SessionSeconds },
                { "last_level_index",               _save.Data.CurrentLevelIndex },
                { "highest_unlocked_level",          _save.Data.HighestUnlockedLevel },
                { "levels_completed_this_session",  levelsThisSession },
            });
        }
    }
}
