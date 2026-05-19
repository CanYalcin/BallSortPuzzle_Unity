using HyperBase.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortPuzzle.UI.Screens
{
    /// <summary>
    /// MonoBehaviour for one world panel in the World Map screen.
    /// Kept in its own file so Unity can resolve the class by filename.
    /// </summary>
    public class WorldPanelView : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI  titleLabel;
        [SerializeField] public TextMeshProUGUI  progressLabel;
        [SerializeField] public Image            lockIcon;
        [SerializeField] public Transform        cellContainer;
        [HideInInspector] public LevelCellWidget cellPrefab;

        public System.Action<int, int> OnLevelSelected;

        public void Refresh(int worldIndex, bool unlocked, PlayerData data)
        {
            if (lockIcon) lockIcon.gameObject.SetActive(!unlocked);

            if (titleLabel) titleLabel.text = unlocked
                ? $"World {worldIndex + 1}"
                : $"World {worldIndex + 1}  🔒";

            // Clear existing cells
            if (cellContainer != null)
                for (int i = cellContainer.childCount - 1; i >= 0; i--)
                    Destroy(cellContainer.GetChild(i).gameObject);

            if (!unlocked)
            {
                if (progressLabel) progressLabel.text = $"Complete World {worldIndex} to unlock";
                return;
            }

            int done = 0;
            for (int i = 0; i < 30; i++)
                if (data.GetLevelStars(worldIndex, i) > 0) done++;
            if (progressLabel) progressLabel.text = $"{done} / 30";

            if (cellPrefab == null || cellContainer == null) return;
            for (int i = 0; i < 30; i++)
            {
                var cell  = Instantiate(cellPrefab, cellContainer);
                int stars = data.GetLevelStars(worldIndex, i);
                bool lk   = i > 0 && data.GetLevelStars(worldIndex, i - 1) == 0;
                cell.Setup(worldIndex, i, stars, lk);
                int li = i;
                cell.OnTapped = () => { if (!lk) OnLevelSelected?.Invoke(worldIndex, li); };
            }
        }
    }
}
