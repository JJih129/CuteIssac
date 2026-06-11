using System.Collections;
using System.Collections.Generic;
using System.Text;
using CuteIssac.Combat;
using CuteIssac.Common.Input;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Data.Combat;
using CuteIssac.Data.Item;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponLoadout : MonoBehaviour
    {
        private sealed class WeaponRuntimeSlot
        {
            public WeaponRuntimeSlot(
                ItemData sourceItem,
                string displayName,
                string hudLabel,
                string motifLabel,
                Sprite icon,
                PlayerAttackDefinition attackDefinition,
                int magazineCapacity,
                int ammoInMagazine,
                int reserveAmmo,
                int reserveAmmoCapacity,
                bool infiniteMagazine,
                bool infiniteReserveAmmo,
                float reloadDuration,
                float dryFireCooldown,
                int shotsPerTrigger,
                float spreadDegrees,
                float knockbackMultiplier,
                ProjectileTraitState projectileTraits,
                bool isStarter,
                ItemRarity rarity)
            {
                SourceItem = sourceItem;
                DisplayName = displayName ?? string.Empty;
                HudLabel = hudLabel ?? string.Empty;
                MotifLabel = motifLabel ?? string.Empty;
                Icon = icon;
                AttackDefinition = attackDefinition;
                MagazineCapacity = Mathf.Max(1, magazineCapacity);
                AmmoInMagazine = Mathf.Clamp(ammoInMagazine, 0, Mathf.Max(1, magazineCapacity));
                ReserveAmmo = Mathf.Max(0, reserveAmmo);
                ReserveAmmoCapacity = Mathf.Max(0, reserveAmmoCapacity);
                InfiniteMagazine = infiniteMagazine;
                InfiniteReserveAmmo = infiniteReserveAmmo;
                ReloadDuration = Mathf.Max(0.05f, reloadDuration);
                DryFireCooldown = Mathf.Max(0.05f, dryFireCooldown);
                ShotsPerTrigger = Mathf.Max(1, shotsPerTrigger);
                SpreadDegrees = Mathf.Clamp(spreadDegrees, 0f, 45f);
                KnockbackMultiplier = Mathf.Max(0.1f, knockbackMultiplier);
                ProjectileTraits = projectileTraits;
                IsStarter = isStarter;
                Rarity = rarity;
            }

            public ItemData SourceItem { get; }
            public string DisplayName { get; }
            public string HudLabel { get; }
            public string MotifLabel { get; }
            public Sprite Icon { get; }
            public PlayerAttackDefinition AttackDefinition { get; }
            public int MagazineCapacity { get; }
            public int AmmoInMagazine { get; set; }
            public int ReserveAmmo { get; set; }
            public int ReserveAmmoCapacity { get; }
            public bool InfiniteMagazine { get; }
            public bool InfiniteReserveAmmo { get; }
            public float ReloadDuration { get; }
            public float DryFireCooldown { get; }
            public int ShotsPerTrigger { get; }
            public float SpreadDegrees { get; }
            public float KnockbackMultiplier { get; }
            public ProjectileTraitState ProjectileTraits { get; }
            public bool IsStarter { get; }
            public ItemRarity Rarity { get; }

            public bool CanReload => !InfiniteMagazine
                && AmmoInMagazine < MagazineCapacity
                && (InfiniteReserveAmmo || ReserveAmmo > 0);

            public void ReloadMagazine()
            {
                if (!CanReload)
                {
                    return;
                }

                int missingAmmo = Mathf.Max(0, MagazineCapacity - AmmoInMagazine);

                if (InfiniteReserveAmmo)
                {
                    AmmoInMagazine += missingAmmo;
                    return;
                }

                int ammoToTransfer = Mathf.Min(missingAmmo, ReserveAmmo);
                AmmoInMagazine += ammoToTransfer;
                ReserveAmmo -= ammoToTransfer;
            }

            public void RestockToFull()
            {
                AmmoInMagazine = MagazineCapacity;

                if (!InfiniteReserveAmmo)
                {
                    ReserveAmmo = ReserveAmmoCapacity;
                }
            }

            public string ResolveReserveAmmoLabel()
            {
                if (InfiniteMagazine)
                {
                    return "INF";
                }

                return InfiniteReserveAmmo
                    ? "INF"
                    : ReserveAmmo.ToString();
            }
        }

        [Header("References")]
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private Transform dropOrigin;

        [Header("Loadout")]
        [SerializeField] [Min(1)] private int maxWeaponSlots = 5;

        [Header("Starter Weapon")]
        [SerializeField] private string starterWeaponName = "Basement Sidearm";
        [SerializeField] private string starterWeaponHudLabel = "Sidearm";
        [SerializeField] private string starterWeaponMotif = "Compact striker-fired pistol relic";
        [SerializeField] [Min(1)] private int starterMagazineCapacity = 12;
        [SerializeField] private bool starterInfiniteMagazine = true;
        [SerializeField] [Min(0)] private int starterReserveAmmoCapacity = 96;
        [SerializeField] [Min(0)] private int starterStartingReserveAmmo = 96;
        [SerializeField] private bool starterInfiniteReserveAmmo = false;
        [SerializeField] [Min(0.05f)] private float starterReloadDuration = 0.95f;
        [SerializeField] [Min(0.05f)] private float starterDryFireCooldown = 0.18f;
        [SerializeField] [Min(1)] private int starterShotsPerTrigger = 1;
        [SerializeField] [Range(0f, 45f)] private float starterSpreadDegrees = 9f;
        [SerializeField] [Min(0.1f)] private float starterKnockbackMultiplier = 1f;

        [Header("Dropped Pickup")]
        [SerializeField] [Min(0.2f)] private float dropForwardDistance = 1.15f;
        [SerializeField] [Min(0f)] private float dropScatterRadius = 0.22f;
        [SerializeField] [Min(0.05f)] private float droppedPickupReactivationDelay = 0.35f;
        [SerializeField] [Min(0.1f)] private float droppedPickupColliderRadius = 0.44f;
        [SerializeField] [Range(0.5f, 1.5f)] private float droppedPickupScale = 0.9f;
        [SerializeField] private int droppedPickupSortingOrder = 32;

        [Header("Weapon Carousel")]
        [SerializeField] [Range(0.05f, 1f)] private float weaponCarouselTimeScale = 0.18f;

        public event System.Action WeaponStateChanged;

        private readonly List<WeaponRuntimeSlot> _weaponSlots = new();
        private readonly HashSet<string> _ownedWeaponItemIds = new(System.StringComparer.Ordinal);
        private int _equippedIndex;
        private float _reloadRemaining;
        private float _reloadDuration;
        private float _dryFireRemaining;
        private bool _isWeaponCarouselOpen;
        private int _weaponCarouselPreviewIndex = -1;
        private float _cachedTimeScale = 1f;
        private float _cachedFixedDeltaTime = 0.02f;

        public bool HasWeapon => CurrentSlot != null;
        public bool IsReloading => _reloadRemaining > 0.0001f;
        public string CurrentWeaponVisualKey
        {
            get
            {
                InitializeStarterWeapon();
                WeaponRuntimeSlot currentSlot = CurrentSlot;
                if (currentSlot == null)
                {
                    return "starter_sidearm";
                }

                if (currentSlot.SourceItem == null || string.IsNullOrWhiteSpace(currentSlot.SourceItem.ItemId))
                {
                    return "starter_sidearm";
                }

                return currentSlot.SourceItem.ItemId;
            }
        }
        public PlayerAttackDefinition CurrentAttackDefinition => CurrentSlot != null && CurrentSlot.AttackDefinition != null
            ? CurrentSlot.AttackDefinition
            : playerCombat != null
                ? playerCombat.StartingAttackDefinition
                : null;
        public float CurrentKnockbackMultiplier => CurrentSlot != null
            ? CurrentSlot.KnockbackMultiplier
            : starterKnockbackMultiplier;
        public ProjectileTraitState CurrentProjectileTraits => CurrentSlot != null
            ? CurrentSlot.ProjectileTraits
            : ProjectileTraitState.Default;
        public GameAudioEventType CurrentFireAudioEventType => ResolveFireAudioEventType(CurrentSlot);

        private WeaponRuntimeSlot CurrentSlot => _equippedIndex >= 0 && _equippedIndex < _weaponSlots.Count
            ? _weaponSlots[_equippedIndex]
            : null;

        private void Awake()
        {
            ResolveReferences();
            InitializeStarterWeapon();
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
            CloseWeaponCarousel(commitSelection: false, restoreTimeScale: true);
        }

        private void OnDestroy()
        {
            PlayerRegistry.Unregister(this);
        }

        public void ProcessInput(PlayerGameplayInputState inputState, float deltaTime)
        {
            ResolveReferences();
            InitializeStarterWeapon();

            bool stateChanged = TickTimers(deltaTime);

            if (_isWeaponCarouselOpen && Time.timeScale <= 0.001f)
            {
                stateChanged |= CloseWeaponCarousel(commitSelection: false, restoreTimeScale: false);
            }

            if (inputState.WeaponCarouselHeld && CanOpenWeaponCarousel())
            {
                stateChanged |= OpenWeaponCarousel();
                stateChanged |= ProcessWeaponCarouselInput(inputState);
            }
            else if (_isWeaponCarouselOpen)
            {
                stateChanged |= CloseWeaponCarousel(commitSelection: true, restoreTimeScale: true);
            }

            if (!_isWeaponCarouselOpen && inputState.CycleWeaponPressed)
            {
                stateChanged |= TryCycleWeaponInternal();
            }

            if (!_isWeaponCarouselOpen && inputState.DropWeaponPressed)
            {
                stateChanged |= TryDropEquippedWeapon();
            }

            if (!_isWeaponCarouselOpen && inputState.ReloadPressed)
            {
                stateChanged |= TryBeginReload();
            }

            if (stateChanged)
            {
                NotifyStateChanged();
            }
        }

        private bool ProcessWeaponCarouselInput(PlayerGameplayInputState inputState)
        {
            bool changed = false;

            if (inputState.WeaponCarouselSelectionDelta != 0)
            {
                changed |= RotateWeaponCarousel(inputState.WeaponCarouselSelectionDelta);
            }

            if (inputState.WeaponCarouselDropPressed)
            {
                changed |= TryDropWeaponCarouselSelection();
            }

            return changed;
        }

        private bool CanOpenWeaponCarousel()
        {
            if (Time.timeScale <= 0.001f)
            {
                return false;
            }

            if (_weaponSlots.Count > 1)
            {
                return true;
            }

            WeaponRuntimeSlot currentSlot = CurrentSlot;
            return currentSlot != null && !currentSlot.IsStarter && currentSlot.SourceItem != null;
        }

        private bool OpenWeaponCarousel()
        {
            if (_isWeaponCarouselOpen)
            {
                return false;
            }

            _isWeaponCarouselOpen = true;
            _weaponCarouselPreviewIndex = Mathf.Clamp(_equippedIndex, 0, _weaponSlots.Count - 1);
            _cachedTimeScale = Time.timeScale > 0.001f ? Time.timeScale : 1f;
            _cachedFixedDeltaTime = Time.fixedDeltaTime > 0.0001f ? Time.fixedDeltaTime : 0.02f;
            ApplyWeaponCarouselTimeScale(Mathf.Clamp(weaponCarouselTimeScale, 0.05f, 1f));
            return true;
        }

        private bool CloseWeaponCarousel(bool commitSelection, bool restoreTimeScale)
        {
            if (!_isWeaponCarouselOpen)
            {
                return false;
            }

            if (commitSelection
                && _weaponCarouselPreviewIndex >= 0
                && _weaponCarouselPreviewIndex < _weaponSlots.Count
                && _weaponCarouselPreviewIndex != _equippedIndex)
            {
                EquipIndex(_weaponCarouselPreviewIndex);
            }

            _isWeaponCarouselOpen = false;
            _weaponCarouselPreviewIndex = -1;

            if (restoreTimeScale)
            {
                RestoreWeaponCarouselTimeScale();
            }

            return true;
        }

        private bool RotateWeaponCarousel(int direction)
        {
            if (!_isWeaponCarouselOpen || _weaponSlots.Count <= 1)
            {
                return false;
            }

            int normalizedDirection = direction < 0 ? -1 : 1;
            int slotCount = _weaponSlots.Count;
            int previewIndex = Mathf.Clamp(_weaponCarouselPreviewIndex, 0, slotCount - 1);
            _weaponCarouselPreviewIndex = (previewIndex + normalizedDirection + slotCount) % slotCount;
            return true;
        }

        private bool TryDropWeaponCarouselSelection()
        {
            if (!_isWeaponCarouselOpen)
            {
                return false;
            }

            WeaponRuntimeSlot selectedSlot = ResolveWeaponCarouselPreviewSlot();
            if (selectedSlot == null || selectedSlot.IsStarter || selectedSlot.SourceItem == null)
            {
                return false;
            }

            int removedIndex = Mathf.Clamp(_weaponCarouselPreviewIndex, 0, _weaponSlots.Count - 1);
            _weaponSlots.RemoveAt(removedIndex);
            UnregisterWeaponSlot(selectedSlot);

            if (_weaponSlots.Count == 0)
            {
                _equippedIndex = 0;
                _weaponCarouselPreviewIndex = -1;
                return false;
            }

            if (removedIndex < _equippedIndex)
            {
                _equippedIndex = Mathf.Max(0, _equippedIndex - 1);
            }
            else if (removedIndex == _equippedIndex)
            {
                _equippedIndex = Mathf.Clamp(_equippedIndex, 0, _weaponSlots.Count - 1);
                CancelReload();
                RefreshPlayerStats();
            }

            _weaponCarouselPreviewIndex = Mathf.Clamp(removedIndex, 0, _weaponSlots.Count - 1);
            SpawnDroppedWeaponPickup(selectedSlot);
            RaiseLoadoutDelta(
                "WEAPON DROPPED",
                $"{selectedSlot.DisplayName} - carousel discard",
                ResolveAccentColor(selectedSlot.Rarity),
                false);
            return true;
        }

        private WeaponRuntimeSlot ResolveWeaponCarouselPreviewSlot()
        {
            if (!_isWeaponCarouselOpen || _weaponSlots.Count == 0)
            {
                return CurrentSlot;
            }

            int previewIndex = Mathf.Clamp(_weaponCarouselPreviewIndex, 0, _weaponSlots.Count - 1);
            return _weaponSlots[previewIndex];
        }

        private void ApplyWeaponCarouselTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = _cachedFixedDeltaTime * timeScale;
        }

        private void RestoreWeaponCarouselTimeScale()
        {
            Time.timeScale = Mathf.Max(0.01f, _cachedTimeScale);
            Time.fixedDeltaTime = Mathf.Max(0.0001f, _cachedFixedDeltaTime);
        }

        public int ResolveShotCount(int baseShotCount)
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            int triggerShotCount = currentSlot != null ? currentSlot.ShotsPerTrigger : starterShotsPerTrigger;
            return Mathf.Max(1, baseShotCount) * Mathf.Max(1, triggerShotCount);
        }

        public float ResolveSpreadDegrees(float fallbackSpreadDegrees)
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            return currentSlot != null
                ? Mathf.Max(0f, currentSlot.SpreadDegrees)
                : Mathf.Max(0f, fallbackSpreadDegrees);
        }

        public bool TryConsumeShot()
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = CurrentSlot;

            if (currentSlot == null || IsReloading || _dryFireRemaining > 0f)
            {
                return false;
            }

            if (currentSlot.InfiniteMagazine)
            {
                return true;
            }

            if (currentSlot.AmmoInMagazine > 0)
            {
                currentSlot.AmmoInMagazine = Mathf.Max(0, currentSlot.AmmoInMagazine - 1);

                if (currentSlot.AmmoInMagazine == 0)
                {
                    TryBeginReload();
                }

                NotifyStateChanged();
                return true;
            }

            if (TryBeginReload())
            {
                return false;
            }

            TriggerDryFire(currentSlot);
            return false;
        }

        public bool CanReceiveAmmoPickup()
        {
            InitializeStarterWeapon();

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                if (CanAcceptAmmoPickup(_weaponSlots[index]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanRestockEquippedWeaponFromPickup()
        {
            InitializeStarterWeapon();
            return CanRestockWeaponFromPickup(CurrentSlot);
        }

        public bool TryRestockEquippedWeaponFromPickup()
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            if (!CanRestockWeaponFromPickup(currentSlot))
            {
                return false;
            }

            currentSlot.RestockToFull();
            CancelReload();
            _dryFireRemaining = 0f;
            NotifyStateChanged();
            return true;
        }

        public bool TryAddAmmo(int amount)
        {
            InitializeStarterWeapon();
            int ammoToApply = Mathf.Max(0, amount);
            if (ammoToApply <= 0)
            {
                return false;
            }

            WeaponRuntimeSlot targetSlot = ResolveAmmoPickupTargetSlot();
            if (targetSlot == null)
            {
                return false;
            }

            bool ammoAdded = TryAddAmmoToSlot(targetSlot, ref ammoToApply);
            if (!ammoAdded)
            {
                return false;
            }

            NotifyStateChanged();
            return true;
        }

        public bool TryAcquireWeapon(ItemData itemData)
        {
            if (itemData == null || !itemData.IsWeaponRelic)
            {
                return false;
            }

            ResolveReferences();
            InitializeStarterWeapon();

            if (TryFindWeaponIndex(itemData, out int existingIndex))
            {
                WeaponRuntimeSlot existingSlot = _weaponSlots[existingIndex];
                existingSlot.RestockToFull();
                EquipIndex(existingIndex);
                RefreshPlayerStats();
                RaiseLoadoutDelta(
                    "AMMO STOCKED",
                    $"{existingSlot.DisplayName} · {existingSlot.MagazineCapacity}/{existingSlot.ResolveReserveAmmoLabel()}",
                    ResolveAccentColor(existingSlot.Rarity),
                    false);
                NotifyStateChanged();
                return true;
            }

            WeaponRuntimeSlot newSlot = CreateRuntimeSlot(itemData);
            if (newSlot == null)
            {
                return false;
            }

            WeaponRuntimeSlot replacedSlot = null;
            if (_weaponSlots.Count >= Mathf.Max(1, maxWeaponSlots))
            {
                int replacementIndex = ResolveReplacementIndex();
                if (replacementIndex < 0)
                {
                    return false;
                }

                replacedSlot = _weaponSlots[replacementIndex];
                UnregisterWeaponSlot(replacedSlot);
                _weaponSlots[replacementIndex] = newSlot;
                RegisterWeaponSlot(newSlot);
                EquipIndex(replacementIndex);
            }
            else
            {
                _weaponSlots.Add(newSlot);
                RegisterWeaponSlot(newSlot);
                EquipIndex(_weaponSlots.Count - 1);
            }

            if (replacedSlot != null && replacedSlot.SourceItem != null)
            {
                SpawnDroppedWeaponPickup(replacedSlot);
            }

            string pickupDetail = $"{newSlot.DisplayName} · {newSlot.MagazineCapacity}/{newSlot.ResolveReserveAmmoLabel()}";
            RefreshPlayerStats();

            if (!string.IsNullOrWhiteSpace(newSlot.MotifLabel))
            {
                pickupDetail = $"{pickupDetail}\n{newSlot.MotifLabel}";
            }

            if (replacedSlot != null)
            {
                pickupDetail = $"{pickupDetail}\nDropped {replacedSlot.DisplayName}";
            }

            RaiseLoadoutDelta(
                replacedSlot != null ? "LOADOUT SWAP" : "WEAPON RELIC",
                pickupDetail,
                ResolveAccentColor(newSlot.Rarity),
                newSlot.Rarity >= ItemRarity.Rare);
            NotifyStateChanged();
            return true;
        }

        public bool ApplyStartingWeapon(ItemData itemData)
        {
            ResolveReferences();
            ClearWeaponSlots();

            if (itemData == null)
            {
                InitializeStarterWeapon();
                NotifyStateChanged();
                return CurrentSlot != null;
            }

            WeaponRuntimeSlot startingSlot = CreateRuntimeSlot(itemData, true);
            if (startingSlot == null)
            {
                InitializeStarterWeapon();
                NotifyStateChanged();
                return false;
            }

            _weaponSlots.Add(startingSlot);
            RegisterWeaponSlot(startingSlot);
            _equippedIndex = 0;
            CancelReload();
            _dryFireRemaining = 0f;
            RefreshPlayerStats();
            NotifyStateChanged();
            return true;
        }

        public bool OwnsWeaponItem(ItemData itemData)
        {
            if (itemData == null)
            {
                return false;
            }

            if (!itemData.IsWeaponRelic)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(itemData.ItemId))
            {
                return _ownedWeaponItemIds.Contains(itemData.ItemId);
            }

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                ItemData sourceItem = _weaponSlots[index].SourceItem;
                if (sourceItem == null)
                {
                    continue;
                }

                if (ReferenceEquals(sourceItem, itemData) || sourceItem.ItemId == itemData.ItemId)
                {
                    return true;
                }
            }

            return false;
        }

        public void AppendOwnedWeaponItems(List<ItemData> destination)
        {
            if (destination == null)
            {
                return;
            }

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                ItemData sourceItem = _weaponSlots[index].SourceItem;
                if (sourceItem == null || destination.Contains(sourceItem))
                {
                    continue;
                }

                destination.Add(sourceItem);
            }
        }

        public void AppendOwnedWeaponItems(List<ItemData> destination, ISet<string> itemIds)
        {
            if (destination == null)
            {
                return;
            }

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                ItemData sourceItem = _weaponSlots[index].SourceItem;
                if (sourceItem == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(sourceItem.ItemId))
                {
                    if (itemIds != null && !itemIds.Add(sourceItem.ItemId))
                    {
                        continue;
                    }
                }
                else if (destination.Contains(sourceItem))
                {
                    continue;
                }

                destination.Add(sourceItem);
            }
        }

        public PlayerWeaponHudState BuildHudState()
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = _isWeaponCarouselOpen
                ? ResolveWeaponCarouselPreviewSlot()
                : CurrentSlot;
            if (currentSlot == null)
            {
                return default;
            }

            string statusLabel = _isWeaponCarouselOpen
                ? "TIME DILATED"
                : IsReloading
                    ? "RELOADING"
                    : currentSlot.AmmoInMagazine > 0
                        ? "HOT"
                        : "EMPTY";
            string ammoLabel = currentSlot.InfiniteMagazine
                ? "INF"
                : $"{currentSlot.AmmoInMagazine}/{currentSlot.ResolveReserveAmmoLabel()}";
            string detailLabel = _isWeaponCarouselOpen
                ? BuildWeaponCarouselDetailLabel(currentSlot)
                : !string.IsNullOrWhiteSpace(currentSlot.MotifLabel)
                    ? currentSlot.MotifLabel
                    : "Modern firearm relic";
            string loadoutLabel = _isWeaponCarouselOpen
                ? BuildWeaponCarouselLoadoutLabel()
                : BuildLoadoutLabel();
            float reloadNormalized = _reloadDuration > 0.0001f
                ? 1f - Mathf.Clamp01(_reloadRemaining / _reloadDuration)
                : 0f;

            return new PlayerWeaponHudState(
                true,
                currentSlot.DisplayName,
                statusLabel,
                ammoLabel,
                detailLabel,
                loadoutLabel,
                currentSlot.Icon,
                ResolveAccentColor(currentSlot.Rarity),
                !_isWeaponCarouselOpen && IsReloading,
                _isWeaponCarouselOpen ? 0f : reloadNormalized,
                _isWeaponCarouselOpen);
        }

        private bool TickTimers(float deltaTime)
        {
            bool changed = false;
            float clampedDeltaTime = Mathf.Max(0f, deltaTime);

            if (_dryFireRemaining > 0f)
            {
                float nextDryFire = Mathf.Max(0f, _dryFireRemaining - clampedDeltaTime);
                if (!Mathf.Approximately(nextDryFire, _dryFireRemaining))
                {
                    _dryFireRemaining = nextDryFire;
                    changed = true;
                }
            }

            if (_reloadRemaining > 0f)
            {
                float nextReloadRemaining = Mathf.Max(0f, _reloadRemaining - clampedDeltaTime);
                if (!Mathf.Approximately(nextReloadRemaining, _reloadRemaining))
                {
                    _reloadRemaining = nextReloadRemaining;
                    changed = true;
                }

                if (_reloadRemaining <= 0f)
                {
                    CompleteReload();
                    changed = true;
                }
            }

            return changed;
        }

        private bool TryCycleWeaponInternal()
        {
            if (_weaponSlots.Count <= 1)
            {
                return false;
            }

            int nextIndex = (_equippedIndex + 1) % _weaponSlots.Count;
            return EquipIndex(nextIndex);
        }

        private bool TryDropEquippedWeapon()
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            if (currentSlot == null || currentSlot.IsStarter || currentSlot.SourceItem == null)
            {
                return false;
            }

            _weaponSlots.RemoveAt(_equippedIndex);
            UnregisterWeaponSlot(currentSlot);

            if (_weaponSlots.Count == 0)
            {
                _equippedIndex = 0;
            }
            else
            {
                _equippedIndex = Mathf.Clamp(_equippedIndex, 0, _weaponSlots.Count - 1);
            }

            CancelReload();
            SpawnDroppedWeaponPickup(currentSlot);
            RefreshPlayerStats();
            RaiseLoadoutDelta(
                "WEAPON DROPPED",
                $"{currentSlot.DisplayName} · floor pickup live",
                ResolveAccentColor(currentSlot.Rarity),
                false);
            return true;
        }

        private bool TryBeginReload()
        {
            InitializeStarterWeapon();
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            if (currentSlot == null || IsReloading || !currentSlot.CanReload)
            {
                return false;
            }

            _reloadDuration = currentSlot.ReloadDuration;
            _reloadRemaining = currentSlot.ReloadDuration;
            _dryFireRemaining = 0f;
            GameAudioEvents.Raise(GameAudioEventType.WeaponReloadStarted, transform.position);
            return true;
        }

        private void CompleteReload()
        {
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            _reloadRemaining = 0f;
            _reloadDuration = 0f;

            if (currentSlot == null)
            {
                return;
            }

            currentSlot.ReloadMagazine();
        }

        private void TriggerDryFire(WeaponRuntimeSlot currentSlot)
        {
            _dryFireRemaining = currentSlot != null
                ? currentSlot.DryFireCooldown
                : Mathf.Max(0.05f, starterDryFireCooldown);
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + Vector3.up * 0.9f,
                "CLICK",
                new Color(1f, 0.64f, 0.42f, 1f),
                0.42f,
                0.42f,
                0.92f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
            GameAudioEvents.Raise(GameAudioEventType.WeaponDryFired, transform.position, false);
        }

        private WeaponRuntimeSlot CreateRuntimeSlot(ItemData itemData)
        {
            return CreateRuntimeSlot(itemData, false);
        }

        private WeaponRuntimeSlot CreateRuntimeSlot(ItemData itemData, bool isStarter)
        {
            if (itemData == null || !itemData.IsWeaponRelic || itemData.WeaponProfile == null)
            {
                return null;
            }

            ItemWeaponProfile profile = itemData.WeaponProfile;
            if (!profile.IsValid)
            {
                return null;
            }

            return new WeaponRuntimeSlot(
                itemData,
                itemData.DisplayName,
                itemData.WeaponHudLabel,
                itemData.WeaponMotif,
                itemData.Icon,
                profile.AttackDefinition,
                profile.MagazineCapacity,
                profile.MagazineCapacity,
                profile.StartingReserveAmmo,
                profile.ReserveAmmoCapacity,
                false,
                profile.InfiniteReserveAmmo,
                profile.ReloadDuration,
                profile.DryFireCooldown,
                profile.ShotsPerTrigger,
                profile.SpreadDegrees,
                profile.KnockbackMultiplier,
                BuildWeaponProjectileTraits(profile),
                isStarter,
                itemData.Rarity);
        }

        private void ClearWeaponSlots()
        {
            if (_isWeaponCarouselOpen)
            {
                RestoreWeaponCarouselTimeScale();
            }

            _weaponSlots.Clear();
            _ownedWeaponItemIds.Clear();
            _equippedIndex = 0;
            _weaponCarouselPreviewIndex = -1;
            _isWeaponCarouselOpen = false;
            CancelReload();
            _dryFireRemaining = 0f;
        }

        private static ProjectileTraitState BuildWeaponProjectileTraits(ItemWeaponProfile profile)
        {
            ProjectileTraitState traits = ProjectileTraitState.Default;
            IReadOnlyList<ProjectileModifier> projectileModifiers = profile?.ProjectileModifiers;

            if (projectileModifiers == null)
            {
                return traits;
            }

            for (int index = 0; index < projectileModifiers.Count; index++)
            {
                ProjectileTraitResolver.Apply(projectileModifiers[index], ref traits);
            }

            return traits;
        }

        private bool TryFindWeaponIndex(ItemData itemData, out int index)
        {
            index = -1;

            if (itemData == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(itemData.ItemId) && !_ownedWeaponItemIds.Contains(itemData.ItemId))
            {
                return false;
            }

            for (int slotIndex = 0; slotIndex < _weaponSlots.Count; slotIndex++)
            {
                ItemData sourceItem = _weaponSlots[slotIndex].SourceItem;
                if (sourceItem == null)
                {
                    continue;
                }

                if (ReferenceEquals(sourceItem, itemData) || sourceItem.ItemId == itemData.ItemId)
                {
                    index = slotIndex;
                    return true;
                }
            }

            return false;
        }

        private void RegisterWeaponSlot(WeaponRuntimeSlot slot)
        {
            ItemData sourceItem = slot != null ? slot.SourceItem : null;
            if (sourceItem == null || string.IsNullOrWhiteSpace(sourceItem.ItemId))
            {
                return;
            }

            _ownedWeaponItemIds.Add(sourceItem.ItemId);
        }

        private void UnregisterWeaponSlot(WeaponRuntimeSlot slot)
        {
            ItemData sourceItem = slot != null ? slot.SourceItem : null;
            if (sourceItem == null || string.IsNullOrWhiteSpace(sourceItem.ItemId))
            {
                return;
            }

            _ownedWeaponItemIds.Remove(sourceItem.ItemId);
        }

        private int ResolveReplacementIndex()
        {
            if (_weaponSlots.Count == 0)
            {
                return -1;
            }

            if (_equippedIndex >= 0
                && _equippedIndex < _weaponSlots.Count
                && !_weaponSlots[_equippedIndex].IsStarter)
            {
                return _equippedIndex;
            }

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                if (!_weaponSlots[index].IsStarter)
                {
                    return index;
                }
            }

            return -1;
        }

        private bool EquipIndex(int nextIndex)
        {
            if (nextIndex < 0 || nextIndex >= _weaponSlots.Count || nextIndex == _equippedIndex)
            {
                return false;
            }

            _equippedIndex = nextIndex;
            CancelReload();
            RefreshPlayerStats();
            return true;
        }

        private void CancelReload()
        {
            _reloadRemaining = 0f;
            _reloadDuration = 0f;
        }

        private void SpawnDroppedWeaponPickup(WeaponRuntimeSlot slot)
        {
            if (slot == null || slot.SourceItem == null)
            {
                return;
            }

            Vector3 spawnPosition = ResolveDropPosition();
            GameObject pickupObject = new($"{slot.DisplayName}DroppedPickup");
            pickupObject.transform.position = spawnPosition;
            pickupObject.transform.localScale = Vector3.one * droppedPickupScale;

            SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = droppedPickupSortingOrder;
            spriteRenderer.color = Color.white;

            pickupObject.AddComponent<PickupVisual>();
            CircleCollider2D triggerCollider = pickupObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = Mathf.Max(0.1f, droppedPickupColliderRadius);
            ItemPickupLogic pickupLogic = pickupObject.AddComponent<ItemPickupLogic>();
            pickupLogic.ConfigureItem(slot.SourceItem);
            triggerCollider.enabled = false;
            StartCoroutine(EnableDroppedPickupAfterDelay(triggerCollider));
        }

        private IEnumerator EnableDroppedPickupAfterDelay(Collider2D triggerCollider)
        {
            yield return new WaitForSeconds(Mathf.Max(0.05f, droppedPickupReactivationDelay));

            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }
        }

        private Vector3 ResolveDropPosition()
        {
            Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position;
            Vector2 forward = playerCombat != null && playerCombat.LastAttackDirection.sqrMagnitude > 0.001f
                ? playerCombat.LastAttackDirection.normalized
                : Vector2.right;
            Vector2 side = new Vector2(-forward.y, forward.x) * Random.Range(-dropScatterRadius, dropScatterRadius);
            Vector2 offset = forward * Mathf.Max(0.2f, dropForwardDistance) + side;
            return origin + new Vector3(offset.x, offset.y, 0f);
        }

        private void InitializeStarterWeapon()
        {
            if (_weaponSlots.Count > 0)
            {
                return;
            }

            ResolveReferences();
            PlayerAttackDefinition startingAttackDefinition = playerCombat != null
                ? playerCombat.StartingAttackDefinition
                : null;
            if (startingAttackDefinition == null || !startingAttackDefinition.IsValid)
            {
                return;
            }

            _weaponSlots.Add(new WeaponRuntimeSlot(
                null,
                starterWeaponName,
                starterWeaponHudLabel,
                starterWeaponMotif,
                null,
                startingAttackDefinition,
                starterMagazineCapacity,
                starterMagazineCapacity,
                Mathf.Max(0, starterStartingReserveAmmo),
                Mathf.Max(0, starterReserveAmmoCapacity),
                starterInfiniteMagazine,
                starterInfiniteMagazine || starterInfiniteReserveAmmo,
                starterReloadDuration,
                starterDryFireCooldown,
                starterShotsPerTrigger,
                starterSpreadDegrees,
                starterKnockbackMultiplier,
                ProjectileTraitState.Default,
                true,
                ItemRarity.Common));
            _equippedIndex = 0;
            RefreshPlayerStats();
        }

        private void ResolveReferences()
        {
            if (playerCombat == null)
            {
                playerCombat = GetComponent<PlayerCombat>();
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }
        }

        private string BuildLoadoutLabel()
        {
            StringBuilder builder = new();
            builder.Append("LOADOUT ").Append(_weaponSlots.Count).Append('/').Append(Mathf.Max(1, maxWeaponSlots)).Append(" · ");

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(" | ");
                }

                if (index == _equippedIndex)
                {
                    builder.Append('>');
                }

                builder.Append(string.IsNullOrWhiteSpace(_weaponSlots[index].HudLabel)
                    ? _weaponSlots[index].DisplayName
                    : _weaponSlots[index].HudLabel);
            }

            return builder.ToString();
        }

        private string BuildWeaponCarouselDetailLabel(WeaponRuntimeSlot previewSlot)
        {
            if (previewSlot == null)
            {
                return "Hold Tab to inspect the current arsenal.";
            }

            StringBuilder builder = new();

            if (!string.IsNullOrWhiteSpace(previewSlot.MotifLabel))
            {
                builder.Append(previewSlot.MotifLabel).Append('\n');
            }

            builder.Append("< > rotate  -  release Tab equip");

            if (!previewSlot.IsStarter && previewSlot.SourceItem != null)
            {
                builder.Append('\n').Append("Down drops highlighted weapon");
            }

            return builder.ToString();
        }

        private string BuildWeaponCarouselLoadoutLabel()
        {
            StringBuilder builder = new();
            builder.Append("CAROUSEL ").Append(_weaponSlots.Count).Append('/').Append(Mathf.Max(1, maxWeaponSlots)).Append(" - ");

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append("   ");
                }

                WeaponRuntimeSlot slot = _weaponSlots[index];
                string label = string.IsNullOrWhiteSpace(slot.HudLabel)
                    ? slot.DisplayName
                    : slot.HudLabel;

                if (index == _weaponCarouselPreviewIndex)
                {
                    builder.Append('[').Append(label).Append(']');
                }
                else if (index == _equippedIndex)
                {
                    builder.Append('{').Append(label).Append('}');
                }
                else
                {
                    builder.Append(label);
                }
            }

            return builder.ToString();
        }

        private void RefreshPlayerStats()
        {
            playerStats?.RefreshCurrentStats();
        }

        private WeaponRuntimeSlot ResolveAmmoPickupTargetSlot()
        {
            WeaponRuntimeSlot currentSlot = CurrentSlot;
            if (CanAcceptAmmoPickup(currentSlot))
            {
                return currentSlot;
            }

            for (int index = 0; index < _weaponSlots.Count; index++)
            {
                WeaponRuntimeSlot slot = _weaponSlots[index];
                if (slot == currentSlot || !CanAcceptAmmoPickup(slot))
                {
                    continue;
                }

                return slot;
            }

            return null;
        }

        private static bool CanAcceptAmmoPickup(WeaponRuntimeSlot slot)
        {
            if (slot == null)
            {
                return false;
            }

            if (slot.InfiniteMagazine && slot.InfiniteReserveAmmo)
            {
                return false;
            }

            if (slot.AmmoInMagazine < slot.MagazineCapacity)
            {
                return true;
            }

            return !slot.InfiniteReserveAmmo && slot.ReserveAmmo < slot.ReserveAmmoCapacity;
        }

        private static bool CanRestockWeaponFromPickup(WeaponRuntimeSlot slot)
        {
            if (slot == null || slot.InfiniteMagazine)
            {
                return false;
            }

            if (slot.AmmoInMagazine < slot.MagazineCapacity)
            {
                return true;
            }

            return !slot.InfiniteReserveAmmo && slot.ReserveAmmo < slot.ReserveAmmoCapacity;
        }

        private static GameAudioEventType ResolveFireAudioEventType(WeaponRuntimeSlot slot)
        {
            if (slot == null || slot.IsStarter || slot.SourceItem == null)
            {
                return GameAudioEventType.PistolFired;
            }

            string itemId = slot.SourceItem.ItemId ?? string.Empty;
            string displayName = slot.DisplayName ?? string.Empty;
            string motif = slot.MotifLabel ?? string.Empty;
            string key = $"{itemId} {displayName} {motif}".ToLowerInvariant();

            if (key.Contains("gatebreach") || key.Contains("shotgun") || key.Contains("590"))
            {
                return GameAudioEventType.ShotgunFired;
            }

            if (key.Contains("longwatch") || key.Contains("sniper") || key.Contains("700"))
            {
                return GameAudioEventType.SniperFired;
            }

            if (key.Contains("vector") || key.Contains("smg"))
            {
                return GameAudioEventType.SmgFired;
            }

            if (key.Contains("patrol") || key.Contains("rifle") || key.Contains("4a1"))
            {
                return GameAudioEventType.AssaultRifleFired;
            }

            if (key.Contains("minigun") || key.Contains("mini gun"))
            {
                return GameAudioEventType.MinigunFired;
            }

            if (key.Contains("bazooka") || key.Contains("rocket") || key.Contains("launcher"))
            {
                return GameAudioEventType.RocketLauncherFired;
            }

            return GameAudioEventType.PistolFired;
        }

        private static bool TryAddAmmoToSlot(WeaponRuntimeSlot slot, ref int remainingAmmo)
        {
            if (slot == null || remainingAmmo <= 0 || !CanAcceptAmmoPickup(slot))
            {
                return false;
            }

            int initialMagazineAmmo = slot.AmmoInMagazine;
            int initialReserveAmmo = slot.ReserveAmmo;

            if (!slot.InfiniteMagazine && slot.AmmoInMagazine < slot.MagazineCapacity)
            {
                int magazineAmmoToAdd = Mathf.Min(slot.MagazineCapacity - slot.AmmoInMagazine, remainingAmmo);
                slot.AmmoInMagazine += magazineAmmoToAdd;
                remainingAmmo -= magazineAmmoToAdd;
            }

            if (remainingAmmo > 0 && !slot.InfiniteReserveAmmo && slot.ReserveAmmo < slot.ReserveAmmoCapacity)
            {
                int reserveAmmoToAdd = Mathf.Min(slot.ReserveAmmoCapacity - slot.ReserveAmmo, remainingAmmo);
                slot.ReserveAmmo += reserveAmmoToAdd;
                remainingAmmo -= reserveAmmoToAdd;
            }

            return slot.AmmoInMagazine != initialMagazineAmmo || slot.ReserveAmmo != initialReserveAmmo;
        }

        private void NotifyStateChanged()
        {
            WeaponStateChanged?.Invoke();
        }

        private void RaiseLoadoutDelta(string headline, string detail, Color accentColor, bool emphasize)
        {
            GameplayRuntimeEvents.RaisePlayerLoadoutDelta(new PlayerLoadoutDeltaSignal(
                transform.position + Vector3.up * 0.8f,
                headline,
                detail,
                accentColor,
                emphasize));
        }

        private static Color ResolveAccentColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Uncommon => new Color(0.52f, 1f, 0.68f, 1f),
                ItemRarity.Rare => new Color(1f, 0.84f, 0.42f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.48f, 0.82f, 1f),
                ItemRarity.Relic => new Color(1f, 0.92f, 0.6f, 1f),
                ItemRarity.Boss => new Color(1f, 0.42f, 0.42f, 1f),
                _ => new Color(0.72f, 0.85f, 1f, 1f)
            };
        }
    }
}
