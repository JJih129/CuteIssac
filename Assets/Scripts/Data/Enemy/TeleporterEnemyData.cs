using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "TeleporterEnemyData", menuName = "CuteIssac/Data/Enemy/Teleporter Enemy Data")]
    public sealed class TeleporterEnemyData : EnemyData
    {
        [Header("Chase")]
        [SerializeField] [Min(0f)] private float orbitRange = 2f;
        [SerializeField] [Range(0f, 1f)] private float orbitBlend = 0.38f;
        [SerializeField] [Min(1f)] private float surgeSpeedMultiplier = 1.12f;

        [Header("Teleport")]
        [SerializeField] [Min(0.1f)] private float teleportInterval = 2.9f;
        [SerializeField] [Min(0f)] private float teleportTelegraphDuration = 0.4f;
        [SerializeField] [Min(0f)] private float postTeleportPause = 0.18f;
        [SerializeField] [Min(0.5f)] private float minTeleportDistance = 2.6f;
        [SerializeField] [Min(0.5f)] private float maxTeleportDistance = 4.6f;
        [SerializeField] [Min(0.05f)] private float teleportCollisionCheckRadius = 0.3f;
        [SerializeField] [Min(3)] private int teleportSampleCount = 8;
        [SerializeField] private Color teleportTelegraphColor = new(0.7f, 0.5f, 1f, 1f);

        public float OrbitRange => orbitRange;
        public float OrbitBlend => orbitBlend;
        public float SurgeSpeedMultiplier => surgeSpeedMultiplier;
        public float TeleportInterval => teleportInterval;
        public float TeleportTelegraphDuration => teleportTelegraphDuration;
        public float PostTeleportPause => postTeleportPause;
        public float MinTeleportDistance => minTeleportDistance;
        public float MaxTeleportDistance => Mathf.Max(minTeleportDistance, maxTeleportDistance);
        public float TeleportCollisionCheckRadius => teleportCollisionCheckRadius;
        public int TeleportSampleCount => Mathf.Max(3, teleportSampleCount);
        public Color TeleportTelegraphColor => teleportTelegraphColor;
    }
}
