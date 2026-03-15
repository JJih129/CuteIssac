using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyVisual))]
    public sealed class ExplosiveChampionEnemyConfigurator : MonoBehaviour
    {
        [SerializeField] private ExplosiveChampionEnemyData enemyData;
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyMovement enemyMovement;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyVisual enemyVisual;

        public ExplosiveChampionEnemyData EnemyData => enemyData;

        private void Awake()
        {
            ResolveReferences();
            ApplyConfiguration();
        }

        public void ApplyConfiguration()
        {
            if (enemyData == null)
            {
                return;
            }

            enemyController?.ConfigureRuntimeData(enemyData.EnemyId, enemyData.ContactDamage);
            enemyMovement?.SetBaseMoveSpeed(enemyData.MoveSpeed);
            enemyHealth?.SetMaxHealth(enemyData.MaxHealth);
            enemyVisual?.ApplyVisualSet(enemyData.VisualSet);
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

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

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();

            if (!Application.isPlaying)
            {
                ApplyConfiguration();
            }
        }
    }
}
