using UnityEngine;

namespace HyperBase.UI
{
    /// <summary>
    /// Adjusts a RectTransform to respect device safe area (notch, home indicator).
    /// Attach to the root panel of any screen that needs safe-area padding.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafe;
        private Vector2Int _lastSize;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void Update()
        {
            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == _lastSafe && size == _lastSize) return;
            _lastSafe = safe;
            _lastSize = size;
            if (size.x == 0 || size.y == 0) return;
            var min = safe.position;
            var max = safe.position + safe.size;
            min.x /= size.x; min.y /= size.y;
            max.x /= size.x; max.y /= size.y;
            _rt.anchorMin = min;
            _rt.anchorMax = max;
        }
    }
}
