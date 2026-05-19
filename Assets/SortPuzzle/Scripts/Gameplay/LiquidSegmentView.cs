using UnityEngine;
using UnityEngine.UI;

namespace SortPuzzle.Gameplay
{
    /// <summary>
    /// One colored ball. Child of TubeView's RectTransform.
    /// Position set via anchoredPosition (local to parent tube).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class LiquidSegmentView : MonoBehaviour
    {
        private Image         _image;
        private RectTransform _rect;

        public int ColorId { get; private set; }

        // ── Property accessors ────────────────────────────────────────────────
        /// <summary>Current local Y (anchoredPosition.y). Settable from coroutines (avoids extra method call sites).</summary>
        public float AnchoredY
        {
            get => _rect.anchoredPosition.y;
            set => _rect.anchoredPosition = new Vector2(0f, value);
        }

        /// <summary>Current screen-space position (used by PrepareForPour to get lifted pos).</summary>
        public Vector2 ScreenPosition => (Vector2)_rect.position;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect  = GetComponent<RectTransform>();
        }

        // ── API ───────────────────────────────────────────────────────────────

        public void SetColor(int colorId, Color color)
        {
            ColorId      = colorId;
            _image.color = color;
        }

        /// <summary>Sets local position in parent tube via anchoredPosition.</summary>
        public void SetAnchoredPosition(Vector2 pos) => _rect.anchoredPosition = pos;

        /// <summary>Shows or hides the ball. Called from TubeView.Refresh only.</summary>
        public void SetVisible(bool visible) => _image.enabled = visible;

        /// <summary>
        /// Hides the ball immediately. Called from TubeView.PrepareForPour only,
        /// to prevent double-ball during pour animation.
        /// </summary>
        public void HideForPour() => _image.enabled = false;

        /// <summary>Sets screen-space position directly (used by PourAnimator for temp balls).</summary>
        public void SetScreenPosition(Vector2 screenPos) => _rect.position = screenPos;
    }
}
