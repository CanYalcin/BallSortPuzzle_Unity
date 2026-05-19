using SortPuzzle.Data;
using SortPuzzle.Economy;
using SortPuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SortPuzzle.UI.Widgets
{
    /// <summary>
    /// Two boost buttons: Undo and Extra Empty Tube.
    /// Badge shows current count. AdIcon shows when count == 0.
    /// Shuffle boost removed.
    /// </summary>
    public class BoostBarWidget : MonoBehaviour
    {
        [Header("Undo")]
        [SerializeField] private Button          _undoBtn;
        [SerializeField] private TextMeshProUGUI _undoBadge;
        [SerializeField] private Image           _undoAdIcon;

        [Header("Extra Tube")]
        [SerializeField] private Button          _extraTubeBtn;
        [SerializeField] private TextMeshProUGUI _extraTubeBadge;
        [SerializeField] private Image           _extraTubeAdIcon;

        private BoostManager    _boosts;
        private LevelController _controller;

        [Inject]
        public void Construct(BoostManager boosts) => _boosts = boosts;

        private void Awake()
        {
            if (_undoBtn)      _undoBtn.onClick.AddListener(() =>      { if (_controller) _controller.OnUndoPressed(); });
            if (_extraTubeBtn) _extraTubeBtn.onClick.AddListener(() => { if (_controller) _controller.OnExtraEmptyTubePressed(); });
        }

        private void Start()
        {
            if (_boosts == null) return;
            _boosts.OnBoostChanged += Refresh;
            _boosts.ForceNotify();
        }

        private void OnDestroy()
        {
            if (_boosts != null) _boosts.OnBoostChanged -= Refresh;
        }

        public void SetController(LevelController controller) => _controller = controller;

        private void Refresh()
        {
            if (_boosts == null) return;
            Apply(_undoBadge,      _undoAdIcon,      _boosts.GetCount(BoostType.Undo));
            Apply(_extraTubeBadge, _extraTubeAdIcon, _boosts.GetCount(BoostType.ExtraEmptyTube));
        }

        private static void Apply(TextMeshProUGUI badge, Image adIcon, int count)
        {
            if (badge)  badge.text = count.ToString();
            if (adIcon) adIcon.gameObject.SetActive(count == 0);
        }
    }
}
