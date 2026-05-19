#if UNITY_EDITOR
using SortPuzzle.Data;
using SortPuzzle.Generation;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortPuzzle.DevTools
{
    /// <summary>
    /// SortPuzzle -> Generate Daily Levels (Hard)
    ///
    /// Generates 30 daily challenge levels at hard difficulty and wires them
    /// into DailyLevelDatabase. Skips slots that already have a valid asset.
    ///
    /// Config (hard daily):
    ///   difficulty = 8  -> 7 colors, 2 empty tubes, 9 total tubes, minPar ~18
    ///   maxAttempts = 200 per level (high par threshold needs more attempts)
    /// </summary>
    public static class GenerateDailyLevels
    {
        private const string DailyFolder    = "Assets/SortPuzzle/Settings/DailyLevels";
        private const string DatabasePath   = "Assets/SortPuzzle/Settings/DailyLevelDatabase.asset";
        private const int    Difficulty     = 8;
        private const int    MaxAttempts    = 200;

        [MenuItem("SortPuzzle/Generate Daily Levels (Hard)")]
        public static void GenerateAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<DailyLevelDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogError($"[GenerateDailyLevels] DailyLevelDatabase not found at: {DatabasePath}");
                return;
            }

            if (!Directory.Exists(DailyFolder))
                Directory.CreateDirectory(DailyFolder);

            // Ensure array is exactly 30
            if (db.DailyLevels == null || db.DailyLevels.Length != 30)
                db.DailyLevels = new LevelData[30];

            int created  = 0;
            int skipped  = 0;
            int failed   = 0;

            for (int i = 0; i < 30; i++)
            {
                string assetName = $"Daily_{(i + 1):D3}";
                string assetPath = $"{DailyFolder}/{assetName}.asset";

                // Slot already valid — skip
                if (db.DailyLevels[i] != null)
                {
                    skipped++;
                    continue;
                }

                // Relink if asset exists on disk but reference is missing
                if (File.Exists(assetPath))
                {
                    var existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                    if (existing != null)
                    {
                        db.DailyLevels[i] = existing;
                        Debug.Log($"[GenerateDailyLevels] Relinked {assetName}.");
                        skipped++;
                        continue;
                    }
                }

                // Generate new level — use worldIndex=-1 to mark as daily
                LevelData ld = LevelGenerator.Generate(
                    difficulty:   Difficulty,
                    worldIndex:   -1,
                    levelIndex:   i,
                    maxAttempts:  MaxAttempts);

                if (ld == null)
                {
                    Debug.LogWarning($"[GenerateDailyLevels] Failed to generate {assetName} after {MaxAttempts} attempts — slot left empty.");
                    failed++;
                    continue;
                }

                ld.DisplayName = $"Daily Challenge — Day {i + 1}";
                AssetDatabase.CreateAsset(ld, assetPath);
                db.DailyLevels[i] = ld;
                created++;
                Debug.Log($"[GenerateDailyLevels] {assetName} — tubes {ld.TubeCount}, colors {ld.ColorCount}, par {ld.ParMoves}.");
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GenerateDailyLevels] Done. Created: {created}, Skipped: {skipped}, Failed: {failed}. " +
                      $"Total filled: {CountFilled(db.DailyLevels)}/30.");
        }

        [MenuItem("SortPuzzle/Dev/Clear Daily Levels")]
        public static void ClearAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<DailyLevelDatabase>(DatabasePath);
            if (db == null) return;

            int deleted = 0;
            if (db.DailyLevels != null)
            {
                for (int i = 0; i < db.DailyLevels.Length; i++)
                {
                    if (db.DailyLevels[i] == null) continue;
                    string path = AssetDatabase.GetAssetPath(db.DailyLevels[i]);
                    db.DailyLevels[i] = null;
                    if (!string.IsNullOrEmpty(path)) { AssetDatabase.DeleteAsset(path); deleted++; }
                }
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GenerateDailyLevels] Cleared {deleted} daily level assets.");
        }

        private static int CountFilled(LevelData[] arr)
        {
            int n = 0;
            if (arr == null) return 0;
            foreach (var l in arr) if (l != null) n++;
            return n;
        }
    }
}
#endif
