using CuteIssac.Data.Dungeon;
using CuteIssac.Core.Run;
using UnityEngine;
using CuteIssac.Data.Item;

namespace CuteIssac.Data.Run
{
    /// <summary>
    /// Optional data asset for bootstrapping a run without hardcoding seed or starting floor values in scene objects.
    /// </summary>
    [CreateAssetMenu(fileName = "RunConfiguration", menuName = "CuteIssac/Data/Run Configuration")]
    public sealed class RunConfiguration : ScriptableObject
    {
        [SerializeField] [Min(1)] private int startingFloorIndex = 1;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 12345;
        [SerializeField] private FloorConfig[] floorSequence;

        [Header("Run Resume")]
        [SerializeField] private RunResumeCurrentRoomPolicy currentRoomResumePolicy = RunResumeCurrentRoomPolicy.RestartEncounter;
        [SerializeField] private bool announceRunResume = true;
        [SerializeField] private bool announceCurrentRoomPolicy = true;
        [SerializeField] private Color runResumeAccentColor = new(0.52f, 0.88f, 1f, 1f);
        [SerializeField] private Color currentRoomPolicyAccentColor = new(1f, 0.72f, 0.34f, 1f);
        [SerializeField] [Min(0.25f)] private float runResumeBannerDuration = 1.6f;
        [SerializeField] [Min(0.25f)] private float currentRoomPolicyBannerDuration = 1.45f;
        [SerializeField] private ActiveItemData[] restorableActiveItems;
        [SerializeField] private ConsumableItemData[] restorableConsumables;
        [SerializeField] private ItemData[] restorableTrinkets;

        public int StartingFloorIndex => Mathf.Max(1, startingFloorIndex);
        public bool UseFixedSeed => useFixedSeed;
        public int FixedSeed => fixedSeed;
        public RunResumeCurrentRoomPolicy CurrentRoomResumePolicy => currentRoomResumePolicy;
        public bool AnnounceRunResume => announceRunResume;
        public bool AnnounceCurrentRoomPolicy => announceCurrentRoomPolicy;
        public Color RunResumeAccentColor => runResumeAccentColor;
        public Color CurrentRoomPolicyAccentColor => currentRoomPolicyAccentColor;
        public float RunResumeBannerDuration => Mathf.Max(0.25f, runResumeBannerDuration);
        public float CurrentRoomPolicyBannerDuration => Mathf.Max(0.25f, currentRoomPolicyBannerDuration);
        public ActiveItemData[] RestorableActiveItems => restorableActiveItems;
        public ConsumableItemData[] RestorableConsumables => restorableConsumables;
        public ItemData[] RestorableTrinkets => restorableTrinkets;

        public bool TryGetFloorConfig(int floorIndex, out FloorConfig floorConfig)
        {
            floorConfig = null;

            if (floorSequence == null || floorSequence.Length == 0)
            {
                return false;
            }

            int index = floorIndex - 1;

            if (index < 0 || index >= floorSequence.Length)
            {
                return false;
            }

            floorConfig = floorSequence[index];
            return floorConfig != null;
        }

        public bool HasFloor(int floorIndex)
        {
            return TryGetFloorConfig(floorIndex, out _);
        }
    }
}
