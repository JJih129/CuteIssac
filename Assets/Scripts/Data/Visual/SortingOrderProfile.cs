using UnityEngine;

namespace CuteIssac.Data.Visual
{
    [CreateAssetMenu(menuName = "CuteIssac/Visual/Sorting Order Profile", fileName = "DefaultSortingOrderProfile")]
    public sealed class SortingOrderProfile : ScriptableObject
    {
        private const string DefaultResourcesPath = "Sorting/DefaultSortingOrderProfile";

        private static SortingOrderProfile s_defaultProfile;
        private static bool s_defaultProfileLoaded;

        [Header("Room Layers")]
        [SerializeField] private int floorOrder = -1;
        [SerializeField] private int wallOrder = 0;
        [SerializeField] private int doorBaseOrder = 34;

        [Header("Actors")]
        [SerializeField] private int pickupOrder = 31;
        [SerializeField] private int droppedWeaponPickupOrder = 32;
        [SerializeField] private int playerBodyOrder = 45;
        [SerializeField] private int playerWeaponBackOffset = -1;
        [SerializeField] private int playerWeaponFrontOffset = 1;

        [Header("Feedback")]
        [SerializeField] private int effectOrder = 60;
        [SerializeField] private int hudWorldTextOrder = 70;

        public int FloorOrder => floorOrder;
        public int WallOrder => wallOrder;
        public int DoorBaseOrder => doorBaseOrder;
        public int PickupOrder => pickupOrder;
        public int DroppedWeaponPickupOrder => droppedWeaponPickupOrder;
        public int PlayerBodyOrder => playerBodyOrder;
        public int PlayerWeaponBackOffset => playerWeaponBackOffset;
        public int PlayerWeaponFrontOffset => playerWeaponFrontOffset;
        public int EffectOrder => effectOrder;
        public int HudWorldTextOrder => hudWorldTextOrder;

        public static int ResolveFloorOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.floorOrder : fallback;
        }

        public static int ResolveWallOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.wallOrder : fallback;
        }

        public static SortingOrderProfile Default
        {
            get
            {
                if (!s_defaultProfileLoaded)
                {
                    s_defaultProfile = Resources.Load<SortingOrderProfile>(DefaultResourcesPath);
                    s_defaultProfileLoaded = true;
                }

                return s_defaultProfile;
            }
        }

        public static int ResolveDoorBaseOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.doorBaseOrder : fallback;
        }

        public static int ResolvePickupOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.pickupOrder : fallback;
        }

        public static int ResolveDroppedWeaponPickupOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.droppedWeaponPickupOrder : fallback;
        }

        public static int ResolvePlayerBodyOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.playerBodyOrder : fallback;
        }

        public static int ResolvePlayerWeaponBackOffset(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.playerWeaponBackOffset : fallback;
        }

        public static int ResolvePlayerWeaponFrontOffset(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.playerWeaponFrontOffset : fallback;
        }

        public static int ResolveEffectOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.effectOrder : fallback;
        }

        public static int ResolveHudWorldTextOrder(SortingOrderProfile profile, int fallback)
        {
            SortingOrderProfile resolved = Resolve(profile);
            return resolved != null ? resolved.hudWorldTextOrder : fallback;
        }

        private static SortingOrderProfile Resolve(SortingOrderProfile profile)
        {
            return profile != null ? profile : Default;
        }
    }
}
