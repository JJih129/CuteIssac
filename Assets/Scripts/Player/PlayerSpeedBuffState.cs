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
        private float _activeDuration;
        private string _activeDisplayName = "스피드 하트";
        private Sprite _activeIcon;
        private int _speedBuffSourceKey;

        public bool IsActive => _remainingDuration > 0f;
        public float RemainingDuration => Mathf.Max(0f, _remainingDuration);
        public float NormalizedRemaining => _activeDuration > 0f ? Mathf.Clamp01(_remainingDuration / _activeDuration) : 0f;
        public string ActiveDisplayName => _activeDisplayName;
        public Sprite ActiveIcon => _activeIcon;
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
            return TryApplyBuff(duration, moveSpeedMultiplier, duplicatePolicy, null, "스피드 하트");
        }

        public bool TryApplyBuff(float duration, float moveSpeedMultiplier, DuplicateBuffPolicy duplicatePolicy, Sprite statusIcon, string displayName)
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
            _activeDuration = resolvedDuration;
            _activeIcon = statusIcon;
            _activeDisplayName = string.IsNullOrWhiteSpace(displayName) ? "스피드 하트" : displayName;
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
            _activeDuration = 0f;
            _activeIcon = null;
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
