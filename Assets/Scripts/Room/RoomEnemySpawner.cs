using System.Collections;
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
        private enum EncounterSynergyFormation
        {
            None = 0,
            Escort = 1,
            Crossfire = 2,
            Siege = 3
        }

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
        [SerializeField] [Min(0f)] private float encounterStartAggroDelay = 1f;
        [SerializeField] [Min(0f)] private float encounterStartAggroDelayJitter = 0f;
        [SerializeField] [Min(0f)] private float minimumDistanceFromPlayer = 2.8f;
        [SerializeField] [Min(0f)] private float preferredSpawnSeparation = 2.2f;
        [SerializeField] [Min(1)] private int roomCandidateSamples = 8;
        [SerializeField] [Range(0f, 0.45f)] private float roomBoundsInsetRatio = 0.16f;
        [SerializeField] [Min(0f)] private float doorSpawnClearanceDistance = 2.25f;
        [SerializeField] [Range(1f, 2f)] private float globalEnemyCountMultiplier = 1.45f;

        [Header("Dormant Release")]
        [SerializeField] [Min(0f)] private float combatDormantReleaseDelay = 0.5f;

        [Header("Spawn Telegraph")]
        [SerializeField] private bool enableSpawnTelegraph = true;
        [SerializeField] [Min(0.05f)] private float spawnTelegraphMinDuration = 0.28f;
        [SerializeField] [Min(0.1f)] private float spawnTelegraphMaxDuration = 0.72f;
        [SerializeField] [Range(0.2f, 0.9f)] private float spawnTelegraphRevealRatio = 0.58f;
        [SerializeField] [Min(0.05f)] private float spawnTelegraphVisibleLeadTime = 0.18f;
        [SerializeField] [Min(0.5f)] private float spawnTelegraphScale = 1.12f;
        [SerializeField] [Min(0.1f)] private float spawnTelegraphPulseSpeed = 5.4f;
        [SerializeField] [Range(0.05f, 1f)] private float spawnTelegraphOpacity = 0.58f;
        [SerializeField] private Vector2 spawnTelegraphLocalOffset = new(0f, -0.28f);

        [Header("Encounter Synergy")]
        [SerializeField] private bool enableEncounterSynergy = true;
        [SerializeField] [Range(0.8f, 1.4f)] private float escortFrontlineSpeedMultiplier = 1.14f;
        [SerializeField] [Range(1f, 1.5f)] private float escortFrontlineContactMultiplier = 1.18f;
        [SerializeField] [Range(0.5f, 1f)] private float crossfireAggroDelayScale = 0.72f;
        [SerializeField] [Range(0.9f, 1.4f)] private float crossfireControllerSpeedMultiplier = 1.12f;
        [SerializeField] [Range(0.9f, 1.3f)] private float siegeUnitSpeedMultiplier = 1.08f;
        [SerializeField] [Min(0.25f)] private float siegeDeathPulseRadius = 0.9f;
        [SerializeField] [Min(0.1f)] private float siegeDeathPulseTelegraphDuration = 0.55f;
        [SerializeField] [Min(0f)] private float siegeDeathPulseDamage = 1f;
        [SerializeField] [Min(0f)] private float siegeDeathPulseKnockback = 4.6f;

        [Header("Champion Enemies")]
        [SerializeField] private bool allowChampionPromotions = true;
        [SerializeField] private ChampionEnemyProfile championProfile;

        private EnemyWaveAssignment _runtimeWaveAssignment;
        private EncounterPacingSettings _runtimeEncounterPacing;
        private RoomType? _runtimeRoomType;
        private bool _hasSpawnedEncounter;
        private bool _hasPreSpawnedEncounter;
        private Coroutine _combatReleaseRoutine;
        private readonly System.Collections.Generic.List<Vector3> _spawnedPositionBuffer = new();
        private readonly System.Collections.Generic.List<EnemyController> _spawnedEnemyBuffer = new();
        private readonly System.Collections.Generic.List<EnemyHealth> _releasedEnemyBuffer = new();
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
            int adjustedEnemyCount = ResolveAdjustedTotalEnemyCount(upcomingWave);
            enemyCount = adjustedEnemyCount;
            guaranteedChampionCount = GetChallengeFollowupGuaranteedChampionCount(adjustedEnemyCount, _currentEncounterWave);
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
                ? ResolveAdjustedTotalEnemyCount(_challengeFollowupWaveAssignment)
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
            _hasPreSpawnedEncounter = false;
            _currentEncounterWave = 0;
            _plannedEncounterWaveCount = 1;
            _challengeFollowupWaveAssignment = null;
            PrewarmWaveIfNeeded(enemyWaveAssignment);
            PreSpawnEncounterIfNeeded(enemyWaveAssignment);
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
            _hasPreSpawnedEncounter = false;
            _currentEncounterWave = 0;
            _plannedEncounterWaveCount = ResolvePlannedWaveCount(roomType, encounterPacing, enemyWaveAssignment);
            _challengeFollowupWaveAssignment = BuildChallengeFollowupWave(enemyWaveAssignment, encounterPacing, _plannedEncounterWaveCount);
            PrewarmWaveIfNeeded(enemyWaveAssignment);
            PrewarmWaveIfNeeded(_challengeFollowupWaveAssignment);
            PreSpawnEncounterIfNeeded(enemyWaveAssignment);
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

            if (_hasPreSpawnedEncounter)
            {
                _hasSpawnedEncounter = true;
                _hasPreSpawnedEncounter = false;
                _currentEncounterWave = 1;
                ScheduleDormantRelease(targetRoom);
                return targetRoom != null ? Mathf.Max(0, targetRoom.AliveEnemyCount) : 0;
            }

            PrewarmWaveIfNeeded(enemyWaveAssignment);
            int spawnedEnemyCount = SpawnWaveAssignment(targetRoom, enemyWaveAssignment, 0, false);

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

            int spawnedEnemyCount = SpawnWaveAssignment(targetRoom, followupWave, _currentEncounterWave, false);

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
            _hasPreSpawnedEncounter = false;
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
                    Mathf.Max(1, ResolveAdjustedSpawnCount(spawnGroup.Count) + prewarmBufferCount));
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

        private int SpawnWaveAssignment(RoomController targetRoom, EnemyWaveAssignment enemyWaveAssignment, int waveIndex, bool dormantSpawn)
        {
            if (targetRoom == null || enemyWaveAssignment == null)
            {
                return 0;
            }

            int spawnIndex = 0;
            int spawnedEnemyCount = 0;
            int promotedChampionCount = 0;
            int totalSpawnCount = ResolveAdjustedTotalEnemyCount(enemyWaveAssignment);
            _spawnedPositionBuffer.Clear();
            _spawnedEnemyBuffer.Clear();

            for (int i = 0; i < enemyWaveAssignment.SpawnGroups.Count; i++)
            {
                EnemyWaveSpawnGroup spawnGroup = enemyWaveAssignment.SpawnGroups[i];

                if (spawnGroup == null || spawnGroup.EnemyPrefab == null || spawnGroup.Count <= 0)
                {
                    continue;
                }

                int adjustedSpawnCount = ResolveAdjustedSpawnCount(spawnGroup.Count);

                for (int countIndex = 0; countIndex < adjustedSpawnCount; countIndex++)
                {
                    if (SpawnEnemyInstance(targetRoom, spawnGroup, spawnIndex, waveIndex, totalSpawnCount, dormantSpawn, ref promotedChampionCount))
                    {
                        spawnedEnemyCount++;
                    }

                    spawnIndex++;
                }
            }

            ApplyEncounterSynergy(targetRoom, waveIndex, dormantSpawn);
            return spawnedEnemyCount;
        }

        private bool SpawnEnemyInstance(RoomController targetRoom, EnemyWaveSpawnGroup spawnGroup, int spawnIndex, int waveIndex, int totalSpawnCount, bool dormantSpawn, ref int promotedChampionCount)
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
            spawnedEnemy.GetComponent<EnemyFormationModifier>()?.PrepareForSpawn();
            spawnedEnemy.GetComponent<EncounterDeathPulseModifier>()?.PrepareForSpawn();
            ApplyEncounterPacing(spawnedEnemy);

            if (dormantSpawn)
            {
                spawnedEnemy.SetCombatDormant(true);
            }
            else
            {
                float aggroDelay = CalculateAggroDelay(spawnIndex, waveIndex);
                spawnedEnemy.ApplySpawnAggroDelay(aggroDelay);
                ApplySpawnTelegraph(spawnedEnemy, spawnIndex, waveIndex, aggroDelay);

                if (spawnIndex == 0)
                {
                    RaiseWaveSpawnThreatFlash(waveIndex);
                }
            }

            if (TryApplyChampionPromotion(spawnedEnemy, targetRoom, spawnGroup, spawnIndex, waveIndex, totalSpawnCount, promotedChampionCount))
            {
                promotedChampionCount++;
            }

            _spawnedEnemyBuffer.Add(spawnedEnemy);
            _spawnedPositionBuffer.Add(spawnedEnemy.transform.position);
            return true;
        }

        private void PreSpawnEncounterIfNeeded(EnemyWaveAssignment enemyWaveAssignment)
        {
            if (enemyWaveAssignment == null || enemyWaveAssignment.TotalEnemyCount <= 0)
            {
                return;
            }

            RoomController targetRoom = roomController;

            if (targetRoom == null || !IsCombatRoomType(GetEffectiveRoomType()))
            {
                return;
            }

            int spawnedEnemyCount = SpawnWaveAssignment(targetRoom, enemyWaveAssignment, 0, true);

            if (spawnedEnemyCount > 0)
            {
                _hasPreSpawnedEncounter = true;
            }
        }

        private void ReleaseDormantEnemies(RoomController targetRoom)
        {
            if (targetRoom == null)
            {
                return;
            }

            _releasedEnemyBuffer.Clear();
            targetRoom.CollectAliveEnemies(_releasedEnemyBuffer);

            for (int i = 0; i < _releasedEnemyBuffer.Count; i++)
            {
                EnemyHealth enemyHealth = _releasedEnemyBuffer[i];
                if (enemyHealth == null)
                {
                    continue;
                }

                EnemyController enemyController = enemyHealth.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    enemyController.SetCombatDormant(false);
                }
            }
        }

        private void ApplyEncounterSynergy(RoomController targetRoom, int waveIndex, bool suppressFeedback)
        {
            if (!enableEncounterSynergy || targetRoom == null || _spawnedEnemyBuffer.Count < 2)
            {
                return;
            }

            RoomType effectiveRoomType = GetEffectiveRoomType();

            if (!IsEncounterSynergyRoomType(effectiveRoomType))
            {
                return;
            }

            int supportCount = 0;
            int frontlineCount = 0;
            int controllerCount = 0;
            int rangedCount = 0;
            int siegeCount = 0;

            for (int index = 0; index < _spawnedEnemyBuffer.Count; index++)
            {
                EnemyController enemy = _spawnedEnemyBuffer[index];

                if (enemy == null)
                {
                    continue;
                }

                if (IsSupportEnemy(enemy))
                {
                    supportCount++;
                }

                if (IsFrontlineEnemy(enemy))
                {
                    frontlineCount++;
                }

                if (IsControllerEnemy(enemy))
                {
                    controllerCount++;
                }

                if (IsRangedEnemy(enemy))
                {
                    rangedCount++;
                }

                if (IsSiegeEnemy(enemy))
                {
                    siegeCount++;
                }
            }

            int escortScore = supportCount > 0 && frontlineCount > 0 ? (supportCount * 2) + frontlineCount : 0;
            int crossfireScore = controllerCount > 0 && rangedCount > 0 ? (controllerCount * 2) + rangedCount : 0;
            int siegeScore = siegeCount > 0 && (supportCount > 0 || rangedCount > 0) ? (siegeCount * 2) + Mathf.Max(supportCount, rangedCount) : 0;
            EncounterSynergyFormation formation = EncounterSynergyFormation.None;
            int bestScore = 0;

            if (escortScore >= 3 && escortScore > bestScore)
            {
                formation = EncounterSynergyFormation.Escort;
                bestScore = escortScore;
            }

            if (crossfireScore >= 3 && crossfireScore > bestScore)
            {
                formation = EncounterSynergyFormation.Crossfire;
                bestScore = crossfireScore;
            }

            if (siegeScore >= 3 && siegeScore > bestScore)
            {
                formation = EncounterSynergyFormation.Siege;
            }

            switch (formation)
            {
                case EncounterSynergyFormation.Escort:
                    ApplyEscortFormation(targetRoom, waveIndex, suppressFeedback);
                    break;
                case EncounterSynergyFormation.Crossfire:
                    ApplyCrossfireFormation(targetRoom, waveIndex, suppressFeedback);
                    break;
                case EncounterSynergyFormation.Siege:
                    ApplySiegeFormation(targetRoom, waveIndex, suppressFeedback);
                    break;
            }
        }

        private void ApplyEscortFormation(RoomController targetRoom, int waveIndex, bool suppressFeedback)
        {
            float speedMultiplier = escortFrontlineSpeedMultiplier + (waveIndex > 0 ? 0.04f : 0f);
            float contactMultiplier = escortFrontlineContactMultiplier + (waveIndex > 0 ? 0.05f : 0f);
            float supportSpeedMultiplier = 1.04f + (waveIndex > 0 ? 0.02f : 0f);
            Color accentColor = new(0.48f, 0.96f, 0.62f, 1f);
            Color criticalColor = Color.Lerp(accentColor, Color.white, 0.24f);

            for (int index = 0; index < _spawnedEnemyBuffer.Count; index++)
            {
                EnemyController enemy = _spawnedEnemyBuffer[index];

                if (enemy == null)
                {
                    continue;
                }

                if (IsFrontlineEnemy(enemy))
                {
                    ApplyFormationModifier(enemy, "escort", EnemyFormationRole.Frontline, accentColor, speedMultiplier, contactMultiplier);
                    continue;
                }

                if (IsSupportEnemy(enemy))
                {
                    ApplyFormationModifier(enemy, "escort", EnemyFormationRole.Support, accentColor, supportSpeedMultiplier, 1f);
                    ApplyPriorityHint(enemy, "escort", EnemyFormationPriorityLevel.Critical, criticalColor);
                }
            }

            if (!suppressFeedback)
            {
                RaiseEncounterSynergyFeedback(targetRoom, "ESCORT FORMATION", "Cut the marked healer before the screen closes.", accentColor);
            }
        }

        private void ApplyCrossfireFormation(RoomController targetRoom, int waveIndex, bool suppressFeedback)
        {
            float aggroScale = Mathf.Clamp(crossfireAggroDelayScale - (waveIndex > 0 ? 0.06f : 0f), 0.45f, 1f);
            float controllerSpeedMultiplier = crossfireControllerSpeedMultiplier + (waveIndex > 0 ? 0.04f : 0f);
            Color accentColor = new(0.44f, 0.78f, 1f, 1f);
            Color focusColor = Color.Lerp(accentColor, Color.white, 0.12f);
            Color criticalColor = Color.Lerp(accentColor, Color.white, 0.28f);

            for (int index = 0; index < _spawnedEnemyBuffer.Count; index++)
            {
                EnemyController enemy = _spawnedEnemyBuffer[index];

                if (enemy == null)
                {
                    continue;
                }

                if (IsControllerEnemy(enemy))
                {
                    enemy.ScaleSpawnAggroDelay(aggroScale);
                    ApplyFormationModifier(enemy, "crossfire", EnemyFormationRole.Controller, accentColor, controllerSpeedMultiplier, 1f);
                    ApplyPriorityHint(enemy, "crossfire", EnemyFormationPriorityLevel.Critical, criticalColor);
                    continue;
                }

                if (IsRangedEnemy(enemy))
                {
                    enemy.ScaleSpawnAggroDelay(Mathf.Lerp(aggroScale, 1f, 0.35f));
                    ApplyFormationModifier(enemy, "crossfire", EnemyFormationRole.Ranged, accentColor, 1.05f, 1f);
                    ApplyPriorityHint(enemy, "crossfire", EnemyFormationPriorityLevel.Focus, focusColor);
                }
            }

            if (!suppressFeedback)
            {
                RaiseEncounterSynergyFeedback(targetRoom, "CROSSFIRE NET", "Break the marked controller to collapse the lane.", accentColor);
            }
        }

        private void ApplySiegeFormation(RoomController targetRoom, int waveIndex, bool suppressFeedback)
        {
            float speedMultiplier = siegeUnitSpeedMultiplier + (waveIndex > 0 ? 0.04f : 0f);
            float deathPulseRadius = siegeDeathPulseRadius + (waveIndex > 0 ? 0.12f : 0f);
            float deathPulseDamage = siegeDeathPulseDamage + (waveIndex > 0 ? 0.25f : 0f);
            Color accentColor = new(1f, 0.62f, 0.3f, 1f);
            Color criticalColor = Color.Lerp(accentColor, Color.white, 0.22f);

            for (int index = 0; index < _spawnedEnemyBuffer.Count; index++)
            {
                EnemyController enemy = _spawnedEnemyBuffer[index];

                if (enemy == null || !IsSiegeEnemy(enemy))
                {
                    continue;
                }

                ApplyFormationModifier(enemy, "siege", EnemyFormationRole.Siege, accentColor, speedMultiplier, 1.08f);
                ApplyDeathPulseModifier(enemy, deathPulseRadius, siegeDeathPulseTelegraphDuration, deathPulseDamage, siegeDeathPulseKnockback, accentColor);
                ApplyPriorityHint(enemy, "siege", EnemyFormationPriorityLevel.Critical, criticalColor);
            }

            if (!suppressFeedback)
            {
                RaiseEncounterSynergyFeedback(targetRoom, "SIEGE NEST", "Crush the marked nest before pressure snowballs.", accentColor);
            }
        }

        private static void ApplyFormationModifier(EnemyController enemy, string formationId, EnemyFormationRole formationRole, Color accentColor, float speedMultiplier, float contactDamageMultiplier)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyFormationModifier modifier = enemy.GetComponent<EnemyFormationModifier>();

            if (modifier == null)
            {
                modifier = enemy.gameObject.AddComponent<EnemyFormationModifier>();
            }

            modifier.ApplyFormation(formationId, formationRole, accentColor, speedMultiplier, contactDamageMultiplier);
            enemy.GetComponent<EnemySpawnTelegraph>()?.ApplyAccent(
                accentColor,
                ResolveFormationTelegraphScaleMultiplier(formationId),
                ResolveFormationTelegraphOpacityMultiplier(formationId));
        }

        private static void ApplyPriorityHint(EnemyController enemy, string formationId, EnemyFormationPriorityLevel priorityLevel, Color accentColor)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyFormationModifier modifier = enemy.GetComponent<EnemyFormationModifier>();

            if (modifier == null)
            {
                return;
            }

            modifier.SetPriorityHint(priorityLevel, accentColor);

            if (priorityLevel == EnemyFormationPriorityLevel.None)
            {
                return;
            }

            float scaleMultiplier = ResolveFormationTelegraphScaleMultiplier(formationId)
                + (priorityLevel == EnemyFormationPriorityLevel.Critical ? 0.12f : 0.06f);
            float opacityMultiplier = ResolveFormationTelegraphOpacityMultiplier(formationId)
                + (priorityLevel == EnemyFormationPriorityLevel.Critical ? 0.18f : 0.1f);
            enemy.GetComponent<EnemySpawnTelegraph>()?.ApplyAccent(accentColor, scaleMultiplier, opacityMultiplier);
        }

        private static void ApplyDeathPulseModifier(EnemyController enemy, float pulseRadius, float telegraphDuration, float pulseDamage, float pulseKnockback, Color pulseColor)
        {
            if (enemy == null)
            {
                return;
            }

            EncounterDeathPulseModifier modifier = enemy.GetComponent<EncounterDeathPulseModifier>();

            if (modifier == null)
            {
                modifier = enemy.gameObject.AddComponent<EncounterDeathPulseModifier>();
            }

            modifier.Configure(pulseRadius, telegraphDuration, pulseDamage, pulseKnockback, pulseColor);
        }

        private static void RaiseEncounterSynergyFeedback(RoomController targetRoom, string title, string subtitle, Color accentColor)
        {
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                title,
                subtitle,
                accentColor,
                1.5f));

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                targetRoom.CameraFocusPosition + new Vector3(0f, 0.92f, 0f),
                title,
                accentColor,
                0.65f,
                0.84f,
                1.06f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private static bool IsEncounterSynergyRoomType(RoomType roomType)
        {
            return roomType == RoomType.Normal
                || roomType == RoomType.Challenge
                || roomType == RoomType.Trap
                || roomType == RoomType.Curse;
        }

        private static float ResolveFormationTelegraphScaleMultiplier(string formationId)
        {
            switch (formationId)
            {
                case "crossfire":
                    return 1.08f;
                case "siege":
                    return 1.12f;
                case "escort":
                    return 1.03f;
                default:
                    return 1f;
            }
        }

        private static float ResolveFormationTelegraphOpacityMultiplier(string formationId)
        {
            switch (formationId)
            {
                case "crossfire":
                    return 1.16f;
                case "siege":
                    return 1.2f;
                case "escort":
                    return 1.08f;
                default:
                    return 1f;
            }
        }

        private static bool IsSupportEnemy(EnemyController enemy)
        {
            return enemy != null && enemy.GetComponent<SupportHealerEnemyBrain>() != null;
        }

        private static bool IsFrontlineEnemy(EnemyController enemy)
        {
            return enemy != null
                && (enemy.GetComponent<ChaserEnemyBrain>() != null
                    || enemy.GetComponent<DasherEnemyBrain>() != null
                    || enemy.GetComponent<ShieldEnemyBrain>() != null
                    || enemy.GetComponent<ExploderEnemyBrain>() != null
                    || enemy.GetComponent<OrbiterEnemyBrain>() != null
                    || enemy.GetComponent<SplitterEnemyBrain>() != null);
        }

        private static bool IsControllerEnemy(EnemyController enemy)
        {
            return enemy != null
                && (enemy.GetComponent<PullerEnemyBrain>() != null
                    || enemy.GetComponent<TeleporterEnemyBrain>() != null);
        }

        private static bool IsRangedEnemy(EnemyController enemy)
        {
            return enemy != null
                && (enemy.GetComponent<ShooterEnemyBrain>() != null
                    || enemy.GetComponent<BurstShooterEnemyBrain>() != null
                    || enemy.GetComponent<HomingShooterEnemyBrain>() != null
                    || enemy.GetComponent<SniperEnemyBrain>() != null
                    || enemy.GetComponent<TurretEnemyBrain>() != null);
        }

        private static bool IsSiegeEnemy(EnemyController enemy)
        {
            return enemy != null
                && (enemy.GetComponent<MineLayerEnemyBrain>() != null
                    || enemy.GetComponent<EnemySpawnerBrain>() != null
                    || enemy.GetComponent<ExploderEnemyBrain>() != null);
        }

        private void ApplySpawnTelegraph(EnemyController spawnedEnemy, int spawnIndex, int waveIndex, float aggroDelay)
        {
            if (!enableSpawnTelegraph || spawnedEnemy == null || aggroDelay <= 0.1f)
            {
                return;
            }

            float revealDelay = CalculateSpawnTelegraphRevealDelay(aggroDelay, waveIndex);

            if (revealDelay <= 0.05f)
            {
                return;
            }

            EnemySpawnTelegraph telegraph = spawnedEnemy.GetComponent<EnemySpawnTelegraph>();

            if (telegraph == null)
            {
                telegraph = spawnedEnemy.gameObject.AddComponent<EnemySpawnTelegraph>();
            }

            telegraph.Configure(
                revealDelay,
                ResolveSpawnTelegraphColor(waveIndex),
                CalculateSpawnTelegraphScale(spawnIndex, waveIndex),
                spawnTelegraphOpacity,
                spawnTelegraphPulseSpeed,
                spawnTelegraphLocalOffset);
        }

        private float CalculateSpawnTelegraphRevealDelay(float aggroDelay, int waveIndex)
        {
            float telegraphDurationMultiplier = _runtimeEncounterPacing != null
                ? _runtimeEncounterPacing.TelegraphDurationMultiplier
                : 1f;
            float revealRatio = spawnTelegraphRevealRatio;

            if (GetEffectiveRoomType() == RoomType.Challenge && waveIndex > 0)
            {
                revealRatio += 0.08f;
            }

            float revealDelay = aggroDelay * Mathf.Clamp(revealRatio * telegraphDurationMultiplier, 0.2f, 0.9f);
            float scaledMaxDuration = Mathf.Max(spawnTelegraphMinDuration, spawnTelegraphMaxDuration * telegraphDurationMultiplier);
            revealDelay = Mathf.Clamp(revealDelay, spawnTelegraphMinDuration, scaledMaxDuration);
            revealDelay = Mathf.Min(revealDelay, Mathf.Max(0f, aggroDelay - spawnTelegraphVisibleLeadTime));
            return revealDelay;
        }

        private float CalculateSpawnTelegraphScale(int spawnIndex, int waveIndex)
        {
            float roomTypeScale = GetEffectiveRoomType() switch
            {
                RoomType.Boss => 1.42f,
                RoomType.MiniBoss => 1.28f,
                RoomType.Challenge => waveIndex > 0 ? 1.18f : 1.08f,
                _ => 1f
            };

            float staggerScale = 1f + ((spawnIndex % 3) * 0.05f);
            return spawnTelegraphScale * roomTypeScale * staggerScale;
        }

        private Color ResolveSpawnTelegraphColor(int waveIndex)
        {
            Color roomAccent = GetEffectiveRoomType() switch
            {
                RoomType.Boss => new Color(0.95f, 0.3f, 0.36f, 1f),
                RoomType.MiniBoss => new Color(0.98f, 0.56f, 0.24f, 1f),
                RoomType.Challenge => new Color(0.99f, 0.64f, 0.22f, 1f),
                RoomType.Normal => new Color(0.48f, 0.84f, 1f, 1f),
                _ => new Color(0.82f, 0.88f, 0.98f, 1f)
            };

            if (GetEffectiveRoomType() == RoomType.Challenge && waveIndex > 0)
            {
                float escalation = Mathf.Clamp01(waveIndex * 0.34f);
                roomAccent = Color.Lerp(roomAccent, new Color(1f, 0.24f, 0.28f, 1f), escalation);
            }

            return roomAccent;
        }

        private void RaiseWaveSpawnThreatFlash(int waveIndex)
        {
            RoomType roomType = GetEffectiveRoomType();

            if (roomType != RoomType.Boss
                && roomType != RoomType.MiniBoss
                && !(roomType == RoomType.Challenge && waveIndex > 0))
            {
                return;
            }

            Color flashColor = ResolveSpawnTelegraphColor(waveIndex);
            float opacity = roomType == RoomType.Boss ? 0.09f : 0.07f;
            float duration = roomType == RoomType.Boss ? 0.42f : 0.32f;
            int pulseCount = roomType == RoomType.Boss || waveIndex > 0 ? 2 : 1;
            float pulseStrength = roomType == RoomType.Boss ? 0.28f : 0.2f;

            GameplayFeedbackEvents.RaiseThreatFlash(new ThreatFlashRequest(
                flashColor,
                opacity,
                duration,
                pulseCount,
                pulseStrength,
                0.96f,
                0.42f));
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
                      float candidateScore = EvaluateSpawnCandidate(targetRoom, candidate);

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
                float candidateScore = EvaluateSpawnCandidate(targetRoom, candidate);

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

        private float EvaluateSpawnCandidate(RoomController targetRoom, Vector3 candidate)
        {
            float playerDistance = GetDistanceToPlayer(candidate);
            float nearestSpawnDistance = GetNearestSpawnDistance(candidate);
            float score = playerDistance + (nearestSpawnDistance * 0.85f);

            float nearestDoorDistance = GetNearestDoorDistance(targetRoom, candidate);
            if (nearestDoorDistance >= 0f)
            {
                score += nearestDoorDistance * 1.15f;

                float doorClearanceDistance = GetDoorSpawnClearanceDistance();
                if (doorClearanceDistance > 0f && nearestDoorDistance < doorClearanceDistance)
                {
                    score -= (doorClearanceDistance - nearestDoorDistance) * 12f;
                }
            }

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

        private float GetDoorSpawnClearanceDistance()
        {
            return Mathf.Max(0f, doorSpawnClearanceDistance);
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

        private int ResolveAdjustedTotalEnemyCount(EnemyWaveAssignment enemyWaveAssignment)
        {
            if (enemyWaveAssignment == null)
            {
                return 0;
            }

            int totalCount = 0;

            for (int index = 0; index < enemyWaveAssignment.SpawnGroups.Count; index++)
            {
                EnemyWaveSpawnGroup spawnGroup = enemyWaveAssignment.SpawnGroups[index];

                if (spawnGroup == null || spawnGroup.EnemyPrefab == null || spawnGroup.Count <= 0)
                {
                    continue;
                }

                totalCount += ResolveAdjustedSpawnCount(spawnGroup.Count);
            }

            return totalCount;
        }

        private int ResolveAdjustedSpawnCount(int baseCount)
        {
            if (baseCount <= 0)
            {
                return 0;
            }

            RoomType roomType = GetEffectiveRoomType();
            float multiplier = roomType == RoomType.Normal || roomType == RoomType.Challenge
                ? Mathf.Max(1f, globalEnemyCountMultiplier)
                : 1f;

            return Mathf.Max(baseCount, Mathf.CeilToInt(baseCount * multiplier));
        }

        private void RaiseChallengeWaveFeedback(RoomController targetRoom, EnemyWaveAssignment waveAssignment, int currentWave, int totalWaves)
        {
            int enemyCount = waveAssignment != null ? ResolveAdjustedTotalEnemyCount(waveAssignment) : 0;
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

        private float GetNearestDoorDistance(RoomController targetRoom, Vector3 candidate)
        {
            if (targetRoom == null)
            {
                return -1f;
            }

            System.Collections.Generic.IReadOnlyList<RoomDoor> roomDoors = targetRoom.RoomDoors;
            if (roomDoors == null || roomDoors.Count == 0)
            {
                return -1f;
            }

            float nearestDistance = float.MaxValue;
            bool foundDoor = false;

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor roomDoor = roomDoors[i];
                if (roomDoor == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(candidate, roomDoor.GetArrivalPosition());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    foundDoor = true;
                }
            }

            return foundDoor ? nearestDistance : -1f;
        }

        private void ScheduleDormantRelease(RoomController targetRoom)
        {
            if (_combatReleaseRoutine != null)
            {
                StopCoroutine(_combatReleaseRoutine);
                _combatReleaseRoutine = null;
            }

            if (combatDormantReleaseDelay <= 0f)
            {
                ReleaseDormantEnemies(targetRoom);
                return;
            }

            _combatReleaseRoutine = StartCoroutine(ReleaseDormantEnemiesRoutine(targetRoom, combatDormantReleaseDelay));
        }

        private IEnumerator ReleaseDormantEnemiesRoutine(RoomController targetRoom, float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            if (targetRoom == null || targetRoom.State != RoomState.Combat)
            {
                _combatReleaseRoutine = null;
                yield break;
            }

            _combatReleaseRoutine = null;
            ReleaseDormantEnemies(targetRoom);
        }

        private void Reset()
        {
            roomController = GetComponent<RoomController>();
            runManager = FindFirstObjectByType<RunManager>(FindObjectsInactive.Exclude);
        }

        private void OnDisable()
        {
            if (_combatReleaseRoutine != null)
            {
                StopCoroutine(_combatReleaseRoutine);
                _combatReleaseRoutine = null;
            }
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
