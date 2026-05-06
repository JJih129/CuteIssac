using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Optional presentation hook for ranged enemies that need animation-timed projectile release.
    /// </summary>
    public interface IEnemyRangedAttackPresentation
    {
        bool IsAttackPresentationActive { get; }
        bool TryPlayAttack(Vector2 aimDirection, EnemyCombat enemyCombat);
    }
}
