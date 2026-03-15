using CuteIssac.Data.Item;
using CuteIssac.Room;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct CurseRewardManifestedSignal
    {
        public CurseRewardManifestedSignal(RoomController room, ItemData itemData)
        {
            Room = room;
            ItemData = itemData;
        }

        public RoomController Room { get; }
        public ItemData ItemData { get; }
        public ItemRarity Rarity => ItemData != null ? ItemData.Rarity : ItemRarity.Common;
        public bool IsValid => Room != null && ItemData != null;
    }
}
