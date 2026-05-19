#if UNITY_EDITOR
using HyperBase.Gameplay;
using SortPuzzle.Data;
using SortPuzzle.Generation;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortPuzzle.DevTools
{
    /// <summary>
    /// SortPuzzle → Generate Levels 4-30 (World 1)
    /// Strips null (deleted) entries from LevelDatabase, then generates
    /// and appends missing levels up to 30.
    ///
    /// Difficulty curve:
    ///   Levels  1- 3 : hand-crafted (kept as-is)
    ///   Levels  4-10 : difficulty 3
    ///   Levels 11-20 : difficulty 5
    ///   Levels 21-30 : difficulty 7
    /// </summary>
    public static class GenerateLevels
    {
        private const string LevelFolder  = "Assets/SortPuzzle/Settings/Levels/World1";
        private const string DatabasePath = "Assets/Settings/Levels/LevelDatabase.asset";

        [MenuItem("SortPuzzle/GenerateLevels")]
        public static void GenerateWorld1Levels()
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogError($"[GenerateLevels] LevelDatabase not found at: {DatabasePath}");
                return;
            }

            if (!Directory.Exists(LevelFolder))
                Directory.CreateDirectory(LevelFolder);

            // Build a fixed-size array[30], preserving valid entries and marking gaps as null
            var levels = new LevelConfig[30];
            var raw    = db.Levels ?? new LevelConfig[0];
            for (int i = 0; i < Mathf.Min(raw.Length, 30); i++)
                levels[i] = raw[i]; // may be null if asset was deleted

            int created = 0;

            for (int i = 0; i < 30; i++)
            {
                string assetName = $"Level_W1_{(i + 1):D3}";
                string assetPath = $"{LevelFolder}/{assetName}.asset";

                // Already valid in database
                if (levels[i] != null) continue;

                // Asset exists on disk but lost its database reference
                if (File.Exists(assetPath))
                {
                    var existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                    if (existing != null) { levels[i] = existing; Debug.Log($"[GenerateLevels] Relinked {assetName}."); continue; }
                }

                // Generate new level for this slot
                int diff = DifficultyFor(i);
                LevelData ld = LevelGenerator.Generate(difficulty: diff, worldIndex: 0, levelIndex: i);

                if (ld == null)
                {
                    Debug.LogWarning($"[GenerateLevels] Failed to generate {assetName} (diff {diff}) — slot left empty.");
                    continue;
                }

                AssetDatabase.CreateAsset(ld, assetPath);
                levels[i] = ld;
                created++;
                Debug.Log($"[GenerateLevels] Created {assetName} — diff {diff}, tubes {ld.TubeCount}, par {ld.ParMoves}.");
            }

            db.Levels = levels;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GenerateLevels] Done. Created {created} levels. Database total: {db.Levels.Length}.");
        }

        private static int DifficultyFor(int index)
        {
            if (index < 10) return 3;
            if (index < 20) return 5;
            return 7;
        }
    }
}
#endif
