using HyperBase.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperBase.Core
{
    /// <summary>
    /// Handles Unity app lifecycle events.
    /// Saves on pause/focus-loss and on quit.
    /// MAX SDK handles its own lifecycle internally — no calls needed here.
    /// </summary>
    public class ApplicationLifecycleHandler : MonoBehaviour, IInitializable
    {
        private SaveManager _save;

        [Inject]
        public void Construct(SaveManager save) => _save = save;

        public void Initialize() => Debug.Log("[LifecycleHandler] Ready.");

        private void OnApplicationPause(bool paused)
        {
            if (paused) _save?.Save();
        }

        private void OnApplicationFocus(bool focus)
        {
#if UNITY_ANDROID
            if (!focus) _save?.Save();
#endif
        }

        private void OnApplicationQuit() => _save?.Save();
    }
}
