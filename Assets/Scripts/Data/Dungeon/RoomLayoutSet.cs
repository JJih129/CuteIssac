using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    /// <summary>
    /// Shared pool of room layouts.
    /// FloorConfig can reference one or more sets so different floors can reuse or swap layout collections without editing RoomData assets.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomLayoutSet", menuName = "CuteIssac/Data/Dungeon/Room Layout Set")]
    public sealed class RoomLayoutSet : ScriptableObject
    {
        [SerializeField] private List<RoomLayoutData> layouts = new();

        public IReadOnlyList<RoomLayoutData> Layouts => layouts;

        /// <summary>
        /// Shared sets can contain layouts for multiple room types.
        /// Filtering here keeps later resolution simpler and makes debug output easier to reason about.
        /// </summary>
        public void CollectLayouts(RoomType roomType, List<RoomLayoutData> results)
        {
            if (results == null)
            {
                return;
            }

            for (int i = 0; i < layouts.Count; i++)
            {
                RoomLayoutData layout = layouts[i];

                if (layout != null && layout.SupportsRoomType(roomType) && !results.Contains(layout))
                {
                    results.Add(layout);
                }
            }
        }
    }
}
