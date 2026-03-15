using CuteIssac.Data.Enemy;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class TeleporterEnemyBrain : EnemyBrain
    {
        [SerializeField] private TeleporterEnemyConfigurator configurator;
        [SerializeField] private Collider2D ownerCollider;

        private float _orbitSign = 1f;
        private float _teleportCooldown;
        private float _telegraphRemaining;
        private float _postTeleportPauseRemaining;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _orbitSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _teleportCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _postTeleportPauseRemaining = 0f;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            TeleporterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null)
            {
                Controller.StopMovement();
                return;
            }

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.StopMovement();
                return;
            }

            if (_postTeleportPauseRemaining > 0f)
            {
                _postTeleportPauseRemaining -= fixedDeltaTime;
                Controller.StopMovement();
                return;
            }

            if (_telegraphRemaining > 0f)
            {
                _telegraphRemaining -= fixedDeltaTime;
                Controller.StopMovement();
                Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TeleportTelegraphColor);

                if (_telegraphRemaining <= 0f)
                {
                    PerformTeleport(enemyData);
                }

                return;
            }

            float distance = toTarget.magnitude;
            Vector2 chaseDirection = toTarget / distance;
            Vector2 moveDirection = ResolveMoveDirection(enemyData, chaseDirection, distance);
            Controller.SetMoveSpeedMultiplier(distance >= enemyData.MaxTeleportDistance ? enemyData.SurgeSpeedMultiplier : 1f);
            Controller.SetDesiredMoveDirection(moveDirection.normalized);

            _teleportCooldown = Mathf.Max(0f, _teleportCooldown - fixedDeltaTime);

            if (_teleportCooldown <= 0f && distance <= enemyData.MaxTeleportDistance + 1f)
            {
                _telegraphRemaining = enemyData.TeleportTelegraphDuration * _runtimeTelegraphDurationMultiplier;
                Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TeleportTelegraphColor);
            }
        }

        private Vector2 ResolveMoveDirection(TeleporterEnemyData enemyData, Vector2 chaseDirection, float distance)
        {
            if (distance <= enemyData.OrbitRange)
            {
                Vector2 perpendicular = new(-chaseDirection.y, chaseDirection.x * _orbitSign);
                return ((chaseDirection * (1f - enemyData.OrbitBlend)) + (perpendicular * enemyData.OrbitBlend)).normalized;
            }

            return chaseDirection;
        }

        private void PerformTeleport(TeleporterEnemyData enemyData)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();

            Vector3 bestCandidate = transform.position;
            float bestScore = float.MinValue;
            Vector2 targetPosition = Controller.TargetPosition;
            float minDistance = enemyData.MinTeleportDistance;
            float maxDistance = enemyData.MaxTeleportDistance;

            for (int sampleIndex = 0; sampleIndex < enemyData.TeleportSampleCount; sampleIndex++)
            {
                float angle = ((360f / enemyData.TeleportSampleCount) * sampleIndex + (GetInstanceID() % 37)) * Mathf.Deg2Rad;
                float radiusLerp = enemyData.TeleportSampleCount <= 1 ? 0.5f : sampleIndex / (float)(enemyData.TeleportSampleCount - 1);
                float radius = Mathf.Lerp(minDistance, maxDistance, Mathf.Clamp01(radiusLerp));
                Vector3 candidate = (Vector3)targetPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

                if (IsBlocked(candidate, enemyData.TeleportCollisionCheckRadius))
                {
                    continue;
                }

                float score = Vector2.Distance(candidate, transform.position) + Vector2.Distance(candidate, targetPosition) * 0.35f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            transform.position = bestCandidate;
            Controller.StopMovement();
            Controller.EnemyVisual?.HandleAttack();
            _teleportCooldown = enemyData.TeleportInterval;
            _postTeleportPauseRemaining = enemyData.PostTeleportPause;
            _orbitSign *= -1f;
        }

        private bool IsBlocked(Vector3 candidate, float checkRadius)
        {
            if (checkRadius <= 0f)
            {
                return false;
            }

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(candidate, checkRadius);

            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];

                if (overlap == null || overlap == ownerCollider || overlap.transform == transform || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (overlap.GetComponentInParent<PlayerController>() != null)
                {
                    continue;
                }

                if (overlap.isTrigger)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<TeleporterEnemyConfigurator>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
