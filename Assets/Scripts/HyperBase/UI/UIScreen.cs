using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HyperBase.UI
{
    public enum ScreenTransition { None, Fade, Scale }

    /// <summary>
    /// Base class for all UI screens. Handles show/hide async transitions via CanvasGroup.
    /// Override HandleLifecycle(evt) in subclasses for per-screen logic.
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        public enum LifecycleEvent { BeforeShow, AfterShow, BeforeHide, AfterHide }

        [Header("Transitions")]
        public ScreenTransition ShowTransition    = ScreenTransition.Fade;
        public ScreenTransition HideTransition    = ScreenTransition.Fade;
        [Range(0.05f, 1f)]
        public float            TransitionDuration = 0.2f;

        protected CanvasGroup CG { get; private set; }
        public    bool        IsVisible { get; private set; }

        protected virtual void Awake()
        {
            CG                = GetComponent<CanvasGroup>();
            CG.alpha          = 0f;
            CG.interactable   = false;
            CG.blocksRaycasts = false;
        }

        /// <summary>Override to react to screen lifecycle events.</summary>
        protected virtual UniTask HandleLifecycle(LifecycleEvent evt) => UniTask.CompletedTask;

        /// <summary>Called on device back button press while this screen is active.</summary>
        public virtual void OnBackPressed() { }

        public async UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            IsVisible         = true;
            CG.interactable   = false;
            CG.blocksRaycasts = false;

            await HandleLifecycle(LifecycleEvent.BeforeShow);
            await AnimateAsync(ShowTransition, 0f, 1f);

            CG.interactable   = true;
            CG.blocksRaycasts = true;
            await HandleLifecycle(LifecycleEvent.AfterShow);
        }

        public async UniTask HideAsync()
        {
            CG.interactable   = false;
            CG.blocksRaycasts = false;

            await HandleLifecycle(LifecycleEvent.BeforeHide);
            await AnimateAsync(HideTransition, 1f, 0f);

            IsVisible = false;
            gameObject.SetActive(false);
            await HandleLifecycle(LifecycleEvent.AfterHide);
        }

        private async UniTask AnimateAsync(ScreenTransition type, float from, float to)
        {
            if (type == ScreenTransition.None) { CG.alpha = to; return; }

            if (type == ScreenTransition.Scale)
            {
                float sf = from < to ? 0.9f : 1f;
                float st = from < to ? 1f   : 0.9f;
                transform.localScale = Vector3.one * sf;
                float e = 0f;
                while (e < TransitionDuration)
                {
                    e += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(e / TransitionDuration);
                    CG.alpha             = Mathf.Lerp(from, to, p);
                    transform.localScale = Vector3.one * Mathf.Lerp(sf, st, p);
                    await UniTask.Yield();
                }
                CG.alpha             = to;
                transform.localScale = Vector3.one * st;
                return;
            }

            CG.alpha = from;
            float el = 0f;
            while (el < TransitionDuration)
            {
                el      += Time.unscaledDeltaTime;
                CG.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(el / TransitionDuration));
                await UniTask.Yield();
            }
            CG.alpha = to;
        }
    }
}
