using System.Collections.Generic;
using CuteIssac.Data.Item;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Data.Debug
{
    /// <summary>
    /// Designer-editable development-only debug actions and item shortcuts.
    /// </summary>
    [CreateAssetMenu(fileName = "DevelopmentDebugCatalog", menuName = "CuteIssac/Data/Debug/Development Debug Catalog")]
    public sealed class DevelopmentDebugCatalog : ScriptableObject
    {
        [SerializeField] private EnemyController bossPrefab;
        [SerializeField] private List<ItemData> grantableItems = new();

        public EnemyController BossPrefab => bossPrefab;
        public IReadOnlyList<ItemData> GrantableItems => grantableItems;
    }
}
