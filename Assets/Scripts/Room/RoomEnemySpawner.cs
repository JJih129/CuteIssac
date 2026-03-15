using CuteIssac.Data.Enemy;
using CuteIssac.Data.Dungeon;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Enemy;
using CuteIssac.Core.Pooling;
using CuteIssac.Core.Run;
using CuteIssac.Core.Spawning;
using CuteIssac.Player;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CuteIssac.Room
{
    /// <summary>
    /// Owns enemy instantiation for one room.
    /// RoomController decides when combat starts, while this component decides what to spawn and where to place it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RoomController that requests combat spawning. Auto-filled from the same object when possible.")]
        [SerializeField] private RoomController roomController;
        [Tooltip("Optional parent used to keep spawned enemies grouped under the room hierarchy.")]
        [SerializeField] private Transform spawnedEnemyParent;
        [SerializeField] private RunManager runManager;

        [Header("Wave Source")]
        [Tooltip("Authored test wave for this room. Generated rooms can override this at runtime with ConfigureWave.")]
        [SerializeField] private EnemyWaveData enemyWaveData;
        [Tooltip("Default room type used when no generated room metadata was injected.")]
        [SerializeField] private RoomType defaultRoomTypeForSpawns = RoomType.Normal;
        [SerializeField] [Min(0)] private int distanceFromStartOverride;
        [SerializeField] [Min(0)] private int targetBudgetOverride;

        [Header("Spawn Points")]
        [Tooltip("Preferred spawn points inside the room. Leave empty to use the room center fallback.")]
        [SerializeField] private Transform[] spawnAnchors;
        [SerializeField] [Min(0f)] private float anchorScatterRadius = 0.25f;
        [SerializeField] private Vector2 fallbackSpawnExtents = new(1.8f, 1.2f);
        [SerializeField] private SpawnReusePolicy spawnReusePolicy = SpawnReusePolicy.Pooled;
        [SerializeField] [Min(0)] private int prewarmBufferCount = 1;

        [Header("Spawn Behavior")]
        [SerializeField] [Min(0f)] private float encounterStartAggroDelay = 0.9f;
        [SerializeField] [Min(0f)] private float encounterStartAggroDelayJitter = 0.35f;
        [SerializeField] [Min(0f)] private float minimumDistanceFromPlayer = 2.8f;
        [SerializeField] [Min(0f)] private float preferredSpawnSeparation = 2.2f;
        [SerializeField] [Min(1)] private int roomCandidateSamples = 8;
        [SerializeField] [Range(0f, 0.45f)] private float roomBoundsInsetRatio = 0.16f;

        [Header("Champion Enemies")]
        [SerializeField] private bool allowChampionPromotions = true;
        [SerializeField] private ChampionEnemyProfile championProfile;

        private EnemyWaveAssignment _runtimeWaveAssignment;
        private EncounterPacingSettings _runtimeEncounterPacing;
        private RoomType? _runtimeRoomType;
        private bool _hasSpawnedEncounter;
        private readonly System.Collections.Generic.List<Vector3> _spawnedPositionBuffer = new();
        private ChampionEnemyProfile _runtimeChampionProfile;
        private EnemyWaveAssignment _challengeFollowupWaveAssignment;
        private int _currentEncounterWave;
        private int _plannedEncounterWaveCount = 1;

        public bool HasSpawnedEncounter => _hasSpawnedEncounter;

        public bool TryGetUpcomingChallengeWaveThreat(
            out int nextWaveNumber,
            out int totalWaveCount,
            out int enemyCount,
            out int guaranteedChampionCount,
            out float championChanceBonus)
        {
            nextWaveNumber = 0;
            totalWaveCount = _plannedEncounterWaveCount;
            enemyCount = 0;
            guaranteedChampionCount = 0;
            championChanceBonus = 0f;

            if (GetEffectiveRoomType() != RoomType.Challenge || !_hasSpawnedEncounter || _currentEncounterWave >= _plannedEncounterWaveCount)
            {
                return false;
            }

            EnemyWaveAssignment upcomingWave = ResolveWaveAssignmentForWaveIndex(_currentEncounterWave);
            if (upcomingWave == null || upcomingWave.TotalEnemyCount <= 0)
            {
                return false;
            }

            nextWaveNumber = _currentEncounterWave + 1;
            totalWaveCount = _plannedEncounterWaveCount;
            enemyCount = upcomingWave.TotalEnemyCount;
            guaranteedChampionCount = GetChallengeFollowupGuaranteedChampionCount(upcomingWave.TotalEnemyCount, _currentEncounterWave);
            championChanceBonus = GetChallengeFollowupChampionChanceBonus(_currentEncounterWave);
            return true;
        }

        public bool TryGetChallengeRewardPressure(
            out int totalWaveCount,
            out int reinforcementEnemyCount,
            out int guaranteedChampionCount,
            out float championChanceBonus)
        {
            totalWaveCount = _plannedEncounterWaveCount;
            reinforcementEnemyCount = 0;
            guaranteedChampionCount = 0;
            championChanceBonus = 0f;

            if (GetEffectiveRoomType() != RoomType.Challenge || !_hasSpawnedEncounter)
            {
                return false;
            }

            reinforcementEnemyCount = _challengeFollowupWaveAssignment != null
                ? _challengeFollowupWaveAssignment.TotalEnemyCount
                : 0;

            for (int waveIndex = 1; waveIndex < _plannedEncounterWaveCount; waveIndex++)
            {
                guaranteedChampionCount = Mathf.Max(
                    guaranteedChampionCount,
                    GetChallengeFollowupGuaranteedChampionCount(reinforcementEnemyCount, waveIndex));
                championChanceBonus = Mathf.Max(
                    championChanceBonus,
                    GetChallengeFollowupChampionChanceBonus(waveIndex));
            }

            return totalWaveCount > 1
                || reinforcementEnemyCount > 0
                || guaranteedChampionCount > 0
                || championChanceBonus > 0f;
        }

        /// <summary>
        /// Non-combat rooms should clear immediately on entry.
        /// This lets RoomController stay state-focused while combat eligibility remains owned by the encounter spawner.
        /// </summary>
        public bool CanStartCombat()
        {
            if (_hasSpawnedEncounter)
            {
                return false;
            }

            if (!IsCombatRoomType(GetEffectiveRoomType()))
            {
                return false;
            }

            EnemyWaveAssignment enemyWaveAssignment = ResolveWaveAssignment();
            return enemyWaveAssignment != null && enemyWaveAssignment.TotalEnemyCount > 0;
        }

        private void Awake()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (runManager == null)
            {
                runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
            }
        }

        /// <summary>
        /// Generated dungeon flow can inject a pre-resolved runtime wave here.
        /// The room then spawns that exact composition on first combat start.
        /// </summary>
        public void ConfigureWave(EnemyWaveAssignment enemyWaveAssignment)
        {
            _runtimeWaveAssignment = enemyWaveAssignment;
            _runtimeEncounterPacing = null;
            _hasSpawnedEncounter = false;
            _currentEncounterWave = 0;
            _plannedEncounterWaveCount = 1;
            _challengeFollowupWaveAssignment = null;
            PrewarmWaveIfNeeded(enemyWaveAssignment);
        }

        /// <summary>
        /// Generated rooms inject their resolved room type and wave here.
        /// External runtime data always wins over the inspector-authored fallback wave.
        /// </summary>
        public void ConfigureEncounter(RoomType roomType, EnemyWaveAssignment enemyWaveAssignment, EncounterPacingSettings encounterPacing = null)
        {
            _runtimeRoomType = roomType;
            _runtimeWaveAssignment = enemyWaveAssignment;
            _runtimeEncounterPacing = encounterPacing;
            _hasSpawnedEncounter = false;
            _currentEncounterWave = 0;
            _plannedEncounterWaveCount = ResolvePlannedWaveCount(roomType, encounterPacing, enemyWaveAssignment);
            _challengeFollowupWaveAssignment = BuildChallengeFollowupWave(enemyWaveAssignment, encounterPacing, _plannedEncounterWaveCount);
            PrewarmWaveIfNeeded(enemyWaveAssignment);
            PrewarmWaveIfNeeded(_challengeFollowupWaveAssignment);
        }

        /// <summary>
        /// Called by RoomController when combat starts for the first time.
        /// Spawning is idempotent so cleared rooms and re-entry never create a second wave.
        /// </summary>
        public int HandleCombatStarted(RoomController combatRoom)
        {
            if (_hasSpawnedEncounter)
            {
                return 0;
            }

            RoomController targetRoom = combatRoom != null ? combatRoom : roomController;

            if (targetRoom == null)
            {
                Debug.LogWarning("RoomEnemySpawner could not spawn because no RoomController was assigned.", this);
                return 0;
            }

            if (!IsCombatRoomType(GetEffectiveRoomType()))
            {
                return 0;
            }

            EnemyWaveAssignment enemyWaveAssignment = ResolveWaveAssignment();

            if (enemyWaveAssignment == null || enemyWaveAssignment.TotalEnemyCount <= 0)
            {
                return 0;
            }

            PrewarmWaveIfNeeded(enemyWaveAssignment);
            int spawnedEnemyCount = SpawnWaveAssignment(targetRoom, enemyWaveAssignment, 0);

            if (spawnedEnemyCount > 0)
            {
                _hasSpawnedEncounter = true;
                _currentEncounterWave = 1;
            }

            return spawnedEnemyCount;
        }

        public bool TryAdvanceChallengeWave(RoomController combatRoom)
        {
            if (!_hasSpawnedEncounter || GetEffectiveRoomType() != RoomType.Challenge)
            {
                return false;
            }

            if (_currentEncounterWave >= _plannedEncounterWaveCount)
            {
                return false;
            }

            RoomController targetRoom = combatRoom != null ? combatRoom : roomController;

            if (targetRoom == null)
            {
                return false;
            }

            EnemyWaveAssignment followupWave = ResolveWaveAssignmentForWaveIndex(_currentEncounterWave);

            if (followupWave == null || followupWave.TotalEnemyCount <= 0)
            {
                return false;
            }

            int clearedWaveNumber = _currentEncounterWave;
            int guaranteedChampionCount = GetChallengeFollowupGuaranteedChampionCount(followupWave.TotalEnemyCount, _currentEncounterWave);
            float championChanceBonus = GetChallengeFollowupChampionChanceBonus(_currentEncounterWave);

            int spawnedEnemyCount = SpawnWaveAssignment(targetRoom, followupWave, _currentEncounterWave);

            if (spawnedEnemyCount <= 0)
            {
                return false;
            }

            _currentEncounterWave++;
            RaiseChallengeWaveIntermissionFeedback(
                targetRoom,
                clearedWaveNumber,
                _plannedEncounterWaveCount,
                followupWave.TotalEnemyCount,
                guaranteedChampionCount,
                championChanceBonus);
            RaiseChallengeWaveFeedback(targetRoom, followupWave, _currentEncounterWave, _plannedEncounterWaveCount);
            return true;
        }

        public void RestoreEncounterResolvedState()
        {
            _hasSpawnedEncounter = true;
            _currentEncounterWave = _plannedEncounterWaveCount;
        }

        private EnemyWaveAssignment ResolveWaveAssignment()
        {
            if (_runtimeWaveAssignment != null)
            {
                return _runtimeWaveAssignment;
            }

            if (enemyWaveData == null)
            {
                return null;
            }

            return enemyWaveData.BuildAssignment(distanceFromStartOverride, targetBudgetOverride);
        }

        private void PrewarmWaveIfNeeded(EnemyWaveAssignment enemyWaveAssignment)
        {
            if (spawnReusePolicy != SpawnReusePolicy.Pooled || enemyWaveAssignment == null)
            {
                return;
            }

            for (int i = 0; i < enemyWaveAssignment.SpawnGroups.Count; i++)
            {
                EnemyWaveSpawnGroup spawnGroup = enemyWaveAssignment.SpawnGroups[i];

                if (spawnGroup?.EnemyPrefab == null)
                {
                    continue;
                }

                PrefabPoolService.Prewarm(
                    spawnGroup.EnemyPrefab.gameObject,
                    Mathf.Max(1, spawnGroup.Count + prewarmBufferCount));
            }
        }

        private RoomType GetEffectiveRoomType()
        {
            return _runtimeRoomType ?? defaultRoomTypeForSpawns;
        }

        private static bool IsCombatRoomType(RoomType roomType)
        {
            return roomType == RoomType.Normal
                || roomType == RoomType.Challenge
                || roomType == RoomType.MiniBoss
                || roomType == RoomType.Boss;
        }

        private int SpawnWaveAssignment(RoomController targetRoom, EnemyWaveAssignment enemyWaveAssignment, int waveIndex)
        {
            if (targetRoom == null || enemyWaveAssignment == null)
            {
                return 0;
            }

            int spawnIndex = 0;
            int spawnedEnemyCount = 0;
            int promotedChampionCount = 0;
            int totalSpawnCount = enemyWaveAssignment.TotalEnemyCount;
            _spawnedPositionBuffer.Clear();

            for (int i = 0; i < enemyWaveAssignment.SpawnGroups.Count; i++)
            {
                EnemyWaveSpawnGroup spawnGroup = enemyWaveAssignment.SpawnGroups[i];

                if (spawnGroup == null || spawnGroup.EnemyPrefab == null || spawnGroup.Count <= 0)
                {
                    continue;
                }

                for (int countIndex = 0; countIndex < spawnGroup.Count; countIndex++)
                {
                    if (SpawnEnemyInstance(targetRoom, spawnGroup, spawnIndex, waveIndex, totalSpawnCount, ref promotedChampionCount))
                    {
                        spawnedEnemyCount++;
                    }

                    spawnIndex++;
                }
            }

            return spawnedEnemyCount;
        }

        private bool SpawnEnemyInstance(RoomController targetRoom, EnemyWaveSpawnGroup spawnGroup, int spawnIndex, int waveIndex, int totalSpawnCount, ref int promotedChampionCount)
        {
            Transform parent = spawnedEnemyParent != null ? spawnedEnemyParent : targetRoom.transform;
            Vector3 spawnPosition = ResolveSpawnPosition(targetRoom, spawnIndex);
            EnemyController spawnedEnemy = GameplaySpawnFactory.SpawnComponent(
                spawnGroup.EnemyPrefab,
                spawnPosition,
                Quaternion.identity,
                parent,
                spawnReusePolicy);

            if (spawnedEnemy == null)
            {
                Debug.LogWarning($"RoomEnemySpawner failed to instantiate enemy prefab for wave '{spawnGroup.EnemyId}'.", this);
                return false;
            }

            EnemyHealth enemyHealth = spawnedEnemy.GetComponent<EnemyHealth>();

            if (enemyHealth == null)
            {
                Debug.LogWarning("Spawned enemy is missing EnemyHealth, so the room cannot track clear state correctly.", spawnedEnemy);
                return false;
            }

            RoomEnemyMember roomEnemyMember = spawnedEnemy.GetComponent<RoomEnemyMember>();

            if (roomEnemyMember == null)
            {
                roomEnemyMember = spawnedEnemy.gameObject.AddComponent<RoomEnemyMember>();
            }

            roomEnemyMember.AssignRoom(targetRoom);
            spawnedEnemy.ApplySpawnAggroDelay(CalculateAggroDelay(spawnIndex, waveIndex));
            ApplyEncounterPacing(spawnedEnemy);
            if (TryApplyChampionPromotion(spawnedEnemy, targetRoom, spawnGroup, spawnIndex, waveIndex, totalSpawnCount, promotedChampionCount))
            {
                promotedChampionCount++;
            }
            _spawnedPositionBuffer.Add(spawnedEnemy.transform.position);
            return true;
        }

        private bool TryApplyChampionPromotion(EnemyController spawnedEnemy, RoomController targetRoom, EnemyWaveSpawnGroup spawnGroup, int spawnIndex, int waveIndex, int totalSpawnCount, int currentChampionCount)
        {
            ChampionEnemyModifier existingModifier = spawnedEnemy != null ? spawnedEnemy.GetComponent<ChampionEnemyModifier>() : null;
            existingModifier?.PrepareForSpawn();

            if (!allowChampionPromotions || spawnedEnemy == null || spawnGroup == null)
            {
                return false;
            }

            ChampionEnemyProfile resolvedChampionProfile = ResolveChampionProfile();

            if (resolvedChampionProfile == null)
            {
                return false;
            }

            RoomType effectiveRoomType = GetEffectiveRoomType();
            int floorIndex = runManager != null && runManager.CurrentContext.HasActiveRun
                ? runManager.CurrentContext.CurrentFloorIndex
                : 1;
            EnemyEncounterTier encounterTier = _runtimeWaveAssignment != null
                ? _runtimeWaveAssignment.EncounterTier
                : EnemyEncounterTier.Normal;
            float championChance = resolvedChampionProfile.EvaluatePromotionChance(floorIndex, encounterTier, effectiveRoomType);
            championChance += GetChallengeFollowupChampionChanceBonus(waveIndex);

            if (championChance <= 0f)
            {
                return false;
            }

            int guaranteedChampionCount = GetChallengeFollowupGuaranteedChampionCount(totalSpawnCount, waveIndex);
            int remainingSpawnSlots = Mathf.Max(0, totalSpawnCount - (spawnIndex + 1));
            bool mustPromoteToMeetGuarantee = guaranteedChampionCount > 0
                && (currentChampionCount + remainingSpawnSlots) < guaranteedChampionCount;

            if (!mustPromoteToMeetGuarantee)
            {
                float promotionRoll = ComputeDeterministicRoll(targetRoom, spawnGroup.EnemyId, spawnIndex, "promotion");

                if (promotionRoll > championChance)
                {
                    return false;
                }
            }

            ChampionEnemyProfile.VariantSettings variant = resolvedChampionProfile.SelectVariant(
                ComputeDeterministicRoll(targetRoom, spawnGroup.EnemyId, spawnIndex, "variant"),
                floorIndex,
                effectiveRoomType,
                waveIndex);

            if (variant == null)
            {
                return false;
            }

            ChampionEnemyModifier modifier = existingModifier != null
                ? existingModifier
                : spawnedEnemy.gameObject.AddComponent<ChampionEnemyModifier>();
            modifier.ApplyChampion(variant);

            string championVariantLabel = FloatingFeedbackLabelUtility.NormalizeEventLabel(variant.DisplayName, "Champion");
            string championFloatingLabel = championVariantLabel == "Champion"
                ? "Champion"
                : $"{championVariantLabel} · Champion";

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                spawnedEnemy.transform.position + Vector3.up * 1.1f,
                championFloatingLabel,
                variant.AccentColor,
                0.58f,
                0.72f,
                variant.FeedbackDuration,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "챔피언 출현",
                $"{variant.DisplayName} 변종이 전장을 압박합니다",
                variant.AccentColor,
                Mathf.Max(1.45f, variant.FeedbackDuration + 0.25f)));

            GameplayRuntimeEvents.RaiseChampionEnemyPromoted(new ChampionEnemyPromotedSignal(
                targetRoom,
                spawnedEnemy,
                variant.DisplayName,
                variant.AccentColor));

            return true;
        }

        private void ApplyEncounterPacing(EnemyController spawnedEnemy)
        {
            if (spawnedEnemy == null || _runtimeEncounterPacing == null)
            {
                return;
            }

            float firstAttackDelayBonus = _runtimeEncounterPacing.FirstAttackDelayBonus;
            float telegraphDurationMultiplier = _runtimeEncounterPacing.TelegraphDurationMultiplier;

            ShooterEnemyBrain shooterEnemyBrain = spawnedEnemy.GetComponent<ShooterEnemyBrain>();

            if (shooterEnemyBrain != null)
            {
                shooterEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            DasherEnemyBrain dasherEnemyBrain = spawnedEnemy.GetComponent<DasherEnemyBrain>();

            if (dasherEnemyBrain != null)
            {
                dasherEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            EnemySpawnerBrain enemySpawnerBrain = spawnedEnemy.GetComponent<EnemySpawnerBrain>();

            if (enemySpawnerBrain != null)
            {
                enemySpawnerBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            HomingShooterEnemyBrain homingShooterEnemyBrain = spawnedEnemy.GetComponent<HomingShooterEnemyBrain>();

            if (homingShooterEnemyBrain != null)
            {
                homingShooterEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            BurstShooterEnemyBrain burstShooterEnemyBrain = spawnedEnemy.GetComponent<BurstShooterEnemyBrain>();

            if (burstShooterEnemyBrain != null)
            {
                burstShooterEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            SniperEnemyBrain sniperEnemyBrain = spawnedEnemy.GetComponent<SniperEnemyBrain>();

            if (sniperEnemyBrain != null)
            {
                sniperEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            MineLayerEnemyBrain mineLayerEnemyBrain = spawnedEnemy.GetComponent<MineLayerEnemyBrain>();

            if (mineLayerEnemyBrain != null)
            {
                mineLayerEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            TeleporterEnemyBrain teleporterEnemyBrain = spawnedEnemy.GetComponent<TeleporterEnemyBrain>();

            if (teleporterEnemyBrain != null)
            {
                teleporterEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            PullerEnemyBrain pullerEnemyBrain = spawnedEnemy.GetComponent<PullerEnemyBrain>();

            if (pullerEnemyBrain != null)
            {
                pullerEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            ExploderEnemyBrain exploderEnemyBrain = spawnedEnemy.GetComponent<ExploderEnemyBrain>();

            if (exploderEnemyBrain != null)
            {
                exploderEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }

            SupportHealerEnemyBrain supportHealerEnemyBrain = spawnedEnemy.GetComponent<SupportHealerEnemyBrain>();

            if (supportHealerEnemyBrain != null)
            {
                supportHealerEnemyBrain.ApplyEncounterPacing(firstAttackDelayBonus, telegraphDurationMultiplier);
            }
        }

        private Vector3 ResolveSpawnPosition(RoomController targetRoom, int spawnIndex)
        {
            Bounds safeSpawnBounds = ResolveSafeSpawnBounds(targetRoom);
            Vector3 bestCandidate = safeSpawnBounds.center;
            float bestScore = float.MinValue;

            if (spawnAnchors != null && spawnAnchors.Length > 0)
            {
                for (int anchorIndex = 0; anchorIndex < spawnAnchors.Length; anchorIndex++)
                {
                    Transform anchor = spawnAnchors[(spawnIndex + anchorIndex) % spawnAnchors.Length];

                    if (anchor == null)
                    {
                        continue;
                    }

                    Vector3 candidate = ClampToSafeSpawnBounds(
                        safeSpawnBounds,
                        anchor.position + ComputeScatterOffset(spawnIndex + anchorIndex, anchorScatterRadius));
                    float candidateScore = EvaluateSpawnCandidate(candidate);

                    if (candidateScore > bestScore)
                    {
                        bestScore = candidateScore;
                        bestCandidate = candidate;
                    }
                }
            }

            for (int sampleIndex = 0; sampleIndex < GetRoomCandidateSamples(); sampleIndex++)
            {
                Vector3 candidate = ComputeRoomSamplePosition(safeSpawnBounds, spawnIndex, sampleIndex);
                float candidateScore = EvaluateSpawnCandidate(candidate);

                if (candidateScore > bestScore)
                {
                    bestScore = candidateScore;
                    bestCandidate = candidate;
                }
            }

            if (bestScore == float.MinValue)
            {
                return ClampToSafeSpawnBounds(safeSpawnBounds, safeSpawnBounds.center + ComputeFallbackOffset(spawnIndex));
            }

            return ClampToSafeSpawnBounds(safeSpawnBounds, bestCandidate);
        }

        private Vector3 ComputeScatterOffset(int spawnIndex, float radius)
        {
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            float angle = 137.5f * spawnIndex * Mathf.Deg2Rad;
            float scaledRadius = radius * (0.45f + ((spawnIndex % 3) * 0.275f));
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * scaledRadius;
        }

        private Vector3 ComputeFallbackOffset(int spawnIndex)
        {
            float horizontal = Mathf.Clamp(fallbackSpawnExtents.x, 0f, 100f);
            float vertical = Mathf.Clamp(fallbackSpawnExtents.y, 0f, 100f);

            if (horizontal <= 0f && vertical <= 0f)
            {
                return Vector3.zero;
            }

            float angle = 83f * spawnIndex * Mathf.Deg2Rad;
            float horizontalSign = (spawnIndex & 1) == 0 ? 1f : -1f;
            float verticalSign = ((spawnIndex / 2) & 1) == 0 ? 1f : -1f;

            return new Vector3(
                Mathf.Cos(angle) * horizontal * 0.55f * horizontalSign,
                Mathf.Sin(angle) * vertical * 0.55f * verticalSign,
                0f);
        }

        private Vector3 ComputeRoomSamplePosition(Bounds safeSpawnBounds, int spawnIndex, int sampleIndex)
        {
            Vector3 center = safeSpawnBounds.center;
            Vector3 extents = safeSpawnBounds.extents;
            float horizontalExtent = Mathf.Max(0.5f, extents.x);
            float verticalExtent = Mathf.Max(0.35f, extents.y);
            float angle = (spawnIndex * 53f + sampleIndex * 137.5f) * Mathf.Deg2Rad;
            float radialScale = 0.42f + (0.58f * ((sampleIndex % 4) / 3f));

            return center + new Vector3(
                Mathf.Cos(angle) * horizontalExtent * radialScale,
                Mathf.Sin(angle) * verticalExtent * radialScale,
                0f);
        }

        private Bounds ResolveSafeSpawnBounds(RoomController targetRoom)
        {
            Bounds roomBounds = targetRoom != null
                ? targetRoom.RoomBounds
                : new Bounds(transform.position, fallbackSpawnExtents * 2f);
            Vector3 center = targetRoom != null
                ? targetRoom.CameraFocusPosition
                : roomBounds.center;
            Vector3 extents = roomBounds.extents;
            float insetScale = 1f - GetRoomBoundsInsetRatio();
            float horizontalExtent = Mathf.Max(0.5f, extents.x * insetScale);
            float verticalExtent = Mathf.Max(0.35f, extents.y * insetScale);

            if (fallbackSpawnExtents.x > 0.01f)
            {
                horizontalExtent = Mathf.Min(horizontalExtent, fallbackSpawnExtents.x);
            }

            if (fallbackSpawnExtents.y > 0.01f)
            {
                verticalExtent = Mathf.Min(verticalExtent, fallbackSpawnExtents.y);
            }

            return new Bounds(center, new Vector3(horizontalExtent * 2f, verticalExtent * 2f, Mathf.Max(0.1f, roomBounds.size.z)));
        }

        private static Vector3 ClampToSafeSpawnBounds(Bounds safeSpawnBounds, Vector3 candidate)
        {
            Vector3 clamped = candidate;
            clamped.x = Mathf.Clamp(clamped.x, safeSpawnBounds.min.x, safeSpawnBounds.max.x);
            clamped.y = Mathf.Clamp(clamped.y, safeSpawnBounds.min.y, safeSpawnBounds.max.y);
            clamped.z = safeSpawnBounds.center.z;
            return clamped;
        }

        private float EvaluateSpawnCandidate(Vector3 candidate)
        {
            float playerDistance = GetDistanceToPlayer(candidate);
            float nearestSpawnDistance = GetNearestSpawnDistance(candidate);
            float score = playerDistance + (nearestSpawnDistance * 0.85f);

            if (GetMinimumDistanceFromPlayer() > 0f && playerDistance < GetMinimumDistanceFromPlayer())
            {
                score -= (GetMinimumDistanceFromPlayer() - playerDistance) * 6f;
            }

            if (GetPreferredSpawnSeparation() > 0f && nearestSpawnDistance < GetPreferredSpawnSeparation())
            {
                score -= (GetPreferredSpawnSeparation() - nearestSpawnDistance) * 3f;
            }

            return score;
        }

        private float GetDistanceToPlayer(Vector3 candidate)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            return playerController != null
                ? Vector2.Distance(candidate, playerController.transform.position)
                : GetMinimumDistanceFromPlayer();
        }

        private float GetNearestSpawnDistance(Vector3 candidate)
        {
            if (_spawnedPositionBuffer.Count == 0)
            {
                return GetPreferredSpawnSeparation();
            }

            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _spawnedPositionBuffer.Count; i++)
            {
                nearestDistance = Mathf.Min(nearestDistance, Vector2.Distance(candidate, _spawnedPositionBuffer[i]));
            }

            return nearestDistance;
        }

        private float CalculateAggroDelay(int spawnIndex, int waveIndex)
        {
            if (GetEncounterStartAggroDelay() <= 0f && GetEncounterStartAggroDelayJitter() <= 0f)
            {
                return 0f;
            }

            float normalizedIndex = (spawnIndex % 5) / 4f;
            float aggroDelay = GetEncounterStartAggroDelay() + (GetEncounterStartAggroDelayJitter() * normalizedIndex);
            return aggroDelay * GetChallengeFollowupAggroDelayMultiplier(waveIndex);
        }

        private float GetEncounterStartAggroDelay()
        {
            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.EncounterStartAggroDelay
                : encounterStartAggroDelay;
        }

        private float GetEncounterStartAggroDelayJitter()
        {
            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.EncounterStartAggroDelayJitter
                : encounterStartAggroDelayJitter;
        }

        private float GetMinimumDistanceFromPlayer()
        {
            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.MinimumDistanceFromPlayer
                : minimumDistanceFromPlayer;
        }

        private float GetPreferredSpawnSeparation()
        {
            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.PreferredSpawnSeparation
                : preferredSpawnSeparation;
        }

        private int GetRoomCandidateSamples()
        {
            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.RoomCandidateSamples
                : Mathf.Max(1, roomCandidateSamples);
        }

        private float GetRoomBoundsInsetRatio()
        {
            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.RoomBoundsInsetRatio
                : Mathf.Clamp(roomBoundsInsetRatio, 0f, 0.45f);
        }

        private float GetChallengeFollowupAggroDelayMultiplier(int waveIndex)
        {
            if (waveIndex <= 0 || GetEffectiveRoomType() != RoomType.Challenge)
            {
                return 1f;
            }

            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.ChallengeFollowupAggroDelayMultiplier
                : 0.65f;
        }

        private float GetChallengeFollowupChampionChanceBonus(int waveIndex)
        {
            if (waveIndex <= 0 || GetEffectiveRoomType() != RoomType.Challenge)
            {
                return 0f;
            }

            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.EvaluateChallengeFollowupChampionChanceBonus(waveIndex)
                : Mathf.Min(0.22f, 0.08f + ((waveIndex - 1) * 0.04f));
        }

        private int GetChallengeFollowupGuaranteedChampionCount(int totalSpawnCount, int waveIndex)
        {
            if (waveIndex <= 0 || totalSpawnCount <= 0 || GetEffectiveRoomType() != RoomType.Challenge)
            {
                return 0;
            }

            return _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.EvaluateChallengeFollowupGuaranteedChampionCount(totalSpawnCount, waveIndex)
                : Mathf.Clamp(waveIndex, 0, Mathf.Min(2, totalSpawnCount));
        }

        private int ResolvePlannedWaveCount(RoomType roomType, EncounterPacingSettings encounterPacing, EnemyWaveAssignment enemyWaveAssignment)
        {
            if (roomType != RoomType.Challenge || enemyWaveAssignment == null || enemyWaveAssignment.TotalEnemyCount <= 0)
            {
                return 1;
            }

            int configuredWaveCount = encounterPacing != null ? encounterPacing.ChallengeWaveCount : 2;
            return Mathf.Clamp(configuredWaveCount, 1, 3);
        }

        private EnemyWaveAssignment BuildChallengeFollowupWave(EnemyWaveAssignment baseWaveAssignment, EncounterPacingSettings encounterPacing, int plannedWaveCount)
        {
            if (GetEffectiveRoomType() != RoomType.Challenge || plannedWaveCount <= 1 || baseWaveAssignment == null || baseWaveAssignment.TotalEnemyCount <= 0)
            {
                return null;
            }

            float reinforcementMultiplier = encounterPacing != null
                ? encounterPacing.ChallengeReinforcementMultiplier
                : 0.7f;
            EnemyWaveAssignment followupWave = new(
                $"{baseWaveAssignment.WaveId}-challenge-followup",
                baseWaveAssignment.EncounterTier,
                baseWaveAssignment.DistanceFromStart,
                Mathf.Max(1, Mathf.RoundToInt(baseWaveAssignment.TargetBudget * reinforcementMultiplier)));

            for (int i = 0; i < baseWaveAssignment.SpawnGroups.Count; i++)
            {
                EnemyWaveSpawnGroup spawnGroup = baseWaveAssignment.SpawnGroups[i];

                if (spawnGroup?.EnemyPrefab == null || spawnGroup.Count <= 0)
                {
                    continue;
                }

                int scaledCount = Mathf.Max(1, Mathf.RoundToInt(spawnGroup.Count * reinforcementMultiplier));
                followupWave.AddSpawn(spawnGroup.EnemyPrefab, spawnGroup.EnemyId, scaledCount, spawnGroup.DifficultyCost);
            }

            return followupWave.TotalEnemyCount > 0 ? followupWave : null;
        }

        private EnemyWaveAssignment ResolveWaveAssignmentForWaveIndex(int waveIndex)
        {
            if (waveIndex <= 0)
            {
                return ResolveWaveAssignment();
            }

            return _challengeFollowupWaveAssignment;
        }

        private void RaiseChallengeWaveFeedback(RoomController targetRoom, EnemyWaveAssignment waveAssignment, int currentWave, int totalWaves)
        {
            int enemyCount = waveAssignment != null ? waveAssignment.TotalEnemyCount : 0;
            int guaranteedChampionCount = GetChallengeFollowupGuaranteedChampionCount(enemyCount, Mathf.Max(0, currentWave - 1));
            float championChanceBonus = GetChallengeFollowupChampionChanceBonus(Mathf.Max(0, currentWave - 1));
            ChallengeThreatPresentation presentation = ChallengeThreatPresentationResolver.Build(
                currentWave,
                totalWaves,
                enemyCount,
                guaranteedChampionCount,
                championChanceBonus);

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                presentation.BannerTitle,
                presentation.DetailSegment,
                presentation.AccentColor,
                presentation.BannerDuration,
                true,
                presentation.BadgeLabel,
                presentation.DetailEyebrow,
                presentation.Stage,
                presentation.LayoutProfile));

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                targetRoom.CameraFocusPosition + Vector3.up * 1.2f,
                presentation.FloatingLabel,
                presentation.AccentColor,
                0.72f,
                0.9f,
                1.1f,
                true,
                presentation.BadgeLabel,
                presentation.Stage,
                presentation.LayoutProfile));
        }

        private static void RaiseChallengeWaveIntermissionFeedback(
            RoomController targetRoom,
            int clearedWave,
            int totalWaves,
            int nextEnemyCount,
            int nextGuaranteedChampionCount,
            float nextChampionChanceBonus)
        {
            ChallengeWaveIntermissionPresentation presentation = ChallengeThreatPresentationResolver.BuildWaveIntermission(
                clearedWave,
                totalWaves,
                nextEnemyCount,
                nextGuaranteedChampionCount,
                nextChampionChanceBonus);

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                targetRoom.CameraFocusPosition + new Vector3(0f, 0.86f, 0f),
                presentation.FloatingLabel,
                presentation.AccentColor,
                0.54f,
                0.72f,
                presentation.Duration,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));

            GameAudioEvents.RaiseUi(
                presentation.AudioEventType,
                presentation.AudioVolumeScale,
                presentation.AudioPitchScale);
        }

        private ChampionEnemyProfile ResolveChampionProfile()
        {
            if (championProfile != null)
            {
                return championProfile;
            }

            if (_runtimeChampionProfile == null)
            {
                _runtimeChampionProfile = ScriptableObject.CreateInstance<ChampionEnemyProfile>();
                _runtimeChampionProfile.hideFlags = HideFlags.HideAndDontSave;
            }

            return _runtimeChampionProfile;
        }

        private float ComputeDeterministicRoll(RoomController targetRoom, string enemyId, int spawnIndex, string salt)
        {
            int seed = runManager != null && runManager.CurrentContext.HasActiveRun
                ? runManager.CurrentContext.Seed
                : 0;
            int floorIndex = runManager != null && runManager.CurrentContext.HasActiveRun
                ? runManager.CurrentContext.CurrentFloorIndex
                : 1;
            int encounterTier = (int)(_runtimeWaveAssignment != null ? _runtimeWaveAssignment.EncounterTier : EnemyEncounterTier.Normal);
            int roomHash = GetStableHashCode(targetRoom != null ? targetRoom.RoomId : name);
            int enemyHash = GetStableHashCode(enemyId);
            int saltHash = GetStableHashCode(salt);

            unchecked
            {
                int hash = seed;
                hash = (hash * 397) ^ floorIndex;
                hash = (hash * 397) ^ encounterTier;
                hash = (hash * 397) ^ roomHash;
                hash = (hash * 397) ^ enemyHash;
                hash = (hash * 397) ^ saltHash;
                hash = (hash * 397) ^ spawnIndex;
                uint normalized = (uint)hash;
                return (normalized & 0x00FFFFFF) / 16777216f;
            }
        }

        private static int GetStableHashCode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;

                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        private void Reset()
        {
            roomController = GetComponent<RoomController>();
            runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
        }

        private void OnValidate()
        {
            if (roomController == null)
            {
                roomController = GetComponent<RoomController>();
            }

            if (runManager == null)
            {
                runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
            }
        }
    }
}
