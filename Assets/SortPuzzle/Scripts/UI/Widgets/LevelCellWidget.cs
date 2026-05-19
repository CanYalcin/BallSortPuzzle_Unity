using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortPuzzle.UI.Screens
{
    /// <summary>
    /// Represents a single level cell on the World Map.
    /// Shows level number, star rating (0-3), and locked state.
    /// Tapping an unlocked cell fires OnTapped.
    /// </summary>
    public class LevelCellWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Image[]         _stars;       // 3 star images
        [SerializeField] private GameObject      _lockOverlay;
        [SerializeField] private Button          _button;

        public System.Action OnTapped;

        private void Awake()
        {
            if (_button) _button.onClick.AddListener(() => OnTapped?.Invoke());
        }

        /// <summary>Configures the cell display.</summary>
        public void Setup(int worldIndex, int levelIndex, int stars, bool locked)
        {
            if (_levelLabel)  _levelLabel.text = (levelIndex + 1).ToString();
            if (_lockOverlay) _lockOverlay.SetActive(locked);
            if (_button)      _button.interactable = !locked;

            if (_stars != null)
                for (int i = 0; i < _stars.Length; i++)
                    if (_stars[i]) _stars[i].enabled = i < stars;
        }
    }
}
