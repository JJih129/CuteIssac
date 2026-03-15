using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Data.Run
{
    /// <summary>
    /// Data-only definition for one selectable starting character/build.
    /// The shared player prefab stays unchanged while runs begin with different items, resources, and stat modifiers.
    /// </summary>
    [CreateAssetMenu(fileName = "StartingBuildData", menuName = "CuteIssac/Data/Run/Starting Build Data")]
    public sealed class StartingBuildData : ScriptableObject
    {
        [SerializeField] private string buildId = "starting-build";
        [SerializeField] private string displayName = "Starting Build";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(0)] private int startingCoins;
        [SerializeField] [Min(0)] private int startingKeys;
        [SerializeField] [Min(0)] private int startingBombs;
        [SerializeField] private List<ItemData> startingPassiveItems = new();
        [SerializeField] private List<StatModifier> statModifiers = new();
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();

        public string BuildId => buildId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int StartingCoins => startingCoins;
        public int StartingKeys => startingKeys;
        public int StartingBombs => startingBombs;
        public IReadOnlyList<ItemData> StartingPassiveItems => startingPassiveItems;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => projectileModifiers;
    }
}
