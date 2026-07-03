using Cysharp.Threading.Tasks;
using HyperBase.Core;
using HyperBase.Data;
using UnityEngine;
using VContainer;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

namespace HyperBase.StoreReview
{
    /// <summary>
    /// Smart one-shot native review prompt.
    /// Conditions: >= 5 levels completed, >= 3 sessions, never prompted before.
    /// Call TryPromptAsync() after positive moments (level complete, win screen).
    /// </summary>
    public class RateUsManager
    {
        private const int MinLevels      = 5;
        private const int MinSessions    = 3;
        private const string PromptedKey = "RateUs_Prompted";

        private readonly SaveManager _save;
        private readonly EventBus    _events;
        private bool _promptedThisSession;

        [Inject]
        public RateUsManager(SaveManager save, EventBus events)
        {
            _save   = save;
            _events = events;
        }

        public bool ShouldPrompt()
        {
            if (_promptedThisSession)                                  return false;
            if (UnityEngine.PlayerPrefs.GetInt(PromptedKey, 0) == 1)  return false;
            if (_save.Data.TotalLevelsCompleted < MinLevels)           return false;
            if (_save.Data.TotalSessionCount    < MinSessions)         return false;
            return true;
        }

        public async UniTask TryPromptAsync()
        {
            if (!ShouldPrompt()) return;
            _promptedThisSession = true;
            UnityEngine.PlayerPrefs.SetInt(PromptedKey, 1);
            UnityEngine.PlayerPrefs.Save();
            Debug.Log("[RateUs] Showing review prompt.");

#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview();
            await UniTask.CompletedTask;
#elif UNITY_ANDROID && !UNITY_EDITOR
            var reviewManager = new ReviewManager();
            var requestOp = reviewManager.RequestReviewFlow();
            await UniTask.WaitUntil(() => requestOp.IsDone);
            if (requestOp.Error != ReviewErrorCode.NoError)
            {
                Debug.LogWarning($"[RateUs] RequestReviewFlow failed: {requestOp.Error}");
                return;
            }
            var launchOp = reviewManager.LaunchReviewFlow(requestOp.GetResult());
            await UniTask.WaitUntil(() => launchOp.IsDone);
            if (launchOp.Error != ReviewErrorCode.NoError)
                Debug.LogWarning($"[RateUs] LaunchReviewFlow failed: {launchOp.Error}");
            // Google's API deliberately never tells us whether the dialog was actually
            // shown or the user rated — same as Apple's. We don't try to infer it.
#else
            await UniTask.CompletedTask;
#endif
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ResetForTesting()
        {
            UnityEngine.PlayerPrefs.DeleteKey(PromptedKey);
            _promptedThisSession = false;
            Debug.Log("[RateUs] Reset for testing.");
        }
    }
}
