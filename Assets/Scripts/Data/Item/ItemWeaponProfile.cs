using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Combat;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [System.Serializable]
    public sealed class ItemWeaponProfile
    {
        [SerializeField] private string firearmMotif = string.Empty;
        [SerializeField] private string hudLabel = string.Empty;
        [SerializeField] private PlayerAttackDefinition attackDefinition;
        [SerializeField] [Min(1)] private int magazineCapacity = 12;
        [SerializeField] [Min(0)] private int reserveAmmoCapacity = 72;
        [SerializeField] [Min(0)] private int startingReserveAmmo = 72;
        [SerializeField] private bool infiniteReserveAmmo;
        [SerializeField] [Min(0.05f)] private float reloadDuration = 1.15f;
        [SerializeField] [Min(0.05f)] private float dryFireCooldown = 0.18f;
        [SerializeField] [Min(1)] private int shotsPerTrigger = 1;
        [SerializeField] [Range(0f, 45f)] private float spreadDegrees = 9f;
        [SerializeField] [Min(0.1f)] private float knockbackMultiplier = 1f;
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();

        public string FirearmMotif => firearmMotif;
        public string HudLabel => hudLabel;
        public PlayerAttackDefinition AttackDefinition => attackDefinition;
        public int MagazineCapacity => Mathf.Max(1, magazineCapacity);
        public int ReserveAmmoCapacity => Mathf.Max(0, reserveAmmoCapacity);
        public int StartingReserveAmmo => infiniteReserveAmmo
            ? Mathf.Max(0, reserveAmmoCapacity)
            : Mathf.Clamp(startingReserveAmmo, 0, Mathf.Max(0, reserveAmmoCapacity));
        public bool InfiniteReserveAmmo => infiniteReserveAmmo;
        public float ReloadDuration => Mathf.Max(0.05f, reloadDuration);
        public float DryFireCooldown => Mathf.Max(0.05f, dryFireCooldown);
        public int ShotsPerTrigger => Mathf.Max(1, shotsPerTrigger);
        public float SpreadDegrees => Mathf.Clamp(spreadDegrees, 0f, 45f);
        public float KnockbackMultiplier => Mathf.Max(0.1f, knockbackMultiplier);
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => projectileModifiers;
        public bool IsValid => attackDefinition != null
            && attackDefinition.IsValid
            && MagazineCapacity > 0
            && (InfiniteReserveAmmo || ReserveAmmoCapacity > 0);
    }
}
