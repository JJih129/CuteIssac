using CuteIssac.Core.Pooling;
using CuteIssac.Core.Spawning;
using CuteIssac.Data.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    public sealed class SplitterEnemyBrain : EnemyBrain
    {
        [SerializeField] private SplitterEnemyConfigurator configurator;
        [SerializeField] private RoomEnemyMember roomEnemyMember;

        private bool _isSubscribedToDeath;
        private bool _hasPrewarmedChildren;
        private bool _hasSplitDuringCurrentLife;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            SubscribeToDeath();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _hasSplitDuringCurrentLife = false;
            PrewarmChildrenIfNeeded();
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            SplitterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null || !Controller.HasTarget)
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

            Controller.SetMoveSpeedMultiplier(enemyData.ChaseSpeedMultiplier);
            Controller.SetDesiredMoveDirection(toTarget.normalized);
        }

        private void HandleDiedWithSource(EnemyHealth enemyHealth)
        {
            if (_hasSplitDuringCurrentLife || enemyHealth == null || enemyHealth != Controller.EnemyHealth)
            {
                return;
            }

            _hasSplitDuringCurrentLife = true;
            SpawnSplitChildren();
        }

        private void SpawnSplitChildren()
        {
            SplitterEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData?.ChildEnemyPrefab == null)
            {
                return;
            }

            if (roomEnemyMember == null)
            {
                roomEnemyMember = GetComponent<RoomEnemyMember>();
            }

            int childCount = Mathf.Max(1, enemyData.SplitChildCount);
            float angleStep = 360f / childCount;
            float baseAngle = Mathf.Atan2(Controller.TargetPosition.y - Controller.Position.y, Controller.TargetPosition.x - Controller.Position.x) * Mathf.Rad2Deg;
            Vector3 parentPosition = transform.position;
            Transform parent = transform.parent;
            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;

            for (int i = 0; i < childCount; i++)
            {
                float angle = baseAngle + (angleStep * i);
                Vector2 offset = Quaternion.Euler(0f, 0f, angle) * Vector2.right * enemyData.ChildSpawnRadius;
                Vector3 spawnPosition = parentPosition + (Vector3)offset;
                EnemyController childEnemy = GameplaySpawnFactory.SpawnComponent(
                    enemyData.ChildEnemyPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    parent,
                    enemyData.ChildSpawnReusePolicy);

                if (childEnemy == null)
                {
                    continue;
                }

                RoomEnemyMember childRoomEnemyMember = childEnemy.GetComponent<RoomEnemyMember>();

                if (childRoomEnemyMember == null)
                {
                    childRoomEnemyMember = childEnemy.gameObject.AddComponent<RoomEnemyMember>();
                }

                if (assignedRoom != null)
                {
                    childRoomEnemyMember.AssignRoom(assignedRoom);
                }

                childEnemy.ApplySpawnAggroDelay(enemyData.ChildSpawnAggroDelay);
                childEnemy.EnemyMovement?.ApplyImpulse(offset.normalized * enemyData.ChildScatterImpulse);
            }
        }

        private void PrewarmChildrenIfNeeded()
        {
            if (_hasPrewarmedChildren || configurator?.EnemyData == null)
            {
                return;
            }

            SplitterEnemyData enemyData = configurator.EnemyData;

            if (enemyData.ChildEnemyPrefab == null || enemyData.ChildSpawnReusePolicy != SpawnReusePolicy.Pooled)
            {
                return;
            }

            _hasPrewarmedChildren = true;
            PrefabPoolService.Prewarm(enemyData.ChildEnemyPrefab.gameObject, enemyData.ChildPrewarmCount);
        }

        private void SubscribeToDeath()
        {
            if (_isSubscribedToDeath || Controller?.EnemyHealth == null)
            {
                return;
            }

            Controller.EnemyHealth.DiedWithSource += HandleDiedWithSource;
            _isSubscribedToDeath = true;
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<SplitterEnemyConfigurator>();
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

        private void OnDestroy()
        {
            if (_isSubscribedToDeath && Controller?.EnemyHealth != null)
            {
                Controller.EnemyHealth.DiedWithSource -= HandleDiedWithSource;
            }
        }
    }
}
