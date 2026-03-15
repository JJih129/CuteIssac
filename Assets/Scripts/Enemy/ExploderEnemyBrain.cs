using CuteIssac.Combat;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Pursues the player, then self-destructs with a short warning once it gets close.
    /// Adds melee-area denial pressure that existing enemies do not provide.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DamageArea))]
    public sealed class ExploderEnemyBrain : EnemyBrain
    {
        [Header("References")]
        [SerializeField] private ExploderEnemyConfigurator configurator;
        [SerializeField] private DamageArea damageArea;
        [SerializeField] private Collider2D ownerCollider;

        private float _windupRemaining;
        private bool _hasExploded;
        private float _armingDelayRemaining;
        private float _runtimeFirstAttackDelayBonus;
        private float _runtimeTelegraphDurationMultiplier = 1f;

        protected override void HandleInitialized()
        {
            ResolveReferences();
            HandleResetState();
        }

        protected override void HandleResetState()
        {
            _windupRemaining = 0f;
            _hasExploded = false;
            _armingDelayRemaining = _runtimeFirstAttackDelayBonus;
            Controller?.EnemyVisual?.StopAttackTelegraph();
        }

        public void ApplyEncounterPacing(float firstAttackDelayBonus, float telegraphDurationMultiplier)
        {
            _runtimeFirstAttackDelayBonus = Mathf.Max(0f, firstAttackDelayBonus);
            _runtimeTelegraphDurationMultiplier = Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        }

        public override void TickBrain(float fixedDeltaTime)
        {
            ExploderEnemyData enemyData = configurator != null ? configurator.EnemyData : null;

            if (enemyData == null || _hasExploded)
            {
                Controller.StopMovement();
                return;
            }

            Vector2 toTarget = Controller.TargetPosition - Controller.Position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                Controller.StopMovement();
                return;
            }

            float distance = toTarget.magnitude;
            Vector2 chaseDirection = toTarget / distance;
            _armingDelayRemaining = Mathf.Max(0f, _armingDelayRemaining - fixedDeltaTime);

            if (_windupRemaining > 0f)
            {
                _windupRemaining -= fixedDeltaTime;
                Controller.SetMoveSpeedMultiplier(enemyData.WindupMoveSpeedMultiplier);
                Controller.StopMovement();
                Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);

                if (_windupRemaining <= 0f)
                {
                    Explode(enemyData);
                }

                return;
            }

            if (_armingDelayRemaining <= 0f && distance <= enemyData.TriggerRange)
            {
                _windupRemaining = enemyData.WindupDuration * _runtimeTelegraphDurationMultiplier;
                Controller.EnemyVisual?.StartAttackTelegraph(enemyData.TelegraphColor);
                return;
            }

            Controller.SetMoveSpeedMultiplier(enemyData.ChaseSpeedMultiplier);
            Controller.SetDesiredMoveDirection(chaseDirection);
        }

        private void Explode(ExploderEnemyData enemyData)
        {
            if (_hasExploded || damageArea == null)
            {
                return;
            }

            _hasExploded = true;
            Controller.StopMovement();
            Controller.EnemyVisual?.StopAttackTelegraph();
            Controller.EnemyVisual?.HandleAttack();

            BombExplosionInfo explosionInfo = new(
                Controller.Position,
                enemyData.ExplosionRadius,
                enemyData.ExplosionDamage,
                enemyData.ExplosionKnockback,
                transform);
            damageArea.ApplyExplosion(in explosionInfo, ownerCollider);

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + Vector3.up * 0.7f,
                "BOOM",
                enemyData.TelegraphColor,
                0.48f,
                0.55f,
                1.18f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));

            EnemyHealth enemyHealth = Controller.EnemyHealth;

            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.ApplyDamage(new CuteIssac.Common.Combat.DamageInfo(
                    enemyHealth.MaxHealth,
                    Vector2.zero,
                    transform));
            }
        }

        private void ResolveReferences()
        {
            if (configurator == null)
            {
                configurator = GetComponent<ExploderEnemyConfigurator>();
            }

            if (damageArea == null)
            {
                damageArea = GetComponent<DamageArea>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
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
