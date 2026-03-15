using CuteIssac.Common.Combat;
using CuteIssac.Enemy;
using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct EnemyKilledSignal
    {
        public EnemyKilledSignal(EnemyHealth enemyHealth, in DamageInfo finalDamageInfo)
        {
            EnemyHealth = enemyHealth;
            FinalDamageInfo = finalDamageInfo;
        }

        public EnemyHealth EnemyHealth { get; }
        public DamageInfo FinalDamageInfo { get; }
        public string EnemyId => EnemyHealth != null ? EnemyHealth.EnemyId : string.Empty;
        public Transform Killer => FinalDamageInfo.Source;
        public Vector3 Position => EnemyHealth != null ? EnemyHealth.transform.position : Vector3.zero;
    }
}
