using CuteIssac.Common.Combat;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Room.Gimmicks
{
    /// <summary>
    /// Base behaviour for candy village gimmicks. It centralizes activation, player contact resolution,
    /// and player damage payload creation while concrete gimmicks own their specific rules.
    /// </summary>
    public abstract class CandyGimmickBase : MonoBehaviour
    {
        [Header("Gimmick")]
        [SerializeField] private bool gimmickActive = true;
        [SerializeField] private Collider2D contactCollider;
        [SerializeField] private bool logDebugMessages;

        public bool IsGimmickActive => gimmickActive && isActiveAndEnabled;
        public Collider2D ContactCollider => contactCollider;
        protected bool LogDebugMessages => logDebugMessages;

        protected virtual void Awake()
        {
            ResolveReferences();
        }

        protected virtual void Reset()
        {
            ResolveReferences();
        }

        protected virtual void OnValidate()
        {
            ResolveReferences();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            TryHandlePlayerContact(other);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            TryHandlePlayerContact(other);
        }

        public void SetActive(bool active)
        {
            gimmickActive = active;
        }

        protected virtual void OnPlayerContact(Collider2D playerCollider, PlayerHealth playerHealth)
        {
        }

        protected bool TryApplyDamageToPlayer(PlayerHealth playerHealth, Collider2D playerCollider, float damage, float knockbackForce)
        {
            if (!IsGimmickActive || playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            float resolvedDamage = Mathf.Max(0f, damage);

            if (resolvedDamage <= 0f)
            {
                return false;
            }

            Vector2 hitDirection = ResolveHitDirection(playerCollider);
            playerHealth.ApplyDamage(new DamageInfo(resolvedDamage, hitDirection, transform, Mathf.Max(0f, knockbackForce)));
            return true;
        }

        protected Vector2 ResolveHitDirection(Collider2D targetCollider)
        {
            Vector2 targetPosition = targetCollider != null
                ? (Vector2)targetCollider.bounds.center
                : (Vector2)transform.position;
            Vector2 hitDirection = targetPosition - (Vector2)transform.position;

            if (hitDirection.sqrMagnitude <= 0.0001f)
            {
                hitDirection = Vector2.up;
            }

            return hitDirection.normalized;
        }

        protected virtual void ResolveReferences()
        {
            if (contactCollider == null)
            {
                contactCollider = GetComponent<Collider2D>();
            }
        }

        private void TryHandlePlayerContact(Collider2D other)
        {
            if (!IsGimmickActive || other == null)
            {
                return;
            }

            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
            {
                return;
            }

            OnPlayerContact(other, playerHealth);
        }
    }
}
