using System.Collections.Generic;
using HyperBase.ObjectPool;
using UnityEngine;
using VContainer;

namespace HyperBase.VFX
{
    public enum VFXType
    {
        None,
        PourSplash,       // balls landing in destination tube
        TubeComplete,     // tube fully sorted — glow/burst
        BoostUsed,        // sparkle on affected tube when a boost is activated
        LevelComplete,    // triggered on puzzle win
        LevelFail,        // triggered on fail screen
        Confetti,         // full-screen burst on win
        WinCelebration,   // extended win effect
        Undo              // brief rewind flash when a move is undone
    }

    /// <summary>
    /// Pooled UI-effect manager. This project's Canvas is Screen Space - Overlay, which
    /// always draws on top of camera-rendered content — real world-space ParticleSystems
    /// can never be visible above it. So effects here are UI prefabs (RectTransform root +
    /// UIBurstEffect driving child Image pieces), spawned as children of a caller-supplied
    /// UI layer and positioned in that layer's local/world space directly — never through a
    /// camera.
    ///
    /// Usage: _vfx.Play(VFXType.PourSplash, vfxLayer, worldPos);
    /// where worldPos is the same "RectTransform world position" convention already used by
    /// TubeView.GetSlotScreenPos() elsewhere in this codebase (Screen Space Overlay canvases
    /// have world-space coordinates that line up with screen pixels).
    /// </summary>
    public class VFXManager
    {
        private readonly VFXConfig         _config;
        private readonly ObjectPoolManager _pool;
        private readonly Dictionary<VFXType, GameObject> _map = new();

        [Inject]
        public VFXManager(VFXConfig config, ObjectPoolManager pool)
        {
            _config = config;
            _pool   = pool;
        }

        public void Initialize()
        {
            if (_config.Effects == null) return;
            foreach (var e in _config.Effects)
            {
                if (e.Prefab == null) continue;
                _map[e.Type] = e.Prefab;
                _pool.Prewarm(e.Prefab, e.PrewarmCount);
            }
            Debug.Log($"[VFXManager] {_map.Count} effect types ready.");
        }

        /// <summary>
        /// Plays a UI burst effect, parented under <paramref name="parent"/> (typically a
        /// full-screen RectTransform layer that renders above gameplay UI), positioned at
        /// <paramref name="worldPos"/>. Auto-returns to the pool when the effect's own
        /// animation finishes — no external timer needed.
        /// </summary>
        public void Play(VFXType type, RectTransform parent, Vector2 worldPos)
        {
            if (!_map.TryGetValue(type, out var prefab)) return;
            var obj = _pool.Rent(prefab);
            var rt  = (RectTransform)obj.transform;
            rt.SetParent(parent, worldPositionStays: false);
            rt.position = new Vector3(worldPos.x, worldPos.y, rt.position.z);

            var effect = obj.GetComponent<UIBurstEffect>();
            if (effect == null)
            {
                Debug.LogWarning($"[VFXManager] Prefab for {type} has no UIBurstEffect — returning immediately.");
                _pool.Return(obj);
                return;
            }
            effect.Play(() => _pool.Return(obj));
        }
    }
}
