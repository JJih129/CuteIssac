using System;

namespace CuteIssac.Player
{
    /// <summary>
    /// Final stat values consumed by gameplay systems.
    /// Using an immutable snapshot makes it obvious when recalculation happens and avoids hidden stat drift.
    /// </summary>
    [Serializable]
    public readonly struct PlayerStatSnapshot
    {
        public PlayerStatSnapshot(float moveSpeed, float damage, float fireInterval)
        {
            MoveSpeed = moveSpeed;
            Damage = damage;
            FireInterval = fireInterval;
        }

        public float MoveSpeed { get; }
        public float Damage { get; }
        public float FireInterval { get; }
    }
}
