using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EncounterDeathPulseModifier : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private RoomEnemyMember roomEnemyMember;

        private float _pulseRadius = 1f;
        private float _pulseTelegraphDuration = 0.55f;
        private float _pulseDamage = 1f;
        private float _pulseKnockback = 4f;
        private Color _pulseColor = new(1f, 0.48f, 0.3f, 1f);
        private bool _isConfigured;
        private bool _pulseConsumed;

        private void Awake()
        {
            ResolveReferences();

            if (enemyHealth != null)
            {
                enemyHealth.Died -= HandleEnemyDied;
                enemyHealth.Died += HandleEnemyDied;
            }
        }

        private void OnEnable()
        {
            _pulseConsumed = false;
        }

        private void OnDestroy()
        {
            if (enemyHealth != null)
            {
                enemyHealth.Died -= HandleEnemyDied;
            }
        }

        public void PrepareForSpawn()
        {
            _isConfigured = false;
            _pulseConsumed = false;
        }

        public void Configure(float pulseRadius, float pulseTelegraphDuration, float pulseDamage, float pulseKnockback, Color pulseColor)
        {
            _pulseRadius = Mathf.Max(0.25f, pulseRadius);
            _pulseTelegraphDuration = Mathf.Max(0.1f, pulseTelegraphDuration);
            _pulseDamage = Mathf.Max(0f, pulseDamage);
            _pulseKnockback = Mathf.Max(0f, pulseKnockback);
            _pulseColor = pulseColor;
            _isConfigured = true;
            _pulseConsumed = false;
        }

        private void HandleEnemyDied()
        {
            if (!_isConfigured || _pulseConsumed)
            {
                return;
            }

            RoomController roomController = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;

            if (roomController == null || roomController.State != RoomState.Combat)
            {
                return;
            }

            _pulseConsumed = true;

            GameObject pulseObject = new("EncounterDeathPulse");
            pulseObject.layer = gameObject.layer;
            pulseObject.transform.SetParent(roomController.transform, false);
            pulseObject.transform.position = transform.position;

            ArenaPressurePulseHazard pulseHazard = pulseObject.AddComponent<ArenaPressurePulseHazard>();
            pulseHazard.Configure(roomController, _pulseRadius, _pulseTelegraphDuration, _pulseDamage, _pulseKnockback, _pulseColor);
        }

        private void ResolveReferences()
        {
            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (roomEnemyMember == null)
            {
                roomEnemyMember = GetComponent<RoomEnemyMember>();
            }
        }
    }
}
