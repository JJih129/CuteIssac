using System;
using System.Collections;
using System.Collections.Generic;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Pooling;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Candy forest apple bomb gimmick. It is intentionally not IDamageable so bullets cannot destroy it.
    /// Use projectile-ignore obstacle settings on the prefab if a collider is also used for presentation or blocking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppleBombGimmickController : MonoBehaviour
    {
        public enum AppleBombState
        {
            Idle = 0,
            Falling = 1,
            Armed = 2,
            Exploded = 3
        }

        public enum CompletionMode
        {
            Destroy = 0,
            Deactivate = 1
        }

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private AreaDamageGimmick areaDamageGimmick;
        [SerializeField] private GameObject explosionEffectPrefab;

        [Header("Timing")]
        [SerializeField] private bool startOnEnable;
        [SerializeField] [Min(0f)] private float fallHeight = 5f;
        [SerializeField] [Min(0.05f)] private float fallDuration = 0.6f;
        [SerializeField] [Min(0.05f)] private float fuseDuration = 2f;

        [Header("Explosion")]
        [SerializeField] [Min(0.1f)] private float explosionRadius = 1.75f;
        [SerializeField] [Min(0f)] private float damage = 1f;
        [SerializeField] [Min(0f)] private float knockbackForce = 7f;
        [SerializeField] private LayerMask fallbackOverlapMask = Physics2D.AllLayers;
        [SerializeField] [Min(8)] private int fallbackBufferSize = 16;
        [SerializeField] private CompletionMode completionMode = CompletionMode.Destroy;

        [Header("Blink")]
        [SerializeField] [Min(0.1f)] private float blinkSpeed = 7f;
        [SerializeField] [Range(0f, 0.4f)] private float blinkScaleAmount = 0.16f;
        [SerializeField] private Color armedBlinkColor = new(1f, 0.35f, 0.18f, 1f);

        private readonly HashSet<int> _fallbackProcessedDamageables = new();
        private Collider2D[] _fallbackOverlapBuffer;
        private Coroutine _sequenceRoutine;
        private AppleBombState _state = AppleBombState.Idle;
        private Vector3 _baseScale;
        private Color _baseColor = Color.white;
        private bool _cachedVisualState;
        private bool _bodyWasSimulated;
        private bool _bodySimulationOverridden;

        public event Action<AppleBombGimmickController> Completed;

        public AppleBombState State => _state;
        public bool IsRunning => _sequenceRoutine != null;
        public float FallHeight => fallHeight;
        public float FallDuration => fallDuration;
        public float FuseDuration => fuseDuration;
        public float ExplosionRadius => explosionRadius;
        public float Damage => damage;
        public float KnockbackForce => knockbackForce;

        private void Awake()
        {
            ResolveReferences();
            CacheVisualState();
        }

        private void OnEnable()
        {
            if (startOnEnable)
            {
                StartDrop(transform.position);
            }
        }

        private void OnDisable()
        {
            StopSequence();
            RestoreBodySimulation();
        }

        public void StartDrop(Vector2 targetPosition)
        {
            StopSequence();
            ResolveReferences();
            CacheVisualState();
            _sequenceRoutine = StartCoroutine(RunBombSequence(targetPosition));
        }

        [ContextMenu("Start Drop At Current Position")]
        public void StartDropAtCurrentPosition()
        {
            StartDrop(transform.position);
        }

        private IEnumerator RunBombSequence(Vector2 targetPosition)
        {
            PrepareBodyForScriptedMovement();

            // Transform movement is used because the fall is a deterministic telegraph, not physics gameplay.
            Vector3 landingPosition = new(targetPosition.x, targetPosition.y, transform.position.z);
            Vector3 startPosition = landingPosition + (Vector3.up * fallHeight);
            transform.position = startPosition;
            _state = AppleBombState.Falling;

            float elapsed = 0f;
            float safeFallDuration = Mathf.Max(0.05f, fallDuration);

            while (elapsed < safeFallDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeFallDuration);
                float eased = 1f - ((1f - normalized) * (1f - normalized));
                transform.position = Vector3.LerpUnclamped(startPosition, landingPosition, eased);
                yield return null;
            }

            transform.position = landingPosition;
            _state = AppleBombState.Armed;
            elapsed = 0f;

            while (elapsed < fuseDuration)
            {
                elapsed += Time.deltaTime;
                UpdateArmedVisual(elapsed);
                yield return null;
            }

            _sequenceRoutine = null;
            Explode();
        }

        [ContextMenu("Explode Now")]
        public void Explode()
        {
            if (_state == AppleBombState.Exploded)
            {
                return;
            }

            _state = AppleBombState.Exploded;
            StopSequence();
            ApplyExplosionDamage();
            SpawnExplosionEffect();
            Completed?.Invoke(this);
            CompleteLifecycle();
        }

        private void ApplyExplosionDamage()
        {
            if (areaDamageGimmick != null)
            {
                areaDamageGimmick.ConfigureAreaDamage(explosionRadius, damage, knockbackForce, restrictToPlayerOnly: true);
                areaDamageGimmick.ApplyAreaDamage();
                return;
            }

            ApplyFallbackAreaDamage();
        }

        private void ApplyFallbackAreaDamage()
        {
            EnsureFallbackBuffer();
            _fallbackProcessedDamageables.Clear();

            ContactFilter2D contactFilter = BuildFallbackContactFilter();
            Vector2 origin = transform.position;
            int hitCount = Physics2D.OverlapCircle(origin, explosionRadius, contactFilter, _fallbackOverlapBuffer);

            while (hitCount >= _fallbackOverlapBuffer.Length)
            {
                _fallbackOverlapBuffer = new Collider2D[_fallbackOverlapBuffer.Length * 2];
                hitCount = Physics2D.OverlapCircle(origin, explosionRadius, contactFilter, _fallbackOverlapBuffer);
            }

            for (int i = 0; i < hitCount; i++)
            {
                TryApplyFallbackDamage(_fallbackOverlapBuffer[i], origin);
            }

            for (int i = 0; i < hitCount; i++)
            {
                _fallbackOverlapBuffer[i] = null;
            }
        }

        private void TryApplyFallbackDamage(Collider2D hit, Vector2 origin)
        {
            if (hit == null || hit.GetComponentInParent<PlayerHealth>() == null)
            {
                return;
            }

            if (!DamageableResolver.TryResolve(hit, out IDamageable damageable))
            {
                return;
            }

            int targetId = ResolveDamageableId(hit, damageable);

            if (_fallbackProcessedDamageables.Contains(targetId))
            {
                return;
            }

            _fallbackProcessedDamageables.Add(targetId);
            Vector2 hitDirection = (Vector2)hit.bounds.center - origin;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.up;
            }

            damageable.ApplyDamage(new DamageInfo(damage, hitDirection.normalized, transform, knockbackForce));
        }

        private ContactFilter2D BuildFallbackContactFilter()
        {
            ContactFilter2D contactFilter = new()
            {
                useLayerMask = true,
                useTriggers = true
            };
            contactFilter.SetLayerMask(fallbackOverlapMask);
            return contactFilter;
        }

        private void EnsureFallbackBuffer()
        {
            int capacity = Mathf.Max(8, fallbackBufferSize);

            if (_fallbackOverlapBuffer == null || _fallbackOverlapBuffer.Length < capacity)
            {
                _fallbackOverlapBuffer = new Collider2D[capacity];
            }
        }

        private void SpawnExplosionEffect()
        {
            if (explosionEffectPrefab != null)
            {
                PooledEffectSpawner.Spawn(explosionEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        private void CompleteLifecycle()
        {
            RestoreBodySimulation();
            RestoreVisualState();

            if (completionMode == CompletionMode.Deactivate)
            {
                if (TryGetComponent(out PooledObject _))
                {
                    PrefabPoolService.Return(gameObject);
                    return;
                }

                gameObject.SetActive(false);
                return;
            }

            PrefabPoolService.Return(gameObject);
        }

        private void UpdateArmedVisual(float elapsed)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            float pulse = 0.5f + (Mathf.Sin(elapsed * blinkSpeed * Mathf.PI * 2f) * 0.5f);
            spriteRenderer.color = Color.Lerp(_baseColor, armedBlinkColor, pulse);
            transform.localScale = _baseScale * (1f + (pulse * blinkScaleAmount));
        }

        private void PrepareBodyForScriptedMovement()
        {
            if (body == null)
            {
                return;
            }

            _bodyWasSimulated = body.simulated;
            _bodySimulationOverridden = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        private void RestoreBodySimulation()
        {
            if (body != null && _bodySimulationOverridden)
            {
                body.simulated = _bodyWasSimulated;
            }

            _bodySimulationOverridden = false;
        }

        private void StopSequence()
        {
            if (_sequenceRoutine == null)
            {
                return;
            }

            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        private void CacheVisualState()
        {
            if (_cachedVisualState)
            {
                return;
            }

            _baseScale = transform.localScale;
            _baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            _cachedVisualState = true;
        }

        private void RestoreVisualState()
        {
            if (!_cachedVisualState)
            {
                return;
            }

            transform.localScale = _baseScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = _baseColor;
            }
        }

        private void ResolveReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (areaDamageGimmick == null)
            {
                areaDamageGimmick = GetComponent<AreaDamageGimmick>();
            }
        }

        private static int ResolveDamageableId(Collider2D fallbackCollider, IDamageable damageable)
        {
            if (damageable is UnityEngine.Object unityObject)
            {
                return unityObject.GetInstanceID();
            }

            return fallbackCollider != null ? fallbackCollider.GetInstanceID() : 0;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
            fallHeight = Mathf.Max(0f, fallHeight);
            fallDuration = Mathf.Max(0.05f, fallDuration);
            fuseDuration = Mathf.Max(0.05f, fuseDuration);
            explosionRadius = Mathf.Max(0.1f, explosionRadius);
            damage = Mathf.Max(0f, damage);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            blinkSpeed = Mathf.Max(0.1f, blinkSpeed);
            fallbackBufferSize = Mathf.Max(8, fallbackBufferSize);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.24f, 0.12f, 0.88f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
