using UnityEngine;
using CuteIssac.Core.Pooling;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Presentation-only bomb view.
    /// Designers can replace the body sprite, pulse root, and explosion effect without touching gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BombVisual : MonoBehaviour
    {
        [Header("Visual References")]
        [Tooltip("Optional sprite renderer for the bomb body.")]
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [Tooltip("Optional root that scales during the countdown pulse.")]
        [SerializeField] private Transform pulseRoot;

        [Header("Effect Prefabs")]
        [Tooltip("Optional effect spawned when the bomb is armed.")]
        [SerializeField] private GameObject armedEffectPrefab;
        [Tooltip("Optional effect spawned when the bomb explodes.")]
        [SerializeField] private ExplosionEffect explosionEffectPrefab;
        [Tooltip("Optional anchor used for spawned effects.")]
        [SerializeField] private Transform effectAnchor;

        [Header("Colors")]
        [SerializeField] private Color idleColor = new(0.14f, 0.14f, 0.14f, 1f);
        [SerializeField] private Color warningColor = new(1f, 0.46f, 0.18f, 1f);
        [SerializeField] private Color explodedColor = new(1f, 0.88f, 0.5f, 0f);

        [Header("Pulse")]
        [SerializeField] [Min(0f)] private float minPulseScale = 0.9f;
        [SerializeField] [Min(0f)] private float maxPulseScale = 1.2f;

        private Vector3 _initialPulseLocalScale = Vector3.one;
        private bool _cachedPulseScale;
        private bool _warnedMissingRenderer;

        public void HandleArmed()
        {
            CachePulseScale();
            ApplyColor(idleColor);
            SetPulseScale(1f);
            SpawnEffect(armedEffectPrefab);
        }

        public void HandleCountdown(float normalizedRemaining)
        {
            CachePulseScale();

            float elapsed = 1f - Mathf.Clamp01(normalizedRemaining);
            float pulseAmount = Mathf.Lerp(minPulseScale, maxPulseScale, elapsed);
            SetPulseScale(pulseAmount);
            ApplyColor(Color.Lerp(idleColor, warningColor, elapsed));
        }

        public void HandleExploded(float explosionRadius)
        {
            ApplyColor(explodedColor);
            SpawnExplosionEffect(explosionRadius);
        }

        private void Reset()
        {
            bodySpriteRenderer = GetComponent<SpriteRenderer>();
            pulseRoot = transform;
        }

        private void OnValidate()
        {
            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (pulseRoot == null)
            {
                pulseRoot = transform;
            }

            CachePulseScale();
        }

        private void CachePulseScale()
        {
            if (_cachedPulseScale || pulseRoot == null)
            {
                return;
            }

            _initialPulseLocalScale = pulseRoot.localScale;
            _cachedPulseScale = true;
        }

        private void SetPulseScale(float multiplier)
        {
            if (pulseRoot == null)
            {
                return;
            }

            if (!_cachedPulseScale)
            {
                CachePulseScale();
            }

            pulseRoot.localScale = _initialPulseLocalScale * Mathf.Max(0.01f, multiplier);
        }

        private void ApplyColor(Color color)
        {
            if (bodySpriteRenderer == null)
            {
                if (!_warnedMissingRenderer)
                {
                    Debug.LogWarning("BombVisual has no SpriteRenderer assigned. Countdown feedback will be skipped.", this);
                    _warnedMissingRenderer = true;
                }

                return;
            }

            bodySpriteRenderer.color = color;
        }

        private void SpawnEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = effectAnchor != null ? effectAnchor.position : transform.position;
            Quaternion spawnRotation = effectAnchor != null ? effectAnchor.rotation : Quaternion.identity;
            PrefabPoolService.Spawn(effectPrefab, spawnPosition, spawnRotation);
        }

        private void SpawnExplosionEffect(float explosionRadius)
        {
            if (explosionEffectPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = effectAnchor != null ? effectAnchor.position : transform.position;
            Quaternion spawnRotation = effectAnchor != null ? effectAnchor.rotation : Quaternion.identity;
            ExplosionEffect effectInstance = PrefabPoolService.Spawn(explosionEffectPrefab, spawnPosition, spawnRotation);

            if (effectInstance != null)
            {
                effectInstance.Configure(explosionRadius);
            }
        }
    }
}
