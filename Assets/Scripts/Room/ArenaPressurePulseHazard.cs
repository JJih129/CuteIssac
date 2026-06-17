using System;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Scene;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Telegraphs a short-lived floor danger zone, then damages the player if they remain inside when it detonates.
    /// Runtime-generated visuals keep the hazard swappable without blocking gameplay iteration on final art.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaPressurePulseHazard : MonoBehaviour
    {
        private const float SpriteWorldSize = 0.64f;

        [Header("Hazard")]
        [Tooltip("씬에 배치된 GameplaySceneContext입니다. 런타임 생성 시 비어 있으면 Active Context를 사용합니다.")]
        [SerializeField] private GameplaySceneContext sceneContext;
        [SerializeField] private RoomController roomController;
        [SerializeField] [Min(0.25f)] private float radius = 1f;
        [SerializeField] [Min(0.1f)] private float telegraphDuration = 0.9f;
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0f)] private float knockbackForce = 4f;

        [Header("Presentation")]
        [SerializeField] private Color accentColor = new(1f, 0.6f, 0.24f, 1f);
        [SerializeField] [Range(0.05f, 1f)] private float telegraphOpacity = 0.56f;
        [SerializeField] private Color outlineColor = new(0.08f, 0.05f, 0.1f, 0.92f);
        [SerializeField] [Range(0.05f, 1f)] private float outlineOpacity = 0.88f;
        [SerializeField] [Min(1f)] private float outlineScaleMultiplier = 1.18f;
        [SerializeField] [Min(0.05f)] private float burstHoldDuration = 0.22f;
        [SerializeField] [Min(0.1f)] private float pulseSpeed = 5.6f;

        private enum HazardPhase
        {
            Telegraph = 0,
            Burst = 1
        }

        private static Sprite s_CircleSprite;
        private static Sprite s_WhiteSprite;

        private HazardPhase _phase;
        private float _phaseTimer;
        private float _phaseOffset;
        private Transform _visualRoot;
        private SpriteRenderer _outlineRenderer;
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _fillRenderer;
        private SpriteRenderer _coreRenderer;

        public Vector2 WorldPosition => transform.position;
        public float Radius => radius;
        public bool IsTelegraphing => _phase == HazardPhase.Telegraph;
        public event Action<ArenaPressurePulseHazard> Completed;

        private void Awake()
        {
            _phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (roomController != null && roomController.State != RoomState.Combat)
            {
                CompleteSelf();
                return;
            }

            _phaseTimer -= Time.deltaTime;

            if (_phase == HazardPhase.Telegraph)
            {
                UpdateTelegraphVisual();

                if (_phaseTimer <= 0f)
                {
                    Detonate();
                }

                return;
            }

            UpdateBurstVisual();

            if (_phaseTimer <= 0f)
            {
                CompleteSelf();
            }
        }

        public void Configure(
            RoomController ownerRoom,
            float configuredRadius,
            float configuredTelegraphDuration,
            float configuredDamage,
            float configuredKnockbackForce,
            Color configuredAccentColor)
        {
            roomController = ownerRoom;
            radius = Mathf.Max(0.25f, configuredRadius);
            telegraphDuration = Mathf.Max(0.1f, configuredTelegraphDuration);
            damage = Mathf.Max(0f, configuredDamage);
            knockbackForce = Mathf.Max(0f, configuredKnockbackForce);
            accentColor = configuredAccentColor;
            _phase = HazardPhase.Telegraph;
            _phaseTimer = telegraphDuration;
            _phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            BuildVisual();
            UpdateTelegraphVisual();
        }

        public void ForceDissipate()
        {
            CompleteSelf();
        }

        public void ResetForReuse()
        {
            roomController = null;
            _phaseTimer = 0f;
            HideVisual();
        }

        private void Detonate()
        {
            ApplyDamageIfPlayerInside();
            _phase = HazardPhase.Burst;
            _phaseTimer = Mathf.Max(0.05f, burstHoldDuration);
            UpdateBurstVisual();
        }

        private void ApplyDamageIfPlayerInside()
        {
            PlayerHealth playerHealth = ResolvePlayerHealth();

            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            Vector2 playerPosition = playerHealth.transform.position;
            Vector2 hazardPosition = transform.position;
            Vector2 offset = playerPosition - hazardPosition;

            if (offset.sqrMagnitude > radius * radius)
            {
                return;
            }

            if (offset.sqrMagnitude <= 0.0001f)
            {
                offset = Vector2.up;
            }

            playerHealth.ApplyDamage(new DamageInfo(damage, offset.normalized, transform, knockbackForce));
        }

        private PlayerHealth ResolvePlayerHealth()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext != null)
            {
                sceneContext.ResolveMissingReferences();

                if (sceneContext.PlayerHealth != null)
                {
                    return sceneContext.PlayerHealth;
                }
            }

            return FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        }

        private void BuildVisual()
        {
            EnsureVisualRoot();
            _visualRoot.gameObject.SetActive(true);
            _visualRoot.gameObject.layer = gameObject.layer;
            _visualRoot.localPosition = Vector3.zero;

            EnsurePart(ref _outlineRenderer, "Outline", GetCircleSprite(), 0);
            EnsurePart(ref _ringRenderer, "Ring", GetCircleSprite(), 1);
            EnsurePart(ref _fillRenderer, "Fill", GetCircleSprite(), 2);
            EnsurePart(ref _coreRenderer, "Core", GetWhiteSprite(), 3);
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot != null)
            {
                return;
            }

            GameObject root = new("PulseVisual");
            root.transform.SetParent(transform, false);
            _visualRoot = root.transform;
        }

        private void EnsurePart(ref SpriteRenderer renderer, string name, Sprite sprite, int sortingOrder)
        {
            if (renderer == null)
            {
                Transform partTransform = _visualRoot.Find(name);

                if (partTransform == null)
                {
                    GameObject child = new(name);
                    partTransform = child.transform;
                    partTransform.SetParent(_visualRoot, false);
                }

                renderer = partTransform.GetComponent<SpriteRenderer>();

                if (renderer == null)
                {
                    renderer = partTransform.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            renderer.gameObject.SetActive(true);
            renderer.gameObject.layer = gameObject.layer;
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }

        private void UpdateTelegraphVisual()
        {
            if (_visualRoot == null)
            {
                return;
            }

            float normalized = 1f - Mathf.Clamp01(_phaseTimer / Mathf.Max(0.01f, telegraphDuration));
            float pulse = 0.5f + (0.5f * Mathf.Sin((Time.time * pulseSpeed) + _phaseOffset));
            float diameterScale = (radius * 2f) / SpriteWorldSize;
            float ringScale = diameterScale * Mathf.Lerp(0.84f, 1.08f, normalized);
            float fillScale = diameterScale * Mathf.Lerp(0.38f, 0.98f, normalized);
            float coreScale = diameterScale * Mathf.Lerp(0.1f, 0.2f, pulse);
            _visualRoot.localScale = Vector3.one * Mathf.Lerp(0.98f, 1.05f, pulse * 0.42f);

            if (_outlineRenderer != null)
            {
                float outlineScale = diameterScale * outlineScaleMultiplier * Mathf.Lerp(0.96f, 1.1f, normalized);
                _outlineRenderer.transform.localScale = new Vector3(outlineScale, outlineScale, 1f);
                _outlineRenderer.color = new Color(
                    outlineColor.r,
                    outlineColor.g,
                    outlineColor.b,
                    Mathf.Clamp01((outlineOpacity * 0.48f) + (normalized * outlineOpacity * 0.42f)));
            }

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                _ringRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Clamp01((telegraphOpacity * 0.32f) + (normalized * telegraphOpacity * 0.48f)));
            }

            if (_fillRenderer != null)
            {
                _fillRenderer.transform.localScale = new Vector3(fillScale, fillScale, 1f);
                _fillRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Clamp01((telegraphOpacity * 0.12f) + (normalized * telegraphOpacity * 0.28f)));
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = new Vector3(coreScale, coreScale, 1f);
                Color coreColor = Color.Lerp(accentColor, Color.white, 0.18f);
                coreColor.a = Mathf.Clamp01((telegraphOpacity * 0.52f) + (pulse * 0.16f));
                _coreRenderer.color = coreColor;
            }
        }

        private void UpdateBurstVisual()
        {
            if (_visualRoot == null)
            {
                return;
            }

            float normalized = 1f - Mathf.Clamp01(_phaseTimer / Mathf.Max(0.01f, burstHoldDuration));
            float diameterScale = (radius * 2f) / SpriteWorldSize;
            float burstScale = Mathf.Lerp(1f, 1.24f, normalized);
            _visualRoot.localScale = Vector3.one * burstScale;

            if (_outlineRenderer != null)
            {
                float outlineScale = diameterScale * outlineScaleMultiplier;
                _outlineRenderer.transform.localScale = new Vector3(outlineScale, outlineScale, 1f);
                _outlineRenderer.color = new Color(
                    outlineColor.r,
                    outlineColor.g,
                    outlineColor.b,
                    Mathf.Lerp(outlineOpacity * 0.88f, 0f, normalized));
            }

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = new Vector3(diameterScale * 1.06f, diameterScale * 1.06f, 1f);
                _ringRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Lerp(0.76f, 0f, normalized));
            }

            if (_fillRenderer != null)
            {
                _fillRenderer.transform.localScale = new Vector3(diameterScale, diameterScale, 1f);
                _fillRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Lerp(0.42f, 0f, normalized));
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = new Vector3(diameterScale * 0.2f, diameterScale * 0.2f, 1f);
                Color coreColor = Color.Lerp(accentColor, Color.white, 0.12f);
                coreColor.a = Mathf.Lerp(1f, 0f, normalized);
                _coreRenderer.color = coreColor;
            }
        }

        private void CompleteSelf()
        {
            HideVisual();
            Completed?.Invoke(this);
        }

        private void HideVisual()
        {
            if (_visualRoot == null)
            {
                return;
            }

            _visualRoot.gameObject.SetActive(false);
        }

        private static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null)
            {
                return s_CircleSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeArenaPressureCircle"
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - normalizedDistance);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            s_CircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_CircleSprite;
        }

        private static Sprite GetWhiteSprite()
        {
            if (s_WhiteSprite != null)
            {
                return s_WhiteSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeArenaPressureWhite"
            };

            Color[] colors = new Color[size * size];

            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = Color.white;
            }

            texture.SetPixels(colors);
            texture.Apply();
            s_WhiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_WhiteSprite;
        }

    }
}
