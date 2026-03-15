using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "OrbiterEnemyData", menuName = "CuteIssac/Data/Enemy/Orbiter Enemy Data")]
    public sealed class OrbiterEnemyData : EnemyData
    {
        [Header("Orbit")]
        [SerializeField] [Min(0f)] private float preferredOrbitRange = 2.4f;
        [SerializeField] [Min(0f)] private float retreatRange = 1.2f;
        [SerializeField] [Min(0f)] private float engageRange = 4.8f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.78f;
        [SerializeField] [Min(1f)] private float surgeSpeedMultiplier = 1.18f;
        [SerializeField] [Min(0.05f)] private float orbitSwapInterval = 1.85f;

        public float PreferredOrbitRange => preferredOrbitRange;
        public float RetreatRange => retreatRange;
        public float EngageRange => engageRange;
        public float OrbitBlend => orbitBlend;
        public float SurgeSpeedMultiplier => surgeSpeedMultiplier;
        public float OrbitSwapInterval => orbitSwapInterval;
    }
}
