#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using HyperBase.Gameplay;
using SortPuzzle.Data;
using SortPuzzle.Generation;
using UnityEditor;
using UnityEngine;

namespace SortPuzzle.Editor
{
    /// <summary>
    /// Parameterized batch level generator. Replaces the old hardcoded 30-level campaign
    /// curve (fixed difficulty 3/5/7 per range) — generates Count levels at a single
    /// Difficulty instead, as loose candidate assets in
    /// Assets/SortPuzzle/Settings/Levels/NewLevels/. Run it multiple times at different
    /// difficulties to build up a mixed-difficulty pool; order/curation happens afterward
    /// by hand, via the staging TestLevelDatabase / TestDailyLevelDatabase assets in that
    /// same folder (playable via BootstrapInstaller's test-database toggle).
    ///
    /// Campaign target: newly generated candidates are auto-appended to
    /// TestLevelDatabase.Levels (it's an open, growable list — order doesn't matter yet).
    /// Daily target: candidates are NOT auto-assigned — TestDailyLevelDatabase.DailyLevels
    /// is a fixed 30-day-slot array, and which specific day a candidate belongs to is a
    /// curatorial decision only you can make, so drag each one into the day slot you want
    /// yourself.
    /// </summary>
    public class GenerateLevels : EditorWindow
    {
        private enum Target { Campaign, Daily }

        private int    _count       = 10;
        private int    _difficulty  = 5;
        private int    _capacity    = 4;
        private int    _emptyTubes  = 2;
        private int    _maxAttempts = 100;
        private Target _target      = Target.Campaign;

        private const string OutputFolder      = "Assets/SortPuzzle/Settings/Levels/NewLevels";
        private const string TestLevelDbPath   = OutputFolder + "/TestLevelDatabase.asset";

        [MenuItem("SortPuzzle/Generate Levels")]
        public static void Open()
        {
            var w = GetWindow<GenerateLevels>("Generate Levels");
            w.minSize = new Vector2(360, 260);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _target      = (Target)EditorGUILayout.EnumPopup("Target",      _target);
            _count       = EditorGUILayout.IntSlider("Count",               _count, 1, 50);
            _difficulty  = EditorGUILayout.IntSlider("Difficulty",          _difficulty, 1, 10);
            _capacity    = EditorGUILayout.IntSlider("Capacity",            _capacity, 3, 6);
            _emptyTubes  = EditorGUILayout.IntSlider("Empty Tubes",         _emptyTubes, 0, 4);
            _maxAttempts = EditorGUILayout.IntField ("Max Attempts/level",  _maxAttempts);

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                _target == Target.Campaign
                    ? "Candidates are appended to TestLevelDatabase automatically."
                    : "Daily levels are fixed 30-day slots — candidates are saved loose only. " +
                      "Drag each one into the day slot you want inside TestDailyLevelDatabase yourself.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
                Run();
        }

        private void Run()
        {
            Directory.CreateDirectory(OutputFolder);

            int startIndex = FindNextIndex();
            int created = 0, failed = 0;
            var made = new List<LevelData>();

            for (int i = 0; i < _count; i++)
            {
                int slot = startIndex + i;
                var ld = LevelGenerator.Generate(difficulty: _difficulty, levelIndex: slot,
                                                  capacity: _capacity, maxAttempts: _maxAttempts,
                                                  emptyTubes: _emptyTubes);
                if (ld == null)
                {
                    Debug.LogWarning($"[GenerateLevels] Candidate {slot} at difficulty {_difficulty} failed to generate — skipped.");
                    failed++;
                    continue;
                }

                ld.DisplayName = $"Candidate D{_difficulty} #{slot + 1}";
                ld.name        = $"Candidate_D{_difficulty}_{slot:D3}";
                string path = $"{OutputFolder}/{ld.name}.asset";
                AssetDatabase.CreateAsset(ld, path);
                made.Add(ld);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_target == Target.Campaign && made.Count > 0)
                AppendToTestLevelDatabase(made);

            if (failed > 0)
                Debug.LogWarning($"[GenerateLevels] Done WITH GAPS. Created {created}, Failed {failed} " +
                                  $"(difficulty {_difficulty} occasionally can't find a qualifying board within " +
                                  $"the attempt budget — this is expected at higher difficulties, just re-run for more).");
            else
                Debug.Log($"[GenerateLevels] Done. Created {created} candidate(s) at difficulty {_difficulty}.");
        }

        /// <summary>
        /// Scans existing Candidate_D{difficulty}_NNN assets for this difficulty and starts
        /// numbering after the highest one found, so repeat runs never overwrite past batches.
        /// </summary>
        private int FindNextIndex()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { OutputFolder });
            int max = -1;
            string prefix = $"Candidate_D{_difficulty}_";
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.StartsWith(prefix) && int.TryParse(name.Substring(prefix.Length), out int idx))
                    max = Mathf.Max(max, idx);
            }
            return max + 1;
        }

        private void AppendToTestLevelDatabase(List<LevelData> made)
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(TestLevelDbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LevelDatabase>();
                AssetDatabase.CreateAsset(db, TestLevelDbPath);
            }
            var list = new List<LevelConfig>(db.Levels ?? new LevelConfig[0]);
            list.AddRange(made);
            db.Levels = list.ToArray();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GenerateLevels] TestLevelDatabase now has {db.Levels.Length} level(s) total.");
        }
    }
}
#endif
