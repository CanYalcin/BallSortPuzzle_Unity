using UnityEngine;

namespace HyperBase.Gameplay
{
    /// <summary>
    /// Ordered database of all level configs.
    /// Loops back to start when all levels are completed — standard hypercasual pattern.
    /// Create via Assets > Create > HyperBase > Level Database
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "HyperBase/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        public LevelConfig[] Levels;

        public int Count => Levels?.Length ?? 0;

        public LevelConfig Get(int index)
        {
            if (Levels == null || Levels.Length == 0) { Debug.LogError("[LevelDatabase] No levels configured!"); return null; }
            return Levels[index % Levels.Length];
        }

        public bool IsValid(int index) => Levels != null && index >= 0 && index < Levels.Length;
    }
}
