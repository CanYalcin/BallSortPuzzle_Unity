using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HyperBase.ObjectPool;
using UnityEngine;
using VContainer;

namespace HyperBase.VFX
{
    public enum VFXType
    {
        None, CoinCollect, CoinExplosion, LevelComplete, LevelFail,
        ButtonTap, StarBurst, Confetti, HitImpact, WinCelebration
    }

    /// <summary>
    /// Pooled particle effect manager.
    /// Usage: _vfx.Play(VFXType.CoinCollect, position);
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

        public void Play(VFXType type, Vector3 pos, Quaternion rot = default, float duration = 0f)
        {
            if (!_map.TryGetValue(type, out var prefab)) return;
            var obj   = _pool.Rent(prefab, pos, rot);
            float dur = duration > 0f ? duration : GetDuration(obj);
            if (dur <= 0f) dur = 2f;
            ReturnAfter(obj, dur).Forget();
        }

        public void PlayAtScreen(VFXType type, Vector2 screenPt, float z = 0f)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var world = cam.ScreenToWorldPoint(new Vector3(screenPt.x, screenPt.y, cam.nearClipPlane));
            world.z   = z;
            Play(type, world);
        }

        private async UniTaskVoid ReturnAfter(GameObject obj, float sec)
        {
            await UniTask.WaitForSeconds(sec, ignoreTimeScale: true);
            if (obj != null) _pool.Return(obj);
        }

        private static float GetDuration(GameObject obj)
        {
            var ps = obj.GetComponentInChildren<ParticleSystem>();
            if (ps == null) return 0f;
            var m = ps.main;
            return m.duration + m.startLifetime.constantMax;
        }
    }
}
