using CuteIssac.Common.Combat;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Minimal chase AI for the prototype.
    /// It periodically finds the player, moves toward them, and applies contact damage through the shared damage contract.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyMovement enemyMovement;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private Transform targetOverride;

        [Header("Targeting")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] [Min(0.1f)] private float retargetInterval = 0.5f;

        [Header("Combat")]
        [SerializeField] [Min(0f)] private float contactDamage = 1f;

        private Transform _target;
        private float _retargetTimer;

        private void Awake()
        {
            if (!TryResolveDependencies())
            {
                enabled = false;
                return;
            }

            enemyHealth.Died += HandleDeath;
            ResolveTarget();
        }

        private void OnDestroy()
        {
            if (enemyHealth != null)
            {
                enemyHealth.Died -= HandleDeath;
            }
        }

        private void FixedUpdate()
        {
            if (enemyHealth.IsDead)
            {
                return;
            }

            _retargetTimer -= Time.fixedDeltaTime;

            if (_target == null || _retargetTimer <= 0f)
            {
                ResolveTarget();
            }

            if (_target == null)
            {
                enemyMovement.Stop();
                return;
            }

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                enemyMovement.Stop();
                enemyVisual?.SetMoveDirection(Vector2.zero);
                return;
            }

            Vector2 moveDirection = toTarget.normalized;
            enemyMovement.SetMoveDirection(moveDirection);
            enemyVisual?.SetMoveDirection(moveDirection);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryApplyContactDamage(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryApplyContactDamage(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyContactDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplyContactDamage(other);
        }

        private void TryApplyContactDamage(Collider2D hitCollider)
        {
            if (enemyHealth.IsDead || contactDamage <= 0f || _target == null)
            {
                return;
            }

            if (hitCollider == null)
            {
                return;
            }

            // Restrict contact damage to the tracked target so enemies do not hurt unrelated damageable objects.
            if (hitCollider.transform != _target && !hitCollider.transform.IsChildOf(_target))
            {
                return;
            }

            if (!DamageableResolver.TryResolve(hitCollider, out IDamageable damageable))
            {
                return;
            }

            Vector2 hitDirection = ((Vector2)hitCollider.transform.position - (Vector2)transform.position).normalized;
            damageable.ApplyDamage(new DamageInfo(contactDamage, hitDirection, transform));
        }

        private bool TryResolveDependencies()
        {
            if (enemyMovement == null)
            {
                enemyMovement = GetComponent<EnemyMovement>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }

            if (enemyMovement == null || enemyHealth == null)
            {
                Debug.LogError("EnemyController requires EnemyMovement and EnemyHealth components.", this);
                return false;
            }

            return true;
        }

        private void ResolveTarget()
        {
            _retargetTimer = retargetInterval;

            if (targetOverride != null)
            {
                _target = targetOverride;
                return;
            }

            // Search by player controller first so the prototype works even before tags are configured.
            PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);

            if (playerController != null)
            {
                _target = playerController.transform;
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                try
                {
                    GameObject taggedPlayer = GameObject.FindWithTag(playerTag);

                    if (taggedPlayer != null)
                    {
                        _target = taggedPlayer.transform;
                        return;
                    }
                }
                catch (UnityException)
                {
                    // Ignore missing tag setup. Direct reference or controller lookup is enough for the prototype.
                }
            }

            _target = null;
        }

        private void HandleDeath()
        {
            enemyMovement.Stop();
            enemyVisual?.SetMoveDirection(Vector2.zero);
            enabled = false;
        }

        private void Reset()
        {
            enemyMovement = GetComponent<EnemyMovement>();
            enemyHealth = GetComponent<EnemyHealth>();
            enemyVisual = GetComponent<EnemyVisual>();
        }

        private void OnValidate()
        {
            if (enemyMovement == null)
            {
                enemyMovement = GetComponent<EnemyMovement>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (enemyVisual == null)
            {
                enemyVisual = GetComponent<EnemyVisual>();
            }
        }
    }
}
