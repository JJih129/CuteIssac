using CuteIssac.Common.Combat;
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
        [SerializeField] private RoomController roomController;
        [SerializeField] [Min(0.25f)] private float radius = 1f;
        [SerializeField] [Min(0.1f)] private float telegraphDuration = 0.9f;
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0f)] private float knockbackForce = 4f;

        [Header("Presentation")]
        [SerializeField] private Color accentColor = new(1f, 0.6f, 0.24f, 1f);
        [SerializeField] [Range(0.05f, 1f)] private float telegraphOpacity = 0.56f;
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
        private SpriteRenderer _ringRenderer;
        private SpriteRenderer _fillRenderer;
        private SpriteRenderer _coreRenderer;

        public Vector2 WorldPosition => transform.position;
        public float Radius => radius;
        public bool IsTelegraphing => _phase == HazardPhase.Telegraph;

        private void Awake()
        {
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (roomController != null && roomController.State != RoomState.Combat)
            {
                DestroySelf();
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
                DestroySelf();
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
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            BuildVisual();
            UpdateTelegraphVisual();
        }

        public void ForceDissipate()
        {
            DestroySelf();
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
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);

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

        private void BuildVisual()
        {
            ClearVisual();

            GameObject root = new("PulseVisual");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            _visualRoot = root.transform;

            _ringRenderer = CreatePart("Ring", GetCircleSprite(), 1);
            _fillRenderer = CreatePart("Fill", GetCircleSprite(), 2);
            _coreRenderer = CreatePart("Core", GetWhiteSprite(), 3);
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, int sortingOrder)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_visualRoot, false);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
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
            float ringScale = diameterScale * Mathf.Lerp(0.8f, 1.05f, normalized);
            float fillScale = diameterScale * Mathf.Lerp(0.36f, 0.96f, normalized);
            float coreScale = diameterScale * Mathf.Lerp(0.08f, 0.18f, pulse);
            _visualRoot.localScale = Vector3.one * Mathf.Lerp(0.98f, 1.05f, pulse * 0.42f);

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                _ringRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Clamp01((telegraphOpacity * 0.22f) + (normalized * telegraphOpacity * 0.46f)));
            }

            if (_fillRenderer != null)
            {
                _fillRenderer.transform.localScale = new Vector3(fillScale, fillScale, 1f);
                _fillRenderer.color = new Color(
                    accentColor.r,
                    accentColor.g,
                    accentColor.b,
                    Mathf.Clamp01((telegraphOpacity * 0.06f) + (normalized * telegraphOpacity * 0.18f)));
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = new Vector3(coreScale, coreScale, 1f);
                _coreRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01((telegraphOpacity * 0.46f) + (pulse * 0.18f)));
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

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = new Vector3(diameterScale * 1.06f, diameterScale * 1.06f, 1f);
                _ringRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.72f, 0f, normalized));
            }

            if (_fillRenderer != null)
            {
                _fillRenderer.transform.localScale = new Vector3(diameterScale, diameterScale, 1f);
                _fillRenderer.color = new Color(accentColor.r, accentColor.g, accentColor.b, Mathf.Lerp(0.38f, 0f, normalized));
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = new Vector3(diameterScale * 0.2f, diameterScale * 0.2f, 1f);
                _coreRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, normalized));
            }
        }

        private void DestroySelf()
        {
            ClearVisual();

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void ClearVisual()
        {
            if (_visualRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_visualRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_visualRoot.gameObject);
            }

            _visualRoot = null;
            _ringRenderer = null;
            _fillRenderer = null;
            _coreRenderer = null;
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
