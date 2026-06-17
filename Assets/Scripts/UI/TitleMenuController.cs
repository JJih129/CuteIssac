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
        private const string GameplaySceneName = "SampleScene";
        private const string SelectedCharacterPrefKey = "meta.selected_character";
        private const string HardModePrefKey = "meta.hard_mode";
        private const string MetaSaveFileName = "meta-save.json";
        private const string RunSaveFileName = "run-save.json";

        [Header("Replaceable Title Art")]
        [Tooltip("타이틀 배경 스프라이트입니다. 비워두면 단색 배경을 사용합니다.")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("타이틀 로고 스프라이트입니다. 비워두면 텍스트 타이틀을 사용합니다.")]
        [SerializeField] private Sprite titleLogoSprite;
        [Tooltip("주 메뉴 옆에 표시되는 키아트입니다. 작업자가 코드 수정 없이 교체할 수 있습니다.")]
        [SerializeField] private Sprite keyArtSprite;
        [Tooltip("타이틀 전체 외곽 패널에 사용할 스프라이트입니다.")]
        [SerializeField] private Sprite shellSprite;
        [Tooltip("메뉴와 내용 패널에 공통으로 사용할 스프라이트입니다.")]
        [SerializeField] private Sprite panelSprite;
        [Tooltip("일반 메뉴 버튼에 사용할 스프라이트입니다.")]
        [SerializeField] private Sprite buttonSprite;
        [Tooltip("비활성화 버튼에 사용할 스프라이트입니다.")]
        [SerializeField] private Sprite disabledButtonSprite;

        [Header("Scene Layout References")]
        [Tooltip("씬에 정적으로 배치된 타이틀 Canvas입니다. 런타임에서 생성하지 않습니다.")]
        [SerializeField] private Canvas titleCanvas;
        [SerializeField] private CanvasScaler sceneCanvasScaler;
        [SerializeField] private RectTransform sceneShellRoot;
        [SerializeField] private RectTransform sceneContentRoot;
        [SerializeField] private ScrollRect sceneContentScrollRect;
        [SerializeField] private Text sceneTitleText;

        [Header("Scene Art Targets")]
        [SerializeField] private Image backdropImage;
        [SerializeField] private Image titleLogoImage;
        [SerializeField] private Image keyArtImage;
        [SerializeField] private Image shellImage;
        [SerializeField] private Image primaryActionsImage;
        [SerializeField] private Image contentPanelImage;
        [SerializeField] private Image utilityBarImage;

        [Header("Scene Buttons")]
        [SerializeField] private Button newRunButton;
        [SerializeField] private Button sceneContinueButton;
        [SerializeField] private Button characterButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TitleMenuButtonId[] menuButtonIds = Array.Empty<TitleMenuButtonId>();

        [Header("Scene Data Catalogs")]
        [Tooltip("타이틀 컬렉션/도감에 포함할 콘텐츠 소스 목록입니다. 비워두면 Resources/TitleCollectionSourceCatalog를 사용합니다.")]
        [SerializeField] private TitleCollectionSourceCatalog collectionSourceCatalog;

        private RectTransform _contentRoot;
        private Text _titleText;
        private CanvasScaler _canvasScaler;
        private ScrollRect _contentScrollRect;
        private RectTransform _shellRoot;
        private Button _continueButton;
        private readonly Dictionary<TitleMenuButtonAction, Button> _menuButtons = new();
        private CharacterProfileCatalog _characterCatalog;
        private MetaCollectionCatalog _collectionCatalog;
        private TitleCollectionSourceCatalog _collectionSourceCatalog;
        private readonly List<MetaCollectionEntry> _runtimeCollectionEntries = new();
        private MetaSaveData _metaSaveData;
        private string _selectedCharacterId = "default";
        private bool _hardModeSelected;
        private bool _resetArmed;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _selectedCharacterId = PlayerPrefs.GetString(SelectedCharacterPrefKey, "default");
            _hardModeSelected = PlayerPrefs.GetInt(HardModePrefKey, 0) == 1;
            _characterCatalog = Resources.Load<CharacterProfileCatalog>("Characters/DefaultCharacterProfileCatalog");
            _collectionCatalog = Resources.Load<MetaCollectionCatalog>("MetaCollectionCatalog");
            _collectionSourceCatalog = collectionSourceCatalog != null
                ? collectionSourceCatalog
                : Resources.Load<TitleCollectionSourceCatalog>("TitleCollectionSourceCatalog");
            RebuildRuntimeCollectionEntries();
            LoadMeta();
            if (!BindStaticSceneLayout())
            {
                enabled = false;
                return;
            }

            ApplySceneArt();
            WireSceneButtons();
            ApplyOptions();
            ShowHome();
        }

        private bool BindStaticSceneLayout()
        {
            _canvasScaler = sceneCanvasScaler;
            _shellRoot = sceneShellRoot;
            _contentRoot = sceneContentRoot;
            _contentScrollRect = sceneContentScrollRect;
            _titleText = sceneTitleText;
            CacheMenuButtonIds();
            newRunButton = ResolveMenuButton(TitleMenuButtonAction.NewRun, newRunButton);
            sceneContinueButton = ResolveMenuButton(TitleMenuButtonAction.Continue, sceneContinueButton);
            characterButton = ResolveMenuButton(TitleMenuButtonAction.Character, characterButton);
            optionsButton = ResolveMenuButton(TitleMenuButtonAction.Options, optionsButton);
            collectionButton = ResolveMenuButton(TitleMenuButtonAction.Collection, collectionButton);
            achievementsButton = ResolveMenuButton(TitleMenuButtonAction.Achievements, achievementsButton);
            statsButton = ResolveMenuButton(TitleMenuButtonAction.Stats, statsButton);
            creditsButton = ResolveMenuButton(TitleMenuButtonAction.Credits, creditsButton);
            resetButton = ResolveMenuButton(TitleMenuButtonAction.ResetData, resetButton);
            quitButton = ResolveMenuButton(TitleMenuButtonAction.Quit, quitButton);
            _continueButton = sceneContinueButton;

            bool isValid = titleCanvas != null
                && _canvasScaler != null
                && _shellRoot != null
                && _contentRoot != null
                && _contentScrollRect != null
                && _titleText != null
                && newRunButton != null
                && _continueButton != null
                && characterButton != null
                && optionsButton != null
                && collectionButton != null
                && achievementsButton != null
                && statsButton != null
                && creditsButton != null
                && resetButton != null
                && quitButton != null;

            if (!isValid)
            {
                Debug.LogError(
                    "TitleMenuController requires scene-authored UI references. Rebuild TitleScene via CuteIssac/Title/Rebuild Static Title Scene UI, then verify the Inspector references.",
                    this);
            }

            return isValid;
        }

        private void CacheMenuButtonIds()
        {
            _menuButtons.Clear();

            if ((menuButtonIds == null || menuButtonIds.Length == 0) && titleCanvas != null)
            {
                menuButtonIds = titleCanvas.GetComponentsInChildren<TitleMenuButtonId>(true);
            }

            if (menuButtonIds == null)
            {
                return;
            }

            for (int index = 0; index < menuButtonIds.Length; index++)
            {
                TitleMenuButtonId buttonId = menuButtonIds[index];
                if (buttonId == null || buttonId.Button == null)
                {
                    continue;
                }

                _menuButtons[buttonId.ActionId] = buttonId.Button;
            }
        }

        private Button ResolveMenuButton(TitleMenuButtonAction actionId, Button fallback)
        {
            return _menuButtons.TryGetValue(actionId, out Button button) && button != null
                ? button
                : fallback;
        }

        private void ApplySceneArt()
        {
            ApplyPanelImage(backdropImage, "Backdrop", new Color(0.18f, 0.2f, 0.22f, 1f), backgroundSprite);
            ApplyPanelImage(shellImage, "TitleShell", new Color(0.09f, 0.095f, 0.105f, 0.96f), shellSprite);
            ApplyPanelImage(primaryActionsImage, "PrimaryActions", new Color(0.125f, 0.14f, 0.155f, 0.92f), panelSprite);
            ApplyPanelImage(contentPanelImage, "ContentScroll", new Color(0.13f, 0.13f, 0.14f, 0.9f), panelSprite);
            ApplyPanelImage(utilityBarImage, "UtilityBar", new Color(0.11f, 0.12f, 0.13f, 0.88f), panelSprite);

            if (titleLogoImage != null)
            {
                titleLogoImage.sprite = titleLogoSprite;
                titleLogoImage.enabled = titleLogoSprite != null;
                titleLogoImage.preserveAspect = true;
                titleLogoImage.raycastTarget = false;
            }

            if (keyArtImage != null)
            {
                keyArtImage.sprite = keyArtSprite;
                keyArtImage.preserveAspect = true;
                keyArtImage.raycastTarget = false;
                keyArtImage.color = keyArtSprite != null ? Color.white : new Color(0.18f, 0.2f, 0.22f, 0.82f);
            }

            ApplyButtonSprite(newRunButton, true);
            ApplyButtonSprite(_continueButton, true);
            ApplyButtonSprite(characterButton, true);
            ApplyButtonSprite(optionsButton, true);
            ApplyButtonSprite(collectionButton, true);
            ApplyButtonSprite(achievementsButton, true);
            ApplyButtonSprite(statsButton, true);
            ApplyButtonSprite(creditsButton, true);
            ApplyButtonSprite(resetButton, true);
            ApplyButtonSprite(quitButton, true);
        }

        private void WireSceneButtons()
        {
            ConfigureButton(TitleMenuButtonAction.NewRun, StartNewRun);
            ConfigureButton(TitleMenuButtonAction.Continue, ContinueRun);
            ConfigureButton(TitleMenuButtonAction.Character, ShowCharacters);
            ConfigureButton(TitleMenuButtonAction.Options, ShowOptions);
            ConfigureButton(TitleMenuButtonAction.Collection, ShowCollection);
            ConfigureButton(TitleMenuButtonAction.Achievements, ShowAchievements);
            ConfigureButton(TitleMenuButtonAction.Stats, ShowStats);
            ConfigureButton(TitleMenuButtonAction.Credits, ShowCredits);
            ConfigureButton(TitleMenuButtonAction.ResetData, ShowResetData);
            ConfigureButton(TitleMenuButtonAction.Quit, QuitGame);
        }

        private void ConfigureButton(TitleMenuButtonAction actionId, UnityEngine.Events.UnityAction action)
        {
            ConfigureButton(ResolveMenuButton(actionId, ResolveSerializedMenuButton(actionId)), action);
        }

        private Button ResolveSerializedMenuButton(TitleMenuButtonAction actionId)
        {
            return actionId switch
            {
                TitleMenuButtonAction.NewRun => newRunButton,
                TitleMenuButtonAction.Continue => _continueButton,
                TitleMenuButtonAction.Character => characterButton,
                TitleMenuButtonAction.Options => optionsButton,
                TitleMenuButtonAction.Collection => collectionButton,
                TitleMenuButtonAction.Achievements => achievementsButton,
                TitleMenuButtonAction.Stats => statsButton,
                TitleMenuButtonAction.Credits => creditsButton,
                TitleMenuButtonAction.ResetData => resetButton,
                TitleMenuButtonAction.Quit => quitButton,
                _ => null
            };
        }

        private static void ConfigureButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void ApplyPanelImage(Image image, string panelName, Color fallbackColor, Sprite overrideSprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = overrideSprite;
            image.color = overrideSprite != null && string.Equals(panelName, "Backdrop", StringComparison.OrdinalIgnoreCase)
                ? Color.white
                : ResolvePanelColor(panelName, fallbackColor);
            image.type = overrideSprite != null && !string.Equals(panelName, "Backdrop", StringComparison.OrdinalIgnoreCase)
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.raycastTarget = !string.Equals(panelName, "Backdrop", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyButtonSprite(Button button, bool interactable)
        {
            if (button == null || button.targetGraphic is not Image image)
            {
                return;
            }

            image.sprite = interactable || disabledButtonSprite == null ? buttonSprite : disabledButtonSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = interactable ? ResolveButtonColor() : new Color(0.12f, 0.13f, 0.14f, 0.9f);
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
            RectTransform characterGrid = CreateCharacterGridRoot(_characterCatalog.Profiles.Count);

            for (int index = 0; index < _characterCatalog.Profiles.Count; index++)
            {
                CharacterProfileData profile = _characterCatalog.Profiles[index];
                if (profile == null)
                {
                    continue;
                }

                bool unlocked = IsCharacterUnlocked(profile);
                CharacterProgressionRecord record = FindCharacterRecord(progression, profile.CharacterId);
                CreateCharacterCard(characterGrid, profile, record, progression, unlockDefinitions, unlocked, profile.CharacterId == _selectedCharacterId);
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

        private RectTransform CreateCharacterGridRoot(int itemCount)
        {
            GameObject gridObject = new("CharacterGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(_contentRoot, false);

            RectTransform gridRoot = gridObject.GetComponent<RectTransform>();
            gridRoot.localScale = Vector3.one;

            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(438f, 302f);
            grid.spacing = new Vector2(16f, 16f);
            grid.childAlignment = TextAnchor.UpperLeft;

            int rows = Mathf.CeilToInt(Mathf.Max(1, itemCount) / 2f);
            LayoutElement layoutElement = gridObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = rows * 302f + Mathf.Max(0, rows - 1) * 16f + 8f;
            layoutElement.minHeight = layoutElement.preferredHeight;
            return gridRoot;
        }

        private void CreateCharacterCard(
            RectTransform parent,
            CharacterProfileData profile,
            CharacterProgressionRecord record,
            MetaProgressionSaveData progression,
            UnlockData[] unlockDefinitions,
            bool unlocked,
            bool selected)
        {
            GameObject cardObject = new(profile.CharacterId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(parent, false);

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.localScale = Vector3.one;

            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = selected
                ? new Color(0.15f, 0.24f, 0.25f, 0.98f)
                : unlocked ? new Color(0.14f, 0.155f, 0.17f, 0.96f) : new Color(0.07f, 0.075f, 0.085f, 0.96f);

            CreateCharacterIcon(cardRect, profile, unlocked);

            string title = unlocked ? ResolveSafeLabel(profile.DisplayName, profile.CharacterId) : "LOCKED";
            Text nameText = CreateCardText(cardRect, "Name", selected ? $"{title}  SELECTED" : title, 20, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            nameText.rectTransform.pivot = new Vector2(0f, 1f);
            nameText.rectTransform.offsetMin = new Vector2(104f, -46f);
            nameText.rectTransform.offsetMax = new Vector2(-14f, -12f);

            string summary = unlocked
                ? BuildCharacterLoadoutSummary(profile)
                : $"Unlock: {ResolveUnlockLabel(profile.UnlockKey)}\n{ResolveUnlockProgressLine(profile.UnlockKey, progression, unlockDefinitions)}";
            Text loadoutText = CreateCardText(cardRect, "Loadout", summary, 13, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.84f, 0.88f, 0.92f, 1f));
            Anchor(loadoutText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            loadoutText.rectTransform.pivot = new Vector2(0f, 1f);
            loadoutText.rectTransform.offsetMin = new Vector2(14f, -140f);
            loadoutText.rectTransform.offsetMax = new Vector2(-14f, -78f);

            Text recordText = CreateCardText(cardRect, "Record", BuildCharacterRecordSummary(record), 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.62f, 0.96f, 1f, 1f));
            Anchor(recordText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            recordText.rectTransform.pivot = new Vector2(0f, 1f);
            recordText.rectTransform.offsetMin = new Vector2(14f, -198f);
            recordText.rectTransform.offsetMax = new Vector2(-14f, -148f);

            Text markText = CreateCardText(cardRect, "Marks", BuildCompactClearMarkSummary(record), 12, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.86f, 0.9f, 0.94f, 1f));
            Anchor(markText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            markText.rectTransform.pivot = new Vector2(0f, 1f);
            markText.rectTransform.offsetMin = new Vector2(14f, -244f);
            markText.rectTransform.offsetMax = new Vector2(-14f, -202f);

            Button selectButton = CreateCardButton(cardRect, unlocked ? selected ? "Selected" : "Select" : "Locked", unlocked && !selected);
            if (unlocked && !selected)
            {
                string id = profile.CharacterId;
                selectButton.onClick.AddListener(() => SelectCharacter(id));
            }
        }

        private static void CreateCharacterIcon(RectTransform parent, CharacterProfileData profile, bool unlocked)
        {
            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(14f, -16f);
            iconRect.sizeDelta = new Vector2(76f, 76f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = unlocked ? profile.Icon : null;
            iconImage.preserveAspect = true;
            iconImage.color = unlocked && profile.Icon != null ? Color.white : new Color(0.18f, 0.19f, 0.2f, 1f);

            if (!unlocked || profile.Icon == null)
            {
                Text iconText = CreateCardText(iconRect, "IconText", unlocked ? "?" : "???", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                Stretch(iconText.rectTransform, Vector2.zero, Vector2.one);
            }
        }

        private static Button CreateCardButton(RectTransform parent, string label, bool interactable)
        {
            GameObject buttonObject = new("SelectButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            Anchor(buttonRect, new Vector2(1f, 0f), new Vector2(1f, 0f));
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-14f, 14f);
            buttonRect.sizeDelta = new Vector2(128f, 40f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = interactable ? ResolveButtonColor() : new Color(0.12f, 0.13f, 0.14f, 0.9f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;

            Text text = CreateCardText(buttonRect, "Label", label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static string BuildCharacterLoadoutSummary(CharacterProfileData profile)
        {
            if (profile == null)
            {
                return "No profile.";
            }

            string weapon = profile.StartingWeaponItem != null ? profile.StartingWeaponItem.DisplayName : "Default weapon";
            string active = profile.StartingActiveItem != null ? profile.StartingActiveItem.DisplayName : "No active";
            int passiveCount = profile.StartingPassiveItems != null ? profile.StartingPassiveItems.Count : 0;
            return $"Coins {profile.StartingCoins}  Keys {profile.StartingKeys}  Bombs {profile.StartingBombs}\nWeapon: {weapon}\nActive: {active}  Passive x{passiveCount}";
        }

        private static string BuildCharacterRecordSummary(CharacterProgressionRecord record)
        {
            if (record == null)
            {
                return "Runs 0  Wins 0  Best F0  Streak 0/0";
            }

            return $"Runs {record.Runs}  Wins {record.Wins}  Best F{record.BestFloor}  Streak {record.CurrentWinStreak}/{record.BestWinStreak}";
        }

        private static string BuildCompactClearMarkSummary(CharacterProgressionRecord record)
        {
            if (record?.ClearMarks == null || record.ClearMarks.Count == 0)
            {
                return "F1 [ ]  F2 [ ]  Final [ ]  Hard [ ]";
            }

            return $"F1 {MarkToken(record.ClearMarks, "floor_1_boss")}  F2 {MarkToken(record.ClearMarks, "floor_2_boss")}  Final {MarkToken(record.ClearMarks, "final_boss")}  Hard {MarkToken(record.ClearMarks, "hard_victory")}\nChallenge {MarkToken(record.ClearMarks, "challenge_room")}  Devil {AnyMarkToken(record.ClearMarks, "deal_devil", "deal_devil_accepted", "deal_devil_declined")}  Angel {AnyMarkToken(record.ClearMarks, "deal_angel", "deal_angel_accepted")}";
        }

        private static string MarkToken(List<string> marks, string markId)
        {
            return ContainsId(marks, markId) ? "[x]" : "[ ]";
        }

        private static string AnyMarkToken(List<string> marks, params string[] markIds)
        {
            if (markIds != null)
            {
                for (int index = 0; index < markIds.Length; index++)
                {
                    if (ContainsId(marks, markIds[index]))
                    {
                        return "[x]";
                    }
                }
            }

            return "[ ]";
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
            List<MetaCollectionEntry> weapons = new();
            List<MetaCollectionEntry> actives = new();
            List<MetaCollectionEntry> passives = new();
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

                    ResolveItemList(entry.Kind, passives, actives, weapons).Add(entry);
                }
            }

            if (knownItems == 0 && progression.DiscoveredItemIds.Count > 0)
            {
                StringBuilder rawIds = new();
                AppendRawIds(rawIds, progression.DiscoveredItemIds);
                knownItems = progression.DiscoveredItemIds.Count;
                discoveredItems = progression.DiscoveredItemIds.Count;
                CreateBodyText(_contentRoot, $"Items {discoveredItems}/{Mathf.Max(knownItems, discoveredItems)}", 28, FontStyle.Bold);
                CreateBodyText(_contentRoot, BuildSectionText(rawIds), 18);
                ResetContentScroll();
                return;
            }

            CreateBodyText(_contentRoot, $"Items {discoveredItems}/{Mathf.Max(knownItems, discoveredItems)}", 28, FontStyle.Bold);
            CreateCollectionSection("Weapons", weapons, progression, false);
            CreateCollectionSection("Actives", actives, progression, false);
            CreateCollectionSection("Passives", passives, progression, false);
            ResetContentScroll();
        }

        private void ShowEnemyCodexBody()
        {
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            List<MetaCollectionEntry> enemies = new();
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

                    enemies.Add(entry);
                }
            }

            if (knownEnemies == 0 && progression.SeenEnemyIds.Count > 0)
            {
                StringBuilder rawIds = new();
                AppendRawIds(rawIds, progression.SeenEnemyIds);
                knownEnemies = progression.SeenEnemyIds.Count;
                seenEnemies = progression.SeenEnemyIds.Count;
                CreateBodyText(_contentRoot, $"Enemy Codex {seenEnemies}/{Mathf.Max(knownEnemies, seenEnemies)}", 28, FontStyle.Bold);
                CreateBodyText(_contentRoot, BuildSectionText(rawIds), 18);
                ResetContentScroll();
                return;
            }

            CreateBodyText(_contentRoot, $"Enemy Codex {seenEnemies}/{Mathf.Max(knownEnemies, seenEnemies)}", 28, FontStyle.Bold);
            CreateCollectionSection("Enemies", enemies, progression, true);
            ResetContentScroll();
        }

        private void ShowAchievements()
        {
            LoadMeta();
            SetTitle("Achievements");
            ClearContent();
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();
            AchievementData[] achievements = Resources.LoadAll<AchievementData>("Achievements");
            List<AchievementData> completedAchievements = new();
            List<AchievementData> pendingAchievements = new();
            int completedCount = 0;
            int validCount = 0;

            for (int index = 0; index < achievements.Length; index++)
            {
                AchievementData achievement = achievements[index];
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.AchievementId))
                {
                    continue;
                }

                validCount++;
                bool completed = ContainsId(progression.CompletedAchievementIds, achievement.AchievementId);
                if (completed)
                {
                    completedCount++;
                    completedAchievements.Add(achievement);
                }
                else
                {
                    pendingAchievements.Add(achievement);
                }
            }

            CreateBodyText(_contentRoot, $"Achievements {completedCount}/{Mathf.Max(validCount, completedCount)}", 28, FontStyle.Bold);
            if (validCount == 0)
            {
                CreateBodyText(_contentRoot, "No achievement definitions found.", 18);
                ResetContentScroll();
                return;
            }

            CreateAchievementSection("Completed", completedAchievements, progression, true);
            CreateAchievementSection("In Progress", pendingAchievements, progression, false);
            ResetContentScroll();
        }

        private void ShowStats()
        {
            LoadMeta();
            SetTitle("Stats");
            ClearContent();
            MetaProgressionSaveData progression = _metaSaveData?.Progression ?? new MetaProgressionSaveData();

            CreateBodyText(_contentRoot, "Account Summary", 28, FontStyle.Bold);
            RectTransform accountGrid = CreateStatsGridRoot("AccountStats", 9, 3, 132f);
            CreateStatCard(accountGrid, "Runs", progression.TotalRuns.ToString(), $"Win rate {FormatRatioPercent(progression.TotalWins, progression.TotalRuns)}");
            CreateStatCard(accountGrid, "Wins", progression.TotalWins.ToString(), $"Defeats {Mathf.Max(0, progression.TotalDefeats)}");
            CreateStatCard(accountGrid, "Abandons", progression.TotalAbandons.ToString(), "Run exits");
            CreateStatCard(accountGrid, "Best Floor", progression.BestFloor.ToString(), "Highest route reached");
            CreateStatCard(accountGrid, "Play Time", FormatSeconds(progression.TotalRunSeconds), "Total run time");
            CreateStatCard(accountGrid, "Streak", $"{progression.CurrentWinStreak}/{progression.BestWinStreak}", "Current / best");
            CreateStatCard(accountGrid, "Rooms", progression.TotalRoomsCleared.ToString(), $"Resolved {Mathf.Max(0, progression.TotalRoomsResolved)}");
            CreateStatCard(accountGrid, "Boss Clears", progression.TotalBossRoomsCleared.ToString(), $"No-bomb {Mathf.Max(0, progression.BossClearsWithoutBombs)}");
            CreateStatCard(accountGrid, "Kills", progression.TotalEnemyKills.ToString(), $"Seen enemies {progression.SeenEnemyIds.Count}");

            CreateBodyText(_contentRoot, "Resources & Discoveries", 24, FontStyle.Bold);
            RectTransform resourceGrid = CreateStatsGridRoot("ResourceStats", 6, 3, 120f);
            CreateStatCard(resourceGrid, "Coins", progression.TotalCoinsCollected.ToString(), "Collected");
            CreateStatCard(resourceGrid, "Keys", progression.TotalKeysCollected.ToString(), "Collected");
            CreateStatCard(resourceGrid, "Bombs", progression.TotalBombsCollected.ToString(), "Collected");
            CreateStatCard(resourceGrid, "Items", progression.DiscoveredItemIds.Count.ToString(), "Discovered");
            CreateStatCard(resourceGrid, "Achievements", progression.CompletedAchievementIds.Count.ToString(), "Completed");
            CreateStatCard(resourceGrid, "No-Hit Rooms", progression.NoHitCombatRoomClears.ToString(), "Clean clears");

            CreateCounterRecordSection("Enemy Kills", progression.EnemyKillCounts, 6);
            CreateCounterRecordSection("Room Type Clears", progression.RoomTypeClearCounts, 6);
            CreateCounterRecordSection("Challenge Ranks", progression.ChallengeClearRankCounts, 6);
            CreateCounterRecordSection("Challenge Pressure", progression.ChallengePressureTierCounts, 6);
            CreateCounterRecordSection("Special Deals", progression.SpecialDealOfferCounts, 6);
            CreateCounterRecordSection("Special Purchases", progression.SpecialDealPurchaseCounts, 6);
            CreateCharacterStatsSection(progression.CharacterRecords);
            ResetContentScroll();
        }

        private void ShowOptions()
        {
            ClearContent();
            SetTitle("Options");
            GameOptionsData options = ResolveOptions();
            CreateBodyText(_contentRoot, $"Resolution: {options.ResolutionWidth}x{options.ResolutionHeight}\nUI Scale: {options.UiScale:0.00}\nMaster Volume: {Mathf.RoundToInt(options.MasterVolume * 100f)}%\nMusic Volume: {Mathf.RoundToInt(options.MusicVolume * 100f)}%\nSFX Volume: {Mathf.RoundToInt(options.SfxVolume * 100f)}%\nFullscreen: {options.Fullscreen}\nCamera Shake: {FormatOnOff(options.CameraShakeEnabled)}\nDamage Numbers: {FormatOnOff(options.DamageNumbersEnabled)}\nHigh Contrast: {FormatOnOff(options.HighContrastUi)}\nReduce Flashes: {FormatOnOff(options.ReduceFlashes)}\nColor Assist: {FormatOnOff(options.ColorBlindAssist)}");
            CreateButton(_contentRoot, "Resolution 1280x720", () => SetResolution(1280, 720));
            CreateButton(_contentRoot, "Resolution 1600x900", () => SetResolution(1600, 900));
            CreateButton(_contentRoot, "Resolution 1920x1080", () => SetResolution(1920, 1080));
            CreateButton(_contentRoot, "Resolution 2560x1440", () => SetResolution(2560, 1440));
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
            ApplyAccessibilityThemeToOpenMenu(options);
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
            GameOptionsService.ApplyAccessibilityState(options);
            GameOptionsService.ApplyUiScale(_canvasScaler, options.UiScale);
            ApplyAccessibilityThemeToOpenMenu(options);
        }

        private void ApplyAccessibilityThemeToOpenMenu(GameOptionsData options)
        {
            if (_shellRoot == null)
            {
                return;
            }

            GameOptionsService.ApplyAccessibilityState(options);
            Image[] images = _shellRoot.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (image == null)
                {
                    continue;
                }

                if (image.GetComponent<Button>() != null)
                {
                    image.color = ResolveButtonColor();
                }
                else
                {
                    image.color = ResolvePanelColor(image.gameObject.name, image.color);
                }
            }

            Text[] texts = _shellRoot.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                Text text = texts[index];
                if (text != null)
                {
                    text.color = ResolveTextColor(text.gameObject.name, text.color);
                }
            }
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

            ApplyButtonSprite(_continueButton, canContinue);

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

        private RectTransform CreateStatsGridRoot(string name, int itemCount, int columns, float cellHeight)
        {
            GameObject gridObject = new(name, typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(_contentRoot, false);

            RectTransform gridRoot = gridObject.GetComponent<RectTransform>();
            gridRoot.localScale = Vector3.one;

            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, columns);
            grid.cellSize = new Vector2(columns <= 2 ? 438f : 286f, cellHeight);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.UpperLeft;

            int rows = Mathf.CeilToInt(Mathf.Max(1, itemCount) / (float)Mathf.Max(1, columns));
            LayoutElement layoutElement = gridObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = rows * cellHeight + Mathf.Max(0, rows - 1) * 14f + 8f;
            layoutElement.minHeight = layoutElement.preferredHeight;
            return gridRoot;
        }

        private static void CreateStatCard(RectTransform parent, string label, string value, string detail)
        {
            GameObject cardObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(parent, false);

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.localScale = Vector3.one;

            Image image = cardObject.GetComponent<Image>();
            image.color = new Color(0.14f, 0.155f, 0.17f, 0.96f);

            Text labelText = CreateCardText(cardRect, "Label", label, 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.62f, 0.96f, 1f, 1f));
            Anchor(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            labelText.rectTransform.pivot = new Vector2(0f, 1f);
            labelText.rectTransform.offsetMin = new Vector2(14f, -34f);
            labelText.rectTransform.offsetMax = new Vector2(-14f, -10f);

            Text valueText = CreateCardText(cardRect, "Value", string.IsNullOrWhiteSpace(value) ? "0" : value, ResolveStatValueSize(value), FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            Anchor(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            valueText.rectTransform.offsetMin = new Vector2(14f, 38f);
            valueText.rectTransform.offsetMax = new Vector2(-14f, -42f);

            Text detailText = CreateCardText(cardRect, "Detail", string.IsNullOrWhiteSpace(detail) ? " " : detail, 12, FontStyle.Normal, TextAnchor.LowerLeft, new Color(0.78f, 0.82f, 0.86f, 1f));
            Anchor(detailText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
            detailText.rectTransform.pivot = new Vector2(0f, 0f);
            detailText.rectTransform.offsetMin = new Vector2(14f, 10f);
            detailText.rectTransform.offsetMax = new Vector2(-14f, 34f);
        }

        private static int ResolveStatValueSize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 30;
            }

            return value.Length > 12 ? 21 : value.Length > 7 ? 25 : 30;
        }

        private void CreateCounterRecordSection(string title, List<MetaProgressionCounterRecord> records, int maxCards)
        {
            CreateBodyText(_contentRoot, title, 24, FontStyle.Bold);
            int count = CountCounterRecords(records);
            if (count == 0)
            {
                CreateBodyText(_contentRoot, "None", 18);
                return;
            }

            int visibleCount = Mathf.Min(Mathf.Max(1, maxCards), count);
            RectTransform gridRoot = CreateStatsGridRoot($"{title}Stats", visibleCount, 3, 104f);
            int emitted = 0;
            for (int index = 0; index < records.Count && emitted < visibleCount; index++)
            {
                MetaProgressionCounterRecord record = records[index];
                if (record == null || string.IsNullOrWhiteSpace(record.Id))
                {
                    continue;
                }

                CreateStatCard(gridRoot, record.Id, Mathf.Max(0, record.Count).ToString(), "Recorded");
                emitted++;
            }
        }

        private static int CountCounterRecords(List<MetaProgressionCounterRecord> records)
        {
            if (records == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < records.Count; index++)
            {
                if (records[index] != null && !string.IsNullOrWhiteSpace(records[index].Id))
                {
                    count++;
                }
            }

            return count;
        }

        private void CreateCharacterStatsSection(List<CharacterProgressionRecord> records)
        {
            CreateBodyText(_contentRoot, "Characters", 24, FontStyle.Bold);
            int count = CountCharacterRecords(records);
            if (count == 0)
            {
                CreateBodyText(_contentRoot, "None", 18);
                return;
            }

            RectTransform gridRoot = CreateStatsGridRoot("CharacterStats", count, 2, 150f);
            for (int index = 0; index < records.Count; index++)
            {
                CharacterProgressionRecord record = records[index];
                if (record == null || string.IsNullOrWhiteSpace(record.CharacterId))
                {
                    continue;
                }

                string value = $"{record.Wins}W / {record.Defeats}D";
                string detail = $"Runs {record.Runs}  Floor {record.BestFloor}  Kills {record.EnemyKills}  Streak {record.CurrentWinStreak}/{record.BestWinStreak}";
                if (record.ClearMarks != null && record.ClearMarks.Count > 0)
                {
                    detail = $"{detail}\nMarks {string.Join(", ", record.ClearMarks)}";
                }

                CreateStatCard(gridRoot, record.CharacterId, value, detail);
            }
        }

        private static int CountCharacterRecords(List<CharacterProgressionRecord> records)
        {
            if (records == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < records.Count; index++)
            {
                if (records[index] != null && !string.IsNullOrWhiteSpace(records[index].CharacterId))
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatRatioPercent(int value, int total)
        {
            if (total <= 0)
            {
                return "0%";
            }

            return $"{Mathf.RoundToInt(Mathf.Clamp01((float)value / total) * 100f)}%";
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
            if (registeredIds == null || _collectionSourceCatalog == null)
            {
                return;
            }

            IReadOnlyList<BalanceConfig> balanceConfigs = _collectionSourceCatalog.BalanceConfigs;
            for (int index = 0; index < balanceConfigs.Count; index++)
            {
                BalanceConfig balanceConfig = balanceConfigs[index];
                if (balanceConfig != null)
                {
                    AppendRunConfigurationCollectionEntries(balanceConfig.RunConfiguration, registeredIds);
                }
            }

            IReadOnlyList<RunConfiguration> runConfigurations = _collectionSourceCatalog.RunConfigurations;
            for (int index = 0; index < runConfigurations.Count; index++)
            {
                AppendRunConfigurationCollectionEntries(runConfigurations[index], registeredIds);
            }

            IReadOnlyList<FloorConfig> floorConfigs = _collectionSourceCatalog.FloorConfigs;
            for (int index = 0; index < floorConfigs.Count; index++)
            {
                AppendFloorConfigCollectionEntries(floorConfigs[index], registeredIds);
            }

            IReadOnlyList<EnemyPoolData> enemyPools = _collectionSourceCatalog.EnemyPools;
            for (int index = 0; index < enemyPools.Count; index++)
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
            if (registeredIds == null || _collectionSourceCatalog == null)
            {
                return;
            }

            IReadOnlyList<DevelopmentDebugCatalog> debugCatalogs = _collectionSourceCatalog.DebugCatalogs;
            for (int catalogIndex = 0; catalogIndex < debugCatalogs.Count; catalogIndex++)
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
            if (registeredIds == null || _collectionSourceCatalog == null)
            {
                return;
            }

            IReadOnlyList<ItemData> itemDataAssets = _collectionSourceCatalog.ItemDataAssets;
            for (int index = 0; index < itemDataAssets.Count; index++)
            {
                TryAddCollectionEntry(itemDataAssets[index], registeredIds);
            }

            IReadOnlyList<ActiveItemData> activeItemAssets = _collectionSourceCatalog.ActiveItemDataAssets;
            for (int index = 0; index < activeItemAssets.Count; index++)
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
                Icon = itemData.Icon,
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
                Icon = activeItemData.Icon,
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
                Icon = ResolveEnemyIcon(enemyEntry.EnemyPrefab),
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
                Icon = ResolveEnemyIcon(enemyController),
                Kind = MetaCollectionEntryKind.Enemy
            });
        }

        private static Sprite ResolveEnemyIcon(EnemyController enemyController)
        {
            if (enemyController == null)
            {
                return null;
            }

            SpriteRenderer spriteRenderer = enemyController.GetComponentInChildren<SpriteRenderer>(true);
            return spriteRenderer != null ? spriteRenderer.sprite : null;
        }

        private static string ResolvePrefabDisplayName(GameObject gameObject)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(gameObject.name))
            {
                return "Enemy";
            }

            return gameObject.name.Replace("(Clone)", string.Empty).Trim();
        }

        private static List<MetaCollectionEntry> ResolveItemList(
            MetaCollectionEntryKind kind,
            List<MetaCollectionEntry> passives,
            List<MetaCollectionEntry> actives,
            List<MetaCollectionEntry> weapons)
        {
            return kind switch
            {
                MetaCollectionEntryKind.ActiveItem => actives,
                MetaCollectionEntryKind.Weapon => weapons,
                _ => passives
            };
        }

        private void CreateCollectionSection(string title, IReadOnlyList<MetaCollectionEntry> entries, MetaProgressionSaveData progression, bool enemySection)
        {
            int count = entries != null ? entries.Count : 0;
            CreateBodyText(_contentRoot, $"{title} ({count})", 24, FontStyle.Bold);

            if (count == 0)
            {
                CreateBodyText(_contentRoot, "None", 18);
                return;
            }

            RectTransform gridRoot = CreateCollectionGridRoot(title, count);
            for (int index = 0; index < count; index++)
            {
                MetaCollectionEntry entry = entries[index];
                if (entry == null)
                {
                    continue;
                }

                bool discovered = enemySection
                    ? ContainsId(progression.SeenEnemyIds, entry.Id)
                    : ContainsId(progression.DiscoveredItemIds, entry.Id);
                bool unlocked = enemySection || IsCollectionEntryUnlocked(entry, _metaSaveData?.Unlocks);
                int killCount = enemySection ? GetCounterValue(progression.EnemyKillCounts, entry.Id) : 0;
                CreateCollectionCard(gridRoot, entry, discovered, unlocked, killCount, enemySection);
            }
        }

        private RectTransform CreateCollectionGridRoot(string title, int itemCount)
        {
            GameObject gridObject = new($"{title}Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(_contentRoot, false);

            RectTransform gridRoot = gridObject.GetComponent<RectTransform>();
            gridRoot.localScale = Vector3.one;

            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(286f, 170f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.UpperLeft;

            int rows = Mathf.CeilToInt(Mathf.Max(1, itemCount) / 3f);
            LayoutElement layoutElement = gridObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = rows * 170f + Mathf.Max(0, rows - 1) * 14f + 8f;
            layoutElement.minHeight = layoutElement.preferredHeight;
            return gridRoot;
        }

        private static void CreateCollectionCard(
            RectTransform parent,
            MetaCollectionEntry entry,
            bool discovered,
            bool unlocked,
            int killCount,
            bool enemyCard)
        {
            bool visible = discovered && unlocked;
            GameObject cardObject = new(entry.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(parent, false);

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.localScale = Vector3.one;

            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = visible
                ? new Color(0.18f, 0.2f, 0.22f, 0.96f)
                : new Color(0.07f, 0.075f, 0.085f, 0.96f);

            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(cardRect, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(14f, -16f);
            iconRect.sizeDelta = new Vector2(72f, 72f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = visible ? entry.Icon : null;
            iconImage.preserveAspect = true;
            iconImage.color = visible && entry.Icon != null
                ? Color.white
                : new Color(0.18f, 0.19f, 0.2f, 1f);

            if (!visible || entry.Icon == null)
            {
                Text iconText = CreateCardText(iconRect, "IconText", visible ? "?" : "???", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                Stretch(iconText.rectTransform, Vector2.zero, Vector2.one);
            }

            string name = visible ? ResolveSafeLabel(entry.DisplayName, entry.Id) : unlocked ? "???" : "LOCKED";
            Text nameText = CreateCardText(cardRect, "Name", name, 18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            nameText.rectTransform.pivot = new Vector2(0f, 1f);
            nameText.rectTransform.offsetMin = new Vector2(96f, -58f);
            nameText.rectTransform.offsetMax = new Vector2(-12f, -14f);

            string detail = visible
                ? BuildCardDescription(entry.Description)
                : unlocked ? "Not discovered yet." : $"Unlock: {UnlockDisplayNameResolver.Resolve(entry.UnlockKey)}";
            Text detailText = CreateCardText(cardRect, "Detail", detail, 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.82f, 0.86f, 0.9f, 1f));
            Anchor(detailText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            detailText.rectTransform.offsetMin = new Vector2(14f, 34f);
            detailText.rectTransform.offsetMax = new Vector2(-14f, -98f);

            string footer = enemyCard
                ? $"Kills {Mathf.Max(0, killCount)}"
                : ResolveCollectionKindLabel(entry.Kind);
            Text footerText = CreateCardText(cardRect, "Footer", footer, 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.62f, 0.96f, 1f, 1f));
            Anchor(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
            footerText.rectTransform.pivot = new Vector2(0f, 0f);
            footerText.rectTransform.offsetMin = new Vector2(14f, 10f);
            footerText.rectTransform.offsetMax = new Vector2(-14f, 32f);
        }

        private static Text CreateCardText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.color = color;
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                size,
                anchor,
                style,
                horizontalOverflow: HorizontalWrapMode.Wrap,
                verticalOverflow: VerticalWrapMode.Truncate);
            return text;
        }

        private static string BuildCardDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "No description recorded.";
            }

            string text = description.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return text.Length <= 92 ? text : text.Substring(0, 89).TrimEnd() + "...";
        }

        private static string ResolveCollectionKindLabel(MetaCollectionEntryKind kind)
        {
            return kind switch
            {
                MetaCollectionEntryKind.ActiveItem => "Active",
                MetaCollectionEntryKind.Weapon => "Weapon",
                MetaCollectionEntryKind.Enemy => "Enemy",
                _ => "Passive"
            };
        }

        private static string ResolveSafeLabel(string value, string fallback)
        {
            return !string.IsNullOrWhiteSpace(value) ? value : fallback;
        }

        private void CreateAchievementSection(
            string title,
            IReadOnlyList<AchievementData> achievements,
            MetaProgressionSaveData progression,
            bool completedSection)
        {
            int count = achievements != null ? achievements.Count : 0;
            CreateBodyText(_contentRoot, $"{title} ({count})", 24, FontStyle.Bold);

            if (count == 0)
            {
                CreateBodyText(_contentRoot, "None", 18);
                return;
            }

            RectTransform gridRoot = CreateAchievementGridRoot(title, count);
            for (int index = 0; index < count; index++)
            {
                AchievementData achievement = achievements[index];
                if (achievement != null)
                {
                    CreateAchievementCard(gridRoot, achievement, progression, completedSection);
                }
            }
        }

        private RectTransform CreateAchievementGridRoot(string title, int itemCount)
        {
            GameObject gridObject = new($"{title}AchievementGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(_contentRoot, false);

            RectTransform gridRoot = gridObject.GetComponent<RectTransform>();
            gridRoot.localScale = Vector3.one;

            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(438f, 188f);
            grid.spacing = new Vector2(16f, 16f);
            grid.childAlignment = TextAnchor.UpperLeft;

            int rows = Mathf.CeilToInt(Mathf.Max(1, itemCount) / 2f);
            LayoutElement layoutElement = gridObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = rows * 188f + Mathf.Max(0, rows - 1) * 16f + 8f;
            layoutElement.minHeight = layoutElement.preferredHeight;
            return gridRoot;
        }

        private static void CreateAchievementCard(
            RectTransform parent,
            AchievementData achievement,
            MetaProgressionSaveData progression,
            bool completed)
        {
            GameObject cardObject = new(achievement.AchievementId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(parent, false);

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.localScale = Vector3.one;

            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = completed
                ? new Color(0.16f, 0.23f, 0.18f, 0.96f)
                : new Color(0.13f, 0.14f, 0.16f, 0.96f);

            Text statusText = CreateCardText(cardRect, "Status", completed ? "DONE" : "TODO", 14, FontStyle.Bold, TextAnchor.MiddleCenter, completed ? new Color(0.52f, 1f, 0.62f, 1f) : new Color(1f, 0.78f, 0.34f, 1f));
            Anchor(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f));
            statusText.rectTransform.pivot = new Vector2(0f, 1f);
            statusText.rectTransform.anchoredPosition = new Vector2(14f, -14f);
            statusText.rectTransform.sizeDelta = new Vector2(64f, 24f);

            Text nameText = CreateCardText(cardRect, "Name", ResolveSafeLabel(achievement.DisplayName, achievement.AchievementId), 19, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            nameText.rectTransform.pivot = new Vector2(0f, 1f);
            nameText.rectTransform.offsetMin = new Vector2(88f, -46f);
            nameText.rectTransform.offsetMax = new Vector2(-14f, -12f);

            string detail = BuildCardDescription(string.IsNullOrWhiteSpace(achievement.Description)
                ? achievement.AchievementId
                : achievement.Description);
            Text detailText = CreateCardText(cardRect, "Detail", detail, 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.84f, 0.88f, 0.92f, 1f));
            Anchor(detailText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            detailText.rectTransform.pivot = new Vector2(0f, 1f);
            detailText.rectTransform.offsetMin = new Vector2(14f, -106f);
            detailText.rectTransform.offsetMax = new Vector2(-14f, -52f);

            float normalized = ResolveAchievementProgress01(achievement, progression, completed);
            CreateProgressBar(cardRect, normalized, completed);

            string footer = completed
                ? ResolveAchievementCompletedAtLabel(progression, achievement.AchievementId)
                : ResolveAchievementProgress(achievement, progression);
            if (achievement.RewardTargetType == AchievementRewardTargetType.UnlockKey && !string.IsNullOrWhiteSpace(achievement.RewardUnlockKey))
            {
                footer = $"{footer}  Reward: {UnlockDisplayNameResolver.Resolve(achievement.RewardUnlockKey)}";
            }

            Text footerText = CreateCardText(cardRect, "Footer", footer, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.62f, 0.96f, 1f, 1f));
            Anchor(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
            footerText.rectTransform.pivot = new Vector2(0f, 0f);
            footerText.rectTransform.offsetMin = new Vector2(14f, 10f);
            footerText.rectTransform.offsetMax = new Vector2(-14f, 32f);
        }

        private static void CreateProgressBar(RectTransform parent, float normalized, bool completed)
        {
            GameObject trackObject = new("ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObject.transform.SetParent(parent, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            Anchor(trackRect, new Vector2(0f, 0f), new Vector2(1f, 0f));
            trackRect.pivot = new Vector2(0f, 0f);
            trackRect.offsetMin = new Vector2(14f, 42f);
            trackRect.offsetMax = new Vector2(-14f, 54f);
            trackObject.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 1f);

            GameObject fillObject = new("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(trackRect, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillObject.GetComponent<Image>().color = completed
                ? new Color(0.42f, 1f, 0.52f, 1f)
                : new Color(1f, 0.72f, 0.28f, 1f);
        }

        private static bool IsCollectionEntryUnlocked(MetaCollectionEntry entry, UnlockSaveData unlocks)
        {
            if (entry == null || entry.UnlockedByDefault || string.IsNullOrWhiteSpace(entry.UnlockKey))
            {
                return true;
            }

            return ContainsId(unlocks?.UnlockedKeys, entry.UnlockKey);
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

            int required = ResolveAchievementRequiredValue(achievement);
            int current = ResolveAchievementCurrentValue(achievement, progression);
            return $"{Mathf.Min(current, required)}/{required}";
        }

        private static float ResolveAchievementProgress01(AchievementData achievement, MetaProgressionSaveData progression, bool completed)
        {
            if (completed)
            {
                return 1f;
            }

            if (achievement == null || progression == null)
            {
                return 0f;
            }

            int required = ResolveAchievementRequiredValue(achievement);
            return Mathf.Clamp01((float)ResolveAchievementCurrentValue(achievement, progression) / Mathf.Max(1, required));
        }

        private static int ResolveAchievementCurrentValue(AchievementData achievement, MetaProgressionSaveData progression)
        {
            if (achievement == null || progression == null)
            {
                return 0;
            }

            return achievement.ConditionType switch
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
        }

        private static int ResolveAchievementRequiredValue(AchievementData achievement)
        {
            if (achievement == null)
            {
                return 1;
            }

            return achievement.ConditionType switch
            {
                AchievementConditionType.EnemySeen => 1,
                AchievementConditionType.ItemDiscovered => 1,
                AchievementConditionType.CharacterClearMark => 1,
                _ => Mathf.Max(1, achievement.RequiredCount)
            };
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

        private Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            return CreateButton(parent, label, onClick, 58f, 22, 180f);
        }

        private Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, float preferredHeight, int fontSize, float preferredWidth)
        {
            GameObject buttonObject = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(34f, preferredHeight);
            element.minHeight = Mathf.Max(32f, preferredHeight - 8f);
            element.preferredWidth = Mathf.Max(80f, preferredWidth);

            Image image = buttonObject.GetComponent<Image>();
            image.color = ResolveButtonColor();
            image.sprite = buttonSprite;
            image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Text text = CreateText("Label", buttonObject.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static Text CreateBodyText(RectTransform parent, string value, int size = 22, FontStyle style = FontStyle.Normal)
        {
            Text text = CreateText("Text", parent, value, size, style, TextAnchor.UpperLeft, ResolveBodyTextColor());
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
            text.color = ResolveTextColor(name, color);
            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                alignment,
                style,
                horizontalOverflow: HorizontalWrapMode.Wrap,
                verticalOverflow: VerticalWrapMode.Truncate);
            return text;
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.GetComponent<Image>();
            image.sprite = ResolvePanelSprite(name);
            image.color = image.sprite != null && string.Equals(name, "Backdrop", StringComparison.OrdinalIgnoreCase)
                ? Color.white
                : ResolvePanelColor(name, color);
            image.type = image.sprite != null && !string.Equals(name, "Backdrop", StringComparison.OrdinalIgnoreCase)
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.raycastTarget = true;
            return panelObject.GetComponent<RectTransform>();
        }

        private Sprite ResolvePanelSprite(string name)
        {
            if (string.Equals(name, "Backdrop", StringComparison.OrdinalIgnoreCase))
            {
                return backgroundSprite;
            }

            if (string.Equals(name, "TitleShell", StringComparison.OrdinalIgnoreCase))
            {
                return shellSprite != null ? shellSprite : panelSprite;
            }

            if (string.Equals(name, "PrimaryActions", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "UtilityBar", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "ContentScroll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Sidebar", StringComparison.OrdinalIgnoreCase))
            {
                return panelSprite;
            }

            return null;
        }

        private static string FormatOnOff(bool value)
        {
            return value ? "On" : "Off";
        }

        private static Color ResolveButtonColor()
        {
            if (GameOptionsService.HighContrastUiEnabled)
            {
                return GameOptionsService.ColorBlindAssistEnabled
                    ? new Color(0.02f, 0.18f, 0.24f, 1f)
                    : new Color(0.04f, 0.04f, 0.04f, 1f);
            }

            return GameOptionsService.ColorBlindAssistEnabled
                ? new Color(0.12f, 0.34f, 0.42f, 0.96f)
                : new Color(0.27f, 0.32f, 0.36f, 0.96f);
        }

        private static Color ResolveBodyTextColor()
        {
            if (GameOptionsService.HighContrastUiEnabled)
            {
                return Color.white;
            }

            return GameOptionsService.ColorBlindAssistEnabled
                ? new Color(0.9f, 0.98f, 1f, 1f)
                : new Color(0.92f, 0.94f, 0.96f, 1f);
        }

        private static Color ResolveTextColor(string name, Color fallback)
        {
            if (!GameOptionsService.HighContrastUiEnabled && !GameOptionsService.ColorBlindAssistEnabled)
            {
                return string.Equals(name, "Text", StringComparison.OrdinalIgnoreCase)
                    ? new Color(0.92f, 0.94f, 0.96f, 1f)
                    : Color.white;
            }

            if (string.Equals(name, "Title", StringComparison.OrdinalIgnoreCase))
            {
                return GameOptionsService.ColorBlindAssistEnabled
                    ? new Color(0.24f, 0.95f, 1f, 1f)
                    : new Color(1f, 0.96f, 0.34f, 1f);
            }

            return ResolveBodyTextColor();
        }

        private static Color ResolvePanelColor(string name, Color fallback)
        {
            if (fallback.a <= 0.02f)
            {
                return fallback;
            }

            if (GameOptionsService.HighContrastUiEnabled)
            {
                return string.Equals(name, "Sidebar", StringComparison.OrdinalIgnoreCase)
                    ? new Color(0f, 0f, 0f, Mathf.Max(0.96f, fallback.a))
                    : new Color(0.015f, 0.015f, 0.015f, Mathf.Max(0.9f, fallback.a));
            }

            Color baseColor = ResolveDefaultPanelColor(name, fallback);
            return GameOptionsService.ColorBlindAssistEnabled
                ? new Color(Mathf.Min(1f, baseColor.r * 0.75f), Mathf.Min(1f, baseColor.g * 1.05f), Mathf.Min(1f, baseColor.b * 1.22f), baseColor.a)
                : baseColor;
        }

        private static Color ResolveDefaultPanelColor(string name, Color fallback)
        {
            if (string.Equals(name, "Backdrop", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.18f, 0.2f, 0.22f, 1f);
            }

            if (string.Equals(name, "TitleShell", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.09f, 0.09f, 0.1f, 0.94f);
            }

            if (string.Equals(name, "Sidebar", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.16f, 0.18f, 0.2f, 0.96f);
            }

            if (string.Equals(name, "ContentScroll", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.13f, 0.13f, 0.14f, 0.9f);
            }

            return fallback;
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
