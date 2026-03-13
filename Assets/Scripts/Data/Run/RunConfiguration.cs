using UnityEngine;

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

        public int StartingFloorIndex => Mathf.Max(1, startingFloorIndex);
        public bool UseFixedSeed => useFixedSeed;
        public int FixedSeed => fixedSeed;
    }
}
