using CuteIssac.Combat;
using CuteIssac.Core.Pooling;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DamageArea))]
    public sealed class EnemyMineController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BombVisual bombVisual;
        [SerializeField] private DamageArea damageArea;

        [Header("Fallback Settings")]
        [SerializeField] [Min(0f)] private float fuseSeconds = 0.5f;
        [SerializeField] [Min(0f)] private float triggerWindupSeconds = 0.42f;
        [SerializeField] [Min(0f)] private float triggerRange = 1.15f;
        [SerializeField] [Min(0.1f)] private float explosionRadius = 1.45f;
        [SerializeField] [Min(0f)] private float explosionDamage = 1.35f;
        [SerializeField] [Min(0f)] private float explosionKnockback = 6f;

        private Transform _instigator;
        private Collider2D _instigatorCollider;
        private float _remainingArmDelay;
        private float _remainingTriggerWindup;
        private bool _isInitialized;
        private bool _hasExploded;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!_isInitialized || _hasExploded)
            {
                return;
            }

            if (_remainingArmDelay > 0f)
            {
                _remainingArmDelay -= Time.deltaTime;
                bombVisual?.HandleCountdown(fuseSeconds > 0.01f ? _remainingArmDelay / fuseSeconds : 0f);
                return;
            }

            if (_remainingTriggerWindup > 0f)
            {
                _remainingTriggerWindup -= Time.deltaTime;
                bombVisual?.HandleCountdown(triggerWindupSeconds > 0.01f ? _remainingTriggerWindup / triggerWindupSeconds : 0f);

                if (_remainingTriggerWindup <= 0f)
                {
                    Explode();
                }

                return;
            }

            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);

            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            if (Vector2.Distance(transform.position, playerHealth.transform.position) <= triggerRange)
            {
                _remainingTriggerWindup = triggerWindupSeconds;
            }
        }

        public void Configure(
            Transform instigator,
            Collider2D instigatorCollider,
            float armDelay,
            float triggerDistance,
            float triggerWindup,
            float radius,
            float damage,
            float knockback)
        {
            _instigator = instigator;
            _instigatorCollider = instigatorCollider;
            fuseSeconds = Mathf.Max(0f, armDelay);
            triggerRange = Mathf.Max(0f, triggerDistance);
            triggerWindupSeconds = Mathf.Max(0f, triggerWindup);
            explosionRadius = Mathf.Max(0.1f, radius);
            explosionDamage = Mathf.Max(0f, damage);
            explosionKnockback = Mathf.Max(0f, knockback);
            _remainingArmDelay = fuseSeconds;
            _remainingTriggerWindup = 0f;
            _hasExploded = false;
            _isInitialized = true;
            bombVisual?.HandleArmed();
        }

        [ContextMenu("Explode Now")]
        public void Explode()
        {
            if (_hasExploded)
            {
                return;
            }

            _hasExploded = true;
            _isInitialized = false;
            BombExplosionInfo explosionInfo = new(
                transform.position,
                explosionRadius,
                explosionDamage,
                explosionKnockback,
                _instigator);
            damageArea?.ApplyExplosion(in explosionInfo, _instigatorCollider);
            bombVisual?.HandleExploded(explosionRadius);
            PrefabPoolService.Return(gameObject);
        }

        private void OnDisable()
        {
            _isInitialized = false;
            _hasExploded = false;
            _remainingArmDelay = 0f;
            _remainingTriggerWindup = 0f;
            _instigator = null;
            _instigatorCollider = null;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (bombVisual == null)
            {
                bombVisual = GetComponent<BombVisual>();
            }

            if (damageArea == null)
            {
                damageArea = GetComponent<DamageArea>();
            }
        }
    }
}
