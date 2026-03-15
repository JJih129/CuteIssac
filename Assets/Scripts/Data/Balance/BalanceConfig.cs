using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Run;
using UnityEngine;

namespace CuteIssac.Data.Balance
{
    /// <summary>
    /// Central balance authoring entry point.
    /// It points at the run configuration that already fans out into floor, pool, and reward assets,
    /// giving designers one stable asset to open when tuning prototype values.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "CuteIssac/Data/Balance/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [SerializeField] private string configId = "default-balance";
        [SerializeField] private RunConfiguration runConfiguration;
        [SerializeField] [TextArea] private string designNotes;

        public string ConfigId => string.IsNullOrWhiteSpace(configId) ? name : configId;
        public RunConfiguration RunConfiguration => runConfiguration;
        public string DesignNotes => designNotes;

        public bool TryGetFloorConfig(int floorIndex, out FloorConfig floorConfig)
        {
            floorConfig = null;
            return runConfiguration != null && runConfiguration.TryGetFloorConfig(floorIndex, out floorConfig);
        }
    }
}
