using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Meta;
using CuteIssac.Core.Run;
using CuteIssac.Core.Save;
using CuteIssac.Core.Settings;
using CuteIssac.Data.Balance;
using CuteIssac.Data.Debug;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Data.Item;
using CuteIssac.Data.Run;
using CuteIssac.Data.Unlock;
using CuteIssac.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class TitleMenuController : MonoBehaviour
    {
        private const string TitleSceneName = "TitleScene";
        private const string GameplaySceneName = "SampleScene";
        private const string SelectedCharacterPrefKey = "meta.selected_character";
        private const string HardModePrefKey = "meta.hard_mode";
        private const string MetaSaveFileName = "meta-save.json";
        private const string RunSaveFileName = "run-save.json";

        private static bool s_IsSceneHooked;

        private RectTransform _contentRoot;
        private Text _titleText;
        private CanvasScaler _canvasScaler;
        private ScrollRect _contentScrollRect;
        private Button _continueButton;
        private CharacterProfileCatalog _characterCatalog;
        private MetaCollectionCatalog _collectionCatalog;
        private readonly List<MetaCollectionEntry> _runtimeCollectionEntries = new();
        private MetaSaveData _metaSaveData;
        private string _selectedCharacterId = "default";
        private bool _hardModeSelected;
        private bool _resetArmed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            if (!s_IsSceneHooked)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneLoaded += HandleSceneLoaded;
                s_IsSceneHooked = true;
            }

            TryCreateForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            TryCreateForScene(scene);
        }

        private static void TryCreateForScene(Scene scene)
        {
            if (!scene.IsValid() || scene.name != TitleSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            new GameObject("TitleMenuController").AddComponent<TitleMenuController>();
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _selectedCharacterId = PlayerPrefs.GetString(SelectedCharacterPrefKey, "default");
            _hardModeSelected = PlayerPrefs.GetInt(HardModePrefKey, 0) == 1;
            _characterCatalog = Resources.Load<CharacterProfileCatalog>("Characters/DefaultCharacterProfileCatalog");
            _collectionCatalog = Resources.Load<MetaCollectionCatalog>("MetaCollectionCatalog");
            RebuildRuntimeCollectionEntries();
            LoadMeta();
            BuildMenu();
            ApplyOptions();
            ShowHome();
        }

        private void StartNewRun()
        {
            CharacterProfileLaunchRequest.RequestCharacter(_selectedCharacterId);
            RunLaunchRequest.RequestNewRunFromFirstFloor(_hardModeSelected);
            SceneManager.LoadScene(GameplaySceneName, LoadSceneMode.Single);
        }

        private void ContinueRun()
        {
            if (!TryLoadRestorableRunSave(out _))
            {
                RefreshContinueButton();
                ShowText("Continue", "No restorable run save exists.");
                return;
            }

            SceneManager.LoadScene(GameplaySceneName, LoadSceneMode.Single);
        }

        private void ShowHome()
        {
            LoadMeta();
            SetTitle("CUTE ISSAC");
            RefreshContinueButton();
            ShowText("Run", $"Selected: {ResolveSelectedCharacterName()}\nContinue: {(TryLoadRestorableRunSave(out _) ? "available" : "none")}");
        }

        private void ShowCharacters()
        {
            LoadMeta();
            ClearContent();
            SetTitle("Character Select");

            if (_characterCatalog == null || _characterCatalog.Profiles.Count == 0)
            {
                CreateBodyText(_contentRoot, "No character catalog found.\nUsing default shared player profile.");
                CreateButton(_contentRoot, "Use Default", () => SelectCharacter("default"));
                return;
            }

            CreateButton(_contentRoot, _hardModeSelected ? "Hard Mode: ON" : "Hard Mode: OFF", ToggleHardMode);
            CreateBodyText(_contentRoot, "Hard Mode records a separate clear mark on victory. Difficulty tuning can build on this flag.", 16);

            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            UnlockData[] unlockDefinitions = Resources.LoadAll<UnlockData>("Unlocks");

            for (int index = 0; index < _characterCatalog.Profiles.Count; index++)
            {
                CharacterProfileData profile = _characterCatalog.Profiles[index];
                if (profile == null)
                {
                    continue;
                }

                string label = profile.CharacterId == _selectedCharacterId
                    ? $"{profile.DisplayName}  SELECTED"
                    : profile.DisplayName;
                string id = profile.CharacterId;
                bool unlocked = IsCharacterUnlocked(profile);
                CreateButton(_contentRoot, unlocked ? label : $"{profile.DisplayName}  LOCKED", () =>
                {
                    if (unlocked)
                    {
                        SelectCharacter(id);
                    }
                });
                string lockLine = unlocked
                    ? string.Empty
                    : $"\nUnlock: {ResolveUnlockLabel(profile.UnlockKey)}\nProgress: {ResolveUnlockProgressLine(profile.UnlockKey, progression, unlockDefinitions)}";
                CharacterProgressionRecord record = FindCharacterRecord(progression, profile.CharacterId);
                string markLine = $"\n{FormatClearMarkSummary(record)}";
                string recordLine = record != null
                    ? $"\nRuns {record.Runs}  Wins {record.Wins}  Best F{record.BestFloor}"
                    : "\nRuns 0  Wins 0  Best F0";
                CreateBodyText(_contentRoot, $"{(string.IsNullOrWhiteSpace(profile.Description) ? profile.CharacterId : profile.Description)}{recordLine}{markLine}{lockLine}", 18);
            }
        }

        private void SelectCharacter(string characterId)
        {
            _selectedCharacterId = string.IsNullOrWhiteSpace(characterId) ? "default" : characterId;
            PlayerPrefs.SetString(SelectedCharacterPrefKey, _selectedCharacterId);
            PlayerPrefs.Save();
            ShowCharacters();
        }

        private void ToggleHardMode()
        {
            _hardModeSelected = !_hardModeSelected;
            PlayerPrefs.SetInt(HardModePrefKey, _hardModeSelected ? 1 : 0);
            PlayerPrefs.Save();
            ShowCharacters();
        }

        private void ShowCollection()
        {
            LoadMeta();
            SetTitle("Collection");
            ClearContent();
            CreateButton(_contentRoot, "Items", ShowCollectionItems);
            CreateButton(_contentRoot, "Enemy Codex", ShowEnemyCodex);
            ShowCollectionItemsBody();
        }

        private void ShowCollectionItems()
        {
            LoadMeta();
            SetTitle("Collection");
            ClearContent();
            CreateButton(_contentRoot, "Items", ShowCollectionItems);
            CreateButton(_contentRoot, "Enemy Codex", ShowEnemyCodex);
            ShowCollectionItemsBody();
        }

        private void ShowEnemyCodex()
        {
            LoadMeta();
            SetTitle("Enemy Codex");
            ClearContent();
            CreateButton(_contentRoot, "Items", ShowCollectionItems);
            CreateButton(_contentRoot, "Enemy Codex", ShowEnemyCodex);
            ShowEnemyCodexBody();
        }

        private void ShowCollectionItemsBody()
        {
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            StringBuilder weapons = new();
            StringBuilder actives = new();
            StringBuilder passives = new();
            int knownItems = 0;
            int discoveredItems = 0;

            IReadOnlyList<MetaCollectionEntry> entries = GetRuntimeCollectionEntries();
            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    MetaCollectionEntry entry = entries[index];
                    if (entry == null || entry.Kind == MetaCollectionEntryKind.Enemy)
                    {
                        continue;
                    }

                    bool discovered = ContainsId(progression.DiscoveredItemIds, entry.Id);
                    bool unlocked = IsCollectionEntryUnlocked(entry, _metaSaveData?.Unlocks);
                    knownItems++;
                    if (discovered)
                    {
                        discoveredItems++;
                    }

                    AppendCollectionEntry(ResolveItemBuilder(entry.Kind, passives, actives, weapons), entry, discovered, 0, unlocked);
                }
            }

            if (knownItems == 0 && progression.DiscoveredItemIds.Count > 0)
            {
                AppendRawIds(passives, progression.DiscoveredItemIds);
                knownItems = progression.DiscoveredItemIds.Count;
                discoveredItems = progression.DiscoveredItemIds.Count;
            }

            CreateBodyText(_contentRoot, $"Items {discoveredItems}/{Mathf.Max(knownItems, discoveredItems)}", 28, FontStyle.Bold);
            CreateBodyText(_contentRoot,
                $"Weapons\n{BuildSectionText(weapons)}\n\nActives\n{BuildSectionText(actives)}\n\nPassives\n{BuildSectionText(passives)}",
                18);
            ResetContentScroll();
        }

        private void ShowEnemyCodexBody()
        {
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            StringBuilder enemies = new();
            int knownEnemies = 0;
            int seenEnemies = 0;

            IReadOnlyList<MetaCollectionEntry> entries = GetRuntimeCollectionEntries();
            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    MetaCollectionEntry entry = entries[index];
                    if (entry == null || entry.Kind != MetaCollectionEntryKind.Enemy)
                    {
                        continue;
                    }

                    bool seen = ContainsId(progression.SeenEnemyIds, entry.Id);
                    int killCount = GetCounterValue(progression.EnemyKillCounts, entry.Id);
                    knownEnemies++;
                    if (seen)
                    {
                        seenEnemies++;
                    }

                    AppendCollectionEntry(enemies, entry, seen, killCount, true);
                }
            }

            if (knownEnemies == 0 && progression.SeenEnemyIds.Count > 0)
            {
                AppendRawIds(enemies, progression.SeenEnemyIds);
                knownEnemies = progression.SeenEnemyIds.Count;
                seenEnemies = progression.SeenEnemyIds.Count;
            }

            CreateBodyText(_contentRoot, $"Enemy Codex {seenEnemies}/{Mathf.Max(knownEnemies, seenEnemies)}", 28, FontStyle.Bold);
            CreateBodyText(_contentRoot, BuildSectionText(enemies), 18);
            ResetContentScroll();
        }

        private void ShowAchievements()
        {
            LoadMeta();
            SetTitle("Achievements");
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            AchievementData[] achievements = Resources.LoadAll<AchievementData>("Achievements");
            StringBuilder builder = new();
            int completedCount = 0;

            for (int index = 0; index < achievements.Length; index++)
            {
                AchievementData achievement = achievements[index];
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.AchievementId))
                {
                    continue;
                }

                bool completed = ContainsId(progression.CompletedAchievementIds, achievement.AchievementId);
                if (completed)
                {
                    completedCount++;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                    builder.Append('\n');
                }

                builder.Append(completed ? "[DONE] " : "[----] ");
                builder.Append(string.IsNullOrWhiteSpace(achievement.DisplayName) ? achievement.AchievementId : achievement.DisplayName);
                if (completed)
                {
                    string completedAt = ResolveAchievementCompletedAtLabel(progression, achievement.AchievementId);
                    if (!string.IsNullOrWhiteSpace(completedAt))
                    {
                        builder.Append("  ");
                        builder.Append(completedAt);
                    }
                }

                builder.Append('\n');
                builder.Append(string.IsNullOrWhiteSpace(achievement.Description) ? achievement.AchievementId : achievement.Description);
                builder.Append('\n');
                builder.Append("Progress: ");
                builder.Append(ResolveAchievementProgress(achievement, progression));

                if (achievement.RewardTargetType == AchievementRewardTargetType.UnlockKey && !string.IsNullOrWhiteSpace(achievement.RewardUnlockKey))
                {
                    builder.Append('\n');
                    builder.Append("Reward: ");
                    builder.Append(UnlockDisplayNameResolver.Resolve(achievement.RewardUnlockKey));
                }
            }

            ShowText(
                $"Achievements {completedCount}/{Mathf.Max(achievements.Length, completedCount)}",
                builder.Length > 0 ? builder.ToString() : "No achievement definitions found.");
        }

        private void ShowStats()
        {
            LoadMeta();
            SetTitle("Stats");
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            ShowText("Meta Progress",
                $"Runs: {progression.TotalRuns}\nWins: {progression.TotalWins}\nDefeats: {progression.TotalDefeats}\nAbandons: {progression.TotalAbandons}\nCurrent Streak: {progression.CurrentWinStreak}\nBest Streak: {progression.BestWinStreak}\nBest Floor: {progression.BestFloor}\nRooms Cleared: {progression.TotalRoomsCleared}\nBoss Clears: {progression.TotalBossRoomsCleared}\nTotal Kills: {progression.TotalEnemyKills}\nNo-Hit Rooms: {progression.NoHitCombatRoomClears}\nNo-Bomb Boss Clears: {progression.BossClearsWithoutBombs}\nSeen Enemies: {progression.SeenEnemyIds.Count}\nPlay Time: {FormatSeconds(progression.TotalRunSeconds)}\n\nEnemy Kills\n{FormatCounterRecords(progression.EnemyKillCounts)}\n\nRoom Type Clears\n{FormatCounterRecords(progression.RoomTypeClearCounts)}\n\nChallenge Ranks\n{FormatCounterRecords(progression.ChallengeClearRankCounts)}\n\nChallenge Pressure\n{FormatCounterRecords(progression.ChallengePressureTierCounts)}\n\nSpecial Reward Offers\n{FormatCounterRecords(progression.SpecialRewardOfferCounts)}\n\nSpecial Deal Offers\n{FormatCounterRecords(progression.SpecialDealOfferCounts)}\n\nSpecial Deal Purchases\n{FormatCounterRecords(progression.SpecialDealPurchaseCounts)}\n\nSpecial Deal Declines\n{FormatCounterRecords(progression.SpecialDealDeclineCounts)}\n\nCharacters\n{FormatCharacterRecords(progression.CharacterRecords)}");
        }

        private void ShowOptions()
        {
            ClearContent();
            SetTitle("Options");
            GameOptionsData options = ResolveOptions();
            CreateBodyText(_contentRoot, $"Resolution: {options.ResolutionWidth}x{options.ResolutionHeight}\nUI Scale: {options.UiScale:0.00}\nMaster Volume: {Mathf.RoundToInt(options.MasterVolume * 100f)}%\nMusic Volume: {Mathf.RoundToInt(options.MusicVolume * 100f)}%\nSFX Volume: {Mathf.RoundToInt(options.SfxVolume * 100f)}%\nFullscreen: {options.Fullscreen}");
            CreateButton(_contentRoot, "Resolution 1280x720", () => SetResolution(1280, 720));
            CreateButton(_contentRoot, "Resolution 1920x1080", () => SetResolution(1920, 1080));
            CreateButton(_contentRoot, "Toggle Fullscreen", ToggleFullscreen);
            CreateButton(_contentRoot, "Master -10", () => AdjustVolume(-0.1f));
            CreateButton(_contentRoot, "Master +10", () => AdjustVolume(0.1f));
            CreateButton(_contentRoot, "Music -10", () => AdjustMusicVolume(-0.1f));
            CreateButton(_contentRoot, "Music +10", () => AdjustMusicVolume(0.1f));
            CreateButton(_contentRoot, "SFX -10", () => AdjustSfxVolume(-0.1f));
            CreateButton(_contentRoot, "SFX +10", () => AdjustSfxVolume(0.1f));
            CreateButton(_contentRoot, "UI Scale -", () => AdjustUiScale(-0.1f));
            CreateButton(_contentRoot, "UI Scale +", () => AdjustUiScale(0.1f));
            CreateButton(_contentRoot, $"Camera Shake: {(options.CameraShakeEnabled ? "On" : "Off")}", ToggleCameraShake);
            CreateButton(_contentRoot, $"Damage Numbers: {(options.DamageNumbersEnabled ? "On" : "Off")}", ToggleDamageNumbers);
            CreateButton(_contentRoot, $"High Contrast: {(options.HighContrastUi ? "On" : "Off")}", ToggleHighContrast);
            CreateButton(_contentRoot, $"Reduce Flashes: {(options.ReduceFlashes ? "On" : "Off")}", ToggleReduceFlashes);
            CreateButton(_contentRoot, $"Color Assist: {(options.ColorBlindAssist ? "On" : "Off")}", ToggleColorAssist);
            CreateButton(_contentRoot, _resetArmed ? "CONFIRM DATA RESET" : "Reset Data", ResetData);
        }

        private void ShowCredits()
        {
            SetTitle("Credits");
            ShowText("Project", "CuteIssac prototype\nRuntime UI, run saves, unlocks, and Isaac-style meta progression scaffold.");
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildMenu()
        {
            InputSystemEventSystemBootstrap.EnsureReady();
            Canvas canvas = CreateCanvas();
            _canvasScaler = canvas.GetComponent<CanvasScaler>();
            CreateBackground(canvas.transform);

            RectTransform shell = CreatePanel("TitleShell", canvas.transform, new Color(0.09f, 0.09f, 0.1f, 0.94f));
            Stretch(shell, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f));

            _titleText = CreateText("Title", shell, "CUTE ISSAC", 46, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            Anchor(_titleText.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f));

            RectTransform sidebar = CreatePanel("Sidebar", shell, new Color(0.16f, 0.18f, 0.2f, 0.96f));
            Anchor(sidebar, new Vector2(0.04f, 0.06f), new Vector2(0.29f, 0.82f));

            _contentRoot = CreateScrollContent("ContentScroll", shell, new Vector2(0.32f, 0.06f), new Vector2(0.96f, 0.82f), out _contentScrollRect);
            AddVerticalLayout(_contentRoot, 16, 24);
            ContentSizeFitter contentFitter = _contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddVerticalLayout(sidebar, 12, 18);
            CreateButton(sidebar, "New Run", StartNewRun);
            _continueButton = CreateButton(sidebar, "Continue", ContinueRun);
            CreateButton(sidebar, "Character", ShowCharacters);
            CreateButton(sidebar, "Collection", ShowCollection);
            CreateButton(sidebar, "Achievements", ShowAchievements);
            CreateButton(sidebar, "Stats", ShowStats);
            CreateButton(sidebar, "Options", ShowOptions);
            CreateButton(sidebar, "Reset Data", ShowResetData);
            CreateButton(sidebar, "Credits", ShowCredits);
            CreateButton(sidebar, "Quit", QuitGame);
            RefreshContinueButton();
        }

        private void SetTitle(string value)
        {
            if (_titleText != null)
            {
                _titleText.text = value;
            }
        }

        private void ShowText(string header, string body)
        {
            ClearContent();
            CreateBodyText(_contentRoot, header, 28, FontStyle.Bold);
            CreateBodyText(_contentRoot, body);
        }

        private void ClearContent()
        {
            if (_contentRoot == null)
            {
                return;
            }

            for (int index = _contentRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(_contentRoot.GetChild(index).gameObject);
            }

            ResetContentScroll();
        }

        private void ResetContentScroll()
        {
            if (_contentScrollRect != null)
            {
                _contentScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void LoadMeta()
        {
            _metaSaveData = null;
            if (!File.Exists(MetaSavePath))
            {
                _metaSaveData = new MetaSaveData();
                return;
            }

            try
            {
                string json = File.ReadAllText(MetaSavePath);
                _metaSaveData = string.IsNullOrWhiteSpace(json) ? new MetaSaveData() : JsonUtility.FromJson<MetaSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"TitleMenuController ignored unreadable meta save: {exception.Message}");
                _metaSaveData = new MetaSaveData();
            }

            _metaSaveData ??= new MetaSaveData();
            NormalizeMeta();
        }

        private void SaveMeta()
        {
            _metaSaveData ??= new MetaSaveData();
            _metaSaveData.LastSavedUtc = DateTime.UtcNow.ToString("O");

            try
            {
                string directory = Path.GetDirectoryName(MetaSavePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(MetaSavePath, JsonUtility.ToJson(_metaSaveData, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"TitleMenuController failed to write meta save: {exception.Message}");
            }
        }

        private GameOptionsData ResolveOptions()
        {
            _metaSaveData ??= new MetaSaveData();
            NormalizeMeta();
            _metaSaveData.Options ??= new GameOptionsData();
            return _metaSaveData.Options;
        }

        private void NormalizeMeta()
        {
            _metaSaveData ??= new MetaSaveData();
            _metaSaveData.Unlocks ??= new UnlockSaveData();
            _metaSaveData.Options ??= new GameOptionsData();
            NormalizeOptions(_metaSaveData.Options);
            _metaSaveData.Progression ??= new MetaProgressionSaveData();
            _metaSaveData.Progression.TotalBossRoomsCleared = Mathf.Max(0, _metaSaveData.Progression.TotalBossRoomsCleared);
            _metaSaveData.Progression.TotalEnemyKills = Mathf.Max(0, _metaSaveData.Progression.TotalEnemyKills);
            _metaSaveData.Progression.CurrentWinStreak = Mathf.Max(0, _metaSaveData.Progression.CurrentWinStreak);
            _metaSaveData.Progression.BestWinStreak = Mathf.Max(_metaSaveData.Progression.CurrentWinStreak, _metaSaveData.Progression.BestWinStreak);
            _metaSaveData.Progression.NoHitCombatRoomClears = Mathf.Max(0, _metaSaveData.Progression.NoHitCombatRoomClears);
            _metaSaveData.Progression.BossClearsWithoutBombs = Mathf.Max(0, _metaSaveData.Progression.BossClearsWithoutBombs);
            _metaSaveData.Progression.DiscoveredItemIds ??= new System.Collections.Generic.List<string>();
            _metaSaveData.Progression.SeenEnemyIds ??= new System.Collections.Generic.List<string>();
            _metaSaveData.Progression.CompletedAchievementIds ??= new System.Collections.Generic.List<string>();
            _metaSaveData.Progression.CompletedAchievements ??= new System.Collections.Generic.List<AchievementCompletionRecord>();
            _metaSaveData.Progression.EnemyKillCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.RoomTypeClearCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.ChallengeClearRankCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.ChallengePressureTierCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.SpecialRewardOfferCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.SpecialDealOfferCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.SpecialDealPurchaseCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.SpecialDealDeclineCounts ??= new System.Collections.Generic.List<MetaProgressionCounterRecord>();
            _metaSaveData.Progression.CharacterRecords ??= new System.Collections.Generic.List<CharacterProgressionRecord>();
        }

        private static void NormalizeOptions(GameOptionsData options)
        {
            if (options == null)
            {
                return;
            }

            options.MasterVolume = Mathf.Clamp01(options.MasterVolume);
            options.MusicVolume = Mathf.Clamp01(options.MusicVolume);
            options.SfxVolume = Mathf.Clamp01(options.SfxVolume);
            options.ResolutionWidth = Mathf.Clamp(options.ResolutionWidth <= 0 ? 1920 : options.ResolutionWidth, 640, 7680);
            options.ResolutionHeight = Mathf.Clamp(options.ResolutionHeight <= 0 ? 1080 : options.ResolutionHeight, 360, 4320);
            options.UiScale = Mathf.Clamp(options.UiScale <= 0f ? 1f : options.UiScale, 0.75f, 1.5f);
        }

        private void ApplyOptions()
        {
            GameOptionsData options = ResolveOptions();
            AudioListener.volume = Mathf.Clamp01(options.MasterVolume);
            GameAudioSystem.SetGlobalVolumes(options.MusicVolume, options.SfxVolume);
            Screen.SetResolution(
                Mathf.Clamp(options.ResolutionWidth <= 0 ? 1920 : options.ResolutionWidth, 640, 7680),
                Mathf.Clamp(options.ResolutionHeight <= 0 ? 1080 : options.ResolutionHeight, 360, 4320),
                options.Fullscreen);
            GameOptionsService.ApplyUiScale(_canvasScaler, options.UiScale);
        }

        private void SetResolution(int width, int height)
        {
            GameOptionsData options = ResolveOptions();
            options.ResolutionWidth = width;
            options.ResolutionHeight = height;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ToggleFullscreen()
        {
            ResolveOptions().Fullscreen = !ResolveOptions().Fullscreen;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void AdjustVolume(float delta)
        {
            ResolveOptions().MasterVolume = Mathf.Clamp01(ResolveOptions().MasterVolume + delta);
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void AdjustMusicVolume(float delta)
        {
            ResolveOptions().MusicVolume = Mathf.Clamp01(ResolveOptions().MusicVolume + delta);
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void AdjustSfxVolume(float delta)
        {
            ResolveOptions().SfxVolume = Mathf.Clamp01(ResolveOptions().SfxVolume + delta);
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void AdjustUiScale(float delta)
        {
            ResolveOptions().UiScale = Mathf.Clamp(ResolveOptions().UiScale + delta, 0.75f, 1.5f);
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ToggleCameraShake()
        {
            ResolveOptions().CameraShakeEnabled = !ResolveOptions().CameraShakeEnabled;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ToggleDamageNumbers()
        {
            ResolveOptions().DamageNumbersEnabled = !ResolveOptions().DamageNumbersEnabled;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ToggleHighContrast()
        {
            ResolveOptions().HighContrastUi = !ResolveOptions().HighContrastUi;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ToggleReduceFlashes()
        {
            ResolveOptions().ReduceFlashes = !ResolveOptions().ReduceFlashes;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ToggleColorAssist()
        {
            ResolveOptions().ColorBlindAssist = !ResolveOptions().ColorBlindAssist;
            ApplyOptions();
            SaveMeta();
            ShowOptions();
        }

        private void ResetData()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                ShowResetData();
                return;
            }

            TryDeleteFile(MetaSavePath, "meta save");
            TryDeleteFile(RunSavePath, "run save");

            _resetArmed = false;
            _selectedCharacterId = "default";
            PlayerPrefs.DeleteKey(SelectedCharacterPrefKey);
            PlayerPrefs.Save();
            _metaSaveData = new MetaSaveData();
            ApplyOptions();
            SaveMeta();
            RefreshContinueButton();
            ShowHome();
        }

        private void ShowResetData()
        {
            ClearContent();
            SetTitle("Reset Data");
            CreateBodyText(_contentRoot, "This deletes meta progression, run save, selected character, unlocks, collection records, achievements, and stats.");
            CreateButton(_contentRoot, _resetArmed ? "CONFIRM RESET" : "Arm Reset", ResetData);
            CreateButton(_contentRoot, "Cancel", () =>
            {
                _resetArmed = false;
                ShowHome();
            });
        }

        private void RefreshContinueButton()
        {
            if (_continueButton == null)
            {
                return;
            }

            bool canContinue = TryLoadRestorableRunSave(out _);
            _continueButton.interactable = canContinue;

            if (_continueButton.targetGraphic is Image image)
            {
                image.color = canContinue
                    ? new Color(0.27f, 0.32f, 0.36f, 0.96f)
                    : new Color(0.16f, 0.17f, 0.18f, 0.7f);
            }

            Text label = _continueButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = canContinue
                    ? Color.white
                    : new Color(0.64f, 0.67f, 0.7f, 0.85f);
            }
        }

        private static void TryDeleteFile(string path, string label)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"TitleMenuController failed to delete {label}: {exception.Message}");
            }
        }

        private string ResolveSelectedCharacterName()
        {
            CharacterProfileData profile = _characterCatalog != null ? _characterCatalog.FindProfile(_selectedCharacterId) : null;
            return profile != null ? profile.DisplayName : "Default";
        }

        private bool IsCharacterUnlocked(CharacterProfileData profile)
        {
            if (profile == null)
            {
                return false;
            }

            if (profile.UnlockedByDefault || string.IsNullOrWhiteSpace(profile.UnlockKey))
            {
                return true;
            }

            if (_metaSaveData?.Unlocks?.UnlockedKeys == null)
            {
                return false;
            }

            for (int index = 0; index < _metaSaveData.Unlocks.UnlockedKeys.Count; index++)
            {
                if (string.Equals(_metaSaveData.Unlocks.UnlockedKeys[index], profile.UnlockKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveUnlockLabel(string unlockKey)
        {
            return string.IsNullOrWhiteSpace(unlockKey)
                ? "Locked by meta progression"
                : UnlockDisplayNameResolver.Resolve(unlockKey);
        }

        private static string ResolveUnlockProgressLine(string unlockKey, MetaProgressionSaveData progression, UnlockData[] unlockDefinitions)
        {
            UnlockData definition = FindUnlockDefinitionByTargetKey(unlockKey, unlockDefinitions);
            if (definition == null)
            {
                return "Meta condition";
            }

            int current = ResolveUnlockCurrentProgress(definition, progression);
            int required = ResolveUnlockRequiredProgress(definition);
            return $"{ResolveUnlockConditionLabel(definition)}  {Mathf.Min(current, required)}/{required}";
        }

        private static UnlockData FindUnlockDefinitionByTargetKey(string unlockKey, UnlockData[] unlockDefinitions)
        {
            if (string.IsNullOrWhiteSpace(unlockKey) || unlockDefinitions == null)
            {
                return null;
            }

            for (int index = 0; index < unlockDefinitions.Length; index++)
            {
                UnlockData definition = unlockDefinitions[index];
                if (definition != null && string.Equals(definition.TargetKey, unlockKey, StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }

            return null;
        }

        private static int ResolveUnlockCurrentProgress(UnlockData definition, MetaProgressionSaveData progression)
        {
            if (definition == null || progression == null)
            {
                return 0;
            }

            return definition.ConditionType switch
            {
                UnlockConditionType.BossKill => string.IsNullOrWhiteSpace(definition.RequiredEnemyId)
                    ? progression.TotalBossRoomsCleared
                    : GetCounterValue(progression.EnemyKillCounts, definition.RequiredEnemyId),
                UnlockConditionType.ReachFloor => progression.BestFloor,
                UnlockConditionType.AcquireItem => ContainsId(progression.DiscoveredItemIds, definition.RequiredItemId) ? 1 : 0,
                UnlockConditionType.CumulativeEnemyKillCount => string.IsNullOrWhiteSpace(definition.RequiredEnemyId)
                    ? progression.TotalEnemyKills
                    : GetCounterValue(progression.EnemyKillCounts, definition.RequiredEnemyId),
                UnlockConditionType.CharacterClearMark => HasCharacterClearMark(progression.CharacterRecords, definition.RequiredCharacterId, definition.RequiredClearMark) ? 1 : 0,
                UnlockConditionType.ItemDiscovered => ContainsId(progression.DiscoveredItemIds, definition.RequiredItemId) ? 1 : 0,
                UnlockConditionType.RoomTypeClearCount => GetCounterValue(progression.RoomTypeClearCounts, definition.RequiredRoomType.ToString()),
                UnlockConditionType.TotalRuns => progression.TotalRuns,
                UnlockConditionType.TotalWins => progression.TotalWins,
                UnlockConditionType.TotalEnemyKills => progression.TotalEnemyKills,
                UnlockConditionType.CurrentWinStreak => progression.CurrentWinStreak,
                UnlockConditionType.BestWinStreak => progression.BestWinStreak,
                UnlockConditionType.AchievementCompleted => ContainsId(progression.CompletedAchievementIds, definition.RequiredAchievementId) ? 1 : 0,
                _ => 0
            };
        }

        private static int ResolveUnlockRequiredProgress(UnlockData definition)
        {
            if (definition == null)
            {
                return 1;
            }

            return definition.ConditionType switch
            {
                UnlockConditionType.ReachFloor => definition.RequiredFloorIndex,
                UnlockConditionType.AcquireItem => 1,
                UnlockConditionType.CharacterClearMark => 1,
                UnlockConditionType.ItemDiscovered => 1,
                UnlockConditionType.AchievementCompleted => 1,
                _ => definition.RequiredCount
            };
        }

        private static string ResolveUnlockConditionLabel(UnlockData definition)
        {
            if (definition == null)
            {
                return "Meta condition";
            }

            return definition.ConditionType switch
            {
                UnlockConditionType.BossKill => string.IsNullOrWhiteSpace(definition.RequiredEnemyId)
                    ? "Clear boss rooms"
                    : $"Defeat {definition.RequiredEnemyId}",
                UnlockConditionType.ReachFloor => $"Reach floor {definition.RequiredFloorIndex}",
                UnlockConditionType.AcquireItem => $"Acquire {definition.RequiredItemId}",
                UnlockConditionType.CumulativeEnemyKillCount => string.IsNullOrWhiteSpace(definition.RequiredEnemyId)
                    ? "Defeat enemies"
                    : $"Defeat {definition.RequiredEnemyId}",
                UnlockConditionType.CharacterClearMark => $"Clear mark {definition.RequiredCharacterId}:{definition.RequiredClearMark}",
                UnlockConditionType.ItemDiscovered => $"Discover {definition.RequiredItemId}",
                UnlockConditionType.RoomTypeClearCount => $"Clear {definition.RequiredRoomType} rooms",
                UnlockConditionType.TotalRuns => "Finish runs",
                UnlockConditionType.TotalWins => "Win runs",
                UnlockConditionType.TotalEnemyKills => "Defeat enemies",
                UnlockConditionType.CurrentWinStreak => "Current win streak",
                UnlockConditionType.BestWinStreak => "Best win streak",
                UnlockConditionType.AchievementCompleted => $"Complete {definition.RequiredAchievementId}",
                _ => "Meta condition"
            };
        }

        private bool TryLoadRestorableRunSave(out RunSaveData saveData)
        {
            saveData = null;

            if (!File.Exists(RunSavePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(RunSavePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                saveData = JsonUtility.FromJson<RunSaveData>(json);
                saveData?.Normalize();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"TitleMenuController ignored unreadable run save: {exception.Message}");
                return false;
            }

            return saveData != null
                && saveData.CurrentFloorIndex > 0
                && saveData.RunState != RunState.Defeat
                && saveData.RunState != RunState.Victory
                && saveData.RunState != RunState.FrontEnd
                && saveData.RunState != RunState.Idle;
        }

        private static string FormatSeconds(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{totalSeconds / 3600:00}:{totalSeconds / 60 % 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatCounterRecords(System.Collections.Generic.List<MetaProgressionCounterRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return "None";
            }

            System.Text.StringBuilder builder = new();

            for (int index = 0; index < records.Count; index++)
            {
                MetaProgressionCounterRecord record = records[index];

                if (record == null || string.IsNullOrWhiteSpace(record.Id))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(record.Id);
                builder.Append(": ");
                builder.Append(Mathf.Max(0, record.Count));
            }

            return builder.Length > 0 ? builder.ToString() : "None";
        }

        private IReadOnlyList<MetaCollectionEntry> GetRuntimeCollectionEntries()
        {
            if (_runtimeCollectionEntries.Count == 0)
            {
                RebuildRuntimeCollectionEntries();
            }

            return _runtimeCollectionEntries;
        }

        private void RebuildRuntimeCollectionEntries()
        {
            _runtimeCollectionEntries.Clear();
            HashSet<string> registeredIds = new(StringComparer.OrdinalIgnoreCase);

            if (_collectionCatalog != null)
            {
                for (int index = 0; index < _collectionCatalog.Entries.Count; index++)
                {
                    MetaCollectionEntry entry = _collectionCatalog.Entries[index];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !registeredIds.Add(entry.Id))
                    {
                        continue;
                    }

                    _runtimeCollectionEntries.Add(entry);
                }
            }

            AppendCharacterCatalogCollectionEntries(registeredIds);
            AppendBalanceConfigCollectionEntries(registeredIds);
            AppendDebugCatalogCollectionEntries(registeredIds);
            AppendResourceCollectionEntries(registeredIds);
        }

        private void AppendCharacterCatalogCollectionEntries(HashSet<string> registeredIds)
        {
            if (_characterCatalog == null || registeredIds == null)
            {
                return;
            }

            for (int profileIndex = 0; profileIndex < _characterCatalog.Profiles.Count; profileIndex++)
            {
                CharacterProfileData profile = _characterCatalog.Profiles[profileIndex];
                if (profile == null)
                {
                    continue;
                }

                TryAddCollectionEntry(profile.StartingWeaponItem, registeredIds);
                TryAddCollectionEntry(profile.StartingActiveItem, registeredIds);

                IReadOnlyList<ItemData> passives = profile.StartingPassiveItems;
                if (passives == null)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < passives.Count; itemIndex++)
                {
                    TryAddCollectionEntry(passives[itemIndex], registeredIds);
                }
            }
        }

        private void AppendBalanceConfigCollectionEntries(HashSet<string> registeredIds)
        {
            if (registeredIds == null)
            {
                return;
            }

            BalanceConfig[] balanceConfigs = Resources.LoadAll<BalanceConfig>("Balance");
            for (int index = 0; index < balanceConfigs.Length; index++)
            {
                BalanceConfig balanceConfig = balanceConfigs[index];
                if (balanceConfig != null)
                {
                    AppendRunConfigurationCollectionEntries(balanceConfig.RunConfiguration, registeredIds);
                }
            }

            RunConfiguration[] runConfigurations = Resources.LoadAll<RunConfiguration>(string.Empty);
            for (int index = 0; index < runConfigurations.Length; index++)
            {
                AppendRunConfigurationCollectionEntries(runConfigurations[index], registeredIds);
            }

            FloorConfig[] floorConfigs = Resources.LoadAll<FloorConfig>(string.Empty);
            for (int index = 0; index < floorConfigs.Length; index++)
            {
                AppendFloorConfigCollectionEntries(floorConfigs[index], registeredIds);
            }

            EnemyPoolData[] enemyPools = Resources.LoadAll<EnemyPoolData>(string.Empty);
            for (int index = 0; index < enemyPools.Length; index++)
            {
                AppendEnemyPoolCollectionEntries(enemyPools[index], registeredIds);
            }
        }

        private void AppendRunConfigurationCollectionEntries(RunConfiguration runConfiguration, HashSet<string> registeredIds)
        {
            if (runConfiguration == null || registeredIds == null)
            {
                return;
            }

            ActiveItemData[] activeItems = runConfiguration.RestorableActiveItems;
            if (activeItems != null)
            {
                for (int index = 0; index < activeItems.Length; index++)
                {
                    TryAddCollectionEntry(activeItems[index], registeredIds);
                }
            }

            ItemData[] trinkets = runConfiguration.RestorableTrinkets;
            if (trinkets != null)
            {
                for (int index = 0; index < trinkets.Length; index++)
                {
                    TryAddCollectionEntry(trinkets[index], registeredIds);
                }
            }

            for (int floorIndex = 1; runConfiguration.TryGetFloorConfig(floorIndex, out FloorConfig floorConfig); floorIndex++)
            {
                AppendFloorConfigCollectionEntries(floorConfig, registeredIds);
            }
        }

        private void AppendFloorConfigCollectionEntries(FloorConfig floorConfig, HashSet<string> registeredIds)
        {
            if (floorConfig == null || registeredIds == null)
            {
                return;
            }

            AppendEnemyPoolCollectionEntries(floorConfig.EnemyPool, registeredIds);
            AppendItemPoolCollectionEntries(floorConfig.TreasureRoomItemPool, registeredIds);
            AppendItemPoolCollectionEntries(floorConfig.ChallengeRoomItemPool, registeredIds);
            AppendItemPoolCollectionEntries(floorConfig.ShopRoomItemPool, registeredIds);
            AppendItemPoolCollectionEntries(floorConfig.BossRewardItemPool, registeredIds);
            AppendItemPoolCollectionEntries(floorConfig.SecretRoomItemPool, registeredIds);
            AppendItemPoolCollectionEntries(floorConfig.CurseRoomItemPool, registeredIds);
        }

        private void AppendItemPoolCollectionEntries(ItemPoolData itemPool, HashSet<string> registeredIds)
        {
            if (itemPool == null || registeredIds == null)
            {
                return;
            }

            IReadOnlyList<ItemPoolEntry> entries = itemPool.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                TryAddCollectionEntry(entries[index].ItemData, registeredIds);
            }
        }

        private void AppendEnemyPoolCollectionEntries(EnemyPoolData enemyPool, HashSet<string> registeredIds)
        {
            if (enemyPool == null || registeredIds == null)
            {
                return;
            }

            AppendEnemySpawnEntries(enemyPool.NormalEnemies, registeredIds);
            AppendEnemySpawnEntries(enemyPool.EliteEnemies, registeredIds);
            AppendEnemySpawnEntries(enemyPool.BossEnemies, registeredIds);
        }

        private void AppendEnemySpawnEntries(IReadOnlyList<EnemySpawnEntry> entries, HashSet<string> registeredIds)
        {
            if (entries == null || registeredIds == null)
            {
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                TryAddCollectionEntry(entries[index], registeredIds);
            }
        }

        private void AppendDebugCatalogCollectionEntries(HashSet<string> registeredIds)
        {
            if (registeredIds == null)
            {
                return;
            }

            DevelopmentDebugCatalog[] debugCatalogs = Resources.LoadAll<DevelopmentDebugCatalog>("Debug");
            for (int catalogIndex = 0; catalogIndex < debugCatalogs.Length; catalogIndex++)
            {
                DevelopmentDebugCatalog debugCatalog = debugCatalogs[catalogIndex];
                if (debugCatalog == null)
                {
                    continue;
                }

                IReadOnlyList<ItemData> grantableItems = debugCatalog.GrantableItems;
                if (grantableItems != null)
                {
                    for (int itemIndex = 0; itemIndex < grantableItems.Count; itemIndex++)
                    {
                        TryAddCollectionEntry(grantableItems[itemIndex], registeredIds);
                    }
                }

                IReadOnlyList<EnemyController> enemyPrefabs = debugCatalog.TestSpawnEnemyPrefabs;
                if (enemyPrefabs != null)
                {
                    for (int enemyIndex = 0; enemyIndex < enemyPrefabs.Count; enemyIndex++)
                    {
                        TryAddCollectionEntry(enemyPrefabs[enemyIndex], registeredIds);
                    }
                }

                TryAddCollectionEntry(debugCatalog.BossPrefab, registeredIds);
            }
        }

        private void AppendResourceCollectionEntries(HashSet<string> registeredIds)
        {
            if (registeredIds == null)
            {
                return;
            }

            ItemData[] itemDataAssets = Resources.LoadAll<ItemData>(string.Empty);
            for (int index = 0; index < itemDataAssets.Length; index++)
            {
                TryAddCollectionEntry(itemDataAssets[index], registeredIds);
            }

            ActiveItemData[] activeItemAssets = Resources.LoadAll<ActiveItemData>(string.Empty);
            for (int index = 0; index < activeItemAssets.Length; index++)
            {
                TryAddCollectionEntry(activeItemAssets[index], registeredIds);
            }
        }

        private void TryAddCollectionEntry(ItemData itemData, HashSet<string> registeredIds)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.ItemId) || registeredIds == null)
            {
                return;
            }

            if (!registeredIds.Add(itemData.ItemId))
            {
                return;
            }

            _runtimeCollectionEntries.Add(new MetaCollectionEntry
            {
                Id = itemData.ItemId,
                DisplayName = string.IsNullOrWhiteSpace(itemData.DisplayName) ? itemData.ItemId : itemData.DisplayName,
                Description = itemData.Description,
                Kind = itemData.IsWeaponRelic || itemData.ItemType == ItemType.Weapon
                    ? MetaCollectionEntryKind.Weapon
                    : MetaCollectionEntryKind.PassiveItem,
                UnlockKey = itemData.UnlockKey,
                UnlockedByDefault = itemData.UnlockedByDefault
            });
        }

        private void TryAddCollectionEntry(ActiveItemData activeItemData, HashSet<string> registeredIds)
        {
            if (activeItemData == null || string.IsNullOrWhiteSpace(activeItemData.ItemId) || registeredIds == null)
            {
                return;
            }

            if (!registeredIds.Add(activeItemData.ItemId))
            {
                return;
            }

            _runtimeCollectionEntries.Add(new MetaCollectionEntry
            {
                Id = activeItemData.ItemId,
                DisplayName = string.IsNullOrWhiteSpace(activeItemData.DisplayName) ? activeItemData.ItemId : activeItemData.DisplayName,
                Description = activeItemData.Description,
                Kind = MetaCollectionEntryKind.ActiveItem,
                UnlockedByDefault = true
            });
        }

        private void TryAddCollectionEntry(EnemySpawnEntry enemyEntry, HashSet<string> registeredIds)
        {
            if (enemyEntry == null || registeredIds == null)
            {
                return;
            }

            string enemyId = !string.IsNullOrWhiteSpace(enemyEntry.EnemyId)
                ? enemyEntry.EnemyId
                : (enemyEntry.EnemyPrefab != null ? enemyEntry.EnemyPrefab.EnemyId : string.Empty);

            if (string.IsNullOrWhiteSpace(enemyId) || !registeredIds.Add(enemyId))
            {
                return;
            }

            _runtimeCollectionEntries.Add(new MetaCollectionEntry
            {
                Id = enemyId,
                DisplayName = enemyEntry.EnemyPrefab != null
                    ? ResolvePrefabDisplayName(enemyEntry.EnemyPrefab.gameObject)
                    : enemyId,
                Description = $"{enemyEntry.EncounterTier} enemy. Cost {Mathf.Max(1, enemyEntry.DifficultyCost)}.",
                Kind = MetaCollectionEntryKind.Enemy
            });
        }

        private void TryAddCollectionEntry(EnemyController enemyController, HashSet<string> registeredIds)
        {
            if (enemyController == null || registeredIds == null)
            {
                return;
            }

            string enemyId = enemyController.EnemyId;
            if (string.IsNullOrWhiteSpace(enemyId) || !registeredIds.Add(enemyId))
            {
                return;
            }

            _runtimeCollectionEntries.Add(new MetaCollectionEntry
            {
                Id = enemyId,
                DisplayName = ResolvePrefabDisplayName(enemyController.gameObject),
                Description = "Encountered enemy.",
                Kind = MetaCollectionEntryKind.Enemy
            });
        }

        private static string ResolvePrefabDisplayName(GameObject gameObject)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(gameObject.name))
            {
                return "Enemy";
            }

            return gameObject.name.Replace("(Clone)", string.Empty).Trim();
        }

        private static StringBuilder ResolveItemBuilder(
            MetaCollectionEntryKind kind,
            StringBuilder passives,
            StringBuilder actives,
            StringBuilder weapons)
        {
            return kind switch
            {
                MetaCollectionEntryKind.ActiveItem => actives,
                MetaCollectionEntryKind.Weapon => weapons,
                _ => passives
            };
        }

        private static bool IsCollectionEntryUnlocked(MetaCollectionEntry entry, UnlockSaveData unlocks)
        {
            if (entry == null || entry.UnlockedByDefault || string.IsNullOrWhiteSpace(entry.UnlockKey))
            {
                return true;
            }

            return ContainsId(unlocks?.UnlockedKeys, entry.UnlockKey);
        }

        private static void AppendCollectionEntry(StringBuilder builder, MetaCollectionEntry entry, bool discovered, int killCount, bool unlocked)
        {
            if (builder == null || entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            if (!discovered)
            {
                builder.Append(unlocked ? "???" : "LOCKED ???");
                builder.Append("  [");
                builder.Append(entry.Id);
                builder.Append(']');
                if (!unlocked && !string.IsNullOrWhiteSpace(entry.UnlockKey))
                {
                    builder.Append("  ");
                    builder.Append(ResolveUnlockLabel(entry.UnlockKey));
                }
                return;
            }

            builder.Append(string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.Id : entry.DisplayName);

            if (entry.Kind == MetaCollectionEntryKind.Enemy)
            {
                builder.Append("  Kills ");
                builder.Append(Mathf.Max(0, killCount));
            }

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                builder.Append('\n');
                builder.Append(entry.Description);
            }
        }

        private static void AppendRawIds(StringBuilder builder, List<string> ids)
        {
            if (builder == null || ids == null)
            {
                return;
            }

            for (int index = 0; index < ids.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(ids[index]))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(ids[index]);
            }
        }

        private static string BuildSectionText(StringBuilder builder)
        {
            return builder != null && builder.Length > 0 ? builder.ToString() : "None";
        }

        private static string ResolveAchievementProgress(AchievementData achievement, MetaProgressionSaveData progression)
        {
            if (achievement == null || progression == null)
            {
                return "0/1";
            }

            int required = achievement.RequiredCount;
            int current = achievement.ConditionType switch
            {
                AchievementConditionType.TotalRuns => progression.TotalRuns,
                AchievementConditionType.TotalWins => progression.TotalWins,
                AchievementConditionType.ReachFloor => progression.BestFloor,
                AchievementConditionType.RoomTypeClearCount => GetCounterValue(progression.RoomTypeClearCounts, achievement.RequiredId),
                AchievementConditionType.EnemySeen => ContainsId(progression.SeenEnemyIds, achievement.RequiredId) ? 1 : 0,
                AchievementConditionType.EnemyKillCount => GetCounterValue(progression.EnemyKillCounts, achievement.RequiredId),
                AchievementConditionType.ItemDiscovered => ContainsId(progression.DiscoveredItemIds, achievement.RequiredId) ? 1 : 0,
                AchievementConditionType.CharacterWin => GetCharacterWins(progression.CharacterRecords, achievement.RequiredId),
                AchievementConditionType.CharacterClearMark => HasAnyCharacterClearMark(progression.CharacterRecords, achievement.RequiredId) ? 1 : 0,
                AchievementConditionType.BossClearCount => progression.TotalBossRoomsCleared,
                AchievementConditionType.NoHitRoomClearCount => progression.NoHitCombatRoomClears,
                AchievementConditionType.BossClearWithoutBombCount => progression.BossClearsWithoutBombs,
                AchievementConditionType.SpecialRewardOfferCount => GetCounterValue(progression.SpecialRewardOfferCounts, achievement.RequiredId),
                AchievementConditionType.SpecialDealOfferCount => GetCounterValue(progression.SpecialDealOfferCounts, achievement.RequiredId),
                AchievementConditionType.SpecialDealPurchaseCount => GetCounterValue(progression.SpecialDealPurchaseCounts, achievement.RequiredId),
                AchievementConditionType.SpecialDealDeclineCount => GetCounterValue(progression.SpecialDealDeclineCounts, achievement.RequiredId),
                AchievementConditionType.ChallengeClearRankCount => GetCounterValue(progression.ChallengeClearRankCounts, achievement.RequiredId),
                AchievementConditionType.ChallengePressureTierCount => GetCounterValue(progression.ChallengePressureTierCounts, achievement.RequiredId),
                AchievementConditionType.TotalEnemyKills => progression.TotalEnemyKills,
                AchievementConditionType.CurrentWinStreak => progression.CurrentWinStreak,
                AchievementConditionType.BestWinStreak => progression.BestWinStreak,
                _ => 0
            };

            return $"{Mathf.Min(current, required)}/{required}";
        }

        private static string ResolveAchievementCompletedAtLabel(MetaProgressionSaveData progression, string achievementId)
        {
            if (progression?.CompletedAchievements == null || string.IsNullOrWhiteSpace(achievementId))
            {
                return string.Empty;
            }

            for (int index = 0; index < progression.CompletedAchievements.Count; index++)
            {
                AchievementCompletionRecord record = progression.CompletedAchievements[index];
                if (record == null
                    || !string.Equals(record.AchievementId, achievementId, StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(record.CompletedAtUtc))
                {
                    continue;
                }

                if (DateTime.TryParse(record.CompletedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime completedAt))
                {
                    return completedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }

                return record.CompletedAtUtc;
            }

            return string.Empty;
        }

        private static string FormatCharacterRecords(List<CharacterProgressionRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return "None";
            }

            StringBuilder builder = new();

            for (int index = 0; index < records.Count; index++)
            {
                CharacterProgressionRecord record = records[index];
                if (record == null || string.IsNullOrWhiteSpace(record.CharacterId))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(record.CharacterId);
                builder.Append(": ");
                builder.Append(record.Wins);
                builder.Append("W/");
                builder.Append(record.Defeats);
                builder.Append("D, runs ");
                builder.Append(record.Runs);
                builder.Append(", best floor ");
                builder.Append(record.BestFloor);
                builder.Append(", kills ");
                builder.Append(record.EnemyKills);
                builder.Append(", streak ");
                builder.Append(record.CurrentWinStreak);
                builder.Append("/");
                builder.Append(record.BestWinStreak);
                builder.Append(", marks ");
                builder.Append(record.ClearMarks != null && record.ClearMarks.Count > 0 ? string.Join(", ", record.ClearMarks) : "none");
            }

            return builder.Length > 0 ? builder.ToString() : "None";
        }

        private static CharacterProgressionRecord FindCharacterRecord(MetaProgressionSaveData progression, string characterId)
        {
            if (progression?.CharacterRecords == null || string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }

            for (int index = 0; index < progression.CharacterRecords.Count; index++)
            {
                CharacterProgressionRecord record = progression.CharacterRecords[index];
                if (record != null && string.Equals(record.CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    return record;
                }
            }

            return null;
        }

        private static string FormatClearMarkSummary(CharacterProgressionRecord record)
        {
            if (record?.ClearMarks == null || record.ClearMarks.Count == 0)
            {
                return "Marks\nBoss: [ ] F1  [ ] F2  [ ] Final  [ ] Hard\nRoutes: [ ] Challenge  [ ] Secret\nDeals: [ ] Devil  [ ] Angel\nChallenge: [ ] S  [ ] A  [ ] Deadly";
            }

            StringBuilder builder = new();
            builder.Append("Marks");
            builder.Append('\n');
            builder.Append("Boss: ");
            AppendClearMarkToken(builder, record.ClearMarks, "floor_1_boss", "F1");
            builder.Append("  ");
            AppendClearMarkToken(builder, record.ClearMarks, "floor_2_boss", "F2");
            builder.Append("  ");
            AppendClearMarkToken(builder, record.ClearMarks, "final_boss", "Final");
            builder.Append("  ");
            AppendClearMarkToken(builder, record.ClearMarks, "hard_victory", "Hard");
            builder.Append('\n');
            builder.Append("Routes: ");
            AppendClearMarkToken(builder, record.ClearMarks, "challenge_room", "Challenge");
            builder.Append("  ");
            AppendClearMarkToken(builder, record.ClearMarks, "secret_route", "Secret");
            builder.Append('\n');
            builder.Append("Deals: ");
            AppendAnyClearMarkToken(builder, record.ClearMarks, "Devil", "deal_devil", "deal_devil_accepted", "deal_devil_declined");
            builder.Append("  ");
            AppendAnyClearMarkToken(builder, record.ClearMarks, "Angel", "deal_angel", "deal_angel_accepted");
            builder.Append('\n');
            builder.Append("Challenge: ");
            AppendClearMarkToken(builder, record.ClearMarks, "challenge_rank_s", "S");
            builder.Append("  ");
            AppendClearMarkToken(builder, record.ClearMarks, "challenge_rank_a", "A");
            builder.Append("  ");
            AppendClearMarkToken(builder, record.ClearMarks, "challenge_pressure_deadly", "Deadly");

            return builder.ToString();
        }

        private static void AppendClearMarkLabel(StringBuilder builder, List<string> marks, string markId, string label)
        {
            if (builder == null || marks == null || string.IsNullOrWhiteSpace(markId))
            {
                return;
            }

            if (!ContainsId(marks, markId))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(label);
        }

        private static void AppendClearMarkToken(StringBuilder builder, List<string> marks, string markId, string label)
        {
            if (builder == null)
            {
                return;
            }

            builder.Append(ContainsId(marks, markId) ? "[x] " : "[ ] ");
            builder.Append(label);
        }

        private static void AppendAnyClearMarkToken(StringBuilder builder, List<string> marks, string label, params string[] markIds)
        {
            if (builder == null)
            {
                return;
            }

            bool hasMark = false;
            if (markIds != null)
            {
                for (int index = 0; index < markIds.Length; index++)
                {
                    if (ContainsId(marks, markIds[index]))
                    {
                        hasMark = true;
                        break;
                    }
                }
            }

            builder.Append(hasMark ? "[x] " : "[ ] ");
            builder.Append(label);
        }

        private static int GetCharacterWins(List<CharacterProgressionRecord> records, string characterId)
        {
            if (records == null || string.IsNullOrWhiteSpace(characterId))
            {
                return 0;
            }

            for (int index = 0; index < records.Count; index++)
            {
                CharacterProgressionRecord record = records[index];
                if (record != null && string.Equals(record.CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(0, record.Wins);
                }
            }

            return 0;
        }

        private static bool HasAnyCharacterClearMark(List<CharacterProgressionRecord> records, string clearMark)
        {
            if (records == null || string.IsNullOrWhiteSpace(clearMark))
            {
                return false;
            }

            for (int index = 0; index < records.Count; index++)
            {
                CharacterProgressionRecord record = records[index];
                if (record?.ClearMarks != null && ContainsId(record.ClearMarks, clearMark))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCharacterClearMark(List<CharacterProgressionRecord> records, string characterId, string clearMark)
        {
            if (records == null || string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            string resolvedMark = string.IsNullOrWhiteSpace(clearMark) ? "run_victory" : clearMark;
            for (int index = 0; index < records.Count; index++)
            {
                CharacterProgressionRecord record = records[index];
                if (record == null || !string.Equals(record.CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return ContainsId(record.ClearMarks, resolvedMark);
            }

            return false;
        }

        private static int GetCounterValue(List<MetaProgressionCounterRecord> records, string id)
        {
            if (records == null || string.IsNullOrWhiteSpace(id))
            {
                return 0;
            }

            for (int index = 0; index < records.Count; index++)
            {
                MetaProgressionCounterRecord record = records[index];
                if (record != null && string.Equals(record.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(0, record.Count);
                }
            }

            return 0;
        }

        private static bool ContainsId(List<string> values, string id)
        {
            if (values == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string MetaSavePath => Path.Combine(Application.persistentDataPath, MetaSaveFileName);
        private string RunSavePath => Path.Combine(Application.persistentDataPath, RunSaveFileName);

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateBackground(Transform parent)
        {
            RectTransform background = CreatePanel("Backdrop", parent, new Color(0.18f, 0.2f, 0.22f, 1f));
            Stretch(background, Vector2.zero, Vector2.one);
        }

        private static void AddVerticalLayout(RectTransform root, int padding, float spacing)
        {
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static RectTransform CreateScrollContent(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out ScrollRect scrollRect)
        {
            RectTransform root = CreatePanel(name, parent, new Color(0.13f, 0.13f, 0.14f, 0.9f));
            Anchor(root, anchorMin, anchorMax);

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 36f;

            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(root, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, Vector2.zero, Vector2.one);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            scroll.viewport = viewport;
            scroll.content = content;
            scrollRect = scroll;
            return content;
        }

        private static Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredHeight = 58f;
            element.minHeight = 48f;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.27f, 0.32f, 0.36f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Text text = CreateText("Label", buttonObject.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static Text CreateBodyText(RectTransform parent, string value, int size = 22, FontStyle style = FontStyle.Normal)
        {
            Text text = CreateText("Text", parent, value, size, style, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 0.96f, 1f));
            LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(52f, size * Mathf.Max(3.2f, CountLines(value) * 1.35f));
            return text;
        }

        private static int CountLines(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 1;
            }

            int count = 1;

            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] == '\n')
                {
                    count++;
                }
            }

            return count;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.color = color;
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                alignment,
                style,
                horizontalOverflow: HorizontalWrapMode.Wrap,
                verticalOverflow: VerticalWrapMode.Truncate);
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return panelObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            Anchor(rectTransform, anchorMin, anchorMax);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
