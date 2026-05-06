using System.Collections;
using CuteIssac.Common.Combat;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Skull turret that warns, then fires a short eye laser toward the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkullLaserTurretController : StationaryGimmickTurretBase
    {
        [Header("Skull Laser")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] [Min(0f)] private float warningDuration = 0.5f;
        [SerializeField] [Min(0.01f)] private float laserDuration = 0.25f;
        [SerializeField] [Min(0.1f)] private float laserRange = 8f;
        [SerializeField] private LayerMask laserHitMask = Physics2D.AllLayers;
        [SerializeField] [Min(4)] private int initialLaserHitBufferSize = 8;
        [SerializeField] private Color warningColor = new(1f, 0.35f, 0.2f, 0.55f);
        [SerializeField] private Color activeLaserColor = new(1f, 0.05f, 0.02f, 1f);

        private RaycastHit2D[] _laserHitBuffer;
        private Coroutine _attackRoutine;

        protected override void Awake()
        {
            ResolveLocalReferences();
            EnsureLaserHitBuffer();
            base.Awake();
            SetLineVisible(false);
        }

        protected override void Reset()
        {
            ResolveLocalReferences();
            EnsureLaserHitBuffer();
            base.Reset();
        }

        protected override void OnValidate()
        {
            ResolveLocalReferences();
            EnsureLaserHitBuffer();
            base.OnValidate();
            warningDuration = Mathf.Max(0f, warningDuration);
            laserDuration = Mathf.Max(0.01f, laserDuration);
            laserRange = Mathf.Max(0.1f, laserRange);
            initialLaserHitBufferSize = Mathf.Max(4, initialLaserHitBufferSize);
        }

        protected override void BeginAttack(PlayerHealth target)
        {
            if (_attackRoutine != null)
            {
                return;
            }

            if (target == null || target.IsDead)
            {
                CompleteAttack();
                return;
            }

            _attackRoutine = StartCoroutine(LaserAttackRoutine(target));
        }

        protected override void OnTurretDeactivated()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            SetLineVisible(false);
            CompleteAttack();
        }

        private IEnumerator LaserAttackRoutine(PlayerHealth target)
        {
            Transform origin = firePoint != null ? firePoint : transform;
            Vector2 direction = ResolveDirectionToTarget(target, origin);
            SetLine(origin.position, direction, warningColor, true);

            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                if (target != null && !target.IsDead)
                {
                    direction = ResolveDirectionToTarget(target, origin);
                    SetLine(origin.position, direction, warningColor, true);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (target != null && !target.IsDead)
            {
                direction = ResolveDirectionToTarget(target, origin);
            }

            SetLine(origin.position, direction, activeLaserColor, true);
            ApplyLaserDamage(origin.position, direction);

            elapsed = 0f;
            while (elapsed < laserDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetLineVisible(false);
            _attackRoutine = null;
            CompleteAttack();
        }

        private void ApplyLaserDamage(Vector2 origin, Vector2 direction)
        {
            EnsureLaserHitBuffer();
            ContactFilter2D contactFilter = new()
            {
                useLayerMask = true,
                useTriggers = true
            };
            contactFilter.SetLayerMask(laserHitMask);

            int hitCount = Physics2D.Raycast(origin, direction, contactFilter, _laserHitBuffer, laserRange);

            while (hitCount >= _laserHitBuffer.Length)
            {
                _laserHitBuffer = new RaycastHit2D[_laserHitBuffer.Length * 2];
                hitCount = Physics2D.Raycast(origin, direction, contactFilter, _laserHitBuffer, laserRange);
            }

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _laserHitBuffer[i];
                _laserHitBuffer[i] = default;

                if (hit.collider == null || hit.transform == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (playerHealth == null || playerHealth.IsDead)
                {
                    continue;
                }

                playerHealth.ApplyDamage(new DamageInfo(Damage, direction, transform));
                return;
            }
        }

        private void SetLine(Vector3 origin, Vector2 direction, Color color, bool visible)
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.positionCount = 2;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + (Vector3)(direction.normalized * laserRange));
            SetLineVisible(visible);
        }

        private void SetLineVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }

        private void ResolveLocalReferences()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponentInChildren<LineRenderer>(true);
            }
        }

        private void EnsureLaserHitBuffer()
        {
            int capacity = Mathf.Max(4, initialLaserHitBufferSize);

            if (_laserHitBuffer == null || _laserHitBuffer.Length < capacity)
            {
                _laserHitBuffer = new RaycastHit2D[capacity];
            }
        }
    }
}
