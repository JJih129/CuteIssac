using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Pooling;
using CuteIssac.Data.Dungeon;
using CuteIssac.Room;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayFeedbackPresenter : MonoBehaviour
    {
        [Header("Optional Templates")]
        [Tooltip("Optional floating feedback view prefab. When empty, a simple TextMesh popup is generated.")]
        [SerializeField] private FloatingFeedbackView floatingFeedbackTemplate;
        [Tooltip("Optional banner view prefab. When empty, a simple top-center UI banner is generated.")]
        [SerializeField] private ScreenFeedbackBannerView bannerTemplate;
        [Tooltip("Optional enemy death effect template. When empty, a simple world-space burst is generated.")]
        [SerializeField] private EnemyDeathEffectView enemyDeathEffectTemplate;
        [Tooltip("Optional room clear effect template. When empty, a simple center pulse is generated.")]
        [SerializeField] private RoomClearEffectView roomClearEffectTemplate;
        [Tooltip("Optional full-screen threat flash template. When empty, a simple overlay flash is generated.")]
        [SerializeField] private ScreenThreatFlashView threatFlashTemplate;

        [Header("Optional References")]
        [Tooltip("Optional gameplay camera used for fallback world-space sorting or future screen projection.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Optional canvas used for top-center banners. If empty, the presenter finds one at runtime.")]
        [SerializeField] private Canvas overlayCanvas;
        [Tooltip("Optional parent under the overlay canvas for spawned banners.")]
        [SerializeField] private RectTransform bannerLayerRoot;
        [Tooltip("Optional minimap view used to keep secret-room reward banner colors aligned with HUD/minimap theming.")]
        [SerializeField] private MinimapPanelView minimapPanelView;

        [Header("Colors")]
        [SerializeField] private Color enemyDamageColor = new(1f, 0.92f, 0.52f, 1f);
        [SerializeField] private Color playerDamageColor = new(1f, 0.42f, 0.42f, 1f);
        [SerializeField] private Color pickupFeedbackColor = new(0.48f, 1f, 0.72f, 1f);
        [SerializeField] private Color roomClearAccentColor = new(0.48f, 0.9f, 1f, 1f);
        [SerializeField] private Color rewardAccentColor = new(1f, 0.8f, 0.36f, 1f);
        [SerializeField] private Color secretRewardAccentColor = new(1f, 0.89f, 0.54f, 1f);
        [SerializeField] private Color challengeRewardAccentColor = new(1f, 0.72f, 0.3f, 1f);
        [SerializeField] private Color challengeEliteRewardAccentColor = new(1f, 0.54f, 0.24f, 1f);
        [SerializeField] private Color challengeDeadlyRewardAccentColor = new(1f, 0.4f, 0.18f, 1f);
        [SerializeField] private Color challengeFinaleAccentColor = new(1f, 0.82f, 0.34f, 1f);
        [SerializeField] private Color bossAccentColor = new(1f, 0.34f, 0.34f, 1f);
        [SerializeField] private Color enemyDeathAccentColor = new(1f, 0.68f, 0.42f, 1f);
        [SerializeField] private Color curseRareAccentColor = new(0.56f, 0.76f, 1f, 1f);
        [SerializeField] private Color curseLegendaryAccentColor = new(1f, 0.66f, 0.22f, 1f);
        [SerializeField] private Color curseRelicAccentColor = new(1f, 0.88f, 0.44f, 1f);
        [SerializeField] private Color curseBossAccentColor = new(1f, 0.42f, 0.36f, 1f);

        [Header("Popup Offsets")]
        [SerializeField] private Vector3 enemyDamageOffset = new(0.1f, 1.24f, 0f);
        [SerializeField] private Vector3 playerDamageOffset = new(0.05f, 1.42f, 0f);
        [SerializeField] private Vector3 pickupOffset = new(0.08f, 0.96f, 0f);
        [SerializeField] private bool damageNumbersOnly = true;
        [SerializeField] private bool suppressEventLabelFeedback = true;
        [SerializeField] private bool suppressNonFeedbackWorldText = true;
        [SerializeField] [Min(4)] private int pickupFeedbackMaxCharacters = 18;

        private int _announcedBossSourceId = -1;
        private FloatingFeedbackView _runtimeFloatingFeedbackTemplate;
        private ScreenFeedbackBannerView _runtimeBannerTemplate;
        private EnemyDeathEffectView _runtimeEnemyDeathEffectTemplate;
        private RoomClearEffectView _runtimeRoomClearEffectTemplate;
        private ScreenThreatFlashView _runtimeThreatFlashTemplate;
        private bool _suppressPresentationForModal;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            WorldTextModalSuppressor.SetSuppressed(false);
            _suppressPresentationForModal = UiModalState.IsGameplayModalActive;
            GameplayFeedbackEvents.FloatingFeedbackRequested += HandleFloatingFeedbackRequested;
            GameplayFeedbackEvents.BannerFeedbackRequested += HandleBannerFeedbackRequested;
            GameplayFeedbackEvents.ThreatFlashRequested += HandleThreatFlashRequested;
            GameplayRuntimeEvents.RoomCleared += HandleRoomCleared;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted += HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.EnemyKilled += HandleEnemyKilled;
            GameplayRuntimeEvents.CurseRewardManifested += HandleCurseRewardManifested;
            BossHudEvents.BossShownOrUpdated += HandleBossShownOrUpdated;
            BossHudEvents.BossHidden += HandleBossHidden;
            UiModalState.GameplayModalStateChanged += HandleGameplayModalStateChanged;
        }

        private void OnDisable()
        {
            GameplayFeedbackEvents.FloatingFeedbackRequested -= HandleFloatingFeedbackRequested;
            GameplayFeedbackEvents.BannerFeedbackRequested -= HandleBannerFeedbackRequested;
            GameplayFeedbackEvents.ThreatFlashRequested -= HandleThreatFlashRequested;
            GameplayRuntimeEvents.RoomCleared -= HandleRoomCleared;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
            GameplayRuntimeEvents.CurseRewardManifested -= HandleCurseRewardManifested;
            BossHudEvents.BossShownOrUpdated -= HandleBossShownOrUpdated;
            BossHudEvents.BossHidden -= HandleBossHidden;
            UiModalState.GameplayModalStateChanged -= HandleGameplayModalStateChanged;
            _suppressPresentationForModal = false;
        }

        private void LateUpdate()
        {
            if (suppressNonFeedbackWorldText)
            {
                WorldTextModalSuppressor.SuppressNonFeedbackWorldText();
            }
        }

        public void ShowEnemyDamage(Vector3 worldPosition, float amount)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            SpawnFloatingFeedback(new FloatingFeedbackRequest(
                worldPosition + enemyDamageOffset,
                Mathf.CeilToInt(amount).ToString(),
                enemyDamageColor,
                0.44f,
                0.54f,
                1.08f,
                visualProfile: FloatingFeedbackVisualProfile.EnemyDamage));
        }

        public void ShowPlayerDamage(Vector3 worldPosition, float amount)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            SpawnFloatingFeedback(new FloatingFeedbackRequest(
                worldPosition + playerDamageOffset,
                $"-{Mathf.CeilToInt(amount)}",
                playerDamageColor,
                0.46f,
                0.58f,
                1.12f,
                visualProfile: FloatingFeedbackVisualProfile.PlayerDamage));
        }

        public void ShowPickupFeedback(Vector3 worldPosition, string label)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (damageNumbersOnly)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            SpawnFloatingFeedback(new FloatingFeedbackRequest(
                worldPosition + pickupOffset,
                CondensePickupLabel(label),
                pickupFeedbackColor,
                0.68f,
                0.76f,
                1.1f,
                visualProfile: FloatingFeedbackVisualProfile.Pickup));
        }

        private void HandleFloatingFeedbackRequested(FloatingFeedbackRequest request)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (!ShouldRenderFloatingFeedback(request.VisualProfile))
            {
                return;
            }

            if (suppressEventLabelFeedback && request.VisualProfile == FloatingFeedbackVisualProfile.EventLabel)
            {
                return;
            }

            SpawnFloatingFeedback(request);
        }

        private void HandleBannerFeedbackRequested(BannerFeedbackRequest request)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            bool hasChallengeMetadata = request.HasChallengeMetadata;
            string badgeLabel = request.ChallengeBadgeLabel;
            ChallengeThreatStage stage = request.ChallengeStage;

            if (!hasChallengeMetadata)
            {
                hasChallengeMetadata = ChallengeThreatPresentationResolver.TryResolveBannerCopy(
                    request.Title,
                    request.Subtitle,
                    out badgeLabel,
                    out _,
                    out stage);
            }

            if (hasChallengeMetadata)
            {
                TryPlayChallengeThreatAudio(badgeLabel, stage);
                TrySpawnChallengeThreatFlash(request, badgeLabel, stage);
            }

            SpawnBannerFeedback(request);
        }

        private void HandleThreatFlashRequested(ThreatFlashRequest request)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            SpawnThreatFlash(request);
        }

        private void HandleRoomCleared(RoomClearSignal signal)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (!signal.HadCombatEncounter)
            {
                return;
            }

            SpawnRoomClearEffect(roomClearAccentColor);
            SpawnBannerFeedback(new BannerFeedbackRequest(
                "방 클리어",
                signal.Room != null ? signal.Room.RoomId : string.Empty,
                roomClearAccentColor,
                1.75f));
        }

        private void HandleEnemyKilled(EnemyKilledSignal signal)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            SpawnEnemyDeathEffect(signal.Position, enemyDeathAccentColor);
        }

        private void HandleRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (!signal.HasRewards)
            {
                return;
            }

            if (signal.Room != null
                && signal.Room.RoomType == RoomType.Challenge
                && signal.HasChallengeBonusPresentation)
            {
                SpawnChallengeRewardPresentation(signal);
                return;
            }

            if (signal.Room != null && signal.Room.RoomType == RoomType.Secret)
            {
                string secretRewardLabel = signal.RewardCount == 1
                    ? "숨겨진 보상 1개"
                    : $"숨겨진 보상 {signal.RewardCount}개";
                SpawnBannerFeedback(new BannerFeedbackRequest(
                    "비밀 보상 발견",
                    secretRewardLabel,
                    ResolveSecretRewardAccentColor(),
                    1.45f));
                return;
            }

            string rewardLabel = signal.RewardCount == 1 ? "보상 1개" : $"보상 {signal.RewardCount}개";
            SpawnBannerFeedback(new BannerFeedbackRequest(
                "보상 드랍",
                rewardLabel,
                rewardAccentColor,
                1.25f));
        }

        private void SpawnChallengeRewardPresentation(RoomRewardPhaseSignal signal)
        {
            Color accentColor = ResolveChallengeRewardAccent(signal.ChallengeClearRank, signal.ChallengePressureTier);
            string title = ResolveChallengeRewardTitle(signal.ChallengeClearRank, signal.ChallengePressureTier);
            string subtitle = ResolveChallengeRewardSubtitle(signal);

            if (signal.IsChallengeFinale)
            {
                SpawnRoomClearEffect(Color.Lerp(roomClearAccentColor, accentColor, 0.58f));
            }

            SpawnBannerFeedback(new BannerFeedbackRequest(
                title,
                subtitle,
                accentColor,
                signal.IsChallengeFinale
                    ? Mathf.Max(2.1f, signal.ChallengePressureTier >= ChallengePressureTier.Elite ? 2.2f : 2.1f)
                    : signal.ChallengePressureTier >= ChallengePressureTier.Elite ? 1.95f : 1.7f,
                true,
                "챌린지",
                ResolveChallengeRewardEyebrow(signal.ChallengePressureTier),
                ResolveChallengeRewardStage(signal.ChallengePressureTier),
                ResolveChallengeRewardLayoutProfile(signal.ChallengePressureTier)));

            if (signal.Room != null)
            {
                GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                    signal.Room.CameraFocusPosition + Vector3.up * 1.3f,
                    ResolveChallengeRewardFloatingLabel(signal),
                    accentColor,
                    0.78f,
                    0.96f,
                    1.2f,
                    true,
                    "챌린지",
                    ResolveChallengeRewardStage(signal.ChallengePressureTier),
                    ResolveChallengeRewardLayoutProfile(signal.ChallengePressureTier)));
            }

            if (signal.ChallengePressureTier != ChallengePressureTier.None)
            {
                SpawnThreatFlash(new ThreatFlashRequest(
                    accentColor,
                    signal.ChallengePressureTier == ChallengePressureTier.Deadly ? 0.12f : 0.085f,
                    signal.ChallengePressureTier == ChallengePressureTier.Deadly ? 0.42f : 0.34f,
                    signal.ChallengePressureTier == ChallengePressureTier.Deadly ? 2 : 1,
                    signal.ChallengePressureTier == ChallengePressureTier.Deadly ? 0.34f : 0.24f,
                    signal.ChallengePressureTier == ChallengePressureTier.Deadly ? 1.08f : 0.96f,
                    signal.ChallengePressureTier == ChallengePressureTier.Deadly ? 0.3f : 0.46f));
            }
            else if (signal.IsChallengeFinale)
            {
                SpawnThreatFlash(new ThreatFlashRequest(
                    accentColor,
                    0.07f,
                    0.3f,
                    1,
                    0.18f,
                    0.92f,
                    0.5f));
            }
        }

        private void HandleCurseRewardManifested(CurseRewardManifestedSignal signal)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (!signal.IsValid || signal.Rarity < Data.Item.ItemRarity.Rare)
            {
                return;
            }

            SpawnBannerFeedback(new BannerFeedbackRequest(
                ResolveCurseRewardBannerTitle(signal.Rarity),
                signal.ItemData.DisplayName,
                ResolveCurseRewardAccentColor(signal.Rarity),
                ResolveCurseRewardDuration(signal.Rarity)));
        }

        private void HandleBossShownOrUpdated(BossHudState hudState)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            if (hudState.SourceId == _announcedBossSourceId)
            {
                return;
            }

            _announcedBossSourceId = hudState.SourceId;
            SpawnBannerFeedback(new BannerFeedbackRequest(
                "보스 등장",
                hudState.BossName,
                bossAccentColor,
                2f));
        }

        private void HandleBossHidden(int sourceId)
        {
            if (_announcedBossSourceId == sourceId)
            {
                _announcedBossSourceId = -1;
            }
        }

        private void SpawnFloatingFeedback(FloatingFeedbackRequest request)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            FloatingFeedbackView feedbackTemplate = ResolveFloatingFeedbackTemplate();
            FloatingFeedbackView feedbackView = PrefabPoolService.Spawn(feedbackTemplate, request.WorldPosition, Quaternion.identity);

            feedbackView.Initialize(request);
        }

        private void SpawnBannerFeedback(BannerFeedbackRequest request)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            RectTransform bannerRoot = EnsureBannerLayerRoot();
            if (bannerRoot == null)
            {
                return;
            }

            ScreenFeedbackBannerView bannerView = PrefabPoolService.Spawn(
                ResolveBannerTemplate(bannerRoot),
                Vector3.zero,
                Quaternion.identity,
                bannerRoot);

            bannerView.Initialize(request);
        }

        private void SpawnEnemyDeathEffect(Vector3 worldPosition, Color accentColor)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            EnemyDeathEffectView effectView = PrefabPoolService.Spawn(
                ResolveEnemyDeathEffectTemplate(),
                worldPosition,
                Quaternion.identity);

            effectView.Initialize(accentColor);
        }

        private void SpawnRoomClearEffect(Color accentColor)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            RectTransform bannerRoot = EnsureBannerLayerRoot();

            if (bannerRoot == null)
            {
                return;
            }

            RoomClearEffectView effectView = PrefabPoolService.Spawn(
                ResolveRoomClearEffectTemplate(bannerRoot),
                Vector3.zero,
                Quaternion.identity,
                bannerRoot);

            effectView.Initialize(accentColor);
        }

        private static void TryPlayChallengeThreatAudio(string badgeLabel, ChallengeThreatStage stage)
        {
            GameAudioEventType eventType = ChallengeThreatPresentationResolver.ResolveWarningAudioEventType(badgeLabel, stage);
            float volumeScale = ChallengeThreatPresentationResolver.ResolveWarningAudioVolumeScale(badgeLabel, stage);
            float pitchScale = ChallengeThreatPresentationResolver.ResolveWarningAudioPitchScale(badgeLabel, stage);
            GameAudioEvents.RaiseUi(eventType, volumeScale, pitchScale);
        }

        private void SpawnThreatFlash(ThreatFlashRequest request)
        {
            if (_suppressPresentationForModal)
            {
                return;
            }

            RectTransform bannerRoot = EnsureBannerLayerRoot();
            if (bannerRoot == null)
            {
                return;
            }

            ScreenThreatFlashView flashView = PrefabPoolService.Spawn(
                ResolveThreatFlashTemplate(bannerRoot),
                Vector3.zero,
                Quaternion.identity,
                bannerRoot);
            flashView.Initialize(
                request.FlashColor,
                request.Opacity,
                request.Duration,
                request.PulseCount,
                request.PulseStrength,
                request.PulseFrequencyScale,
                request.DecaySoftness);
        }

        private void HandleGameplayModalStateChanged(bool isModalActive)
        {
            _suppressPresentationForModal = isModalActive;

            if (isModalActive)
            {
                UiModalDismissRegistry.DismissAll();
            }
        }

        private void TrySpawnChallengeThreatFlash(BannerFeedbackRequest request, string badgeLabel, ChallengeThreatStage stage)
        {
            SpawnThreatFlash(new ThreatFlashRequest(
                ChallengeThreatPresentationResolver.ResolveWarningFlashColor(badgeLabel, request.AccentColor),
                ChallengeThreatPresentationResolver.ResolveWarningFlashOpacity(badgeLabel, stage),
                ChallengeThreatPresentationResolver.ResolveWarningFlashDuration(stage),
                ChallengeThreatPresentationResolver.ResolveWarningFlashPulseCount(stage),
                ChallengeThreatPresentationResolver.ResolveWarningFlashPulseStrength(badgeLabel, stage),
                ChallengeThreatPresentationResolver.ResolveWarningFlashFrequencyScale(badgeLabel, stage),
                ChallengeThreatPresentationResolver.ResolveWarningFlashDecaySoftness(badgeLabel, stage)));
        }

        private RectTransform EnsureBannerLayerRoot()
        {
            ResolveReferences();

            if (bannerLayerRoot != null)
            {
                return bannerLayerRoot;
            }

            if (overlayCanvas == null)
            {
                return null;
            }

            GameObject layerObject = new("FeedbackBannerLayer");
            RectTransform layerRect = layerObject.AddComponent<RectTransform>();
            layerRect.SetParent(overlayCanvas.transform, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            bannerLayerRoot = layerRect;
            return bannerLayerRoot;
        }

        private string CondensePickupLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return string.Empty;
            }

            string normalized = label.Replace('\n', ' ').Replace('\r', ' ').Trim();
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            if (normalized.Length <= pickupFeedbackMaxCharacters)
            {
                return normalized;
            }

            return normalized.Substring(0, Mathf.Max(1, pickupFeedbackMaxCharacters - 1)).TrimEnd() + "…";
        }

        private bool ShouldRenderFloatingFeedback(FloatingFeedbackVisualProfile visualProfile)
        {
            if (!damageNumbersOnly)
            {
                return true;
            }

            return visualProfile == FloatingFeedbackVisualProfile.EnemyDamage
                || visualProfile == FloatingFeedbackVisualProfile.PlayerDamage;
        }

        private void ResolveReferences()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (overlayCanvas == null)
            {
                overlayCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Exclude);
            }

            if (minimapPanelView == null)
            {
                minimapPanelView = FindFirstObjectByType<MinimapPanelView>(FindObjectsInactive.Exclude);
            }
        }

        private Color ResolveSecretRewardAccentColor()
        {
            return minimapPanelView != null
                ? minimapPanelView.SecretRewardThemeColor
                : secretRewardAccentColor;
        }

        private Color ResolveCurseRewardAccentColor(Data.Item.ItemRarity rarity)
        {
            return rarity switch
            {
                Data.Item.ItemRarity.Rare => curseRareAccentColor,
                Data.Item.ItemRarity.Legendary => curseLegendaryAccentColor,
                Data.Item.ItemRarity.Relic => curseRelicAccentColor,
                Data.Item.ItemRarity.Boss => curseBossAccentColor,
                _ => rewardAccentColor
            };
        }

        private static string ResolveCurseRewardBannerTitle(Data.Item.ItemRarity rarity)
        {
            return rarity switch
            {
                Data.Item.ItemRarity.Rare => "진귀한 대가 출현",
                Data.Item.ItemRarity.Legendary => "금단의 대가 출현",
                Data.Item.ItemRarity.Relic => "유물의 대가 출현",
                Data.Item.ItemRarity.Boss => "왕관의 대가 출현",
                _ => "저주 보상 출현"
            };
        }

        private static float ResolveCurseRewardDuration(Data.Item.ItemRarity rarity)
        {
            return rarity switch
            {
                Data.Item.ItemRarity.Relic => 2.1f,
                Data.Item.ItemRarity.Boss => 2.15f,
                _ => 1.85f
            };
        }

        private Color ResolveChallengeRewardAccent(ChallengeClearRank challengeClearRank, ChallengePressureTier challengePressureTier)
        {
            if (challengeClearRank == ChallengeClearRank.S)
            {
                return challengeFinaleAccentColor;
            }

            if (challengePressureTier == ChallengePressureTier.Deadly)
            {
                return challengeDeadlyRewardAccentColor;
            }

            if (challengePressureTier == ChallengePressureTier.Elite)
            {
                return challengeEliteRewardAccentColor;
            }

            return challengeRewardAccentColor;
        }

        private static string ResolveChallengeRewardTitle(ChallengeClearRank challengeClearRank, ChallengePressureTier challengePressureTier)
        {
            if (challengeClearRank == ChallengeClearRank.S)
            {
                return "도전방 완전 제압";
            }

            if (challengeClearRank == ChallengeClearRank.A)
            {
                return "도전 추가 보상 확보";
            }

            return challengePressureTier switch
            {
                ChallengePressureTier.Deadly => "치명 압박 돌파",
                ChallengePressureTier.Elite => "엘리트 압박 돌파",
                ChallengePressureTier.Reinforced => "증원 압박 돌파",
                _ => "도전 보상 확보"
            };
        }

        private static string ResolveChallengeRewardSubtitle(RoomRewardPhaseSignal signal)
        {
            string rewardLine;
            if (signal.BonusRewardSelections > 0 && signal.BonusItemRolls > 0)
            {
                rewardLine = $"+보상 {signal.BonusRewardSelections} / +아이템 {signal.BonusItemRolls}";
            }
            else if (signal.BonusRewardSelections > 0)
            {
                rewardLine = $"+보상 {signal.BonusRewardSelections}";
            }
            else if (signal.BonusItemRolls > 0)
            {
                rewardLine = $"+아이템 {signal.BonusItemRolls}";
            }
            else
            {
                rewardLine = signal.RewardCount == 1 ? "보상 1개 개방" : $"보상 {signal.RewardCount}개 개방";
            }

            string pressureLine = signal.ChallengePressureTier switch
            {
                ChallengePressureTier.Deadly => $"{rewardLine} · 최고 압박 정복",
                ChallengePressureTier.Elite => $"{rewardLine} · 엘리트 압박 정복",
                ChallengePressureTier.Reinforced => $"{rewardLine} · 증원 방어 성공",
                _ => rewardLine
            };

            return signal.IsChallengeFinale
                ? $"{pressureLine} · 최종 웨이브 정리"
                : pressureLine;
        }

        private static string ResolveChallengeRewardEyebrow(ChallengePressureTier challengePressureTier)
        {
            return challengePressureTier switch
            {
                ChallengePressureTier.Deadly => "최고 압박",
                ChallengePressureTier.Elite => "엘리트 돌파",
                ChallengePressureTier.Reinforced => "증원 돌파",
                _ => "최종 정리"
            };
        }

        private static string ResolveChallengeRewardFloatingLabel(RoomRewardPhaseSignal signal)
        {
            if (signal.IsChallengeFinale && signal.ChallengeClearRank == ChallengeClearRank.S)
            {
                return "도전 완전 제압";
            }

            return signal.ChallengePressureTier switch
            {
                ChallengePressureTier.Deadly => "최고 보상",
                ChallengePressureTier.Elite => "엘리트 보상",
                ChallengePressureTier.Reinforced => "증원 보상",
                _ when signal.ChallengeClearRank == ChallengeClearRank.S => "S 보상",
                _ when signal.ChallengeClearRank == ChallengeClearRank.A => "A 보상",
                _ => "도전 보상"
            };
        }

        private static ChallengeThreatStage ResolveChallengeRewardStage(ChallengePressureTier challengePressureTier)
        {
            return challengePressureTier switch
            {
                ChallengePressureTier.Deadly => ChallengeThreatStage.ElitePressure,
                ChallengePressureTier.Elite => ChallengeThreatStage.EliteReinforcement,
                ChallengePressureTier.Reinforced => ChallengeThreatStage.PromotionPressure,
                _ => ChallengeThreatStage.Baseline
            };
        }

        private static ChallengeBannerLayoutProfile ResolveChallengeRewardLayoutProfile(ChallengePressureTier challengePressureTier)
        {
            return challengePressureTier switch
            {
                ChallengePressureTier.Deadly => ChallengeBannerLayoutProfile.EliteWarning,
                ChallengePressureTier.Elite => ChallengeBannerLayoutProfile.EliteWarning,
                ChallengePressureTier.Reinforced => ChallengeBannerLayoutProfile.Pace,
                _ => ChallengeBannerLayoutProfile.Baseline
            };
        }

        private FloatingFeedbackView ResolveFloatingFeedbackTemplate()
        {
            if (floatingFeedbackTemplate != null)
            {
                return floatingFeedbackTemplate;
            }

            if (_runtimeFloatingFeedbackTemplate != null)
            {
                return _runtimeFloatingFeedbackTemplate;
            }

            GameObject popupObject = new("FloatingFeedbackTemplate");
            popupObject.SetActive(false);
            popupObject.transform.SetParent(transform, false);
            _runtimeFloatingFeedbackTemplate = popupObject.AddComponent<FloatingFeedbackView>();
            return _runtimeFloatingFeedbackTemplate;
        }

        private ScreenFeedbackBannerView ResolveBannerTemplate(RectTransform bannerRoot)
        {
            if (bannerTemplate != null)
            {
                return bannerTemplate;
            }

            if (_runtimeBannerTemplate != null)
            {
                return _runtimeBannerTemplate;
            }

            GameObject bannerObject = new("ScreenFeedbackBannerTemplate");
            bannerObject.SetActive(false);
            RectTransform rectTransform = bannerObject.AddComponent<RectTransform>();
            rectTransform.SetParent(bannerRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -96f);
            rectTransform.sizeDelta = new Vector2(980f, 184f);
            _runtimeBannerTemplate = bannerObject.AddComponent<ScreenFeedbackBannerView>();
            return _runtimeBannerTemplate;
        }

        private EnemyDeathEffectView ResolveEnemyDeathEffectTemplate()
        {
            if (enemyDeathEffectTemplate != null)
            {
                return enemyDeathEffectTemplate;
            }

            if (_runtimeEnemyDeathEffectTemplate != null)
            {
                return _runtimeEnemyDeathEffectTemplate;
            }

            GameObject effectObject = new("EnemyDeathEffectTemplate");
            effectObject.SetActive(false);
            effectObject.transform.SetParent(transform, false);
            _runtimeEnemyDeathEffectTemplate = effectObject.AddComponent<EnemyDeathEffectView>();
            return _runtimeEnemyDeathEffectTemplate;
        }

        private RoomClearEffectView ResolveRoomClearEffectTemplate(RectTransform bannerRoot)
        {
            if (roomClearEffectTemplate != null)
            {
                return roomClearEffectTemplate;
            }

            if (_runtimeRoomClearEffectTemplate != null)
            {
                return _runtimeRoomClearEffectTemplate;
            }

            GameObject effectObject = new("RoomClearEffectTemplate");
            effectObject.SetActive(false);
            RectTransform rectTransform = effectObject.AddComponent<RectTransform>();
            rectTransform.SetParent(bannerRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            _runtimeRoomClearEffectTemplate = effectObject.AddComponent<RoomClearEffectView>();
            return _runtimeRoomClearEffectTemplate;
        }

        private ScreenThreatFlashView ResolveThreatFlashTemplate(RectTransform bannerRoot)
        {
            if (threatFlashTemplate != null)
            {
                return threatFlashTemplate;
            }

            if (_runtimeThreatFlashTemplate != null)
            {
                return _runtimeThreatFlashTemplate;
            }

            GameObject flashObject = new("ScreenThreatFlashTemplate");
            flashObject.SetActive(false);
            RectTransform rectTransform = flashObject.AddComponent<RectTransform>();
            rectTransform.SetParent(bannerRoot, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            _runtimeThreatFlashTemplate = flashObject.AddComponent<ScreenThreatFlashView>();
            return _runtimeThreatFlashTemplate;
        }
    }
}
