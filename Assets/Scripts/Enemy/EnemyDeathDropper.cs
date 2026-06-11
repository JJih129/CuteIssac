using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Small Isaac-style death dropper for regular enemies.
    /// Kept separate from EnemyHealth so health stays combat-only and pooled enemies can be reconfigured per room.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyDeathDropper : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] [Range(0f, 1f)] private float dropChance = 0.08f;
        [SerializeField] [Min(0f)] private float coinWeight = 0.78f;
        [SerializeField] [Min(0f)] private float ammoWeight = 0.12f;
        [SerializeField] [Min(0f)] private float bombWeight = 0.10f;
        [SerializeField] [Min(0f)] private float keyWeight = 0.12f;
        [SerializeField] private Transform spawnedPickupParent;

        private bool _hasDroppedThisLife;

        public void Configure(float nextDropChance, float nextCoinWeight, float nextAmmoWeight, float nextBombWeight, float nextKeyWeight, Transform pickupParent)
        {
            dropChance = Mathf.Clamp01(nextDropChance);
            coinWeight = Mathf.Max(0f, nextCoinWeight);
            ammoWeight = Mathf.Max(0f, nextAmmoWeight);
            bombWeight = Mathf.Max(0f, nextBombWeight);
            keyWeight = Mathf.Max(0f, nextKeyWeight);
            spawnedPickupParent = pickupParent;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            _hasDroppedThisLife = false;
            ResolveReferences();

            if (enemyHealth != null)
            {
                enemyHealth.DiedWithSource -= HandleEnemyDied;
                enemyHealth.DiedWithSource += HandleEnemyDied;
            }
        }

        private void OnDisable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.DiedWithSource -= HandleEnemyDied;
            }
        }

        private void HandleEnemyDied(EnemyHealth deadEnemy)
        {
            if (_hasDroppedThisLife || deadEnemy == null || deadEnemy != enemyHealth)
            {
                return;
            }

            _hasDroppedThisLife = true;

            if (dropChance <= 0f || Random.value > dropChance)
            {
                return;
            }

            EnemyDropKind dropKind = EnemyDropRules.RollDropKind(coinWeight, ammoWeight, bombWeight, keyWeight, 0f);
            Vector3 dropPosition = enemyHealth.transform.position;

            switch (dropKind)
            {
                case EnemyDropKind.Coins:
                    SpawnCoinDrops(dropPosition);
                    break;
                case EnemyDropKind.Ammo:
                    RuntimePickupFactory.SpawnDefaultEnemyDropPickup(
                        EnemyDropKind.Ammo,
                        dropPosition,
                        spawnedPickupParent,
                        "EnemyAmmoDrop");
                    break;
                case EnemyDropKind.Bomb:
                    RuntimePickupFactory.SpawnDefaultEnemyDropPickup(
                        EnemyDropKind.Bomb,
                        dropPosition,
                        spawnedPickupParent,
                        "EnemyBombDrop");
                    break;
                case EnemyDropKind.Key:
                    RuntimePickupFactory.SpawnDefaultEnemyDropPickup(
                        EnemyDropKind.Key,
                        dropPosition,
                        spawnedPickupParent,
                        "EnemyKeyDrop");
                    break;
            }
        }

        private void SpawnCoinDrops(Vector3 center)
        {
            int coinCount = EnemyDropRules.RollCoinDropCount(
                EnemyDropRules.SingleCoinChance,
                EnemyDropRules.DoubleCoinChance,
                EnemyDropRules.TripleCoinChance);

            for (int i = 0; i < coinCount; i++)
            {
                Vector3 position = center + ResolveCoinOffset(i, coinCount);
                RuntimePickupFactory.SpawnDefaultResourcePickup(
                    ResourcePickupType.Coin,
                    position,
                    spawnedPickupParent,
                    "EnemyCoinDrop");
            }
        }

        private static Vector3 ResolveCoinOffset(int index, int totalCount)
        {
            if (totalCount <= 1)
            {
                return Vector3.zero;
            }

            float angle = (90f + (index * (360f / totalCount))) * Mathf.Deg2Rad;
            const float radius = 0.24f;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        private void ResolveReferences()
        {
            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }
        }
    }
}
