using UnityEngine;

namespace CuteIssac.Data.Combat
{
    /// <summary>
    /// Player attack tuning data. Keeps fire rate and projectile choice outside the combat component.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerAttackDefinition", menuName = "CuteIssac/Data/Combat/Player Attack Definition")]
    public sealed class PlayerAttackDefinition : ScriptableObject
    {
        [SerializeField] private ProjectileDefinition projectileDefinition;
        [SerializeField] [Min(0.01f)] private float fireInterval = 0.3f;
        [SerializeField] private Vector2 muzzleOffset = new(0.65f, 0f);

        public ProjectileDefinition ProjectileDefinition => projectileDefinition;
        public float FireInterval => fireInterval;
        public Vector2 MuzzleOffset => muzzleOffset;
        public bool IsValid => projectileDefinition != null && projectileDefinition.IsValid;
    }
}
