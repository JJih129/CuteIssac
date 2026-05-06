using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Applies one non-stacking movement speed buff through the existing PlayerStats move-speed multiplier path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpeedBuffState : MonoBehaviour
    {
        public enum DuplicateBuffPolicy
        {
            Ignore = 0,
            RefreshDuration = 1
        }

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Defaults")]
        [SerializeField] [Min(0.05f)] private float defaultDuration = 10f;
        [SerializeField] [Min(0.05f)] private float defaultMoveSpeedMultiplier = 1.2f;
        [SerializeField] private DuplicateBuffPolicy defaultDuplicatePolicy = DuplicateBuffPolicy.RefreshDuration;
        [SerializeField] private bool logDebugMessages;

        private float _remainingDuration;
        private int _speedBuffSourceKey;

        public bool IsActive => _remainingDuration > 0f;
        public float RemainingDuration => Mathf.Max(0f, _remainingDuration);
        public float DefaultDuration => defaultDuration;
        public float DefaultMoveSpeedMultiplier => defaultMoveSpeedMultiplier;
        public DuplicateBuffPolicy DefaultDuplicatePolicy => defaultDuplicatePolicy;

        private void Awake()
        {
            ResolveReferences();
            _speedBuffSourceKey = GetInstanceID();
        }

        private void Update()
        {
            if (_remainingDuration <= 0f)
            {
                return;
            }

            _remainingDuration = Mathf.Max(0f, _remainingDuration - Time.deltaTime);

            if (_remainingDuration <= 0f)
            {
                ClearBuff();
            }
        }

        private void OnDisable()
        {
            ClearBuff();
        }

        public bool TryApplyBuff(float duration, float moveSpeedMultiplier, DuplicateBuffPolicy duplicatePolicy)
        {
            ResolveReferences();

            if (playerStats == null)
            {
                return false;
            }

            float resolvedDuration = Mathf.Max(0f, duration);
            float resolvedMultiplier = Mathf.Max(0.05f, moveSpeedMultiplier);

            if (resolvedDuration <= 0f || resolvedMultiplier <= 0f)
            {
                return false;
            }

            if (IsActive && duplicatePolicy == DuplicateBuffPolicy.Ignore)
            {
                return false;
            }

            _remainingDuration = resolvedDuration;
            playerStats.SetRuntimeObstacleMoveSpeedMultiplier(ResolveSourceKey(), resolvedMultiplier);

            if (logDebugMessages)
            {
                Debug.Log($"Speed buff applied. Duration={resolvedDuration:0.##}, multiplier={resolvedMultiplier:0.##}", this);
            }

            return true;
        }

        public bool TryApplyDefaultBuff()
        {
            return TryApplyBuff(defaultDuration, defaultMoveSpeedMultiplier, defaultDuplicatePolicy);
        }

        private void ClearBuff()
        {
            if (playerStats != null)
            {
                playerStats.ClearRuntimeObstacleMoveSpeedMultiplier(ResolveSourceKey());
            }

            _remainingDuration = 0f;
        }

        private int ResolveSourceKey()
        {
            if (_speedBuffSourceKey == 0)
            {
                _speedBuffSourceKey = GetInstanceID();
            }

            return _speedBuffSourceKey;
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
            defaultDuration = Mathf.Max(0.05f, defaultDuration);
            defaultMoveSpeedMultiplier = Mathf.Max(0.05f, defaultMoveSpeedMultiplier);
        }
    }
}
