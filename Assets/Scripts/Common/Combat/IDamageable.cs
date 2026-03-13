namespace CuteIssac.Common.Combat
{
    /// <summary>
    /// Common damage contract shared by enemies, players, and destructible props.
    /// Combat code should only depend on this interface, not on concrete health components.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(in DamageInfo damageInfo);
    }
}
