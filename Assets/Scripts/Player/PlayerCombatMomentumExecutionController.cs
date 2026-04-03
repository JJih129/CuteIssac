using CuteIssac.Core.Gameplay;
using CuteIssac.Enemy;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombatMomentumController))]
    public sealed class PlayerCombatMomentumExecutionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCombatMomentumController momentumController;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Kill Sustain")]
        [SerializeField] [Min(0f)] private float baseKillSustain = 0.12f;
        [SerializeField] [Min(0f)] private float matchingFormationSustainBonus = 0.14f;
        [SerializeField] [Min(0f)] private float focusKillSustainBonus = 0.08f;
        [SerializeField] [Min(0f)] private float criticalKillSustainBonus = 0.2f;

        [Header("Execution Payoff")]
        [SerializeField] [Min(0f)] private float criticalExecutionHeal = 0.45f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            GameplayRuntimeEvents.EnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameplayRuntimeEvents.EnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(EnemyKilledSignal signal)
        {
            if (momentumController == null
                || !momentumController.IsMomentumActive
                || playerHealth == null
                || playerHealth.IsDead
                || !IsPlayerKill(signal.Killer))
            {
                return;
            }

            EnemyFormationModifier formationModifier = signal.EnemyHealth != null
                ? signal.EnemyHealth.GetComponent<EnemyFormationModifier>()
                : null;
            RoomController assignedRoom = signal.EnemyHealth != null
                ? signal.EnemyHealth.GetComponent<RoomEnemyMember>()?.AssignedRoom
                : null;

            string activeFormationId = momentumController.ActiveFormationId;
            bool matchesActiveFormation = formationModifier != null
                && !string.IsNullOrWhiteSpace(activeFormationId)
                && formationModifier.FormationId == activeFormationId;

            EnemyFormationPriorityLevel priorityLevel = formationModifier != null
                ? formationModifier.PriorityLevel
                : EnemyFormationPriorityLevel.None;

            float sustainDuration = baseKillSustain;

            if (matchesActiveFormation)
            {
                sustainDuration += matchingFormationSustainBonus;
            }

            sustainDuration += priorityLevel switch
            {
                EnemyFormationPriorityLevel.Focus => focusKillSustainBonus,
                EnemyFormationPriorityLevel.Critical => criticalKillSustainBonus,
                _ => 0f
            };

            float sustainedDuration = momentumController.SustainMomentum(sustainDuration);
            float restoredHealth = TryResolveExecutionHeal(priorityLevel);

            if (!matchesActiveFormation
                && priorityLevel == EnemyFormationPriorityLevel.None
                && restoredHealth <= 0f)
            {
                return;
            }

            if (sustainedDuration <= 0.01f
                && restoredHealth <= 0.01f
                && priorityLevel != EnemyFormationPriorityLevel.Critical)
            {
                return;
            }

            Color accentColor = formationModifier != null
                ? (priorityLevel != EnemyFormationPriorityLevel.None ? formationModifier.PriorityColor : formationModifier.AccentColor)
                : momentumController.AccentColor;

            GameplayRuntimeEvents.RaiseMomentumExecuted(new MomentumExecutionSignal(
                assignedRoom,
                signal.Position,
                accentColor,
                formationModifier != null ? formationModifier.FormationId : activeFormationId,
                priorityLevel,
                matchesActiveFormation,
                momentumController.ChainCount,
                sustainedDuration,
                restoredHealth));
        }

        private float TryResolveExecutionHeal(EnemyFormationPriorityLevel priorityLevel)
        {
            if (priorityLevel != EnemyFormationPriorityLevel.Critical
                || playerHealth == null
                || criticalExecutionHeal <= 0f
                || playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            {
                return 0f;
            }

            float previousHealth = playerHealth.CurrentHealth;

            if (!playerHealth.RestoreHealth(criticalExecutionHeal))
            {
                return 0f;
            }

            return Mathf.Max(0f, playerHealth.CurrentHealth - previousHealth);
        }

        private bool IsPlayerKill(Transform source)
        {
            if (source == null)
            {
                return false;
            }

            return source == transform
                || source.IsChildOf(transform);
        }

        private void ResolveReferences()
        {
            if (momentumController == null)
            {
                momentumController = GetComponent<PlayerCombatMomentumController>();
            }

            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }
        }
    }
}
