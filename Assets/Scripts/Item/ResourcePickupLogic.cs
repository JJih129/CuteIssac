using CuteIssac.Core.Pooling;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Grants a simple inventory resource when collected.
    /// Coins, keys, and bombs all share this path so their prefabs stay presentation-driven.
    /// </summary>
    public sealed class ResourcePickupLogic : BasePickupLogic
    {
        [Header("Resource Reward")]
        [SerializeField] private ResourcePickupType resourceType;
        [SerializeField] [Min(1)] private int amount = 1;

        public ResourcePickupType ResourceType => resourceType;
        public int Amount => amount;

        public void Configure(ResourcePickupType nextResourceType, int nextAmount)
        {
            resourceType = nextResourceType;
            amount = Mathf.Max(1, nextAmount);
        }

        protected override bool TryCollect(PlayerInventory inventory, PlayerHealth health, PlayerItemManager itemManager)
        {
            if (inventory == null || amount <= 0)
            {
                return false;
            }

            switch (resourceType)
            {
                case ResourcePickupType.Coin:
                    inventory.AddCoins(amount);
                    return true;
                case ResourcePickupType.Key:
                    inventory.AddKeys(amount);
                    return true;
                case ResourcePickupType.Bomb:
                    inventory.AddBombs(amount);
                    return true;
                default:
                    return false;
            }
        }

        protected override string BuildPickupFeedbackLabel()
        {
            string resourceLabel = resourceType switch
            {
                ResourcePickupType.Coin => "COIN",
                ResourcePickupType.Key => "KEY",
                ResourcePickupType.Bomb => "BOMB",
                _ => "RESOURCE"
            };

            return amount > 1
                ? $"+{amount} {resourceLabel}"
                : $"+1 {resourceLabel}";
        }

        protected override Color ResolvePickupFeedbackColor()
        {
            return resourceType switch
            {
                ResourcePickupType.Coin => new Color(0.95f, 0.82f, 0.25f, 1f),
                ResourcePickupType.Key => new Color(0.72f, 0.86f, 1f, 1f),
                ResourcePickupType.Bomb => new Color(1f, 0.56f, 0.24f, 1f),
                _ => base.ResolvePickupFeedbackColor()
            };
        }
    }

    public enum EnemyDropKind
    {
        None = 0,
        Coins = 1,
        Ammo = 2,
        Bomb = 3
    }

    public static class EnemyDropRules
    {
        public const float CoinDropChance = 0.50f;
        public const float AmmoDropChance = 0.05f;
        public const float BombDropChance = 0.05f;
        public const float NoDropChance = 0.40f;
        public const float SingleCoinChance = 0.50f;
        public const float DoubleCoinChance = 0.40f;
        public const float TripleCoinChance = 0.10f;

        public static EnemyDropKind RollDropKind(
            float coinWeight,
            float ammoWeight,
            float bombWeight,
            float noDropWeight)
        {
            float resolvedCoinWeight = Mathf.Max(0f, coinWeight);
            float resolvedAmmoWeight = Mathf.Max(0f, ammoWeight);
            float resolvedBombWeight = Mathf.Max(0f, bombWeight);
            float resolvedNoDropWeight = Mathf.Max(0f, noDropWeight);
            float totalWeight = resolvedCoinWeight + resolvedAmmoWeight + resolvedBombWeight + resolvedNoDropWeight;

            if (totalWeight <= 0.0001f)
            {
                return EnemyDropKind.None;
            }

            float roll = Random.value * totalWeight;
            if (roll < resolvedCoinWeight)
            {
                return EnemyDropKind.Coins;
            }

            roll -= resolvedCoinWeight;
            if (roll < resolvedAmmoWeight)
            {
                return EnemyDropKind.Ammo;
            }

            roll -= resolvedAmmoWeight;
            if (roll < resolvedBombWeight)
            {
                return EnemyDropKind.Bomb;
            }

            return EnemyDropKind.None;
        }

        public static int RollCoinDropCount(float singleWeight, float doubleWeight, float tripleWeight)
        {
            float resolvedSingleWeight = Mathf.Max(0f, singleWeight);
            float resolvedDoubleWeight = Mathf.Max(0f, doubleWeight);
            float resolvedTripleWeight = Mathf.Max(0f, tripleWeight);
            float totalWeight = resolvedSingleWeight + resolvedDoubleWeight + resolvedTripleWeight;

            if (totalWeight <= 0.0001f)
            {
                return 1;
            }

            float roll = Random.value * totalWeight;
            if (roll < resolvedSingleWeight)
            {
                return 1;
            }

            roll -= resolvedSingleWeight;
            if (roll < resolvedDoubleWeight)
            {
                return 2;
            }

            return 3;
        }
    }

    public static class RuntimePickupFactory
    {
        public const float DefaultPickupScale = 0.72f;
        public const float DefaultPickupColliderRadius = 0.42f;
        public const int DefaultPickupSortingOrder = 31;
        public static readonly Color DefaultAmmoPickupBaseColor = new(0.96f, 0.78f, 0.28f, 1f);
        public static readonly Color DefaultAmmoPickupCollectedColor = new(1f, 1f, 1f, 0.24f);
        public static readonly Color DefaultCoinPickupBaseColor = new(0.98f, 0.84f, 0.26f, 1f);
        public static readonly Color DefaultCoinPickupCollectedColor = new(1f, 0.96f, 0.74f, 0.24f);
        public static readonly Color DefaultBombPickupBaseColor = new(1f, 0.54f, 0.26f, 1f);
        public static readonly Color DefaultBombPickupCollectedColor = new(1f, 0.9f, 0.78f, 0.24f);

        private static Sprite _ammoClipSprite;
        private static Sprite _coinSprite;
        private static Sprite _bombSprite;
        private static Transform _templateRoot;
        private static GameObject _coinPickupTemplate;
        private static GameObject _bombPickupTemplate;
        private static GameObject _ammoPickupTemplate;

        public static void PrewarmDefaultPickups(int coinCount = 24, int bombCount = 4, int ammoCount = 4)
        {
            PrewarmTemplate(GetOrCreateCoinPickupTemplate(), coinCount);
            PrewarmTemplate(GetOrCreateBombPickupTemplate(), bombCount);
            PrewarmTemplate(GetOrCreateAmmoPickupTemplate(), ammoCount);
        }

        public static GameObject SpawnCoinPickup(
            Vector3 position,
            Transform parent,
            float pickupScale,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            string objectName = "CandyCoinPickup")
        {
            GameObject pickupObject = SpawnPickupObject(
                GetOrCreateCoinPickupTemplate(),
                position,
                parent,
                pickupScale,
                objectName);
            ConfigureResourcePickup(
                pickupObject,
                pickupColliderRadius,
                pickupSortingOrder,
                baseColor,
                collectedColor,
                ResourcePickupType.Coin,
                1,
                ResolveCoinSprite());
            return pickupObject;
        }

        public static GameObject SpawnBombPickup(
            Vector3 position,
            Transform parent,
            float pickupScale,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            string objectName = "BombPickup")
        {
            GameObject pickupObject = SpawnPickupObject(
                GetOrCreateBombPickupTemplate(),
                position,
                parent,
                pickupScale,
                objectName);
            ConfigureResourcePickup(
                pickupObject,
                pickupColliderRadius,
                pickupSortingOrder,
                baseColor,
                collectedColor,
                ResourcePickupType.Bomb,
                1,
                ResolveBombSprite());
            return pickupObject;
        }

        public static GameObject SpawnAmmoPickup(
            Vector3 position,
            Transform parent,
            float pickupScale,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            int amount = 1,
            bool restockEquipped = true,
            string objectName = "AmmoPickup")
        {
            GameObject pickupObject = SpawnPickupObject(
                GetOrCreateAmmoPickupTemplate(),
                position,
                parent,
                pickupScale,
                objectName);
            ConfigureAmmoPickup(
                pickupObject,
                pickupColliderRadius,
                pickupSortingOrder,
                baseColor,
                collectedColor,
                Mathf.Max(1, amount),
                restockEquipped,
                ResolveAmmoClipSprite());
            return pickupObject;
        }

        private static void ConfigureResourcePickup(
            GameObject pickupObject,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            ResourcePickupType resourceType,
            int amount,
            Sprite sprite)
        {
            if (pickupObject == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = pickupObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = pickupSortingOrder;
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = baseColor;
            }

            CircleCollider2D triggerCollider = pickupObject.GetComponent<CircleCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = Mathf.Max(0.1f, pickupColliderRadius);
            }

            if (pickupObject.TryGetComponent(out ResourcePickupLogic pickupLogic))
            {
                pickupLogic.Configure(resourceType, Mathf.Max(1, amount));
            }

            if (pickupObject.TryGetComponent(out PickupVisual pickupVisual))
            {
                pickupVisual.ApplyRuntimeVisual(sprite, baseColor, collectedColor);
            }
        }

        private static void ConfigureAmmoPickup(
            GameObject pickupObject,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            int amount,
            bool restockEquipped,
            Sprite sprite)
        {
            if (pickupObject == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = pickupObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = pickupSortingOrder;
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = baseColor;
            }

            CircleCollider2D triggerCollider = pickupObject.GetComponent<CircleCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = Mathf.Max(0.1f, pickupColliderRadius);
            }

            if (pickupObject.TryGetComponent(out AmmoPickupLogic pickupLogic))
            {
                pickupLogic.Configure(amount, restockEquipped);
            }

            if (pickupObject.TryGetComponent(out PickupVisual pickupVisual))
            {
                pickupVisual.ApplyRuntimeVisual(sprite, baseColor, collectedColor);
            }
        }

        private static GameObject SpawnPickupObject(GameObject template, Vector3 position, Transform parent, float pickupScale, string objectName)
        {
            GameObject pickupObject = PrefabPoolService.Spawn(template, position, Quaternion.identity, parent);
            if (pickupObject == null)
            {
                return null;
            }

            pickupObject.name = objectName;
            pickupObject.transform.localScale = Vector3.one * Mathf.Max(0.2f, pickupScale);
            return pickupObject;
        }

        private static GameObject GetOrCreateCoinPickupTemplate()
        {
            if (_coinPickupTemplate != null)
            {
                return _coinPickupTemplate;
            }

            _coinPickupTemplate = CreateResourcePickupTemplate(
                "RuntimeCandyCoinPickupTemplate",
                ResourcePickupType.Coin,
                ResolveCoinSprite(),
                DefaultCoinPickupBaseColor,
                DefaultCoinPickupCollectedColor);
            return _coinPickupTemplate;
        }

        private static GameObject GetOrCreateBombPickupTemplate()
        {
            if (_bombPickupTemplate != null)
            {
                return _bombPickupTemplate;
            }

            _bombPickupTemplate = CreateResourcePickupTemplate(
                "RuntimeBombPickupTemplate",
                ResourcePickupType.Bomb,
                ResolveBombSprite(),
                DefaultBombPickupBaseColor,
                DefaultBombPickupCollectedColor);
            return _bombPickupTemplate;
        }

        private static GameObject GetOrCreateAmmoPickupTemplate()
        {
            if (_ammoPickupTemplate != null)
            {
                return _ammoPickupTemplate;
            }

            GameObject template = CreateBasePickupTemplate(
                "RuntimeAmmoPickupTemplate",
                ResolveAmmoClipSprite(),
                DefaultAmmoPickupBaseColor,
                DefaultAmmoPickupCollectedColor);
            AmmoPickupLogic pickupLogic = template.AddComponent<AmmoPickupLogic>();
            pickupLogic.Configure(1, true);
            _ammoPickupTemplate = template;
            return _ammoPickupTemplate;
        }

        private static GameObject CreateResourcePickupTemplate(
            string templateName,
            ResourcePickupType resourceType,
            Sprite sprite,
            Color baseColor,
            Color collectedColor)
        {
            GameObject template = CreateBasePickupTemplate(templateName, sprite, baseColor, collectedColor);
            ResourcePickupLogic pickupLogic = template.AddComponent<ResourcePickupLogic>();
            pickupLogic.Configure(resourceType, 1);
            return template;
        }

        private static GameObject CreateBasePickupTemplate(string templateName, Sprite sprite, Color baseColor, Color collectedColor)
        {
            GameObject template = new(templateName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            template.SetActive(false);
            template.transform.SetParent(EnsureTemplateRoot(), false);
            template.transform.localScale = Vector3.one * DefaultPickupScale;

            SpriteRenderer spriteRenderer = template.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = DefaultPickupSortingOrder;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = baseColor;

            PickupVisual pickupVisual = template.AddComponent<PickupVisual>();
            CircleCollider2D triggerCollider = template.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = DefaultPickupColliderRadius;

            pickupVisual.ApplyRuntimeVisual(spriteRenderer.sprite, baseColor, collectedColor);
            template.AddComponent<PooledObject>();
            return template;
        }

        private static Transform EnsureTemplateRoot()
        {
            if (_templateRoot != null)
            {
                return _templateRoot;
            }

            GameObject rootObject = new("RuntimePickupTemplates")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Object.DontDestroyOnLoad(rootObject);
            _templateRoot = rootObject.transform;
            return _templateRoot;
        }

        private static void PrewarmTemplate(GameObject template, int count)
        {
            if (template == null || count <= 0)
            {
                return;
            }

            PrefabPoolService.Prewarm(template, count);
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

            _coinSprite = RuntimeShopIconFactory.GetCandyCoinSprite();
            return _coinSprite;
        }

        private static Sprite ResolveBombSprite()
        {
            if (_bombSprite != null)
            {
                return _bombSprite;
            }

            _bombSprite = RuntimeShopIconFactory.GetAppleBombSprite();
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

            Color32 transparent = new(0, 0, 0, 0);
            Color32 solid = new(255, 255, 255, 255);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                string row = pattern[height - 1 - y];
                for (int x = 0; x < width; x++)
                {
                    pixels[(y * width) + x] = row[x] == 'X' ? solid : transparent;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                12f);
            sprite.name = spriteName;
            return sprite;
        }

        private static void FillRect(Color32[] pixels, int width, int x, int y, int rectWidth, int rectHeight, Color32 color)
        {
            int maxY = Mathf.Min(y + rectHeight, pixels.Length / width);
            int maxX = Mathf.Min(x + rectWidth, width);

            for (int py = Mathf.Max(0, y); py < maxY; py++)
            {
                int rowOffset = py * width;
                for (int px = Mathf.Max(0, x); px < maxX; px++)
                {
                    pixels[rowOffset + px] = color;
                }
            }
        }
    }
}
