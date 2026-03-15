namespace CuteIssac.Combat
{
    /// <summary>
    /// Lets shared projectile logic choose which category of target it may damage.
    /// </summary>
    public enum ProjectileDamageTarget
    {
        Any = 0,
        PlayerOnly = 1,
        EnemyOnly = 2
    }
}
