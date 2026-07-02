using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#elif UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace HyperBase.Notifications
{
    /// <summary>
    /// Generic local-notification service. Schedule or cancel a notification by an
    /// arbitrary string id — this class knows nothing about daily challenges, streaks,
    /// or any other feature. Callers (DailyManager today; a future promo/discount
    /// system tomorrow) own their own ids, titles, and copy.
    ///
    /// Editor and unsupported platforms: no-ops (logged only, nothing scheduled).
    /// Android / iOS device builds: real local notifications via
    /// com.unity.mobile.notifications.
    /// </summary>
    public class NotificationManager
    {
        private const string ChannelId          = "hyper_default";
        private const string ChannelName        = "Reminders";
        private const string ChannelDescription = "Daily puzzle reminders and streak alerts.";

        private bool _ready;

        /// <summary>Sets up the platform notification channel and requests permission. Call once at app start, before any Schedule/Cancel calls.</summary>
        public void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.Initialize();
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id          = ChannelId,
                Name        = ChannelName,
                Description = ChannelDescription,
                Importance  = Importance.Default,
            });

            // Android 13+ (API 33) requires this to be requested at runtime; on older
            // versions the permission is implicitly granted and this is a harmless no-op.
            const string postNotifications = "android.permission.POST_NOTIFICATIONS";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(postNotifications))
                UnityEngine.Android.Permission.RequestUserPermission(postNotifications);
#elif UNITY_IOS && !UNITY_EDITOR
            RequestIOSAuthorizationAsync().Forget();
#endif
            _ready = true;
            Debug.Log("[NotificationManager] Initialized.");
        }

#if UNITY_IOS && !UNITY_EDITOR
        private async UniTaskVoid RequestIOSAuthorizationAsync()
        {
            using var request = new AuthorizationRequest(
                AuthorizationOption.Alert | AuthorizationOption.Sound | AuthorizationOption.Badge,
                registerForRemoteNotifications: false);
            await UniTask.WaitUntil(() => request.IsFinished);
            Debug.Log($"[NotificationManager] iOS authorization granted: {request.Granted}");
        }
#endif

        /// <summary>
        /// Schedules a one-time local notification to fire at an absolute local date/time.
        /// Calling this again with the same id replaces the previously scheduled one — both
        /// platforms have replace-on-duplicate-id semantics natively, this just makes that
        /// explicit. Past times are ignored (logged, not scheduled).
        /// </summary>
        public void ScheduleAt(string id, string title, string body, DateTime fireTimeLocal)
        {
            if (!_ready)
            {
                Debug.LogWarning($"[NotificationManager] ScheduleAt('{id}') called before Initialize() — ignored.");
                return;
            }
            if (fireTimeLocal <= DateTime.Now)
            {
                Debug.LogWarning($"[NotificationManager] ScheduleAt('{id}') target time {fireTimeLocal:yyyy-MM-dd HH:mm} is in the past — skipped.");
                return;
            }

            Cancel(id);

#if UNITY_ANDROID && !UNITY_EDITOR
            var notification = new AndroidNotification
            {
                Title    = title,
                Text     = body,
                FireTime = fireTimeLocal,
            };
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, StableAndroidId(id));
#elif UNITY_IOS && !UNITY_EDITOR
            var notification = new iOSNotification
            {
                Identifier       = id,
                Title            = title,
                Body             = body,
                ShowInForeground = false,
                Trigger          = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = fireTimeLocal - DateTime.Now,
                    Repeats      = false,
                },
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#endif
            Debug.Log($"[NotificationManager] Scheduled '{id}' for {fireTimeLocal:yyyy-MM-dd HH:mm} local.");
        }

        /// <summary>Convenience overload — schedules relative to now instead of an absolute time.</summary>
        public void ScheduleIn(string id, string title, string body, TimeSpan delay)
            => ScheduleAt(id, title, body, DateTime.Now + delay);

        /// <summary>Cancels a single pending notification by id. Safe to call for an id that isn't scheduled.</summary>
        public void Cancel(string id)
        {
            if (!_ready) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelNotification(StableAndroidId(id));
#elif UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveScheduledNotification(id);
#endif
        }

        /// <summary>Cancels every pending notification scheduled by this app, regardless of id.</summary>
        public void CancelAll()
        {
            if (!_ready) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
            Debug.Log("[NotificationManager] CancelAll.");
        }

        /// <summary>
        /// Android needs a stable int id; iOS and this class's own Cancel(string) work off
        /// the caller's string id directly. This hash lets any caller invent a new string id
        /// on the fly (no central registry to update) while still getting a deterministic,
        /// collision-resistant-enough int for Android's API.
        /// </summary>
        private static int StableAndroidId(string id)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in id) hash = hash * 31 + c;
                return (hash & 0x7FFFFFFF) % 100000 + 1000;
            }
        }
    }
}
