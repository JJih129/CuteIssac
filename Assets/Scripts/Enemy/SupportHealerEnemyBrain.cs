using CuteIssac.Data.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class SupportHealerEnemyBrain : EnemyBrain
    {
        [SerializeField] private SupportHealerEnemyConfigurator configurator;
        [SerializeField] private RoomEnemyMember roomEnemyMember;

        private float _healCooldown;
        private float _telegraphRemaining;
        private float _allyScanRemaining;
        private float _strafeSwapRemaining;
        private float _strafeSign = 1f;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;
        private EnemyHealth _preparedHealTarget;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            SupportHealerEnemyData enemyData = configurator != null ? configurator.EnemyData : null;
            _healCooldown = _runtimeFirstAttackDelayBonus;
            _telegraphRemaining = 0f;
            _preparedHealTarget = null;
            _allyScanRemaining = 0f;
            _strafeSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            _strafeSwapRemaining = enemyData != null ? enemyData.StrafeSwapInterval : 1f;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            SupportHealerEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

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

            float distanceToPlayer = toTarget.magnitude;
            Vector2 aimDirection = toTarget / distanceToPlayer;

            if (_telegraphRemaining > 0f)
            {
                _telegraphRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(enemyData.MoveSpeedWhileTelegraphing);
                Controller.StopMovement();

                if (_telegraphRemaining <= 0f)
                {
                    ExecuteHeal(enemyData);
                }

                return;
            }

            _healCooldown = Mathf.Max(0f, _healCooldown - fixedDeltaTime);
            _allyScanRemaining = Mathf.Max(0f, _allyScanRemaining - fixedDeltaTime);
            _strafeSwapRemaining -= fixedDeltaTime;

            if (_strafeSwapRemaining <= 0f)
            {
                _strafeSwapRemaining = enemyData.StrafeSwapInterval;
                _strafeSign *= -1f;
            }

            Vector2 moveDirection = ResolveMoveDirection(enemyData, aimDirection, distanceToPlayer);
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection(moveDirection);

            if (_healCooldown > 0f)
            {
                return;
            }

            if (_allyScanRemaining <= 0f || !IsValidHealTarget(_preparedHealTarget, enemyData.HealRange))
            {
                _preparedHealTarget = FindHealTarget(enemyData.HealRange);
                _allyScanRemaining = enemyData.AllyScanInterval;
            }

            if (_preparedHealTarget == null)
            {
                return;
            }

            _telegraphRemaining = enemyData.TelegraphDuration * _runtimeTelegraphDurationMultiplier;
            Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);
        }

        private Vector2 ResolveMoveDirection(SupportHealerEnemyData enemyData, Vector2 aimDirection, float distanceToPlayer)
        {
            if (distanceToPlayer > enemyData.PreferredRange)
            {
                return aimDirection;
            }

            if (distanceToPlayer < enemyData.RetreatRange)
            {
                return -aimDirection;
            }

            Vector2 strafeDirection = new(-aimDirection.y, aimDirection.x * _strafeSign);
            return strafeDirection * Mathf.Clamp01(enemyData.StrafeBlend);
        }

        private void ExecuteHeal(SupportHealerEnemyData enemyData)
        {
            Controller.EnemyVisual?.StopAttackTelegraph();
            Controller.EnemyVisual?.HandleAttack();
            _healCooldown = enemyData.HealInterval;

            EnemyHealth healTarget = IsValidHealTarget(_preparedHealTarget, enemyData.HealRange)
                ? _preparedHealTarget
                : FindHealTarget(enemyData.HealRange);

            _preparedHealTarget = null;

            if (healTarget == null)
            {
                return;
            }

            healTarget.RestoreHealth(enemyData.HealAmount);
        }

        private EnemyHealth FindHealTarget(float healRange)
        {
            EnemyHealth[] candidates = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;
            Vector2 position = Controller.Position;
            EnemyHealth bestTarget = null;
            float bestMissingHealth = 0f;
            float bestDistanceSq = float.MaxValue;
            float maxDistanceSq = healRange * healRange;

            for (int i = 0; i < candidates.Length; i++)
            {
                EnemyHealth candidate = candidates[i];

                if (!IsValidHealTarget(candidate, healRange))
                {
                    continue;
                }

                if (candidate == Controller.EnemyHealth)
                {
                    continue;
                }

                RoomEnemyMember candidateRoomMember = candidate.GetComponent<RoomEnemyMember>();

                if (assignedRoom != null && (candidateRoomMember == null || candidateRoomMember.AssignedRoom != assignedRoom))
                {
                    continue;
                }

                float distanceSq = ((Vector2)candidate.transform.position - position).sqrMagnitude;

                if (distanceSq > maxDistanceSq)
                {
                    continue;
                }

                float missingHealth = candidate.MaxHealth - candidate.CurrentHealth;

                if (missingHealth > bestMissingHealth + 0.01f
                    || (Mathf.Abs(missingHealth - bestMissingHealth) <= 0.01f && distanceSq < bestDistanceSq))
                {
                    bestTarget = candidate;
                    bestMissingHealth = missingHealth;
                    bestDistanceSq = distanceSq;
                }
            }

            return bestTarget;
        }

        private bool IsValidHealTarget(EnemyHealth candidate, float healRange)
        {
            if (candidate == null || candidate.IsDead || candidate.CurrentHealth >= candidate.MaxHealth)
            {
                return false;
            }

            float distanceSq = ((Vector2)candidate.transform.position - Controller.Position).sqrMagnitude;
            return distanceSq <= healRange * healRange;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<SupportHealerEnemyConfigurator>();
            }

            if (roomEnemyMember == null)
            {
                roomEnemyMember = GetComponent<RoomEnemyMember>();
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
