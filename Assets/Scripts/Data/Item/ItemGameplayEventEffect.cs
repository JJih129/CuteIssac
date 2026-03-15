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
        [SerializeField] [Min(0)] private int coinAmount = 1;
        [SerializeField] [Min(0.1f)] private float timedEffectDuration = 8f;
        [SerializeField] private string feedbackLabel = string.Empty;
        [SerializeField] private List<StatModifier> statModifiers = new();
        [SerializeField] private List<ProjectileModifier> projectileModifiers = new();

        public GameplayEventTriggerType TriggerType => triggerType;
        public ItemGameplayEventEffectType EffectType => effectType;
        public bool RequirePlayerSource => requirePlayerSource;
        public bool RequireCombatEncounter => requireCombatEncounter;
        public int CoinAmount => Mathf.Max(0, coinAmount);
        public float TimedEffectDuration => Mathf.Max(0.1f, timedEffectDuration);
        public string FeedbackLabel => feedbackLabel;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;
        public IReadOnlyList<ProjectileModifier> ProjectileModifiers => projectileModifiers;

        public string ResolveFeedbackLabel(string fallback)
        {
            return FloatingFeedbackLabelUtility.NormalizeEventLabel(feedbackLabel, fallback);
        }
    }
}
