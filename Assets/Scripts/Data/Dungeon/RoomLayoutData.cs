using System.Collections.Generic;
using CuteIssac.Dungeon;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// Authored room layout asset.
    /// This represents a concrete scene prefab arrangement and the doorway directions it supports.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomLayoutData", menuName = "CuteIssac/Data/Dungeon/Room Layout Data")]
    public sealed class RoomLayoutData : ScriptableObject
    {
        [SerializeField] private string layoutId = "layout";
        [SerializeField] private RoomController roomPrefab;
        [SerializeField] private List<RoomType> supportedRoomTypes = new() { RoomType.Normal };
        [SerializeField] private RoomDoorMask supportedDoorMask = RoomDoorMask.Up | RoomDoorMask.Right | RoomDoorMask.Down | RoomDoorMask.Left;
        [SerializeField] [Min(0)] private int selectionWeight = 1;

        public string LayoutId => layoutId;
        public RoomController RoomPrefab => roomPrefab;
        public RoomDoorMask SupportedDoorMask => supportedDoorMask;
        public int SelectionWeight => selectionWeight;
        public bool HasPrefab => roomPrefab != null;

        public bool SupportsRoomType(RoomType roomType)
        {
            for (int i = 0; i < supportedRoomTypes.Count; i++)
            {
                if (supportedRoomTypes[i] == roomType)
                {
                    return true;
                }
            }

            return false;
        }

        public bool SupportsDoors(RoomDoorMask requiredDoorMask)
        {
            return (supportedDoorMask & requiredDoorMask) == requiredDoorMask;
        }

        /// <summary>
        /// Centralized compatibility check used by the layout resolver.
        /// Layouts without a prefab are ignored so a resolved node is always ready for scene instantiation.
        /// </summary>
        public bool IsCompatible(RoomType roomType, RoomDoorMask requiredDoorMask)
        {
            return HasPrefab && SupportsRoomType(roomType) && SupportsDoors(requiredDoorMask);
        }
    }
}
