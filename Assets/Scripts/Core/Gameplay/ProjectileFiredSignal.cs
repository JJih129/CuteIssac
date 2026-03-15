using UnityEngine;

namespace CuteIssac.Core.Gameplay
{
    public readonly struct ProjectileFiredSignal
    {
        public ProjectileFiredSignal(Transform source, Vector3 origin, Vector2 direction, int projectileCount)
        {
            Source = source;
            Origin = origin;
            Direction = direction;
            ProjectileCount = projectileCount;
        }

        public Transform Source { get; }
        public Vector3 Origin { get; }
        public Vector2 Direction { get; }
        public int ProjectileCount { get; }
    }
}
