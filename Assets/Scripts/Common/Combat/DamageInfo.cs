using UnityEngine;

namespace CuteIssac.Common.Combat
{
    /// <summary>
    /// Lightweight hit payload shared between projectiles and any damageable target.
    /// Keep this data generic so future weapons or hazards can reuse the same contract.
    /// </summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, Vector2 hitDirection, Transform source)
        {
            Amount = amount;
            HitDirection = hitDirection;
            Source = source;
        }

        public float Amount { get; }
        public Vector2 HitDirection { get; }
        public Transform Source { get; }
    }
}
