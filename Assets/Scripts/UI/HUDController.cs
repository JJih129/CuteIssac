using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.UI
{
    /// <summary>
    /// Connects player-facing data sources to HUD panel views.
    /// This class should stay thin: it subscribes to health and inventory changes, then forwards presentation data to replaceable views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Data Sources")]
        [Tooltip("Optional. Assign the player's health component here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Optional. Assign the player's inventory component here. If empty, the controller tries to find one in the scene.")]
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Panel Views")]
        [Tooltip("Replaceable health panel view. This owns heart slot visuals and value text.")]
        [SerializeField] private HealthPanelView healthPanelView;
        [Tooltip("Replaceable resource panel view for coins, keys, and bombs.")]
        [SerializeField] private ResourcePanelView resourcePanelView;
        [Tooltip("Replaceable active item slot view. Current prototype shows a reserved placeholder.")]
        [SerializeField] private ActiveItemPanelView activeItemPanelView;
        [Tooltip("Replaceable boss HP panel view. Hidden until a boss is explicitly shown.")]
        [SerializeField] private BossHpPanelView bossHpPanelView;
        [Tooltip("Replaceable minimap panel view. Current prototype only reserves the space.")]
        [SerializeField] private MinimapPanelView minimapPanelView;

        private bool _warnedMissingHealth;
        private bool _warnedMissingInventory;

        private void Awake()
        {
            ResolveReferences();
            RefreshAll();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshAll();
        }

        private void Start()
        {
            // Scene object Awake order is not guaranteed across roots.
            // Refresh once more after all Awake calls so initial HP/resources are correct even in test scenes.
            ResolveReferences();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Optional helper for tooling or tests that want to inject a prepared HUD view hierarchy at runtime.
        /// The normal game path should use inspector-assigned panel references on a scene or prefab HUD root.
        /// </summary>
        public void ConfigureViews(
            HealthPanelView healthView,
            ResourcePanelView resourceView,
            ActiveItemPanelView activeView,
            BossHpPanelView bossView,
            MinimapPanelView minimapView)
        {
            healthPanelView = healthView;
            resourcePanelView = resourceView;
            activeItemPanelView = activeView;
            bossHpPanelView = bossView;
            minimapPanelView = minimapView;
            RefreshAll();
        }

        public void RefreshViewState()
        {
            RefreshAll();
        }

        public void ShowBoss(string bossName, float normalizedHealth)
        {
            if (bossHpPanelView != null)
            {
                bossHpPanelView.ShowBoss(bossName, normalizedHealth);
            }
        }

        public void HideBoss()
        {
            if (bossHpPanelView != null)
            {
                bossHpPanelView.HideBoss();
            }
        }

        public void SetMinimapVisible(bool visible)
        {
            if (minimapPanelView != null)
            {
                minimapPanelView.SetVisible(visible);
            }
        }

        private void ResolveReferences()
        {
            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);

                if (playerHealth == null && !_warnedMissingHealth)
                {
                    Debug.LogWarning("HUDController could not find a PlayerHealth source. Assign one in the inspector for reliable scene setup.", this);
                    _warnedMissingHealth = true;
                }
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);

                if (playerInventory == null && !_warnedMissingInventory)
                {
                    Debug.LogWarning("HUDController could not find a PlayerInventory source. Assign one in the inspector for reliable scene setup.", this);
                    _warnedMissingInventory = true;
                }
            }
        }

        private void Subscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
                playerHealth.HealthChanged += HandleHealthChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.ResourcesChanged -= HandleResourcesChanged;
                playerInventory.ResourcesChanged += HandleResourcesChanged;
                playerInventory.InventoryChanged -= HandleInventoryChanged;
                playerInventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
            }

            if (playerInventory != null)
            {
                playerInventory.ResourcesChanged -= HandleResourcesChanged;
                playerInventory.InventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            if (healthPanelView != null)
            {
                healthPanelView.SetHealth(currentHealth, maxHealth);
            }
        }

        private void HandleResourcesChanged(PlayerResourceSnapshot resources)
        {
            if (resourcePanelView != null)
            {
                resourcePanelView.SetResources(resources);
            }
        }

        private void HandleInventoryChanged()
        {
            if (activeItemPanelView != null)
            {
                activeItemPanelView.ShowPlaceholder();
            }
        }

        private void RefreshAll()
        {
            if (healthPanelView != null && playerHealth != null)
            {
                healthPanelView.SetHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (resourcePanelView != null && playerInventory != null)
            {
                resourcePanelView.SetResources(playerInventory.Resources);
            }

            if (activeItemPanelView != null)
            {
                activeItemPanelView.ShowPlaceholder();
            }

            if (bossHpPanelView != null)
            {
                bossHpPanelView.HideBoss();
            }

            if (minimapPanelView != null)
            {
                minimapPanelView.ShowPlaceholder();
            }
        }
    }
}
