using CuteIssac.Dungeon;
using CuteIssac.Enemy;
using CuteIssac.Player;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [CreateAssetMenu(menuName = "CuteIssac/Items/Active Effects/Void Warp", fileName = "ActiveVoidWarpEffect")]
    public sealed class ActiveVoidWarpEffectData : ActiveItemEffectData
    {
        [SerializeField] [Min(0.1f)] private float collisionCheckRadius = 0.45f;
        [SerializeField] [Range(0f, 0.45f)] private float roomBoundsInsetRatio = 0.12f;
        [SerializeField] [Min(6)] private int sampleCount = 20;

        private Collider2D[] _overlapBuffer;

        public override bool TryApply(PlayerActiveItemController controller)
        {
            if (controller == null)
            {
                return false;
            }

            RoomNavigationController roomNavigationController = Object.FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);
            RoomController currentRoom = roomNavigationController != null ? roomNavigationController.CurrentRoom : null;

            if (currentRoom == null)
            {
                return false;
            }

            Bounds roomBounds = currentRoom.RoomBounds;
            Collider2D[] ignoredColliders = controller.GetComponentsInChildren<Collider2D>(true);
            Vector3 bestCandidate = currentRoom.DefaultPlayerSpawnPosition;
            float bestScore = float.NegativeInfinity;

            for (int index = 0; index < Mathf.Max(6, sampleCount); index++)
            {
                Vector3 candidate = ResolveCandidate(roomBounds, index, Mathf.Max(6, sampleCount));

                if (IsBlocked(candidate, ignoredColliders))
                {
                    continue;
                }

                float score = ScoreCandidate(candidate);

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestCandidate = candidate;
            }

            if (bestScore == float.NegativeInfinity && IsBlocked(bestCandidate, ignoredColliders))
            {
                bestCandidate = controller.transform.position;
            }

            return controller.TryWarpToPosition(bestCandidate);
        }

        private Vector3 ResolveCandidate(Bounds roomBounds, int index, int totalSamples)
        {
            Vector3 center = roomBounds.center;
            Vector3 extents = roomBounds.extents;
            float insetX = extents.x * Mathf.Clamp01(roomBoundsInsetRatio);
            float insetY = extents.y * Mathf.Clamp01(roomBoundsInsetRatio);
            float usableHalfWidth = Mathf.Max(0.2f, extents.x - insetX);
            float usableHalfHeight = Mathf.Max(0.2f, extents.y - insetY);
            float angle = ((360f / totalSamples) * index + 19f) * Mathf.Deg2Rad;
            float radialLerp = totalSamples <= 1 ? 0.5f : index / (float)(totalSamples - 1);
            float radiusScale = Mathf.Lerp(0.35f, 1f, radialLerp);

            return new Vector3(
                center.x + Mathf.Cos(angle) * usableHalfWidth * radiusScale,
                center.y + Mathf.Sin(angle) * usableHalfHeight * radiusScale,
                center.z);
        }

        private bool IsBlocked(Vector3 candidate, Collider2D[] ignoredColliders)
        {
            EnsureOverlapBuffer();
            ContactFilter2D contactFilter = new()
            {
                useTriggers = true
            };
            int overlapCount = Physics2D.OverlapCircle(candidate, collisionCheckRadius, contactFilter, _overlapBuffer);

            while (overlapCount >= _overlapBuffer.Length)
            {
                _overlapBuffer = new Collider2D[_overlapBuffer.Length * 2];
                overlapCount = Physics2D.OverlapCircle(candidate, collisionCheckRadius, contactFilter, _overlapBuffer);
            }

            for (int index = 0; index < overlapCount; index++)
            {
                Collider2D overlap = _overlapBuffer[index];
                _overlapBuffer[index] = null;

                if (overlap == null || overlap.isTrigger)
                {
                    continue;
                }

                if (ignoredColliders != null)
                {
                    bool ignored = false;

                    for (int ignoreIndex = 0; ignoreIndex < ignoredColliders.Length; ignoreIndex++)
                    {
                        if (overlap == ignoredColliders[ignoreIndex])
                        {
                            ignored = true;
                            break;
                        }
                    }

                    if (ignored)
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        private void EnsureOverlapBuffer()
        {
            int capacity = Mathf.Max(8, sampleCount);
            if (_overlapBuffer == null || _overlapBuffer.Length < capacity)
            {
                _overlapBuffer = new Collider2D[capacity];
            }
        }

        private static float ScoreCandidate(Vector3 candidate)
        {
            float nearestEnemyDistance = float.MaxValue;

            for (int index = 0; index < EnemyRegistry.Count; index++)
            {
                EnemyHealth enemy = EnemyRegistry.GetAt(index);

                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float distance = Vector2.Distance(candidate, enemy.transform.position);
                nearestEnemyDistance = Mathf.Min(nearestEnemyDistance, distance);
            }

            return nearestEnemyDistance;
        }
    }
}
