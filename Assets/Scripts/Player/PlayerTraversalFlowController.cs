using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Applies a short traversal burst after room transitions so recommended paths feel more deliberate.
    /// The stat layer stays isolated from momentum and item buffs so art or tuning swaps do not affect combat systems.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerTraversalFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerScreenFeedback playerScreenFeedback;

        [Header("Traversal Burst")]
        [SerializeField] [Min(0.1f)] private float baseBurstDuration = 0.42f;
        [SerializeField] [Min(0.1f)] private float guidedBurstDuration = 0.82f;
        [SerializeField] [Range(1f, 1.5f)] private float baseMoveSpeedMultiplier = 1.08f;
        [SerializeField] [Range(1f, 1.8f)] private float guidedMoveSpeedMultiplier = 1.2f;
        [SerializeField] [Min(0f)] private float baseImpulseStrength = 2.4f;
        [SerializeField] [Min(0f)] private float guidedImpulseStrength = 4.1f;
        [SerializeField] [Range(0f, 0.15f)] private float specialRoomMoveSpeedBonus = 0.05f;
        [SerializeField] [Range(0f, 0.4f)] private float specialRoomDurationBonus = 0.16f;
        [SerializeField] [Min(0f)] private float specialRoomImpulseBonus = 0.85f;

        [Header("Feedback")]
        [SerializeField] [Range(0f, 1f)] private float baseScreenFeedbackScale = 0.16f;
        [SerializeField] [Range(0f, 1f)] private float guidedScreenFeedbackScale = 0.34f;
        [SerializeField] [Min(0.1f)] private float guidedFeedbackLifetime = 0.6f;
        [SerializeField] [Min(0f)] private float guidedFeedbackRiseDistance = 0.58f;

        private readonly List<StatModifier> _traversalStatModifiers = new();

        private float _burstExpiresAt;

        private void Awake()
        {
            ResolveReferences();
            ClearBurst();
        }

        private void OnDisable()
        {
            ClearBurst();
        }

        private void Update()
        {
            if (_burstExpiresAt <= 0f || Time.time < _burstExpiresAt)
            {
                return;
            }

            ClearBurst();
        }

        public void TriggerTraversalBurst(Vector2 travelDirection, Color accentColor, bool guidedTarget, RoomType targetRoomType)
        {
            ResolveReferences();

            if (playerStats == null || playerMovement == null)
            {
                return;
            }

            Vector2 resolvedDirection = travelDirection.sqrMagnitude > 0.001f
                ? travelDirection.normalized
                : Vector2.right;
            float roomWeight = ResolveSpecialRoomWeight(targetRoomType);
            float duration = Mathf.Max(
                0.12f,
                (guidedTarget ? guidedBurstDuration : baseBurstDuration) + (specialRoomDurationBonus * roomWeight));
            float moveSpeedMultiplier = Mathf.Max(
                1f,
                (guidedTarget ? guidedMoveSpeedMultiplier : baseMoveSpeedMultiplier) + (specialRoomMoveSpeedBonus * roomWeight));
            float impulseStrength = Mathf.Max(
                0f,
                (guidedTarget ? guidedImpulseStrength : baseImpulseStrength) + (specialRoomImpulseBonus * roomWeight));

            _burstExpiresAt = Time.time + duration;
            RebuildTraversalModifiers(moveSpeedMultiplier);
            playerMovement.ApplyImpulse(resolvedDirection * impulseStrength);
            playerScreenFeedback?.PlayHitFeedback(guidedTarget ? guidedScreenFeedbackScale : baseScreenFeedbackScale);

            if (!guidedTarget)
            {
                return;
            }

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + new Vector3(resolvedDirection.x, resolvedDirection.y, 0f) * 0.38f + Vector3.up * 0.48f,
                ResolveGuidedLabel(targetRoomType),
                Color.Lerp(accentColor, Color.white, 0.2f),
                guidedFeedbackLifetime,
                guidedFeedbackRiseDistance,
                1.08f,
                visualProfile: FloatingFeedbackVisualProfile.Momentum));
        }

        private void RebuildTraversalModifiers(float moveSpeedMultiplier)
        {
            _traversalStatModifiers.Clear();

            if (playerStats == null || moveSpeedMultiplier <= 1f)
            {
                playerStats?.SetTraversalRuntimeModifiers(null, null);
                return;
            }

            _traversalStatModifiers.Add(new StatModifier(
                PlayerStatType.MoveSpeed,
                StatModifierOperation.Multiply,
                moveSpeedMultiplier));

            playerStats.SetTraversalRuntimeModifiers(_traversalStatModifiers, null);
        }

        private void ClearBurst()
        {
            _burstExpiresAt = 0f;
            _traversalStatModifiers.Clear();
            playerStats?.SetTraversalRuntimeModifiers(null, null);
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            if (playerScreenFeedback == null)
            {
                playerScreenFeedback = GetComponent<PlayerScreenFeedback>();
            }

            if (playerScreenFeedback == null)
            {
                playerScreenFeedback = gameObject.AddComponent<PlayerScreenFeedback>();
            }
        }

        private static string ResolveGuidedLabel(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => "TREASURE FLOW",
                RoomType.Shop => "SHOP LINE",
                RoomType.Boss => "BOSS PUSH",
                RoomType.Secret => "SECRET TRAIL",
                RoomType.Challenge => "CHALLENGE RUN",
                RoomType.MiniBoss => "ELITE PUSH",
                RoomType.Curse => "CURSE ROUTE",
                RoomType.Trap => "TRAP BREAK",
                _ => "FLOW ROUTE"
            };
        }

        private static float ResolveSpecialRoomWeight(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 1f,
                RoomType.Treasure => 0.82f,
                RoomType.Secret => 0.8f,
                RoomType.MiniBoss => 0.78f,
                RoomType.Challenge => 0.7f,
                RoomType.Shop => 0.62f,
                RoomType.Curse => 0.58f,
                RoomType.Trap => 0.42f,
                _ => 0f
            };
        }
    }
}
