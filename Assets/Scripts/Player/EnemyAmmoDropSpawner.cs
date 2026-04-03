using CuteIssac.Core.Gameplay;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class EnemyAmmoDropSpawner : MonoBehaviour
    {
        private enum EnemyDropKind
        {
            None = 0,
            Coins = 1,
            Ammo = 2,
            Bomb = 3
        }

        [Header("References")]
        [SerializeField] private PlayerWeaponLoadout weaponLoadout;

        [Header("Drop Rules")]
        [SerializeField] [Range(0f, 1f)] private float coinDropChance = 0.50f;
        [SerializeField] [Range(0f, 1f)] private float ammoDropChance = 0.05f;
        [SerializeField] [Range(0f, 1f)] private float bombDropChance = 0.05f;
        [SerializeField] [Range(0f, 1f)] private float noDropChance = 0.40f;
        [SerializeField] [Range(0f, 1f)] private float singleCoinChance = 0.50f;
        [SerializeField] [Range(0f, 1f)] private float doubleCoinChance = 0.40f;
        [SerializeField] [Range(0f, 1f)] private float tripleCoinChance = 0.10f;
        [SerializeField] [Min(0f)] private float spawnScatterRadius = 0.28f;

        [Header("Pickup Presentation")]
        [SerializeField] [Min(0.1f)] private float pickupColliderRadius = 0.42f;
        [SerializeField] [Min(0.2f)] private float pickupScale = 0.72f;
        [SerializeField] private int pickupSortingOrder = 31;
        [SerializeField] private Color ammoPickupBaseColor = new(0.96f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color ammoPickupCollectedColor = new(1f, 1f, 1f, 0.24f);
        [SerializeField] private Color coinPickupBaseColor = new(0.98f, 0.84f, 0.26f, 1f);
        [SerializeField] private Color coinPickupCollectedColor = new(1f, 0.96f, 0.74f, 0.24f);
        [SerializeField] private Color bombPickupBaseColor = new(1f, 0.54f, 0.26f, 1f);
        [SerializeField] private Color bombPickupCollectedColor = new(1f, 0.9f, 0.78f, 0.24f);

        private static Sprite _ammoClipSprite;
        private static Sprite _coinSprite;
        private static Sprite _bombSprite;

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
                    if (weaponLoadout != null && weaponLoadout.CanRestockEquippedWeaponFromPickup())
                    {
                        SpawnAmmoPickup(ResolveSpawnPosition(signal.Position));
                    }
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
            float coinWeight = Mathf.Max(0f, coinDropChance);
            float ammoWeight = Mathf.Max(0f, ammoDropChance);
            float bombWeight = Mathf.Max(0f, bombDropChance);
            float noDropWeight = Mathf.Max(0f, noDropChance);
            float totalWeight = coinWeight + ammoWeight + bombWeight + noDropWeight;

            if (totalWeight <= 0.0001f)
            {
                return EnemyDropKind.None;
            }

            float roll = Random.value * totalWeight;
            if (roll < coinWeight)
            {
                return EnemyDropKind.Coins;
            }

            roll -= coinWeight;
            if (roll < ammoWeight)
            {
                return EnemyDropKind.Ammo;
            }

            roll -= ammoWeight;
            if (roll < bombWeight)
            {
                return EnemyDropKind.Bomb;
            }

            return EnemyDropKind.None;
        }

        private int ResolveCoinDropCount()
        {
            float singleWeight = Mathf.Max(0f, singleCoinChance);
            float doubleWeight = Mathf.Max(0f, doubleCoinChance);
            float tripleWeight = Mathf.Max(0f, tripleCoinChance);
            float totalWeight = singleWeight + doubleWeight + tripleWeight;

            if (totalWeight <= 0.0001f)
            {
                return 1;
            }

            float roll = Random.value * totalWeight;
            if (roll < singleWeight)
            {
                return 1;
            }

            roll -= singleWeight;
            if (roll < doubleWeight)
            {
                return 2;
            }

            return 3;
        }

        private void SpawnCoinDrops(Vector3 origin, int count)
        {
            int coinCount = Mathf.Max(1, count);
            for (int index = 0; index < coinCount; index++)
            {
                SpawnResourcePickup(
                    ResolveSpawnPosition(origin),
                    ResourcePickupType.Coin,
                    1,
                    ResolveCoinSprite(),
                    coinPickupBaseColor,
                    coinPickupCollectedColor,
                    "CandyCoinPickup");
            }
        }

        private void SpawnBombPickup(Vector3 position)
        {
            SpawnResourcePickup(
                position,
                ResourcePickupType.Bomb,
                1,
                ResolveBombSprite(),
                bombPickupBaseColor,
                bombPickupCollectedColor,
                "BombPickup");
        }

        private void SpawnAmmoPickup(Vector3 position)
        {
            GameObject pickupObject = new("AmmoPickup");
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = Vector3.one * pickupScale;

            SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = pickupSortingOrder;
            spriteRenderer.sprite = ResolveAmmoClipSprite();
            spriteRenderer.color = ammoPickupBaseColor;

            PickupVisual pickupVisual = pickupObject.AddComponent<PickupVisual>();
            CircleCollider2D triggerCollider = pickupObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = Mathf.Max(0.1f, pickupColliderRadius);

            AmmoPickupLogic pickupLogic = pickupObject.AddComponent<AmmoPickupLogic>();
            pickupLogic.Configure(1, true);

            pickupVisual.ApplyRuntimeVisual(spriteRenderer.sprite, ammoPickupBaseColor, ammoPickupCollectedColor);
        }

        private void SpawnResourcePickup(
            Vector3 position,
            ResourcePickupType resourceType,
            int amount,
            Sprite sprite,
            Color baseColor,
            Color collectedColor,
            string objectName)
        {
            GameObject pickupObject = new(objectName);
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = Vector3.one * pickupScale;

            SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = pickupSortingOrder;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = baseColor;

            PickupVisual pickupVisual = pickupObject.AddComponent<PickupVisual>();
            CircleCollider2D triggerCollider = pickupObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = Mathf.Max(0.1f, pickupColliderRadius);

            ResourcePickupLogic pickupLogic = pickupObject.AddComponent<ResourcePickupLogic>();
            pickupLogic.Configure(resourceType, amount);

            pickupVisual.ApplyRuntimeVisual(spriteRenderer.sprite, baseColor, collectedColor);
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

        private static Sprite ResolveAmmoClipSprite()
        {
            if (_ammoClipSprite != null)
            {
                return _ammoClipSprite;
            }

            const int width = 16;
            const int height = 22;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeAmmoClipPickup"
            };

            Color32[] pixels = new Color32[width * height];
            Color32 solid = new(255, 255, 255, 255);

            FillRect(pixels, width, 5, 2, 6, 15, solid);
            FillRect(pixels, width, 4, 16, 8, 2, solid);
            FillRect(pixels, width, 6, 18, 4, 2, solid);
            FillRect(pixels, width, 5, 0, 6, 2, solid);

            texture.SetPixels32(pixels);
            texture.Apply();

            _ammoClipSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                16f);
            _ammoClipSprite.name = "RuntimeAmmoClipPickup";
            return _ammoClipSprite;
        }

        private static Sprite ResolveCoinSprite()
        {
            if (_coinSprite != null)
            {
                return _coinSprite;
            }

            _coinSprite = CreateSpriteFromPattern(
                "RuntimeCoinPickup",
                new[]
                {
                    "....XXXX....",
                    "..XXXXXXXX..",
                    ".XXXXXXXXXX.",
                    ".XXX....XXX.",
                    "XXX......XXX",
                    "XXX......XXX",
                    "XXX......XXX",
                    "XXX......XXX",
                    ".XXX....XXX.",
                    ".XXXXXXXXXX.",
                    "..XXXXXXXX..",
                    "....XXXX...."
                });
            return _coinSprite;
        }

        private static Sprite ResolveBombSprite()
        {
            if (_bombSprite != null)
            {
                return _bombSprite;
            }

            _bombSprite = CreateSpriteFromPattern(
                "RuntimeBombPickup",
                new[]
                {
                    ".....XX.....",
                    "....XXXX....",
                    "....XX......",
                    "...XXXX.....",
                    "..XXXXXX....",
                    ".XXXXXXXX...",
                    ".XXXXXXXX...",
                    ".XXXXXXXX...",
                    "..XXXXXX....",
                    "..XXXXXX....",
                    "...XXXX.....",
                    "....XX......"
                });
            return _bombSprite;
        }

        private static Sprite CreateSpriteFromPattern(string spriteName, string[] pattern)
        {
            int height = pattern.Length;
            int width = height > 0 ? pattern[0].Length : 0;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = spriteName
            };

            Color32[] pixels = new Color32[width * height];
            Color32 solid = new(255, 255, 255, 255);

            for (int y = 0; y < height; y++)
            {
                string row = pattern[height - 1 - y];
                for (int x = 0; x < width; x++)
                {
                    if (row[x] != '.')
                    {
                        pixels[(y * width) + x] = solid;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                16f);
            sprite.name = spriteName;
            return sprite;
        }

        private static void FillRect(Color32[] pixels, int textureWidth, int x, int y, int width, int height, Color32 color)
        {
            int minX = Mathf.Max(0, x);
            int minY = Mathf.Max(0, y);
            int maxX = Mathf.Min(textureWidth, x + width);
            int maxY = Mathf.Min((pixels.Length / textureWidth), y + height);

            for (int pixelY = minY; pixelY < maxY; pixelY++)
            {
                int rowOffset = pixelY * textureWidth;
                for (int pixelX = minX; pixelX < maxX; pixelX++)
                {
                    pixels[rowOffset + pixelX] = color;
                }
            }
        }
    }
}
