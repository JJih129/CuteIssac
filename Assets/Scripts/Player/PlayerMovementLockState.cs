using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Tracks temporary movement locks from hazards without changing input ownership.
    /// PlayerMovement reads this state and suppresses input velocity while locks are active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMovementLockState : MonoBehaviour
    {
        [SerializeField] private bool logDebugMessages;

        private readonly List<LockSource> _lockSources = new();

        public bool IsLocked => _lockSources.Count > 0;
        public int ActiveLockCount => _lockSources.Count;

        private void Update()
        {
            if (_lockSources.Count == 0)
            {
                return;
            }

            float currentTime = Time.time;

            for (int i = _lockSources.Count - 1; i >= 0; i--)
            {
                if (currentTime < _lockSources[i].EndsAt)
                {
                    continue;
                }

                RemoveLockAt(i);
            }
        }

        private void OnDisable()
        {
            _lockSources.Clear();
        }

        public void LockMovement(int sourceKey, float duration)
        {
            int resolvedSourceKey = sourceKey != 0 ? sourceKey : GetInstanceID();
            float resolvedDuration = Mathf.Max(0f, duration);

            if (resolvedDuration <= 0f)
            {
                return;
            }

            float endsAt = Time.time + resolvedDuration;

            for (int i = 0; i < _lockSources.Count; i++)
            {
                LockSource source = _lockSources[i];

                if (source.SourceKey != resolvedSourceKey)
                {
                    continue;
                }

                source.EndsAt = Mathf.Max(source.EndsAt, endsAt);
                _lockSources[i] = source;
                return;
            }

            _lockSources.Add(new LockSource(resolvedSourceKey, endsAt));

            if (logDebugMessages)
            {
                Debug.Log($"Movement locked for {resolvedDuration:0.##}s by {resolvedSourceKey}.", this);
            }
        }

        public void UnlockMovement(int sourceKey)
        {
            for (int i = _lockSources.Count - 1; i >= 0; i--)
            {
                if (_lockSources[i].SourceKey == sourceKey)
                {
                    RemoveLockAt(i);
                    return;
                }
            }
        }

        public void ClearAllLocks()
        {
            _lockSources.Clear();
        }

        private void RemoveLockAt(int index)
        {
            int lastIndex = _lockSources.Count - 1;

            if (index != lastIndex)
            {
                _lockSources[index] = _lockSources[lastIndex];
            }

            _lockSources.RemoveAt(lastIndex);
        }

        private struct LockSource
        {
            public LockSource(int sourceKey, float endsAt)
            {
                SourceKey = sourceKey;
                EndsAt = endsAt;
            }

            public int SourceKey { get; }
            public float EndsAt { get; set; }
        }
    }
}
