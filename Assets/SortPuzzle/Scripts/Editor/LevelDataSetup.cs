#if UNITY_EDITOR
using SortPuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace SortPuzzle.Editor
{
    /// <summary>
    /// One-shot editor tool to populate Level_W1_001 with actual ball data.
    /// Run via: SortPuzzle -> Setup Level 1 Data
    /// Safe to run multiple times — just overwrites the same data.
    /// </summary>
    public static class LevelDataSetup
    {
        [MenuItem("SortPuzzle/Setup Level 1 Data")]
        public static void SetupLevel1()
        {
            const string path = "Assets/SortPuzzle/Settings/Levels/World1/Level_W1_001.asset";
            var ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (ld == null)
            {
                Debug.LogError($"[LevelDataSetup] Could not find asset at {path}");
                return;
            }

            ld.TubeCount     = 4;
            ld.EmptyTubeCount = 2;
            ld.TubeCapacity  = 4;
            ld.ColorCount    = 2;
            ld.ParMoves      = 4;
            ld.GoldReward    = 10;

            // Layout:
            // Tube 0: [1,1,2,2]  (bottom=1, top=2) — mixed red+blue
            // Tube 1: [2,2,1,1]  (bottom=2, top=1) — mixed blue+red
            // Tube 2: [0,0,0,0]  — empty
            // Tube 3: [0,0,0,0]  — empty
            // Solved by moving all 1s to tube 2, all 2s to tube 3
            ld.Tubes = new TubeRow[4];
            ld.Tubes[0] = new TubeRow(new int[] { 1, 1, 2, 2 });
            ld.Tubes[1] = new TubeRow(new int[] { 2, 2, 1, 1 });
            ld.Tubes[2] = new TubeRow(new int[] { 0, 0, 0, 0 });
            ld.Tubes[3] = new TubeRow(new int[] { 0, 0, 0, 0 });

            EditorUtility.SetDirty(ld);
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelDataSetup] Level 1 data populated successfully.");
        }

[MenuItem("SortPuzzle/Setup Level 2 (Normal)")]
        public static void SetupLevel2()
        {
            const string path = "Assets/SortPuzzle/Settings/Levels/World1/Level_W1_002.asset";
            var ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (ld == null)
            {
                ld = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(ld, path);
            }
            ld.LevelIndex    = 1;
            ld.WorldIndex    = 0;
            ld.DisplayName   = "Level 2";
            ld.SceneName     = "GameScene";
            ld.TubeCount     = 6;
            ld.EmptyTubeCount = 2;
            ld.TubeCapacity  = 4;
            ld.ColorCount    = 4;
            ld.ParMoves      = 12;
            ld.GoldReward    = 20;
            ld.DifficultyRating = 3;
            ld.SoftCurrencyReward = 20;
            // Layout: 4 colors mixed across 4 tubes, 2 empty
            // Color 1=Red, 2=Blue, 3=Green, 4=Yellow
            ld.Tubes = new TubeRow[6];
            ld.Tubes[0] = new TubeRow(new int[] { 1, 2, 3, 4 });
            ld.Tubes[1] = new TubeRow(new int[] { 4, 3, 2, 1 });
            ld.Tubes[2] = new TubeRow(new int[] { 2, 1, 4, 3 });
            ld.Tubes[3] = new TubeRow(new int[] { 3, 4, 1, 2 });
            ld.Tubes[4] = new TubeRow(new int[] { 0, 0, 0, 0 });
            ld.Tubes[5] = new TubeRow(new int[] { 0, 0, 0, 0 });
            EditorUtility.SetDirty(ld);
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelDataSetup] Level 2 (Normal) populated.");
        }

        [MenuItem("SortPuzzle/Setup Level 3 (Hard)")]
        public static void SetupLevel3()
        {
            const string path = "Assets/SortPuzzle/Settings/Levels/World1/Level_W1_003.asset";
            var ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (ld == null)
            {
                ld = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(ld, path);
            }
            ld.LevelIndex    = 2;
            ld.WorldIndex    = 0;
            ld.DisplayName   = "Level 3";
            ld.SceneName     = "GameScene";
            ld.TubeCount     = 8;
            ld.EmptyTubeCount = 2;
            ld.TubeCapacity  = 4;
            ld.ColorCount    = 6;
            ld.ParMoves      = 22;
            ld.GoldReward    = 40;
            ld.DifficultyRating = 7;
            ld.SoftCurrencyReward = 40;
            // Layout: 6 colors across 6 tubes, 2 empty — tightly packed
            // 1=Red 2=Blue 3=Green 4=Yellow 5=Purple 6=Orange
            ld.Tubes = new TubeRow[8];
            ld.Tubes[0] = new TubeRow(new int[] { 1, 2, 3, 4 });
            ld.Tubes[1] = new TubeRow(new int[] { 5, 6, 1, 2 });
            ld.Tubes[2] = new TubeRow(new int[] { 3, 4, 5, 6 });
            ld.Tubes[3] = new TubeRow(new int[] { 2, 1, 6, 5 });
            ld.Tubes[4] = new TubeRow(new int[] { 4, 3, 2, 1 });
            ld.Tubes[5] = new TubeRow(new int[] { 6, 5, 4, 3 });
            ld.Tubes[6] = new TubeRow(new int[] { 0, 0, 0, 0 });
            ld.Tubes[7] = new TubeRow(new int[] { 0, 0, 0, 0 });
            EditorUtility.SetDirty(ld);
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelDataSetup] Level 3 (Hard) populated.");
        }

    }
}
#endif
