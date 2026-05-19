using HyperBase.Data;
using UnityEngine;
using VContainer;

namespace HyperBase.Haptics
{
    /// <summary>
    /// Cross-platform haptic feedback.
    /// iOS: UIImpactFeedbackGenerator via UnityEngine.iOS.Haptics.
    /// Android: android.os.Vibrator via AndroidJavaObject.
    /// Respects HapticsEnabled in PlayerData.
    /// </summary>
    public class HapticsManager
    {
        private readonly SaveManager _saveManager;

        public enum HapticType { Light, Medium, Heavy, Success, Warning, Error, Selection }

        [Inject]
        public HapticsManager(SaveManager saveManager) => _saveManager = saveManager;

        public void Play(HapticType type)
        {
            if (!_saveManager.Data.HapticsEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            switch (type)
            {
                case HapticType.Light:
                    UnityEngine.iOS.Haptics.PlayImpactFeedback(UnityEngine.iOS.ImpactFeedbackStyle.Light);
                    break;
                case HapticType.Medium:
                    UnityEngine.iOS.Haptics.PlayImpactFeedback(UnityEngine.iOS.ImpactFeedbackStyle.Medium);
                    break;
                case HapticType.Heavy:
                    UnityEngine.iOS.Haptics.PlayImpactFeedback(UnityEngine.iOS.ImpactFeedbackStyle.Heavy);
                    break;
                case HapticType.Success:
                    UnityEngine.iOS.Haptics.PlayNotificationFeedback(UnityEngine.iOS.NotificationFeedbackType.Success);
                    break;
                case HapticType.Warning:
                    UnityEngine.iOS.Haptics.PlayNotificationFeedback(UnityEngine.iOS.NotificationFeedbackType.Warning);
                    break;
                case HapticType.Error:
                    UnityEngine.iOS.Haptics.PlayNotificationFeedback(UnityEngine.iOS.NotificationFeedbackType.Failure);
                    break;
                case HapticType.Selection:
                    UnityEngine.iOS.Haptics.PlaySelectionFeedback();
                    break;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            long ms = type switch
            {
                HapticType.Light     => 10L,
                HapticType.Medium    => 20L,
                HapticType.Heavy     => 40L,
                HapticType.Success   => 30L,
                HapticType.Warning   => 50L,
                HapticType.Error     => 80L,
                HapticType.Selection => 5L,
                _                    => 20L
            };
            try
            {
                using var player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator?.Call("vibrate", ms);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsManager] Vibration failed: {e.Message}");
            }
#endif
        }

        public void LightImpact()  => Play(HapticType.Light);
        public void MediumImpact() => Play(HapticType.Medium);
        public void HeavyImpact()  => Play(HapticType.Heavy);
        public void Success()      => Play(HapticType.Success);
        public void Warning()      => Play(HapticType.Warning);
        public void Error()        => Play(HapticType.Error);
        public void Selection()    => Play(HapticType.Selection);

        public void SetEnabled(bool enabled) => _saveManager.Data.HapticsEnabled = enabled;
    }
}
