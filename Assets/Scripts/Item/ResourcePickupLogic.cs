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
        private const int MaxCachedFeedbackAmount = 32;
        private static readonly string[] s_coinFeedbackLabels = BuildFeedbackLabelCache("COIN");
        private static readonly string[] s_keyFeedbackLabels = BuildFeedbackLabelCache("KEY");
        private static readonly string[] s_bombFeedbackLabels = BuildFeedbackLabelCache("BOMB");

        [Header("Resource Reward")]
        [SerializeField] private ResourcePickupType resourceType;
        [SerializeField] [Min(1)] private int amount = 1;

        public ResourcePickupType ResourceType => resourceType;
        public int Amount => amount;

        public void Configure(ResourcePickupType nextResourceType, int nextAmount)
        {
            resourceType = nextResourceType;
            amount = Mathf.Max(1, nextAmount);
            InvalidatePickupFeedbackCache();
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
            string[] labelCache = resourceType switch
            {
                ResourcePickupType.Coin => s_coinFeedbackLabels,
                ResourcePickupType.Key => s_keyFeedbackLabels,
                ResourcePickupType.Bomb => s_bombFeedbackLabels,
                _ => null
            };

            if (labelCache != null && amount > 0 && amount <= MaxCachedFeedbackAmount)
            {
                return labelCache[amount];
            }

            string resourceName = resourceType switch
            {
                ResourcePickupType.Coin => "COIN",
                ResourcePickupType.Key => "KEY",
                ResourcePickupType.Bomb => "BOMB",
                _ => "RESOURCE"
            };

            return string.Concat("+", Mathf.Max(1, amount).ToString(), " ", resourceName);
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

        private static string[] BuildFeedbackLabelCache(string resourceName)
        {
            string[] labels = new string[MaxCachedFeedbackAmount + 1];

            for (int index = 1; index < labels.Length; index++)
            {
                labels[index] = string.Concat("+", index.ToString(), " ", resourceName);
            }

            return labels;
        }
    }

    public enum EnemyDropKind
    {
        None = 0,
        Coins = 1,
        Ammo = 2,
        Bomb = 3,
        Key = 4
    }

    public static class EnemyDropRules
    {
        public const float CoinDropChance = 0.50f;
        public const float AmmoDropChance = 0.05f;
        public const float BombDropChance = 0.05f;
        public const float KeyDropChance = 0.03f;
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
            return RollDropKind(coinWeight, ammoWeight, bombWeight, 0f, noDropWeight);
        }

        public static EnemyDropKind RollDropKind(
            float coinWeight,
            float ammoWeight,
            float bombWeight,
            float keyWeight,
            float noDropWeight)
        {
            float resolvedCoinWeight = Mathf.Max(0f, coinWeight);
            float resolvedAmmoWeight = Mathf.Max(0f, ammoWeight);
            float resolvedBombWeight = Mathf.Max(0f, bombWeight);
            float resolvedKeyWeight = Mathf.Max(0f, keyWeight);
            float resolvedNoDropWeight = Mathf.Max(0f, noDropWeight);
            float totalWeight = resolvedCoinWeight + resolvedAmmoWeight + resolvedBombWeight + resolvedKeyWeight + resolvedNoDropWeight;

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

            roll -= resolvedBombWeight;
            if (roll < resolvedKeyWeight)
            {
                return EnemyDropKind.Key;
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
        public static readonly Color DefaultKeyPickupBaseColor = new(0.68f, 0.86f, 1f, 1f);
        public static readonly Color DefaultKeyPickupCollectedColor = new(0.9f, 0.98f, 1f, 0.24f);

        private static Sprite _ammoClipSprite;
        private static Sprite _coinSprite;
        private static Sprite _bombSprite;
        private static Sprite _keySprite;
        private static Transform _templateRoot;
        private static GameObject _coinPickupTemplate;
        private static GameObject _bombPickupTemplate;
        private static GameObject _keyPickupTemplate;
        private static GameObject _ammoPickupTemplate;

        public static void PrewarmDefaultPickups(int coinCount = 24, int bombCount = 4, int ammoCount = 4, int keyCount = 4)
        {
            PrewarmTemplate(GetOrCreateCoinPickupTemplate(), coinCount);
            PrewarmTemplate(GetOrCreateBombPickupTemplate(), bombCount);
            PrewarmTemplate(GetOrCreateAmmoPickupTemplate(), ammoCount);
            PrewarmTemplate(GetOrCreateKeyPickupTemplate(), keyCount);
        }

        public static GameObject SpawnDefaultResourcePickup(
            ResourcePickupType resourceType,
            Vector3 position,
            Transform parent,
            string objectName)
        {
            return SpawnResourcePickup(
                resourceType,
                position,
                parent,
                DefaultPickupScale,
                DefaultPickupColliderRadius,
                DefaultPickupSortingOrder,
                ResolveDefaultResourceBaseColor(resourceType),
                ResolveDefaultResourceCollectedColor(resourceType),
                objectName);
        }

        public static GameObject SpawnDefaultEnemyDropPickup(
            EnemyDropKind dropKind,
            Vector3 position,
            Transform parent,
            string objectName,
            bool restockEquippedAmmo = false)
        {
            return SpawnEnemyDropPickup(
                dropKind,
                position,
                parent,
                DefaultPickupScale,
                DefaultPickupColliderRadius,
                DefaultPickupSortingOrder,
                ResolveDefaultEnemyDropBaseColor(dropKind),
                ResolveDefaultEnemyDropCollectedColor(dropKind),
                objectName,
                restockEquippedAmmo);
        }

        public static GameObject SpawnResourcePickup(
            ResourcePickupType resourceType,
            Vector3 position,
            Transform parent,
            float pickupScale,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            string objectName)
        {
            return resourceType switch
            {
                ResourcePickupType.Coin => SpawnCoinPickup(
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    objectName),
                ResourcePickupType.Bomb => SpawnBombPickup(
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    objectName),
                ResourcePickupType.Key => SpawnKeyPickup(
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    objectName),
                _ => null
            };
        }

        public static GameObject SpawnEnemyDropPickup(
            EnemyDropKind dropKind,
            Vector3 position,
            Transform parent,
            float pickupScale,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            string objectName,
            bool restockEquippedAmmo = false)
        {
            return dropKind switch
            {
                EnemyDropKind.Coins => SpawnResourcePickup(
                    ResourcePickupType.Coin,
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    objectName),
                EnemyDropKind.Bomb => SpawnResourcePickup(
                    ResourcePickupType.Bomb,
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    objectName),
                EnemyDropKind.Key => SpawnResourcePickup(
                    ResourcePickupType.Key,
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    objectName),
                EnemyDropKind.Ammo => SpawnAmmoPickup(
                    position,
                    parent,
                    pickupScale,
                    pickupColliderRadius,
                    pickupSortingOrder,
                    baseColor,
                    collectedColor,
                    1,
                    restockEquippedAmmo,
                    objectName),
                _ => null
            };
        }

        private static Color ResolveDefaultResourceBaseColor(ResourcePickupType resourceType)
        {
            return resourceType switch
            {
                ResourcePickupType.Bomb => DefaultBombPickupBaseColor,
                ResourcePickupType.Key => DefaultKeyPickupBaseColor,
                _ => DefaultCoinPickupBaseColor
            };
        }

        private static Color ResolveDefaultResourceCollectedColor(ResourcePickupType resourceType)
        {
            return resourceType switch
            {
                ResourcePickupType.Bomb => DefaultBombPickupCollectedColor,
                ResourcePickupType.Key => DefaultKeyPickupCollectedColor,
                _ => DefaultCoinPickupCollectedColor
            };
        }

        private static Color ResolveDefaultEnemyDropBaseColor(EnemyDropKind dropKind)
        {
            return dropKind switch
            {
                EnemyDropKind.Ammo => DefaultAmmoPickupBaseColor,
                EnemyDropKind.Bomb => DefaultBombPickupBaseColor,
                EnemyDropKind.Key => DefaultKeyPickupBaseColor,
                _ => DefaultCoinPickupBaseColor
            };
        }

        private static Color ResolveDefaultEnemyDropCollectedColor(EnemyDropKind dropKind)
        {
            return dropKind switch
            {
                EnemyDropKind.Ammo => DefaultAmmoPickupCollectedColor,
                EnemyDropKind.Bomb => DefaultBombPickupCollectedColor,
                EnemyDropKind.Key => DefaultKeyPickupCollectedColor,
                _ => DefaultCoinPickupCollectedColor
            };
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

        public static GameObject SpawnKeyPickup(
            Vector3 position,
            Transform parent,
            float pickupScale,
            float pickupColliderRadius,
            int pickupSortingOrder,
            Color baseColor,
            Color collectedColor,
            string objectName = "KeyPickup")
        {
            GameObject pickupObject = SpawnPickupObject(
                GetOrCreateKeyPickupTemplate(),
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
                ResourcePickupType.Key,
                1,
                ResolveKeySprite());
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

            RuntimePickupComponentCache componentCache = ResolvePickupComponentCache(pickupObject);
            SpriteRenderer spriteRenderer = componentCache != null
                ? componentCache.SpriteRenderer
                : pickupObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = pickupSortingOrder;
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = baseColor;
            }

            CircleCollider2D triggerCollider = componentCache != null
                ? componentCache.TriggerCollider
                : pickupObject.GetComponent<CircleCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = Mathf.Max(0.1f, pickupColliderRadius);
            }

            ResourcePickupLogic pickupLogic = componentCache != null
                ? componentCache.ResourcePickupLogic
                : pickupObject.GetComponent<ResourcePickupLogic>();
            if (pickupLogic != null)
            {
                pickupLogic.Configure(resourceType, Mathf.Max(1, amount));
            }

            PickupVisual pickupVisual = componentCache != null
                ? componentCache.PickupVisual
                : pickupObject.GetComponent<PickupVisual>();
            if (pickupVisual != null)
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

            RuntimePickupComponentCache componentCache = ResolvePickupComponentCache(pickupObject);
            SpriteRenderer spriteRenderer = componentCache != null
                ? componentCache.SpriteRenderer
                : pickupObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = pickupSortingOrder;
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = baseColor;
            }

            CircleCollider2D triggerCollider = componentCache != null
                ? componentCache.TriggerCollider
                : pickupObject.GetComponent<CircleCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = Mathf.Max(0.1f, pickupColliderRadius);
            }

            AmmoPickupLogic pickupLogic = componentCache != null
                ? componentCache.AmmoPickupLogic
                : pickupObject.GetComponent<AmmoPickupLogic>();
            if (pickupLogic != null)
            {
                pickupLogic.Configure(amount, restockEquipped);
            }

            PickupVisual pickupVisual = componentCache != null
                ? componentCache.PickupVisual
                : pickupObject.GetComponent<PickupVisual>();
            if (pickupVisual != null)
            {
                pickupVisual.ApplyRuntimeVisual(sprite, baseColor, collectedColor);
            }
        }

        private static RuntimePickupComponentCache ResolvePickupComponentCache(GameObject pickupObject)
        {
            if (pickupObject != null && pickupObject.TryGetComponent(out RuntimePickupComponentCache componentCache))
            {
                componentCache.ResolveReferences();
                return componentCache;
            }

            return null;
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

        private static GameObject GetOrCreateKeyPickupTemplate()
        {
            if (_keyPickupTemplate != null)
            {
                return _keyPickupTemplate;
            }

            _keyPickupTemplate = CreateResourcePickupTemplate(
                "RuntimeKeyPickupTemplate",
                ResourcePickupType.Key,
                ResolveKeySprite(),
                DefaultKeyPickupBaseColor,
                DefaultKeyPickupCollectedColor);
            return _keyPickupTemplate;
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
            template.GetComponent<RuntimePickupComponentCache>()?.ResolveReferences();
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
            template.GetComponent<RuntimePickupComponentCache>()?.ResolveReferences();
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
            RuntimePickupComponentCache componentCache = template.AddComponent<RuntimePickupComponentCache>();
            componentCache.ResolveReferences();
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

            PrefabPoolService.EnsurePrewarmed(template, count);
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

        private static Sprite ResolveKeySprite()
        {
            if (_keySprite != null)
            {
                return _keySprite;
            }

            _keySprite = CreateSpriteFromPattern(
                "RuntimeKeyPickup",
                new[]
                {
                    "..XXXX....",
                    ".XX..XX...",
                    ".XX..XX...",
                    "..XXXX....",
                    "....XX....",
                    "....XX....",
                    "....XXXX..",
                    "....XX....",
                    "....XXXX..",
                    "....XX...."
                });
            return _keySprite;
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

    [DisallowMultipleComponent]
    public sealed class RuntimePickupComponentCache : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CircleCollider2D triggerCollider;
        [SerializeField] private PickupVisual pickupVisual;
        [SerializeField] private ResourcePickupLogic resourcePickupLogic;
        [SerializeField] private AmmoPickupLogic ammoPickupLogic;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public CircleCollider2D TriggerCollider => triggerCollider;
        public PickupVisual PickupVisual => pickupVisual;
        public ResourcePickupLogic ResourcePickupLogic => resourcePickupLogic;
        public AmmoPickupLogic AmmoPickupLogic => ammoPickupLogic;

        public void ResolveReferences()
        {
            if (spriteRenderer == null)
            {
                TryGetComponent(out spriteRenderer);
            }

            if (triggerCollider == null)
            {
                TryGetComponent(out triggerCollider);
            }

            if (pickupVisual == null)
            {
                TryGetComponent(out pickupVisual);
            }

            if (resourcePickupLogic == null)
            {
                TryGetComponent(out resourcePickupLogic);
            }

            if (ammoPickupLogic == null)
            {
                TryGetComponent(out ammoPickupLogic);
            }
        }
    }
}
