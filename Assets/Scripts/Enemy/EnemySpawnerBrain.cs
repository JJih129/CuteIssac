using System.Collections.Generic;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Spawning;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Keeps distance from the player and periodically summons helper enemies.
    /// Summoned enemies are registered to the same room so clear conditions remain correct.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnerBrain : EnemyBrain
    {
        [Header("Summon")]
        [SerializeField] private EnemyController[] summonPrefabs;
        [SerializeField] private Transform summonAnchor;
        [SerializeField] private Transform spawnedEnemyParent;
        [SerializeField] [Min(0.5f)] private float summonCooldown = 4.8f;
        [SerializeField] [Min(0f)] private float initialSummonDelay = 0.8f;
        [SerializeField] [Min(0.05f)] private float summonWindup = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float summonWindupMoveSpeedMultiplier = 0.35f;
        [SerializeField] [Min(1)] private int summonCountPerCycle = 1;
        [SerializeField] [Min(1)] private int maxAliveSummons = 3;
        [SerializeField] [Min(0f)] private float summonTriggerRange = 5.5f;
        [SerializeField] [Min(0f)] private float summonScatterRadius = 0.85f;
        [SerializeField] private Color summonTelegraphColor = new(0.98f, 0.74f, 0.3f, 1f);
        [SerializeField] private SpawnReusePolicy summonSpawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int summonPrewarmBufferCount = 1;

        [Header("Movement")]
        [SerializeField] [Min(0f)] private float preferredRange = 4.75f;
        [SerializeField] [Min(0f)] private float retreatRange = 2.6f;
        [SerializeField] [Range(0f, 1f)] private float strafeBlend = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.4f;

        private readonly List<EnemyHealth> _aliveSummons = new();

        private RoomEnemyMember _roomEnemyMember;
        private float _summonCooldownRemaining;
        private float _summonWindupRemaining;
        private float _initialSummonDelayRemaining;
        private int _spawnCursor;
        private float _orbitSign = 1f;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;

        protected override void HandleInitialized()
        {
            _roomEnemyMember = GetComponent<RoomEnemyMember>();

            if (summonAnchor == null && Controller.EnemyVisual != null)
            {
                summonAnchor = Controller.EnemyVisual.AttackEffectAnchor;
            }

            PrewarmSummonPrefabs();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            ClearSummonTracking();
            _summonWindupRemaining = 0f;
            _initialSummonDelayRemaining = initialSummonDelay + _runtimeFirstAttackDelayBonus;
            _summonCooldownRemaining = summonCooldown * 0.45f;
            _spawnCursor = 0;
            _orbitSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            CleanupSummons();

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.EnemyVisual?.StopAttackTelegraph();
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 aimDirection = toTarget / distance;
            _initialSummonDelayRemaining = Mathf.Max(0f, _initialSummonDelayRemaining - fixedDeltaTime);

            if (_summonWindupRemaining > 0f)
            {
                _summonWindupRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(summonWindupMoveSpeedMultiplier);
                Controller.StopMovement();

                if (_summonWindupRemaining <= 0f)
                {
                    ExecuteSummon();
                    Controller.EnemyVisual?.StopAttackTelegraph();
                    Controller.EnemyVisual?.HandleAttack();
                    _summonCooldownRemaining = summonCooldown;
                    Controller.SetMoveSpeedMultiplier(1f);
                }

                return;
            }

            _summonCooldownRemaining = Mathf.Max(0f, _summonCooldownRemaining - fixedDeltaTime);

            if (CanStartSummon(distance))
            {
                _summonWindupRemaining = summonWindup * _runtimeTelegraphDurationMultiplier;
                Controller.EnemyVisual?.StartAttackTelegraph(summonTelegraphColor);
                return;
            }

            Controller.EnemyVisual?.StopAttackTelegraph();
            Controller.SetMoveSpeedMultiplier(1f);
            Controller.SetDesiredMoveDirection(ResolveMoveDirection(aimDirection, distance));
        }

        private bool CanStartSummon(float distance)
        {
            return _summonCooldownRemaining <= 0f
                && _initialSummonDelayRemaining <= 0f
                && distance <= summonTriggerRange
                && CountAvailableSummonSlots() > 0
                && HasSummonPrefab();
        }

        private int CountAvailableSummonSlots()
        {
            return Mathf.Max(0, maxAliveSummons - _aliveSummons.Count);
        }

        private bool HasSummonPrefab()
        {
            if (summonPrefabs == null || summonPrefabs.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < summonPrefabs.Length; i++)
            {
                if (summonPrefabs[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2 ResolveMoveDirection(Vector2 aimDirection, float distance)
        {
            Vector2 perpendicular = new Vector2(-aimDirection.y, aimDirection.x * _orbitSign);

            if (distance < retreatRange)
            {
                return (-aimDirection + (perpendicular * orbitBlend)).normalized;
            }

            if (distance > preferredRange)
            {
                return (aimDirection + (perpendicular * 0.2f)).normalized;
            }

            return perpendicular * Mathf.Clamp01(strafeBlend);
        }

        private void ExecuteSummon()
        {
            int summonsToSpawn = Mathf.Min(summonCountPerCycle, CountAvailableSummonSlots());

            if (summonsToSpawn <= 0)
            {
                return;
            }

            PrewarmSummonPrefabs();

            for (int i = 0; i < summonsToSpawn; i++)
            {
                EnemyController summonPrefab = ResolveNextSummonPrefab();

                if (summonPrefab == null)
                {
                    continue;
                }

                Vector3 spawnPosition = ResolveSummonPosition(i);
                Transform parent = spawnedEnemyParent != null ? spawnedEnemyParent : transform.parent;
                EnemyController spawnedEnemy = GameplaySpawnFactory.SpawnComponent(
                    summonPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    parent,
                    summonSpawnReusePolicy);

                if (spawnedEnemy == null)
                {
                    continue;
                }

                RoomEnemyMember roomEnemyMember = spawnedEnemy.GetComponent<RoomEnemyMember>();

                if (roomEnemyMember == null)
                {
                    roomEnemyMember = spawnedEnemy.gameObject.AddComponent<RoomEnemyMember>();
                }

                RoomController roomController = _roomEnemyMember != null ? _roomEnemyMember.AssignedRoom : null;

                if (roomController != null)
                {
                    roomEnemyMember.AssignRoom(roomController);
                }

                EnemyHealth summonHealth = spawnedEnemy.GetComponent<EnemyHealth>();

                if (summonHealth != null)
                {
                    summonHealth.Died += HandleSummonDied;
                    _aliveSummons.Add(summonHealth);
                }
            }
        }

        private EnemyController ResolveNextSummonPrefab()
        {
            if (summonPrefabs == null || summonPrefabs.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < summonPrefabs.Length; i++)
            {
                int index = (_spawnCursor + i) % summonPrefabs.Length;
                EnemyController summonPrefab = summonPrefabs[index];

                if (summonPrefab == null)
                {
                    continue;
                }

                _spawnCursor = index + 1;
                return summonPrefab;
            }

            return null;
        }

        private void PrewarmSummonPrefabs()
        {
            if (summonSpawnReusePolicy != SpawnReusePolicy.Pooled || summonPrefabs == null || summonPrefabs.Length == 0)
            {
                return;
            }

            int prewarmCount = Mathf.Max(1, maxAliveSummons + summonPrewarmBufferCount);

            for (int i = 0; i < summonPrefabs.Length; i++)
            {
                EnemyController summonPrefab = summonPrefabs[i];

                if (summonPrefab == null)
                {
                    continue;
                }

                PrefabPoolService.Prewarm(summonPrefab.gameObject, prewarmCount);
            }
        }

        private Vector3 ResolveSummonPosition(int summonIndex)
        {
            Transform anchor = summonAnchor != null ? summonAnchor : transform;
            float angle = ((360f / Mathf.Max(1, summonCountPerCycle)) * summonIndex) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * summonScatterRadius;
            return anchor.position + offset;
        }

        private void CleanupSummons()
        {
            for (int i = _aliveSummons.Count - 1; i >= 0; i--)
            {
                EnemyHealth summonHealth = _aliveSummons[i];

                if (summonHealth != null && !summonHealth.IsDead)
                {
                    continue;
                }

                if (summonHealth != null)
                {
                    summonHealth.Died -= HandleSummonDied;
                }

                _aliveSummons.RemoveAt(i);
            }
        }

        private void ClearSummonTracking()
        {
            for (int i = _aliveSummons.Count - 1; i >= 0; i--)
            {
                EnemyHealth summonHealth = _aliveSummons[i];

                if (summonHealth != null)
                {
                    summonHealth.Died -= HandleSummonDied;
                }
            }

            _aliveSummons.Clear();
        }

        private void HandleSummonDied()
        {
            CleanupSummons();
        }

        private void Reset()
        {
            EnemyVisual enemyVisual = GetComponent<EnemyVisual>();
            summonAnchor = enemyVisual != null ? enemyVisual.AttackEffectAnchor : null;
        }

        private void OnDisable()
        {
            ClearSummonTracking();
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }
    }
}
