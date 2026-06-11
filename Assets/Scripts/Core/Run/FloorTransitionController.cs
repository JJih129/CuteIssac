using System.Collections;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    [DisallowMultipleComponent]
    public sealed class FloorTransitionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private DungeonInstantiationDebugRunner dungeonRunner;
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [SerializeField] private FloorExit floorExitTemplate;

        [Header("Feedback")]
        [SerializeField] private bool announceFloorStart = true;
        [SerializeField] private Color transitionAccentColor = new(0.66f, 0.9f, 1f, 1f);
        [SerializeField] [Min(0.15f)] private float transitionBannerDuration = 1.25f;
        [SerializeField] [Min(0f)] private float generationDelay = 0.15f;
        [SerializeField] [Min(0f)] private float bossClearPortalDelay = 1.2f;

        private RoomController _trackedBossRoom;
        private FloorExit _activeFloorExit;
        private Coroutine _transitionRoutine;
        private Coroutine _bossPortalRoutine;
        private int _activeFloorExitTargetFloorIndex = -1;

        private const float BossPortalRewardSeparation = 1.85f;
        private const float BossPortalBoundsPadding = 1.05f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
                runManager.FloorTransitionStarted -= HandleFloorTransitionStarted;
                runManager.FloorTransitionStarted += HandleFloorTransitionStarted;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
                runManager.FloorTransitionCompleted += HandleFloorTransitionCompleted;
                runManager.RunEnded -= HandleRunEnded;
                runManager.RunEnded += HandleRunEnded;
            }

            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
                dungeonInstantiator.DungeonInstantiated += HandleDungeonInstantiated;
            }

            TrySyncWithCurrentRun();
        }

        private void OnDisable()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.FloorTransitionStarted -= HandleFloorTransitionStarted;
                runManager.FloorTransitionCompleted -= HandleFloorTransitionCompleted;
                runManager.RunEnded -= HandleRunEnded;
            }

            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
            }

            UnbindBossRoom();
            DestroyActiveFloorExit();

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            if (_bossPortalRoutine != null)
            {
                StopCoroutine(_bossPortalRoutine);
                _bossPortalRoutine = null;
            }
        }

        private void HandleRunStarted(RunContext context)
        {
            StartGenerationRoutine(context.CurrentFloorIndex, false);
        }

        private void HandleFloorTransitionStarted(RunFloorTransitionInfo info)
        {
            if (_bossPortalRoutine != null)
            {
                StopCoroutine(_bossPortalRoutine);
                _bossPortalRoutine = null;
            }

            DestroyActiveFloorExit();
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "다음 층 이동",
                $"{info.NextFloorIndex}층으로 내려갑니다",
                transitionAccentColor,
                transitionBannerDuration));
        }

        private void HandleFloorTransitionCompleted(RunFloorTransitionInfo info)
        {
            StartGenerationRoutine(info.NextFloorIndex, true);
        }

        private void HandleRunEnded(RunContext context, RunEndReason endReason)
        {
            if (_bossPortalRoutine != null)
            {
                StopCoroutine(_bossPortalRoutine);
                _bossPortalRoutine = null;
            }

            DestroyActiveFloorExit();
            UnbindBossRoom();
        }

        private void HandleDungeonInstantiated(DungeonInstantiationResult result)
        {
            if (_bossPortalRoutine != null)
            {
                StopCoroutine(_bossPortalRoutine);
                _bossPortalRoutine = null;
            }

            DestroyActiveFloorExit();
            BindBossRoom(result);
        }

        private void HandleBossRoomCleared(RoomController roomController)
        {
            QueueBossPortal(roomController, useDelay: true);
        }

        private void HandleFloorExitActivated(FloorExit floorExit)
        {
            if (floorExit == null || runManager == null)
            {
                return;
            }

            int expectedNextFloorIndex = runManager.CurrentContext.CurrentFloorIndex + 1;
            if (floorExit.TargetFloorIndex != expectedNextFloorIndex || !runManager.HasFloor(floorExit.TargetFloorIndex))
            {
                UnityEngine.Debug.LogWarning(
                    $"FloorTransitionController ignored a stale floor exit. Target={floorExit.TargetFloorIndex}, expected={expectedNextFloorIndex}.",
                    floorExit);
                DestroyActiveFloorExit();
                return;
            }

            DestroyActiveFloorExit();
            runManager.AdvanceFloor();
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (dungeonRunner == null)
            {
                dungeonRunner = GetComponent<DungeonInstantiationDebugRunner>();
            }

            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = GetComponent<DungeonInstantiator>();
            }
        }

        private void TrySyncWithCurrentRun()
        {
            if (runManager == null || !runManager.CurrentContext.HasActiveRun)
            {
                return;
            }

            if (dungeonInstantiator != null && dungeonInstantiator.CurrentInstance != null)
            {
                BindBossRoom(dungeonInstantiator.CurrentInstance);
                return;
            }

            StartGenerationRoutine(runManager.CurrentContext.CurrentFloorIndex, false);
        }

        private void StartGenerationRoutine(int floorIndex, bool isTransition)
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }

            _transitionRoutine = StartCoroutine(GenerateFloorRoutine(floorIndex, isTransition));
        }

        private IEnumerator GenerateFloorRoutine(int floorIndex, bool isTransition)
        {
            if (generationDelay > 0f && isTransition)
            {
                yield return new WaitForSeconds(generationDelay);
            }
            else
            {
                yield return null;
            }

            _transitionRoutine = null;

            if (!TryResolveFloorConfig(floorIndex, out FloorConfig floorConfig))
            {
                if (runManager != null && floorIndex > runManager.CurrentContext.CurrentFloorIndex)
                {
                    runManager.EndRun(RunEndReason.Victory);
                }

                yield break;
            }

            if (dungeonRunner == null)
            {
                UnityEngine.Debug.LogError("FloorTransitionController requires a DungeonInstantiationDebugRunner reference.", this);
                yield break;
            }

            int seed = ResolveFloorSeed(runManager != null ? runManager.CurrentContext.Seed : 0, floorIndex);
            DungeonInstantiationResult result = dungeonRunner.GenerateAndInstantiateDungeon(floorConfig, seed, runManager != null ? runManager.CurrentContext : null);

            if (result == null)
            {
                UnityEngine.Debug.LogError($"FloorTransitionController failed to instantiate floor {floorIndex}.", this);
                yield break;
            }

            if (announceFloorStart)
            {
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    $"{floorIndex}층",
                    SanitizeFloorLabel(floorConfig.name),
                    transitionAccentColor,
                    transitionBannerDuration));
            }
        }

        private bool TryResolveFloorConfig(int floorIndex, out FloorConfig floorConfig)
        {
            if (runManager != null && runManager.TryGetFloorConfig(floorIndex, out floorConfig))
            {
                return true;
            }

            floorConfig = null;
            return false;
        }

        private void BindBossRoom(DungeonInstantiationResult result)
        {
            UnbindBossRoom();

            if (result == null || result.DungeonMap == null)
            {
                return;
            }

            foreach (var roomPair in result.RoomsByPosition)
            {
                if (!result.DungeonMap.TryGetRoom(roomPair.Key, out DungeonRoomNode roomNode) ||
                    roomNode == null ||
                    roomNode.RoomType != RoomType.Boss ||
                    roomPair.Value == null)
                {
                    continue;
                }

                _trackedBossRoom = roomPair.Value;
                _trackedBossRoom.RoomCleared -= HandleBossRoomCleared;
                _trackedBossRoom.RoomCleared += HandleBossRoomCleared;

                if (_trackedBossRoom.HasResolvedRoom)
                {
                    QueueBossPortal(_trackedBossRoom, useDelay: false);
                }

                break;
            }
        }

        private void UnbindBossRoom()
        {
            if (_trackedBossRoom != null)
            {
                _trackedBossRoom.RoomCleared -= HandleBossRoomCleared;
                _trackedBossRoom = null;
            }
        }

        private IEnumerator SpawnBossPortalRoutine(RoomController roomController, int nextFloorIndex)
        {
            if (bossClearPortalDelay > 0f)
            {
                yield return new WaitForSeconds(bossClearPortalDelay);
            }

            _bossPortalRoutine = null;

            if (roomController == null)
            {
                yield break;
            }

            if (HasActiveFloorExitFor(nextFloorIndex))
            {
                yield break;
            }

            SpawnFloorExit(roomController, nextFloorIndex, true);
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "차원 포탈 개방",
                $"{nextFloorIndex}층 포탈이 방 중앙에 생성되었습니다",
                transitionAccentColor,
                2.05f));
        }

        private void QueueBossPortal(RoomController roomController, bool useDelay)
        {
            if (roomController == null)
            {
                return;
            }

            if (!TryResolveNextFloorIndex(out int nextFloorIndex))
            {
                DestroyActiveFloorExit();
                runManager?.EndRun(RunEndReason.Victory);
                return;
            }

            if (HasActiveFloorExitFor(nextFloorIndex))
            {
                return;
            }

            if (_bossPortalRoutine != null)
            {
                StopCoroutine(_bossPortalRoutine);
                _bossPortalRoutine = null;
            }

            if (useDelay && bossClearPortalDelay > 0f)
            {
                _bossPortalRoutine = StartCoroutine(SpawnBossPortalRoutine(roomController, nextFloorIndex));
                return;
            }

            SpawnFloorExit(roomController, nextFloorIndex, true);
        }

        private bool TryResolveNextFloorIndex(out int nextFloorIndex)
        {
            nextFloorIndex = runManager != null
                ? runManager.CurrentContext.CurrentFloorIndex + 1
                : 0;

            return runManager != null && runManager.HasFloor(nextFloorIndex);
        }

        private bool HasActiveFloorExitFor(int targetFloorIndex)
        {
            return _activeFloorExit != null
                && _activeFloorExitTargetFloorIndex == targetFloorIndex
                && _activeFloorExit.TargetFloorIndex == targetFloorIndex;
        }

        private void SpawnFloorExit(RoomController roomController, int nextFloorIndex, bool preferRoomCenter)
        {
            if (roomController == null)
            {
                return;
            }

            DestroyActiveFloorExit();

            FloorExit floorExit;

            if (floorExitTemplate != null)
            {
                floorExit = Instantiate(floorExitTemplate, roomController.transform);
            }
            else
            {
                GameObject floorExitObject = new("FloorExit");
                floorExitObject.transform.SetParent(roomController.transform, false);
                floorExit = floorExitObject.AddComponent<FloorExit>();
            }

            floorExit.transform.position = ResolveFloorExitPosition(roomController, preferRoomCenter);
            floorExit.Configure(runManager, nextFloorIndex, transitionAccentColor);
            floorExit.Activated -= HandleFloorExitActivated;
            floorExit.Activated += HandleFloorExitActivated;
            _activeFloorExit = floorExit;
            _activeFloorExitTargetFloorIndex = nextFloorIndex;
        }

        private void DestroyActiveFloorExit()
        {
            if (_activeFloorExit == null)
            {
                _activeFloorExitTargetFloorIndex = -1;
                return;
            }

            _activeFloorExit.Activated -= HandleFloorExitActivated;

            if (Application.isPlaying)
            {
                Destroy(_activeFloorExit.gameObject);
            }
            else
            {
                DestroyImmediate(_activeFloorExit.gameObject);
            }

            _activeFloorExit = null;
            _activeFloorExitTargetFloorIndex = -1;
        }

        private static Vector3 ResolveFloorExitPosition(RoomController roomController, bool preferRoomCenter)
        {
            Bounds roomBounds = roomController.RoomBounds;
            Vector3 center = roomBounds.center;

            if (preferRoomCenter)
            {
                return ResolveCenteredFloorExitPosition(roomController, roomBounds, center);
            }

            if (roomController.TryGetLastEnemyDeathPosition(out Vector3 lastEnemyDeathPosition))
            {
                return new Vector3(lastEnemyDeathPosition.x, lastEnemyDeathPosition.y, roomController.transform.position.z);
            }

            return new Vector3(center.x, center.y - 1.25f, roomController.transform.position.z);
        }

        private static Vector3 ResolveCenteredFloorExitPosition(RoomController roomController, Bounds roomBounds, Vector3 center)
        {
            Vector3 portalPosition = new(center.x, center.y, roomController.transform.position.z);
            RoomRewardSpawner rewardSpawner = roomController.GetComponent<RoomRewardSpawner>();

            if (rewardSpawner == null || !rewardSpawner.TryGetPrimaryRewardAnchorPosition(out Vector3 rewardPosition))
            {
                return portalPosition;
            }

            Vector2 portalToReward = (Vector2)portalPosition - (Vector2)rewardPosition;
            float minDistanceSqr = BossPortalRewardSeparation * BossPortalRewardSeparation;
            if (portalToReward.sqrMagnitude >= minDistanceSqr)
            {
                return portalPosition;
            }

            Vector2 offsetDirection = portalToReward.sqrMagnitude > 0.0001f
                ? portalToReward.normalized
                : Vector2.down;
            Vector2 separatedPosition = (Vector2)rewardPosition + offsetDirection * BossPortalRewardSeparation;
            separatedPosition.x = Mathf.Clamp(
                separatedPosition.x,
                roomBounds.min.x + BossPortalBoundsPadding,
                roomBounds.max.x - BossPortalBoundsPadding);
            separatedPosition.y = Mathf.Clamp(
                separatedPosition.y,
                roomBounds.min.y + BossPortalBoundsPadding,
                roomBounds.max.y - BossPortalBoundsPadding);

            return new Vector3(separatedPosition.x, separatedPosition.y, roomController.transform.position.z);
        }

        private static int ResolveFloorSeed(int runSeed, int floorIndex)
        {
            unchecked
            {
                return (runSeed * 397) ^ (floorIndex * 104729);
            }
        }

        private static string SanitizeFloorLabel(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "던전 진입";
            }

            string cleaned = rawName
                .Replace("DungeonConfig", string.Empty)
                .Replace("Floor", string.Empty)
                .Trim();

            return int.TryParse(cleaned, out int floorNumber)
                ? $"{floorNumber}층 구역"
                : cleaned;
        }
    }
}
