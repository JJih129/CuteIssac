using System;
using System.IO;
using CuteIssac.Core.Meta;
using CuteIssac.Core.Run;
using CuteIssac.Core.Settings;
using UnityEngine;

namespace CuteIssac.Core.Save
{
    /// <summary>
    /// Coordinates account-level meta save and optional active run snapshots without coupling gameplay systems to file IO.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSaveSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnlockManager unlockManager;
        [SerializeField] private GameOptionsService gameOptionsService;
        [SerializeField] private RunSaveSystem runSaveSystem;
        [SerializeField] private RunManager runManager;

        [Header("Meta Storage")]
        [SerializeField] private string metaSaveFileName = "meta-save.json";
        [SerializeField] private string legacyUnlockFileName = "meta-unlocks.json";
        [SerializeField] private bool autoLoadMetaOnAwake = true;
        [SerializeField] private bool saveMetaOnApplicationQuit = true;

        [Header("Run Storage")]
        [SerializeField] private bool saveRunSnapshot = true;
        [SerializeField] private bool deleteRunSnapshotWhenRunEnds = true;

        public string MetaSaveFilePath => Path.Combine(Application.persistentDataPath, metaSaveFileName);
        public string LegacyUnlockFilePath => Path.Combine(Application.persistentDataPath, legacyUnlockFileName);

        private void Awake()
        {
            ResolveReferences();

            if (autoLoadMetaOnAwake)
            {
                LoadMetaState();
            }
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (unlockManager != null)
            {
                unlockManager.UnlockStateChanged -= HandleMetaChanged;
                unlockManager.UnlockStateChanged += HandleMetaChanged;
            }

            if (gameOptionsService != null)
            {
                gameOptionsService.OptionsChanged -= HandleOptionsChanged;
                gameOptionsService.OptionsChanged += HandleOptionsChanged;
            }

            if (runManager != null)
            {
                runManager.RunEnded -= HandleRunEnded;
                runManager.RunEnded += HandleRunEnded;
            }
        }

        private void OnDisable()
        {
            if (unlockManager != null)
            {
                unlockManager.UnlockStateChanged -= HandleMetaChanged;
            }

            if (gameOptionsService != null)
            {
                gameOptionsService.OptionsChanged -= HandleOptionsChanged;
            }

            if (runManager != null)
            {
                runManager.RunEnded -= HandleRunEnded;
            }
        }

        private void OnApplicationQuit()
        {
            if (saveMetaOnApplicationQuit)
            {
                SaveMetaState();
            }

            if (saveRunSnapshot)
            {
                runSaveSystem?.SaveCurrentRun();
            }
        }

        [ContextMenu("Save Meta State")]
        public void SaveMetaState()
        {
            ResolveReferences();

            MetaSaveData saveData = BuildMetaSaveData();

            if (saveData == null)
            {
                return;
            }

            string directory = Path.GetDirectoryName(MetaSaveFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(MetaSaveFilePath, JsonUtility.ToJson(saveData, true));

            if (saveRunSnapshot)
            {
                runSaveSystem?.SaveCurrentRun();
            }
        }

        [ContextMenu("Load Meta State")]
        public bool LoadMetaState()
        {
            ResolveReferences();

            if (!TryLoadMetaSaveData(out MetaSaveData saveData))
            {
                return false;
            }

            unlockManager?.ImportSaveData(saveData.Unlocks);
            gameOptionsService?.Import(saveData.Options);
            return true;
        }

        [ContextMenu("Delete Run Snapshot")]
        public void DeleteRunSnapshot()
        {
            runSaveSystem?.DeleteRunSave();
        }

        public bool TryLoadMetaSaveData(out MetaSaveData saveData)
        {
            saveData = null;

            if (File.Exists(MetaSaveFilePath))
            {
                string json = File.ReadAllText(MetaSaveFilePath);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    saveData = JsonUtility.FromJson<MetaSaveData>(json);
                    return saveData != null;
                }
            }

            return TryMigrateLegacyUnlockFile(out saveData);
        }

        public bool TryLoadRunSnapshot(out RunSaveData runSaveData)
        {
            runSaveData = null;
            return runSaveSystem != null && runSaveSystem.TryLoadLatestRun(out runSaveData);
        }

        private MetaSaveData BuildMetaSaveData()
        {
            MetaSaveData saveData = new
            ()
            {
                LastSavedUtc = DateTime.UtcNow.ToString("O")
            };

            if (unlockManager != null)
            {
                saveData.Unlocks = unlockManager.ExportSaveData();
            }

            if (gameOptionsService != null)
            {
                saveData.Options = gameOptionsService.Export();
            }

            return saveData;
        }

        private bool TryMigrateLegacyUnlockFile(out MetaSaveData saveData)
        {
            saveData = null;

            if (!File.Exists(LegacyUnlockFilePath))
            {
                return false;
            }

            string json = File.ReadAllText(LegacyUnlockFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            UnlockSaveData legacyUnlockData = JsonUtility.FromJson<UnlockSaveData>(json);

            if (legacyUnlockData == null)
            {
                return false;
            }

            saveData = new MetaSaveData
            {
                LastSavedUtc = DateTime.UtcNow.ToString("O"),
                Unlocks = legacyUnlockData,
                Options = gameOptionsService != null ? gameOptionsService.Export() : new GameOptionsData()
            };

            File.WriteAllText(MetaSaveFilePath, JsonUtility.ToJson(saveData, true));
            return true;
        }

        private void HandleMetaChanged()
        {
            SaveMetaState();
        }

        private void HandleOptionsChanged(GameOptionsData _)
        {
            SaveMetaState();
        }

        private void HandleRunEnded(RunContext _, RunEndReason __)
        {
            SaveMetaState();

            if (!saveRunSnapshot || !deleteRunSnapshotWhenRunEnds)
            {
                return;
            }

            runSaveSystem?.DeleteRunSave();
        }

        private void ResolveReferences()
        {
            if (unlockManager == null)
            {
                unlockManager = GetComponent<UnlockManager>();
            }

            if (gameOptionsService == null)
            {
                gameOptionsService = GetComponent<GameOptionsService>();
            }

            if (runSaveSystem == null)
            {
                runSaveSystem = GetComponent<RunSaveSystem>();
            }

            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }
        }
    }
}
