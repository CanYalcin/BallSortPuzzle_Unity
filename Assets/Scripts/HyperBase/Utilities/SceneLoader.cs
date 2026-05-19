using Cysharp.Threading.Tasks;
using HyperBase.UI;
using HyperBase.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace HyperBase.Utilities
{
    /// <summary>
    /// Async scene loading with LoadingScreen progress and minimum display time.
    /// </summary>
    public class SceneLoader
    {
        private const float MinDisplaySec = 0.8f;
        private readonly UIManager _ui;
        private bool _loading;

        [Inject]
        public SceneLoader(UIManager ui) => _ui = ui;

        public async UniTaskVoid LoadSceneAsync(string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (_loading) { Debug.LogWarning($"[SceneLoader] Already loading. Ignoring '{sceneName}'."); return; }
            _loading = true;

            var screen = _ui.GetScreen<LoadingScreen>();
            if (screen != null)
            {
                screen.SetProgress(0f);
                await _ui.ShowScreenAsync<LoadingScreen>(addToHistory: false);
            }

            float start = Time.unscaledTime;
            var op      = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null) { Debug.LogError($"[SceneLoader] Scene not found: '{sceneName}'"); _loading = false; return; }
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                screen?.SetProgress(op.progress / 0.9f * 0.9f);
                await UniTask.Yield();
            }

            float wait = MinDisplaySec - (Time.unscaledTime - start);
            if (wait > 0f) await UniTask.WaitForSeconds(wait, ignoreTimeScale: true);

            screen?.SetProgress(1f);
            await UniTask.WaitForSeconds(0.1f, ignoreTimeScale: true);

            op.allowSceneActivation = true;
            await op;
            _loading = false;
            Debug.Log($"[SceneLoader] Loaded '{sceneName}'.");
        }

        public void ReloadCurrent()
            => LoadSceneAsync(SceneManager.GetActiveScene().name).Forget();
    }
}
