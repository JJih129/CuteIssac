using System.Collections.Generic;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Item;
using CuteIssac.UI;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Lightweight visual-only fallback for curse rooms.
    /// It builds a small blood altar from runtime sprites so curse rooms still read as authored even when no dedicated prefab is assigned yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurseRoomEntryVisual : MonoBehaviour
    {
        private enum AltarState
        {
            Dormant,
            RewardReady,
            RewardClaimed,
        }

        [Header("Theme")]
        [SerializeField] private Color altarBaseColor = new(0.18f, 0.06f, 0.08f, 1f);
        [SerializeField] private Color altarTopColor = new(0.34f, 0.08f, 0.12f, 1f);
        [SerializeField] private Color sigilGlowColor = new(0.88f, 0.16f, 0.3f, 0.78f);
        [SerializeField] private Color sigilCoreColor = new(0.64f, 0.08f, 0.16f, 0.92f);
        [SerializeField] private Color candleColor = new(0.96f, 0.82f, 0.62f, 0.94f);
        [SerializeField] private Color labelColor = new(1f, 0.84f, 0.9f, 0.96f);
        [SerializeField] private Color rewardReadyGlowColor = new(1f, 0.74f, 0.3f, 0.96f);
        [SerializeField] private Color rewardReadyLabelColor = new(1f, 0.94f, 0.76f, 1f);
        [SerializeField] private Color claimedGlowColor = new(0.52f, 0.16f, 0.24f, 0.5f);
        [SerializeField] private Color claimedLabelColor = new(0.8f, 0.7f, 0.76f, 0.88f);
        [SerializeField] private Color uncommonRewardColor = new(0.58f, 0.9f, 0.72f, 0.96f);
        [SerializeField] private Color rareRewardColor = new(0.42f, 0.74f, 1f, 0.98f);
        [SerializeField] private Color legendaryRewardColor = new(1f, 0.62f, 0.22f, 1f);
        [SerializeField] private Color relicRewardColor = new(1f, 0.9f, 0.48f, 1f);
        [SerializeField] private Color bossRewardColor = new(1f, 0.44f, 0.34f, 1f);

        [Header("Layout")]
        [SerializeField] private Vector2 altarBaseSize = new(1.8f, 0.56f);
        [SerializeField] private Vector2 altarTopSize = new(1.16f, 0.24f);
        [SerializeField] private Vector2 sigilGlowSize = new(1.34f, 1.34f);
        [SerializeField] private Vector2 sigilCoreSize = new(0.72f, 0.72f);
        [SerializeField] private Vector2 candleSize = new(0.16f, 0.34f);
        [SerializeField] private Vector2 leftCandleOffset = new(-0.92f, 0.18f);
        [SerializeField] private Vector2 rightCandleOffset = new(0.92f, 0.18f);
        [SerializeField] private Vector3 labelOffset = new(0f, 0.98f, 0f);
        [SerializeField] private bool showWorldLabel;
        [SerializeField] [Min(0.05f)] private float labelCharacterSize = 0.12f;
        [SerializeField] [Min(1)] private int labelFontSize = 48;

        [Header("Motion")]
        [SerializeField] [Min(0f)] private float glowPulseSpeed = 2.2f;
        [SerializeField] [Range(0f, 0.5f)] private float glowPulseAmplitude = 0.18f;
        [SerializeField] [Min(0f)] private float labelFloatSpeed = 1.4f;
        [SerializeField] [Min(0f)] private float labelFloatAmplitude = 0.04f;
        [SerializeField] [Min(0f)] private float rewardReadyPulseSpeed = 3.6f;
        [SerializeField] [Range(0f, 0.7f)] private float rewardReadyPulseAmplitude = 0.3f;
        [SerializeField] [Min(0f)] private float claimedPulseSpeed = 1.2f;
        [SerializeField] [Range(0f, 0.3f)] private float claimedPulseAmplitude = 0.08f;

        [Header("Feedback")]
        [SerializeField] private Color rewardReadyBannerColor = new(0.94f, 0.5f, 0.24f, 1f);
        [SerializeField] private Color rewardClaimedBannerColor = new(0.72f, 0.24f, 0.3f, 1f);
        [SerializeField] [Min(0.25f)] private float rewardReadyBannerDuration = 1.65f;
        [SerializeField] [Min(0.25f)] private float rewardClaimedBannerDuration = 1.4f;

        [Header("Burst VFX")]
        [SerializeField] [Min(0.05f)] private float rewardReadyBurstLifetime = 0.7f;
        [SerializeField] [Min(0.05f)] private float rewardClaimedBurstLifetime = 0.5f;
        [SerializeField] private Color rewardReadyBurstColor = new(1f, 0.78f, 0.32f, 0.92f);
        [SerializeField] private Color rewardClaimedBurstColor = new(0.72f, 0.26f, 0.3f, 0.72f);
        [SerializeField] private Vector2 rewardReadyBurstStartSize = new(0.52f, 0.52f);
        [SerializeField] private Vector2 rewardReadyBurstEndSize = new(1.92f, 1.92f);
        [SerializeField] private Vector2 rewardClaimedBurstStartSize = new(0.44f, 0.44f);
        [SerializeField] private Vector2 rewardClaimedBurstEndSize = new(1.2f, 1.2f);

        private static Sprite _cachedWhiteSprite;

        private struct ActiveBurst
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 StartScale;
            public Vector3 EndScale;
            public Color StartColor;
            public Color EndColor;
            public float SpawnTime;
            public float Lifetime;
        }

        private RoomController _roomController;
        private MinimapPanelView _minimapPanelView;
        private SpriteRenderer _sigilGlowRenderer;
        private TextMesh _labelText;
        private Vector3 _labelBaseLocalPosition;
        private Color _accentColor;
        private AltarState _altarState;
        private bool _built;
        private float _phaseOffset;
        private ItemRarity? _manifestedRewardRarity;
        private readonly List<ActiveBurst> _activeBursts = new();

        public void Configure(RoomController roomController, Color accentColor)
        {
            _roomController = roomController;
            _accentColor = accentColor;
            ResolveVisualThemeReferences();
            RefreshStateFromRoom();

            if (_built)
            {
                ApplyTheme();
            }
        }

        private void Awake()
        {
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            if (_roomController == null)
            {
                _roomController = GetComponentInParent<RoomController>();
            }

            ResolveVisualThemeReferences();
            BuildIfNeeded();
            RefreshStateFromRoom();
            ApplyTheme();
        }

        private void OnEnable()
        {
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardPhaseCompleted += HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected -= HandleRoomRewardCollected;
            GameplayRuntimeEvents.RoomRewardCollected += HandleRoomRewardCollected;
            GameplayRuntimeEvents.CurseRewardManifested -= HandleCurseRewardManifested;
            GameplayRuntimeEvents.CurseRewardManifested += HandleCurseRewardManifested;
            RefreshStateFromRoom();
            ApplyTheme();
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.RoomRewardPhaseCompleted -= HandleRoomRewardPhaseCompleted;
            GameplayRuntimeEvents.RoomRewardCollected -= HandleRoomRewardCollected;
            GameplayRuntimeEvents.CurseRewardManifested -= HandleCurseRewardManifested;
            ClearActiveBursts();
        }

        private void Update()
        {
            if (!_built)
            {
                return;
            }

            ResolvePulseProfile(out float pulseSpeed, out float pulseAmplitude);
            float pulse = 0.5f + (0.5f * Mathf.Sin((Time.unscaledTime * pulseSpeed) + _phaseOffset));

            if (_sigilGlowRenderer != null)
            {
                ResolveThemeColors(out Color targetGlowColor, out _);
                Color glowColor = Color.Lerp(targetGlowColor, Color.Lerp(Color.white, _accentColor, 0.2f), pulse * 0.4f);
                glowColor.a = Mathf.Clamp01(targetGlowColor.a + (pulse * pulseAmplitude));
                _sigilGlowRenderer.color = glowColor;
                _sigilGlowRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f + pulseAmplitude, pulse);
            }

            if (_labelText != null && showWorldLabel)
            {
                float bob = Mathf.Sin((Time.unscaledTime * labelFloatSpeed) + _phaseOffset) * labelFloatAmplitude;
                _labelText.transform.localPosition = _labelBaseLocalPosition + (Vector3.up * bob);
            }

            UpdateActiveBursts();
        }

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            Sprite whiteSprite = GetWhiteSprite();
            if (whiteSprite == null)
            {
                return;
            }

            CreateSpriteLayer("SigilGlow", whiteSprite, Vector3.zero, sigilGlowSize, 45f, sigilGlowColor, 0);
            _sigilGlowRenderer = CreateSpriteLayer("SigilCore", whiteSprite, Vector3.zero, sigilCoreSize, 45f, sigilCoreColor, 1);
            CreateSpriteLayer("AltarBase", whiteSprite, new Vector3(0f, -0.14f, 0f), altarBaseSize, 0f, altarBaseColor, 2);
            CreateSpriteLayer("AltarTop", whiteSprite, new Vector3(0f, 0.06f, 0f), altarTopSize, 0f, altarTopColor, 3);
            CreateSpriteLayer("LeftCandle", whiteSprite, new Vector3(leftCandleOffset.x, leftCandleOffset.y, 0f), candleSize, 0f, candleColor, 4);
            CreateSpriteLayer("RightCandle", whiteSprite, new Vector3(rightCandleOffset.x, rightCandleOffset.y, 0f), candleSize, 0f, candleColor, 4);
            CreateSpriteLayer("LeftFlame", whiteSprite, new Vector3(leftCandleOffset.x, leftCandleOffset.y + 0.24f, 0f), candleSize * 0.46f, 45f, Color.Lerp(candleColor, Color.white, 0.35f), 5);
            CreateSpriteLayer("RightFlame", whiteSprite, new Vector3(rightCandleOffset.x, rightCandleOffset.y + 0.24f, 0f), candleSize * 0.46f, 45f, Color.Lerp(candleColor, Color.white, 0.35f), 5);

            if (showWorldLabel)
            {
                GameObject labelObject = new("CurseLabel");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = labelOffset;
                _labelText = labelObject.AddComponent<TextMesh>();
                _labelText.text = ResolveLabelText();
                _labelText.anchor = TextAnchor.MiddleCenter;
                _labelText.alignment = TextAlignment.Center;
                _labelText.fontSize = labelFontSize;
                _labelText.characterSize = labelCharacterSize;
                _labelText.color = labelColor;
                _labelText.gameObject.layer = gameObject.layer;
                LocalizedUiFontProvider.Apply(_labelText);
                _labelBaseLocalPosition = _labelText.transform.localPosition;
            }
            _built = true;
        }

        private void ApplyTheme()
        {
            if (!_built)
            {
                return;
            }

            ResolveThemeColors(out Color targetGlowColor, out Color targetLabelColor);

            if (_sigilGlowRenderer != null)
            {
                _sigilGlowRenderer.color = targetGlowColor;
            }

            if (_labelText != null)
            {
                _labelText.text = ResolveLabelText();
                _labelText.color = targetLabelColor;
                _labelText.gameObject.SetActive(showWorldLabel);
            }
        }

        private void RefreshStateFromRoom()
        {
            if (_roomController == null)
            {
                _altarState = AltarState.Dormant;
                _manifestedRewardRarity = null;
                return;
            }

            if (_roomController.HasRewardContent)
            {
                _altarState = AltarState.RewardReady;
                return;
            }

            _altarState = _roomController.State == RoomState.Rewarded
                ? AltarState.RewardClaimed
                : AltarState.Dormant;

            if (_altarState == AltarState.Dormant)
            {
                _manifestedRewardRarity = null;
            }
        }

        private void HandleRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            if (_roomController == null || signal.Room != _roomController || !signal.HasRewards)
            {
                return;
            }

            TransitionToState(AltarState.RewardReady, true);
        }

        private void HandleRoomRewardCollected(RoomRewardCollectedSignal signal)
        {
            if (_roomController == null || !signal.IsValid || signal.Room != _roomController)
            {
                return;
            }

            TransitionToState(AltarState.RewardClaimed, true);
        }

        private void HandleCurseRewardManifested(CurseRewardManifestedSignal signal)
        {
            if (_roomController == null || !signal.IsValid || signal.Room != _roomController)
            {
                return;
            }

            if (!_manifestedRewardRarity.HasValue || signal.Rarity > _manifestedRewardRarity.Value)
            {
                _manifestedRewardRarity = signal.Rarity;
            }

            if (_altarState == AltarState.RewardReady)
            {
                ApplyTheme();
            }
        }

        private void TransitionToState(AltarState nextState, bool announce)
        {
            if (_altarState == nextState)
            {
                return;
            }

            _altarState = nextState;
            ApplyTheme();

            if (!announce)
            {
                return;
            }

            switch (_altarState)
            {
                case AltarState.RewardReady:
                    SpawnRewardBurst(true);
                    GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                        "피의 제단 활성",
                        "대가가 기다린다",
                        ResolveBannerAccentColor(rewardReadyBannerColor),
                        rewardReadyBannerDuration));
                    break;
                case AltarState.RewardClaimed:
                    SpawnRewardBurst(false);
                    GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                        "피의 제단 안정",
                        "제단이 잠잠해졌다",
                        ResolveBannerAccentColor(rewardClaimedBannerColor),
                        rewardClaimedBannerDuration));
                    break;
            }
        }

        private Color ResolveBannerAccentColor(Color fallbackColor)
        {
            Color accentColor = _accentColor == default ? fallbackColor : _accentColor;
            return Color.Lerp(fallbackColor, accentColor, 0.28f);
        }

        private void ResolveVisualThemeReferences()
        {
            if (_minimapPanelView == null)
            {
                _minimapPanelView = FindFirstObjectByType<MinimapPanelView>(FindObjectsInactive.Exclude);
            }
        }

        private void ResolveThemeColors(out Color targetGlowColor, out Color targetLabelColor)
        {
            Color accentColor = _accentColor == default ? new Color(0.72f, 0.16f, 0.28f, 1f) : _accentColor;
            Color synchronizedRewardTheme = _minimapPanelView != null
                ? _minimapPanelView.CurseAltarRewardThemeColor
                : rewardReadyGlowColor;
            Color rarityThemeColor = ResolveRewardRarityThemeColor();

            switch (_altarState)
            {
                case AltarState.RewardReady:
                    targetGlowColor = Color.Lerp(synchronizedRewardTheme, rarityThemeColor, 0.62f);
                    targetGlowColor = Color.Lerp(targetGlowColor, accentColor, 0.16f);
                    targetLabelColor = Color.Lerp(rewardReadyLabelColor, rarityThemeColor, 0.32f);
                    return;
                case AltarState.RewardClaimed:
                    targetGlowColor = claimedGlowColor;
                    targetLabelColor = claimedLabelColor;
                    return;
                default:
                    targetGlowColor = Color.Lerp(sigilGlowColor, accentColor, 0.35f);
                    targetLabelColor = Color.Lerp(labelColor, Color.Lerp(accentColor, Color.white, 0.45f), 0.28f);
                    return;
            }
        }

        private void ResolvePulseProfile(out float pulseSpeed, out float pulseAmplitude)
        {
            switch (_altarState)
            {
                case AltarState.RewardReady:
                    pulseSpeed = _minimapPanelView != null
                        ? Mathf.Max(rewardReadyPulseSpeed, _minimapPanelView.StatusEmphasisPulseSpeed)
                        : rewardReadyPulseSpeed;
                    pulseAmplitude = _minimapPanelView != null
                        ? Mathf.Max(rewardReadyPulseAmplitude, _minimapPanelView.StatusEmphasisTintStrength)
                        : rewardReadyPulseAmplitude;
                    return;
                case AltarState.RewardClaimed:
                    pulseSpeed = claimedPulseSpeed;
                    pulseAmplitude = claimedPulseAmplitude;
                    return;
                default:
                    pulseSpeed = glowPulseSpeed;
                    pulseAmplitude = glowPulseAmplitude;
                    return;
            }
        }

        private string ResolveLabelText()
        {
            if (_altarState == AltarState.RewardReady && _manifestedRewardRarity.HasValue)
            {
                return _manifestedRewardRarity.Value switch
                {
                    ItemRarity.Common => "대가가 깨어난다",
                    ItemRarity.Uncommon => "기묘한 대가",
                    ItemRarity.Rare => "진귀한 대가",
                    ItemRarity.Legendary => "금단의 대가",
                    ItemRarity.Relic => "유물의 대가",
                    ItemRarity.Boss => "왕관의 대가",
                    _ => "대가가 기다린다"
                };
            }

            return _altarState switch
            {
                AltarState.RewardReady => "대가가 기다린다",
                AltarState.RewardClaimed => "제단이 잠잠해졌다",
                _ => "피의 제단"
            };
        }

        private void SpawnRewardBurst(bool rewardReady)
        {
            Sprite whiteSprite = GetWhiteSprite();
            if (whiteSprite == null)
            {
                return;
            }

            Color themeColor = rewardReady
                ? ResolveRewardReadyBurstColor()
                : rewardClaimedBurstColor;
            Vector2 startSize = rewardReady ? rewardReadyBurstStartSize : rewardClaimedBurstStartSize;
            Vector2 endSize = rewardReady ? rewardReadyBurstEndSize : rewardClaimedBurstEndSize;
            float lifetime = rewardReady ? rewardReadyBurstLifetime : rewardClaimedBurstLifetime;
            float now = Time.unscaledTime;

            CreateBurstLayer(
                rewardReady ? "RewardReadyBurstOuter" : "RewardClaimedBurstOuter",
                whiteSprite,
                Vector3.zero,
                new Vector3(startSize.x, startSize.y, 1f),
                new Vector3(endSize.x, endSize.y, 1f),
                45f,
                themeColor,
                new Color(themeColor.r, themeColor.g, themeColor.b, 0f),
                rewardReady ? 6 : 4,
                now,
                lifetime);

            CreateBurstLayer(
                rewardReady ? "RewardReadyBurstInner" : "RewardClaimedBurstInner",
                whiteSprite,
                Vector3.zero,
                new Vector3(startSize.x * 0.6f, startSize.y * 0.6f, 1f),
                new Vector3(endSize.x * 0.72f, endSize.y * 0.72f, 1f),
                rewardReady ? 0f : 45f,
                Color.Lerp(themeColor, Color.white, rewardReady ? 0.24f : 0.08f),
                new Color(themeColor.r, themeColor.g, themeColor.b, 0f),
                rewardReady ? 7 : 5,
                now,
                lifetime * 0.9f);
        }

        private Color ResolveRewardReadyBurstColor()
        {
            Color synchronizedRewardTheme = _minimapPanelView != null
                ? _minimapPanelView.CurseAltarRewardThemeColor
                : rewardReadyBurstColor;
            return Color.Lerp(synchronizedRewardTheme, ResolveRewardRarityThemeColor(), 0.72f);
        }

        private Color ResolveRewardRarityThemeColor()
        {
            if (!_manifestedRewardRarity.HasValue)
            {
                return rewardReadyGlowColor;
            }

            return _manifestedRewardRarity.Value switch
            {
                ItemRarity.Common => rewardReadyGlowColor,
                ItemRarity.Uncommon => uncommonRewardColor,
                ItemRarity.Rare => rareRewardColor,
                ItemRarity.Legendary => legendaryRewardColor,
                ItemRarity.Relic => relicRewardColor,
                ItemRarity.Boss => bossRewardColor,
                _ => rewardReadyGlowColor
            };
        }

        private void CreateBurstLayer(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 startScale,
            Vector3 endScale,
            float rotationZ,
            Color startColor,
            Color endColor,
            int sortingOrder,
            float spawnTime,
            float lifetime)
        {
            GameObject burstObject = new(name);
            burstObject.transform.SetParent(transform, false);
            burstObject.transform.localPosition = localPosition;
            burstObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            burstObject.transform.localScale = startScale;
            burstObject.layer = gameObject.layer;

            SpriteRenderer spriteRenderer = burstObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = startColor;
            spriteRenderer.sortingOrder = sortingOrder;

            _activeBursts.Add(new ActiveBurst
            {
                Transform = burstObject.transform,
                Renderer = spriteRenderer,
                StartScale = startScale,
                EndScale = endScale,
                StartColor = startColor,
                EndColor = endColor,
                SpawnTime = spawnTime,
                Lifetime = Mathf.Max(0.05f, lifetime)
            });
        }

        private void UpdateActiveBursts()
        {
            if (_activeBursts.Count == 0)
            {
                return;
            }

            float now = Time.unscaledTime;

            for (int i = _activeBursts.Count - 1; i >= 0; i--)
            {
                ActiveBurst burst = _activeBursts[i];

                if (burst.Transform == null || burst.Renderer == null)
                {
                    _activeBursts.RemoveAt(i);
                    continue;
                }

                float normalized = Mathf.Clamp01((now - burst.SpawnTime) / burst.Lifetime);
                burst.Transform.localScale = Vector3.LerpUnclamped(burst.StartScale, burst.EndScale, normalized);
                burst.Renderer.color = Color.LerpUnclamped(burst.StartColor, burst.EndColor, normalized);

                if (normalized >= 1f)
                {
                    Destroy(burst.Transform.gameObject);
                    _activeBursts.RemoveAt(i);
                }
            }
        }

        private void ClearActiveBursts()
        {
            for (int i = _activeBursts.Count - 1; i >= 0; i--)
            {
                if (_activeBursts[i].Transform != null)
                {
                    Destroy(_activeBursts[i].Transform.gameObject);
                }
            }

            _activeBursts.Clear();
        }

        private SpriteRenderer CreateSpriteLayer(
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 localSize,
            float rotationZ,
            Color color,
            int sortingOrder)
        {
            GameObject child = new(name);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            child.transform.localScale = new Vector3(localSize.x, localSize.y, 1f);
            child.layer = gameObject.layer;

            SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_cachedWhiteSprite != null)
            {
                return _cachedWhiteSprite;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CurseRoomEntryWhiteTexture"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            _cachedWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _cachedWhiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return _cachedWhiteSprite;
        }
    }
}
