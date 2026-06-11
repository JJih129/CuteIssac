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
    [DefaultExecutionOrder(130)]
    [DisallowMultipleComponent]
    public sealed class GameSaveSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnlockManager unlockManager;
        [SerializeField] private MetaProgressionManager metaProgressionManager;
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

            if (metaProgressionManager != null)
            {
                metaProgressionManager.ProgressionChanged -= HandleMetaChanged;
                metaProgressionManager.ProgressionChanged += HandleMetaChanged;
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

            if (metaProgressionManager != null)
            {
                metaProgressionManager.ProgressionChanged -= HandleMetaChanged;
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

            try
            {
                string directory = Path.GetDirectoryName(MetaSaveFilePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(MetaSaveFilePath, JsonUtility.ToJson(saveData, true));
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"GameSaveSystem failed to write meta save: {exception.Message}", this);
                return;
            }

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
            metaProgressionManager?.Import(saveData.Progression);
            unlockManager?.EvaluateProgressionUnlocks(metaProgressionManager != null ? metaProgressionManager.Progression : saveData.Progression);
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

            try
            {
                if (File.Exists(MetaSaveFilePath))
                {
                    string json = File.ReadAllText(MetaSaveFilePath);

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        saveData = JsonUtility.FromJson<MetaSaveData>(json);
                        NormalizeMetaSaveData(saveData);
                        return saveData != null;
                    }
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"GameSaveSystem ignored unreadable meta save: {exception.Message}", this);
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

            if (metaProgressionManager != null)
            {
                saveData.Progression = metaProgressionManager.Export();
            }

            return saveData;
        }

        private bool TryMigrateLegacyUnlockFile(out MetaSaveData saveData)
        {
            saveData = null;

            try
            {
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
                    Options = gameOptionsService != null ? gameOptionsService.Export() : new GameOptionsData(),
                    Progression = metaProgressionManager != null ? metaProgressionManager.Export() : new MetaProgressionSaveData()
                };

                NormalizeMetaSaveData(saveData);
                File.WriteAllText(MetaSaveFilePath, JsonUtility.ToJson(saveData, true));
                return true;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"GameSaveSystem failed to migrate legacy unlock save: {exception.Message}", this);
                saveData = null;
                return false;
            }
        }

        private void HandleMetaChanged()
        {
            SaveMetaState();
        }

        private static void NormalizeMetaSaveData(MetaSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.Unlocks ??= new UnlockSaveData();
            saveData.Options ??= new GameOptionsData();
            saveData.Progression ??= new MetaProgressionSaveData();
            saveData.Progression.TotalBossRoomsCleared = Mathf.Max(0, saveData.Progression.TotalBossRoomsCleared);
            saveData.Progression.TotalEnemyKills = Mathf.Max(0, saveData.Progression.TotalEnemyKills);
            saveData.Progression.CurrentWinStreak = Mathf.Max(0, saveData.Progression.CurrentWinStreak);
            saveData.Progression.BestWinStreak = Mathf.Max(saveData.Progression.CurrentWinStreak, saveData.Progression.BestWinStreak);
            saveData.Progression.NoHitCombatRoomClears = Mathf.Max(0, saveData.Progression.NoHitCombatRoomClears);
            saveData.Progression.BossClearsWithoutBombs = Mathf.Max(0, saveData.Progression.BossClearsWithoutBombs);
            saveData.Progression.DiscoveredItemIds ??= new System.Collections.Generic.List<string>();
            saveData.Progression.SeenEnemyIds ??= new System.Collections.Generic.List<string>();
            saveData.Progression.CompletedAchievementIds ??= new System.Collections.Generic.List<string>();
            saveData.Progression.EnemyKillCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            saveData.Progression.RoomTypeClearCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            saveData.Progression.CharacterRecords ??= new System.Collections.Generic.List<CharacterProgressionRecord>();
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

            if (metaProgressionManager == null)
            {
                metaProgressionManager = GetComponent<MetaProgressionManager>();
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
