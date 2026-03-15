using System;
using CuteIssac.Common.Combat;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Reusable health component for enemies.
    /// Implements IDamageable so projectiles and future hazards can damage enemies through one common path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float maxHealth = 10f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] [Min(1f)] private float regularEnemyHealthMultiplier = 2f;
        [SerializeField] [Min(1f)] private float bossHealthMultiplier = 12f;
        [SerializeField] private ChampionEnemyModifier championEnemyModifier;
        [SerializeField] private EnemyVisual enemyVisual;

        public event Action Damaged;
        public event Action Died;
        public event Action<EnemyHealth> DiedWithSource;

        public float BaseMaxHealth => maxHealth;
        public float MaxHealth => ResolveEffectiveMaxHealth();
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public string EnemyId => _enemyController != null ? _enemyController.EnemyId : ResolveFallbackEnemyId();

        private EnemyController _enemyController;
        private EnemyMovement _enemyMovement;
        private DamageInfo _lastDamageInfo;
        private bool _isBossEnemy;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
            _enemyMovement = GetComponent<EnemyMovement>();
            if (championEnemyModifier == null)
            {
                championEnemyModifier = GetComponent<ChampionEnemyModifier>();
            }
            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }
            _isBossEnemy = GetComponent<BossEnemyController>() != null;
            ResetForSpawn();
        }

        public void ApplyDamage(in DamageInfo damageInfo)
        {
            if (IsDead)
            {
                return;
            }

            float damageAmount = Mathf.Max(0f, damageInfo.Amount);
            if (championEnemyModifier != null)
            {
                damageAmount = championEnemyModifier.ModifyIncomingDamage(damageAmount, in damageInfo);
            }

            if (damageAmount <= 0f)
            {
                return;
            }

            DamageInfo resolvedDamageInfo = new DamageInfo(damageAmount, damageInfo.HitDirection, damageInfo.Source, damageInfo.KnockbackForce);
            _lastDamageInfo = resolvedDamageInfo;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);
            ApplyKnockback(resolvedDamageInfo);
            Damaged?.Invoke();
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                ResolveDamageFeedbackPosition(in resolvedDamageInfo),
                Mathf.CeilToInt(damageAmount).ToString(),
                new Color(1f, 0.92f, 0.52f, 1f),
                0.52f,
                0.65f,
                1.24f,
                visualProfile: FloatingFeedbackVisualProfile.EnemyDamage));
            GameAudioEvents.Raise(GameAudioEventType.Hit, transform.position, true, 0.9f);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void RestoreToFull()
        {
            ResetForSpawn();
        }

        public bool RestoreHealth(float amount)
        {
            if (IsDead)
            {
                return false;
            }

            float healAmount = Mathf.Max(0f, amount);

            if (healAmount <= 0f || CurrentHealth >= MaxHealth)
            {
                return false;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healAmount);
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                ResolveHealFeedbackPosition(),
                $"+{Mathf.CeilToInt(healAmount)}",
                new Color(0.48f, 1f, 0.6f, 1f),
                0.5f,
                0.58f,
                1.12f,
                visualProfile: FloatingFeedbackVisualProfile.Pickup));
            return true;
        }

        public void SetMaxHealth(float healthValue)
        {
            maxHealth = Mathf.Max(1f, healthValue);

            if (!IsDead)
            {
                CurrentHealth = Mathf.Min(CurrentHealth <= 0f ? MaxHealth : CurrentHealth, MaxHealth);
            }
        }

        public void ResetForSpawn()
        {
            IsDead = false;
            CurrentHealth = MaxHealth;
            _lastDamageInfo = default;
        }

        private float ResolveEffectiveMaxHealth()
        {
            float multiplier = _isBossEnemy ? bossHealthMultiplier : regularEnemyHealthMultiplier;
            return maxHealth * Mathf.Max(1f, multiplier);
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            DiedWithSource?.Invoke(this);
            Died?.Invoke();
            GameAudioEvents.Raise(GameAudioEventType.EnemyDied, transform.position);
            GameplayRuntimeEvents.RaiseEnemyKilled(new EnemyKilledSignal(this, _lastDamageInfo));
            GameplayRuntimeEvents.RaiseEnemyDied(this);

            if (destroyOnDeath)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        private void ApplyKnockback(in DamageInfo damageInfo)
        {
            if (_enemyMovement == null || damageInfo.KnockbackForce <= 0f)
            {
                return;
            }

            Vector2 direction = damageInfo.HitDirection.sqrMagnitude > 0.0001f
                ? damageInfo.HitDirection.normalized
                : Vector2.zero;

            if (direction == Vector2.zero)
            {
                return;
            }

            _enemyMovement.ApplyImpulse(direction * damageInfo.KnockbackForce);
        }

        private string ResolveFallbackEnemyId()
        {
            return string.IsNullOrWhiteSpace(gameObject.name)
                ? "enemy"
                : gameObject.name.Replace("(Clone)", string.Empty).Trim();
        }

        private void Reset()
        {
            if (championEnemyModifier == null)
            {
                championEnemyModifier = GetComponent<ChampionEnemyModifier>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }
        }

        private void OnValidate()
        {
            if (championEnemyModifier == null)
            {
                championEnemyModifier = GetComponent<ChampionEnemyModifier>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }
        }

        private Vector3 ResolveDamageFeedbackPosition(in DamageInfo damageInfo)
        {
            Vector3 anchorPosition = ResolveFeedbackAnchorPosition();
            Vector2 hitDirection = damageInfo.HitDirection.sqrMagnitude > 0.0001f
                ? damageInfo.HitDirection.normalized
                : Vector2.zero;
            return anchorPosition + new Vector3(hitDirection.x * 0.18f, 0.18f, 0f);
        }

        private Vector3 ResolveHealFeedbackPosition()
        {
            return ResolveFeedbackAnchorPosition() + new Vector3(0.08f, 0.28f, 0f);
        }

        private Vector3 ResolveFeedbackAnchorPosition()
        {
            if (enemyVisual != null && enemyVisual.HitEffectAnchor != null)
            {
                return enemyVisual.HitEffectAnchor.position;
            }

            return transform.position;
        }
    }
}
