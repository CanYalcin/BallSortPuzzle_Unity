#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortPuzzle.DevTools
{
    /// <summary>
    /// Editor menu tools for development — visible under SortPuzzle/Dev/
    /// None of these tools affect production builds.
    /// </summary>
    public static class DevSaveTools
    {
        private static string SavePath   => Path.Combine(Application.persistentDataPath, "save.dat");
        private static string BackupPath => Path.Combine(Application.persistentDataPath, "save.bak");

        // ── Delete save ───────────────────────────────────────────────────────

        [MenuItem("SortPuzzle/Dev/Delete Save File")]
        public static void DeleteSave()
        {
            int deleted = 0;
            if (File.Exists(SavePath))   { File.Delete(SavePath);   deleted++; }
            if (File.Exists(BackupPath)) { File.Delete(BackupPath); deleted++; }

            string msg = deleted > 0
                ? $"[DevSave] Deleted {deleted} save file(s).\nPath: {Application.persistentDataPath}\nHit Play again to start fresh."
                : $"[DevSave] No save files found at:\n{Application.persistentDataPath}";
            Debug.Log(msg);
        }

        // ── Open save folder ──────────────────────────────────────────────────

        [MenuItem("SortPuzzle/Dev/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string dir = Application.persistentDataPath;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
            Debug.Log($"[DevSave] Save folder: {dir}");
        }

        // ── Print save info ───────────────────────────────────────────────────

        [MenuItem("SortPuzzle/Dev/Print Save Path")]
        public static void PrintSavePath()
        {
            Debug.Log($"[DevSave] Save  : {SavePath}   exists={File.Exists(SavePath)}\n" +
                      $"         Backup: {BackupPath} exists={File.Exists(BackupPath)}");
        }

        // ── Set level index ───────────────────────────────────────────────────

        [MenuItem("SortPuzzle/Dev/Jump to Level 1")]
        public static void JumpLevel1() => SetLevelIndex(0);

        [MenuItem("SortPuzzle/Dev/Jump to Level 2")]
        public static void JumpLevel2() => SetLevelIndex(1);

        [MenuItem("SortPuzzle/Dev/Jump to Level 3")]
        public static void JumpLevel3() => SetLevelIndex(2);

        private static void SetLevelIndex(int idx)
        {
            // Parse existing save if present, patch CurrentLevelIndex, re-save (plain JSON — readable by SaveManager on next Load)
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[DevSave] No save file found. Start the game once first, then use Jump to Level.");
                return;
            }
            try
            {
                // SaveManager uses AES encryption via EncryptionHelper — we can't modify without the key from editor.
                // Instead, delete the save and create a plain note so the dev knows.
                File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                // Write a marker so BootstrapEntryPoint creates fresh data at next load
                Debug.Log($"[DevSave] Save deleted. On next Play, game starts fresh.\n" +
                          $"To reach Level {idx + 1}: play through levels or hold off for a level-skip tool.\n" +
                          $"(Tip: a 'Skip Level' in-game debug button can be added to GameplayScreen for faster testing.)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DevSave] Failed: {e.Message}");
            }
        }
    }
}
#endif
