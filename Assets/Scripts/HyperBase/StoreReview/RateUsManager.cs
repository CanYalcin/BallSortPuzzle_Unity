using Cysharp.Threading.Tasks;
using HyperBase.Core;
using HyperBase.Data;
using UnityEngine;
using VContainer;

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
#endif
            // Android: add com.google.play.review via OpenUPM and call
            //   var rm = new Google.Play.Review.ReviewManager();
            //   var req = rm.RequestReviewFlow(); await req;
            //   if(req.Error == Google.Play.Review.ReviewErrorCode.NoError)
            //     await rm.LaunchReviewFlow(req.GetResult());

            await UniTask.CompletedTask;
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
