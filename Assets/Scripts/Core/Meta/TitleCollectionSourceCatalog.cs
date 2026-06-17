using System.Collections.Generic;
using CuteIssac.Data.Balance;
using CuteIssac.Data.Debug;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Enemy;
using CuteIssac.Data.Item;
using CuteIssac.Data.Run;
using UnityEngine;

namespace CuteIssac.Core.Meta
{
    /// <summary>
    /// 타이틀 컬렉션/도감 화면에서 참조할 콘텐츠 소스를 명시적으로 관리하는 카탈로그입니다.
    /// 런타임 전체 Resources 스캔을 피하고, 작업자가 Inspector에서 포함 대상을 검토/수정할 수 있게 분리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TitleCollectionSourceCatalog", menuName = "CuteIssac/Data/Meta/Title Collection Source Catalog")]
    public sealed class TitleCollectionSourceCatalog : ScriptableObject
    {
        [SerializeField] private List<BalanceConfig> balanceConfigs = new();
        [SerializeField] private List<RunConfiguration> runConfigurations = new();
        [SerializeField] private List<FloorConfig> floorConfigs = new();
        [SerializeField] private List<EnemyPoolData> enemyPools = new();
        [SerializeField] private List<DevelopmentDebugCatalog> debugCatalogs = new();
        [SerializeField] private List<ItemData> itemDataAssets = new();
        [SerializeField] private List<ActiveItemData> activeItemDataAssets = new();

        public IReadOnlyList<BalanceConfig> BalanceConfigs => balanceConfigs;
        public IReadOnlyList<RunConfiguration> RunConfigurations => runConfigurations;
        public IReadOnlyList<FloorConfig> FloorConfigs => floorConfigs;
        public IReadOnlyList<EnemyPoolData> EnemyPools => enemyPools;
        public IReadOnlyList<DevelopmentDebugCatalog> DebugCatalogs => debugCatalogs;
        public IReadOnlyList<ItemData> ItemDataAssets => itemDataAssets;
        public IReadOnlyList<ActiveItemData> ActiveItemDataAssets => activeItemDataAssets;
    }
}
