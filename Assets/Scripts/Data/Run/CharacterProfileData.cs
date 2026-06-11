using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Item;
using UnityEngine;

namespace CuteIssac.Data.Run
{
    [CreateAssetMenu(fileName = "CharacterProfileData", menuName = "CuteIssac/Data/Run/Character Profile")]
    public sealed class CharacterProfileData : ScriptableObject
    {
        [SerializeField] private string characterId = "default";
        [SerializeField] private string displayName = "Default";
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool unlockedByDefault = true;
        [SerializeField] private string unlockKey;
        [SerializeField] [Min(0)] private int startingCoins;
        [SerializeField] [Min(0)] private int startingKeys = 1;
        [SerializeField] [Min(0)] private int startingBombs;
        [SerializeField] private ItemData startingWeaponItem;
        [SerializeField] private ActiveItemData startingActiveItem;
        [SerializeField] private List<ItemData> startingPassiveItems = new();
        [SerializeField] private List<StatModifier> statModifiers = new();
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool UnlockedByDefault => unlockedByDefault;
        public string UnlockKey => unlockKey;
        public int StartingCoins => startingCoins;
        public int StartingKeys => startingKeys;
        public int StartingBombs => startingBombs;
        public ItemData StartingWeaponItem => startingWeaponItem;
        public ActiveItemData StartingActiveItem => startingActiveItem;
        public IReadOnlyList<ItemData> StartingPassiveItems => startingPassiveItems;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => projectileModifiers;
    }
}
