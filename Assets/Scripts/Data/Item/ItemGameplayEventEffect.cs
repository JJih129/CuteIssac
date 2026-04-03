using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Feedback;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    [Serializable]
    public sealed class ItemGameplayEventEffect
    {
        [SerializeField] private GameplayEventTriggerType triggerType = GameplayEventTriggerType.EnemyKilled;
        [SerializeField] private ItemGameplayEventEffectType effectType = ItemGameplayEventEffectType.AddCoins;
        [SerializeField] private bool requirePlayerSource = true;
        [SerializeField] private bool requireCombatEncounter = true;
        [SerializeField] [Range(0f, 1f)] private float triggerChance = 1f;
        [SerializeField] [Min(0f)] private float internalCooldown = 0f;
        [SerializeField] [Min(0f)] private float luckBonusChancePerPoint = 0f;
        [SerializeField] [Min(0)] private int coinAmount = 1;
        [SerializeField] [Min(0)] private int resourceAmount = 1;
        [SerializeField] [Min(0f)] private float healAmount = 1f;
        [SerializeField] [Min(0)] private int activeChargeAmount = 1;
        [SerializeField] [Min(0.05f)] private float invulnerabilityDuration = 1.5f;
        [SerializeField] [Min(0.1f)] private float timedEffectDuration = 8f;
        [SerializeField] private string feedbackLabel = string.Empty;
        [SerializeField] private List<StatModifier> statModifiers = new();
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();

        public GameplayEventTriggerType TriggerType => triggerType;
        public ItemGameplayEventEffectType EffectType => effectType;
        public bool RequirePlayerSource => requirePlayerSource;
        public bool RequireCombatEncounter => requireCombatEncounter;
        public float TriggerChance => Mathf.Clamp01(triggerChance);
        public float InternalCooldown => Mathf.Max(0f, internalCooldown);
        public float LuckBonusChancePerPoint => Mathf.Max(0f, luckBonusChancePerPoint);
        public int CoinAmount => Mathf.Max(0, coinAmount);
        public int ResourceAmount => Mathf.Max(0, resourceAmount);
        public float HealAmount => Mathf.Max(0f, healAmount);
        public int ActiveChargeAmount => Mathf.Max(0, activeChargeAmount);
        public float InvulnerabilityDuration => Mathf.Max(0.05f, invulnerabilityDuration);
        public float TimedEffectDuration => Mathf.Max(0.1f, timedEffectDuration);
        public string FeedbackLabel => feedbackLabel;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => projectileModifiers;

        public string ResolveFeedbackLabel(string fallback)
        {
            return FloatingFeedbackLabelUtility.NormalizeEventLabel(feedbackLabel, fallback);
        }

        public float ResolveEffectiveTriggerChance(float playerLuck)
        {
            float resolvedLuck = Mathf.Max(0f, playerLuck);
            return Mathf.Clamp01(TriggerChance + (resolvedLuck * LuckBonusChancePerPoint));
        }
    }
}
