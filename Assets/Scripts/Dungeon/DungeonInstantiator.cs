using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Converts generated dungeon data into real scene objects.
    /// Generation stays data-only, while this class owns prefab creation, door wiring, and cleanup for one instantiated floor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DungeonInstantiator : MonoBehaviour
    {
        [Header("Scene Targets")]
        [SerializeField] private Transform roomsRoot;
        [SerializeField] private RoomNavigationController roomNavigationController;
        [SerializeField] private PlayerController playerController;

        [Header("Placement")]
        [SerializeField] private Vector2 roomWorldSpacing = new(40f, 24f);
        [SerializeField] private bool clearPreviousRoomsBeforeInstantiate = true;
        [SerializeField] private bool movePlayerToStartRoom = true;

        public event Action<DungeonInstantiationResult> DungeonInstantiated;

        public DungeonInstantiationResult CurrentInstance { get; private set; }

        public DungeonInstantiationResult InstantiateDungeon(DungeonMap dungeonMap)
        {
            if (dungeonMap == null)
            {
                Debug.LogError("DungeonInstantiator requires a DungeonMap.");
                return null;
            }

            if (clearPreviousRoomsBeforeInstantiate)
            {
                ClearInstantiatedDungeon();
            }

            Transform targetRoot = EnsureRoomsRoot();
            DungeonInstantiationResult result = new(dungeonMap, targetRoot);

            InstantiateRooms(dungeonMap, result, targetRoot);
            WireDoors(dungeonMap, result);
            ApplyNavigation(result);

            CurrentInstance = result;
            DungeonInstantiated?.Invoke(result);
            return result;
        }

        [ContextMenu("Clear Instantiated Dungeon")]
        public void ClearInstantiatedDungeon()
        {
            if (roomsRoot == null)
            {
                return;
            }

            for (int i = roomsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = roomsRoot.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            CurrentInstance = null;
        }

        private Transform EnsureRoomsRoot()
        {
            if (roomsRoot != null)
            {
                return roomsRoot;
            }

            GameObject rootObject = new("GeneratedRooms");
            rootObject.transform.SetParent(transform, false);
            roomsRoot = rootObject.transform;
            return roomsRoot;
        }

        private void InstantiateRooms(DungeonMap dungeonMap, DungeonInstantiationResult result, Transform targetRoot)
        {
            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;
                RoomLayoutData layout = roomNode.ResolvedLayout;

                if (layout == null || layout.RoomPrefab == null)
                {
                    Debug.LogWarning($"DungeonInstantiator skipped room {roomNode.GridPosition} because no resolved room layout prefab was available.", this);
                    continue;
                }

                Vector3 worldPosition = ToWorldPosition(roomNode.GridPosition);
                RoomController roomInstance = Instantiate(layout.RoomPrefab, worldPosition, Quaternion.identity, targetRoot);
                roomInstance.name = BuildRoomName(roomNode, layout);
                ConfigureGeneratedRoom(roomInstance, roomNode, dungeonMap.FloorConfig);
                result.RegisterRoom(roomNode.GridPosition, roomInstance, roomNode.RoomType == RoomType.Start);
            }
        }

        private void WireDoors(DungeonMap dungeonMap, DungeonInstantiationResult result)
        {
            foreach (RoomController roomController in result.BuildRoomArray())
            {
                if (roomController == null)
                {
                    continue;
                }

                IReadOnlyList<RoomDoor> roomDoors = roomController.RoomDoors;

                for (int i = 0; i < roomDoors.Count; i++)
                {
                    if (roomDoors[i] != null)
                    {
                        roomDoors[i].SetConnection(null, null);
                    }
                }
            }

            foreach (KeyValuePair<GridPosition, DungeonRoomNode> roomPair in dungeonMap.RoomsByPosition)
            {
                DungeonRoomNode roomNode = roomPair.Value;

                if (!result.TryGetRoom(roomNode.GridPosition, out RoomController roomController))
                {
                    continue;
                }

                for (int i = 0; i < roomNode.Connections.Count; i++)
                {
                    RoomConnection connection = roomNode.Connections[i];

                    if (!result.TryGetRoom(connection.TargetPosition, out RoomController targetRoom))
                    {
                        continue;
                    }

                    if (!roomController.TryGetDoor(connection.Direction, out RoomDoor sourceDoor))
                    {
                        Debug.LogWarning($"Room {roomController.name} is missing a {connection.Direction} door for dungeon wiring.", roomController);
                        continue;
                    }

                    RoomDirection oppositeDirection = RoomDirectionUtility.Opposite(connection.Direction);

                    if (!targetRoom.TryGetDoor(oppositeDirection, out RoomDoor targetDoor))
                    {
                        Debug.LogWarning($"Room {targetRoom.name} is missing a {oppositeDirection} door for dungeon wiring.", targetRoom);
                        continue;
                    }

                    if (!dungeonMap.TryGetRoom(connection.TargetPosition, out DungeonRoomNode targetNode))
                    {
                        continue;
                    }

                    if (!ShouldWireConnection(dungeonMap, roomNode, targetNode))
                    {
                        continue;
                    }

                    sourceDoor.SetConnection(targetRoom, targetDoor);
                    sourceDoor.ConfigureEntryCost(
                        targetNode.RoomData != null ? targetNode.RoomData.EntryKeyCost : 0,
                        targetNode.RoomData != null && targetNode.RoomData.ConsumeEntryCostOnce);
                    sourceDoor.ConfigureHealthEntryCost(
                        ShouldRequireCurseHealthEntryCost(roomNode, targetNode) ? 1f : 0f,
                        true,
                        true);

                    ConfigureSecretDoorAccess(roomNode, targetNode, sourceDoor);
                }
            }
        }

        private void ApplyNavigation(DungeonInstantiationResult result)
        {
            if (result == null || result.StartRoom == null)
            {
                return;
            }

            if (roomNavigationController == null)
            {
                roomNavigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            if (roomNavigationController != null)
            {
                roomNavigationController.ConfigureGeneratedRooms(result.BuildRoomArray(), result.StartRoom, playerController);
                return;
            }

            if (movePlayerToStartRoom && playerController != null)
            {
                playerController.transform.position = result.StartRoom.DefaultPlayerSpawnPosition;
            }
        }

        private Vector3 ToWorldPosition(GridPosition gridPosition)
        {
            return transform.position + new Vector3(gridPosition.X * roomWorldSpacing.x, gridPosition.Y * roomWorldSpacing.y, 0f);
        }

        private static void ConfigureGeneratedRoom(RoomController roomInstance, DungeonRoomNode roomNode, FloorConfig floorConfig)
        {
            if (roomInstance == null || roomNode == null)
            {
                return;
            }

            string runtimeRoomId = roomNode.RoomData != null && !string.IsNullOrWhiteSpace(roomNode.RoomData.RoomId)
                ? roomNode.RoomData.RoomId
                : roomInstance.RoomId;

            roomInstance.ConfigureRuntimeMetadata(runtimeRoomId, roomNode.RoomType);

            RoomEnemySpawner roomEnemySpawner = roomInstance.GetComponent<RoomEnemySpawner>();

            if (roomEnemySpawner != null)
            {
                roomEnemySpawner.ConfigureEncounter(
                    roomNode.RoomType,
                    roomNode.AssignedEnemyWave,
                    floorConfig != null ? floorConfig.GetEncounterPacing(roomNode.RoomType) : null);
            }

            RoomRewardSpawner roomRewardSpawner = roomInstance.GetComponent<RoomRewardSpawner>();

            if (roomRewardSpawner != null)
            {
                roomRewardSpawner.ConfigureFloorRewardPool(
                    roomNode.RoomType,
                    floorConfig != null ? floorConfig.GetRewardPool(roomNode.RoomType) : null);
                roomRewardSpawner.ConfigureChallengeRewards(
                    roomNode.RoomType,
                    floorConfig != null ? floorConfig.ChallengeRewardSettings : null);
                roomRewardSpawner.ConfigureSecretRewards(
                    roomNode.RoomType,
                    floorConfig != null ? floorConfig.SecretRoomRewardSettings : null);
            }

            RoomThemeController roomThemeController = roomInstance.GetComponent<RoomThemeController>();

            if (roomThemeController != null)
            {
                roomThemeController.ApplyTheme(floorConfig != null ? floorConfig.RoomTheme : null);
            }

            RoomTypeContentController roomTypeContentController = roomInstance.GetComponent<RoomTypeContentController>();

            if (roomTypeContentController != null)
            {
                roomTypeContentController.ConfigureRoom(
                    roomNode.RoomType,
                    roomNode.RoomData,
                    floorConfig != null ? floorConfig.GetItemPool(roomNode.RoomType) : null);
            }
        }

        private static string BuildRoomName(DungeonRoomNode roomNode, RoomLayoutData layout)
        {
            string layoutId = !string.IsNullOrWhiteSpace(layout.LayoutId) ? layout.LayoutId : "layout";
            return $"{roomNode.RoomType}_{layoutId}_{roomNode.GridPosition.X}_{roomNode.GridPosition.Y}";
        }

        private static void ConfigureSecretDoorAccess(DungeonRoomNode sourceNode, DungeonRoomNode targetNode, RoomDoor sourceDoor)
        {
            if (sourceNode == null || targetNode == null || sourceDoor == null)
            {
                return;
            }

            if (sourceNode.RoomType == RoomType.Secret || targetNode.RoomType != RoomType.Secret)
            {
                sourceDoor.ConfigureRevealRequirement(false, true);
                return;
            }

            SecretDoorAccessController secretDoorAccessController = sourceDoor.GetComponent<SecretDoorAccessController>();

            if (secretDoorAccessController == null)
            {
                secretDoorAccessController = sourceDoor.gameObject.AddComponent<SecretDoorAccessController>();
            }

            secretDoorAccessController.Configure(sourceDoor, SecretDoorRevealMode.BombOnly);
        }

        private static bool ShouldWireConnection(DungeonMap dungeonMap, DungeonRoomNode sourceNode, DungeonRoomNode targetNode)
        {
            if (dungeonMap == null || sourceNode == null || targetNode == null)
            {
                return false;
            }

            if (sourceNode.RoomType == RoomType.Treasure)
            {
                return IsPrimaryTreasureEntrance(dungeonMap, sourceNode, targetNode.GridPosition);
            }

            if (targetNode.RoomType == RoomType.Treasure)
            {
                return IsPrimaryTreasureEntrance(dungeonMap, targetNode, sourceNode.GridPosition);
            }

            return true;
        }

        private static bool ShouldRequireCurseHealthEntryCost(DungeonRoomNode sourceNode, DungeonRoomNode targetNode)
        {
            if (sourceNode == null || targetNode == null)
            {
                return false;
            }

            return sourceNode.RoomType != RoomType.Curse && targetNode.RoomType == RoomType.Curse;
        }

        private static bool IsPrimaryTreasureEntrance(DungeonMap dungeonMap, DungeonRoomNode treasureNode, GridPosition candidateNeighborPosition)
        {
            if (dungeonMap == null || treasureNode == null || treasureNode.RoomType != RoomType.Treasure)
            {
                return true;
            }

            GridPosition primaryNeighborPosition = candidateNeighborPosition;
            bool hasPrimaryNeighbor = false;
            int bestDistance = int.MaxValue;

            for (int index = 0; index < treasureNode.Connections.Count; index++)
            {
                RoomConnection connection = treasureNode.Connections[index];

                if (!dungeonMap.TryGetRoom(connection.TargetPosition, out DungeonRoomNode neighborNode) || neighborNode == null)
                {
                    continue;
                }

                if (!hasPrimaryNeighbor || neighborNode.DistanceFromStart < bestDistance)
                {
                    bestDistance = neighborNode.DistanceFromStart;
                    primaryNeighborPosition = neighborNode.GridPosition;
                    hasPrimaryNeighbor = true;
                }
            }

            return !hasPrimaryNeighbor || primaryNeighborPosition.Equals(candidateNeighborPosition);
        }
    }
}
