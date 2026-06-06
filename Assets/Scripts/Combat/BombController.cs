using CuteIssac.Core.Pooling;
using UnityEngine;

namespace CuteIssac.Combat
{
    /// <summary>
    /// Owns bomb gameplay orchestration only: fuse countdown and explosion dispatch.
    /// Damage application and presentation are delegated to dedicated components.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BombController : MonoBehaviour, IPooledObjectLifecycle
    {
        [Header("References")]
        [SerializeField] private BombVisual bombVisual;
        [SerializeField] private DamageArea damageArea;

        [Header("Explosion")]
        [SerializeField] [Min(0.1f)] private float fuseSeconds = 2f;
        [SerializeField] [Min(0.1f)] private float explosionRadius = 2.15f;
        [SerializeField] [Min(0f)] private float explosionDamage = 30f;
        [SerializeField] [Min(0f)] private float explosionKnockback = 8f;
        private Transform _instigator;
        private Collider2D _instigatorCollider;
        private float _remainingFuse;
        private bool _hasExploded;

        public float ExplosionRadius => explosionRadius;

        private void Awake()
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

        private void OnEnable()
        {
            ResetFuseState();
        }

        private void Update()
        {
            if (_hasExploded)
            {
                return;
            }

            _remainingFuse -= Time.deltaTime;
            bombVisual?.HandleCountdown(fuseSeconds > 0.01f ? _remainingFuse / fuseSeconds : 0f);

            if (_remainingFuse <= 0f)
            {
                Explode();
            }
        }

        public void Initialize(Transform instigator, Collider2D instigatorCollider)
        {
            _instigator = instigator;
            _instigatorCollider = instigatorCollider;
        }

        [ContextMenu("Explode Now")]
        public void Explode()
        {
            if (_hasExploded)
            {
                return;
            }

            _hasExploded = true;
            Vector2 explosionPosition = transform.position;
            BombExplosionInfo explosionInfo = new(
                explosionPosition,
                explosionRadius,
                explosionDamage,
                explosionKnockback,
                _instigator);
            damageArea?.ApplyExplosion(in explosionInfo, _instigatorCollider);
            bombVisual?.HandleExploded(explosionRadius);
            PrefabPoolService.Return(gameObject);
        }

        public void OnPoolSpawned()
        {
            ResetFuseState();
        }

        public void OnPoolDespawned()
        {
            _instigator = null;
            _instigatorCollider = null;
            _remainingFuse = 0f;
            _hasExploded = false;
        }

        private void ResetFuseState()
        {
            _remainingFuse = fuseSeconds;
            _hasExploded = false;
            bombVisual?.HandleArmed();
        }

        private void Reset()
        {
            bombVisual = GetComponent<BombVisual>();
            damageArea = GetComponent<DamageArea>();
        }

        private void OnValidate()
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
