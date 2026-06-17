using CuteIssac.Core.Gameplay;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class EnemyAmmoDropSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponLoadout weaponLoadout;

        [Header("Drop Rules")]
        [SerializeField] [Range(0f, 1f)] private float coinDropChance = CuteIssac.Item.EnemyDropRules.CoinDropChance;
        [SerializeField] [Range(0f, 1f)] private float ammoDropChance = CuteIssac.Item.EnemyDropRules.AmmoDropChance;
        [SerializeField] [Range(0f, 1f)] private float bombDropChance = CuteIssac.Item.EnemyDropRules.BombDropChance;
        [SerializeField] [Range(0f, 1f)] private float noDropChance = CuteIssac.Item.EnemyDropRules.NoDropChance;
        [SerializeField] [Range(0f, 1f)] private float singleCoinChance = CuteIssac.Item.EnemyDropRules.SingleCoinChance;
        [SerializeField] [Range(0f, 1f)] private float doubleCoinChance = CuteIssac.Item.EnemyDropRules.DoubleCoinChance;
        [SerializeField] [Range(0f, 1f)] private float tripleCoinChance = CuteIssac.Item.EnemyDropRules.TripleCoinChance;
        [SerializeField] [Min(0f)] private float spawnScatterRadius = 0.28f;

        [Header("Pickup Presentation")]
        [SerializeField] [Min(0.1f)] private float pickupColliderRadius = 0.42f;
        [SerializeField] [Min(0.2f)] private float pickupScale = 0.72f;
        [SerializeField] private int pickupSortingOrder = 31;
        [SerializeField] private Color ammoPickupBaseColor = new(0.96f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color ammoPickupCollectedColor = new(1f, 1f, 1f, 0.24f);
        [SerializeField] private Color coinPickupBaseColor = Color.white;
        [SerializeField] private Color coinPickupCollectedColor = new(1f, 1f, 1f, 0.24f);
        [SerializeField] private Color bombPickupBaseColor = new(1f, 0.54f, 0.26f, 1f);
        [SerializeField] private Color bombPickupCollectedColor = new(1f, 0.9f, 0.78f, 0.24f);

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            GameplayRuntimeEvents.EnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(EnemyKilledSignal signal)
        {
            ResolveReferences();

            if (!IsOwnedKill(signal))
            {
                return;
            }

            switch (ResolveDropKind())
            {
                case EnemyDropKind.Coins:
                    SpawnCoinDrops(signal.Position, ResolveCoinDropCount());
                    break;
                case EnemyDropKind.Ammo:
                    SpawnAmmoPickup(ResolveSpawnPosition(signal.Position));
                    break;
                case EnemyDropKind.Bomb:
                    SpawnBombPickup(ResolveSpawnPosition(signal.Position));
                    break;
            }
        }

        private bool IsOwnedKill(EnemyKilledSignal signal)
        {
            Transform killer = signal.Killer;
            return killer != null && (killer == transform || killer.IsChildOf(transform));
        }

        private EnemyDropKind ResolveDropKind()
        {
            return CuteIssac.Item.EnemyDropRules.RollDropKind(coinDropChance, ammoDropChance, bombDropChance, noDropChance);
        }

        private int ResolveCoinDropCount()
        {
            return CuteIssac.Item.EnemyDropRules.RollCoinDropCount(singleCoinChance, doubleCoinChance, tripleCoinChance);
        }

        private void SpawnCoinDrops(Vector3 origin, int count)
        {
            int coinCount = Mathf.Max(1, count);
            for (int index = 0; index < coinCount; index++)
            {
                RuntimePickupFactory.SpawnResourcePickup(
                    ResourcePickupType.Coin,
                    ResolveSpawnPosition(origin),
                    null,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    coinPickupBaseColor,
                    coinPickupCollectedColor,
                    "CoinPickup");
            }
        }

        private void SpawnBombPickup(Vector3 position)
        {
            RuntimePickupFactory.SpawnResourcePickup(
                ResourcePickupType.Bomb,
                position,
                null,
                pickupScale,
                pickupColliderRadius,
                pickupSortingOrder,
                bombPickupBaseColor,
                bombPickupCollectedColor,
                "BombPickup");
        }

        private void SpawnAmmoPickup(Vector3 position)
        {
            RuntimePickupFactory.SpawnEnemyDropPickup(
                EnemyDropKind.Ammo,
                position,
                null,
                pickupScale,
                pickupColliderRadius,
                pickupSortingOrder,
                ammoPickupBaseColor,
                ammoPickupCollectedColor,
                "AmmoPickup",
                true);
        }

        private Vector3 ResolveSpawnPosition(Vector3 enemyPosition)
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, spawnScatterRadius);
            return enemyPosition + new Vector3(offset.x, offset.y, 0f);
        }

        private void ResolveReferences()
        {
            if (weaponLoadout == null)
            {
                weaponLoadout = GetComponent<PlayerWeaponLoadout>();
            }
        }
    }
}
