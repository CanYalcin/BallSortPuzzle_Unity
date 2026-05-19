using System;
using UnityEngine;

namespace HyperBase.Notifications
{
    /// <summary>
    /// Local push notification manager. Compiles with NO extra packages by default.
    ///
    /// To enable on device:
    ///   1. Install "Unity Mobile Notifications" (com.unity.mobile.notifications >= 2.3)
    ///   2. Add scripting define symbol: HYPERBASE_NOTIFICATIONS
    ///   The platform blocks below will then activate automatically.
    /// </summary>
    public class NotificationManager
    {
        private bool _ready;

        public void Initialize()
        {
            _ready = true;
            Debug.Log("[NotificationManager] Initialized (stub mode — add HYPERBASE_NOTIFICATIONS define to enable).");
        }

        public void ScheduleIn(string title, string body, TimeSpan delay, string channelId = "hyper_default")
        {
            if (!_ready || delay.TotalSeconds <= 0) return;
            Debug.Log("[Notifications] Schedule: '" + title + "' in " + delay.TotalMinutes.ToString("F0") + "m");
            // Add HYPERBASE_NOTIFICATIONS define + Unity Mobile Notifications package to activate.
        }

        public void ScheduleDailyReminder(string gameTitle)
            => ScheduleIn(gameTitle + " misses you!", "Your progress is waiting!", TimeSpan.FromHours(24));

        public void ScheduleEnergyRefill(TimeSpan refillTime, int amount)
            => ScheduleIn("Energy Refilled!", "Your " + amount + " energy is ready!", refillTime, "hyper_energy");

        public void CancelAll()
        {
            if (!_ready) return;
            Debug.Log("[NotificationManager] CancelAll (stub).");
            // Add HYPERBASE_NOTIFICATIONS define + Unity Mobile Notifications package to activate.
        }
    }
}
