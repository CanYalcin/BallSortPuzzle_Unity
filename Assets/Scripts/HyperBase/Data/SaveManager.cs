using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace HyperBase.Data
{
    /// <summary>
    /// Loads and saves PlayerData as AES-encrypted JSON.
    /// Sync Save for pause/quit. Async SaveAsync during gameplay.
    /// Auto-backup rotation: save.dat -> save.bak. Falls back on corruption.
    /// </summary>
    public class SaveManager
    {
        private const string FileName       = "save.dat";
        private const string BackupFileName = "save.bak";

        private static string SavePath   => Path.Combine(Application.persistentDataPath, FileName);
        private static string BackupPath => Path.Combine(Application.persistentDataPath, BackupFileName);

        private PlayerData _data;

        // Single place where a new PlayerData is ever constructed.
        public PlayerData Data => _data ??= new PlayerData();

        public bool IsLoaded { get; private set; }

        public void Load()
        {
            PlayerData fromDisk = ReadFromDisk(SavePath) ?? ReadFromDisk(BackupPath);
            _data               = fromDisk; // may be null; Data property will init lazily
            IsLoaded            = true;
            Debug.Log($"[SaveManager] Loaded — Level:{Data.CurrentLevelIndex} Soft:{Data.SoftCurrency}");
        }

        public void Save()
        {
            if (_data == null) return;
            _data.LastSaveTime = DateTime.UtcNow.ToString("O");
            try
            {
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, overwrite: true);
                string json = JsonConvert.SerializeObject(_data, Formatting.None);
                File.WriteAllText(SavePath, EncryptionHelper.Encrypt(json));
            }
            catch (Exception e) { Debug.LogError($"[SaveManager] Save failed: {e.Message}"); }
        }

        public async UniTaskVoid SaveAsync()
        {
            if (_data == null) return;
            _data.LastSaveTime = DateTime.UtcNow.ToString("O");
            try
            {
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, overwrite: true);
                string json = JsonConvert.SerializeObject(_data, Formatting.None);
                await File.WriteAllTextAsync(SavePath, EncryptionHelper.Encrypt(json));
            }
            catch (Exception e) { Debug.LogError($"[SaveManager] Async save failed: {e.Message}"); }
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))   File.Delete(SavePath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            _data = null; // Next access to Data property auto-creates a fresh instance
            Debug.Log("[SaveManager] Save deleted — fresh data will be created on next access.");
        }

        private static PlayerData ReadFromDisk(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string enc  = File.ReadAllText(path);
                string json = EncryptionHelper.Decrypt(enc);
                return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<PlayerData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Could not read {Path.GetFileName(path)}: {e.Message}");
                return null;
            }
        }
    }
}
