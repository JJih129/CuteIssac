using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Run
{
    /// <summary>
    /// Resource-loaded catalog for build selection at run start.
    /// </summary>
    [CreateAssetMenu(fileName = "StartingBuildCatalog", menuName = "CuteIssac/Data/Run/Starting Build Catalog")]
    public sealed class StartingBuildCatalog : ScriptableObject
    {
        [SerializeField] private List<StartingBuildData> builds = new();
        [SerializeField] private StartingBuildData defaultBuild;

        public IReadOnlyList<StartingBuildData> Builds => builds;
        public StartingBuildData DefaultBuild => defaultBuild != null ? defaultBuild : GetFirstValidBuild();

        private StartingBuildData GetFirstValidBuild()
        {
            for (int index = 0; index < builds.Count; index++)
            {
                if (builds[index] != null)
                {
                    return builds[index];
                }
            }

            return null;
        }
    }
}
