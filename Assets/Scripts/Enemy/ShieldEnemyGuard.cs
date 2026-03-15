using CuteIssac.Common.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Child hit receiver placed on the front shield collider.
    /// Projectiles hitting this collider are absorbed before they reach EnemyHealth on the root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShieldEnemyGuard : MonoBehaviour, IDamageable
    {
        [SerializeField] private ShieldEnemyConfigurator configurator;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private Transform feedbackAnchor;

        private float _feedbackCooldownRemaining;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            _feedbackCooldownRemaining = Mathf.Max(0f, _feedbackCooldownRemaining - Time.deltaTime);
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            ShieldEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null)
            {
                return;
            }

            enemyVisual?.HandleDamaged();

            if (_feedbackCooldownRemaining > 0f)
            {
                return;
            }

            _feedbackCooldownRemaining = enemyData.BlockFeedbackCooldown;
            Vector3 feedbackPosition = feedbackAnchor != null ? feedbackAnchor.position : transform.position;
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                feedbackPosition + Vector3.up * 0.35f,
                "BLOCK",
                enemyData.BlockFeedbackColor,
                0.42f,
                0.45f,
                1.05f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
            GameAudioEvents.Raise(GameAudioEventType.Hit, feedbackPosition, true, 0.75f);
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponentInParent<ShieldEnemyConfigurator>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponentInParent<EnemyVisual>();
            }

            if (feedbackAnchor == null)
            {
                feedbackAnchor = transform;
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
