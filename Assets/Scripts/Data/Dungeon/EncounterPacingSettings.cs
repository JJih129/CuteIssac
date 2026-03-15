using UnityEngine;

namespace CuteIssac.Data.Dungeon
{
    [System.Serializable]
    public sealed class EncounterPacingSettings
    {
        [SerializeField] [Min(0f)] private float encounterStartAggroDelay = 0.9f;
        [SerializeField] [Min(0f)] private float encounterStartAggroDelayJitter = 0.35f;
        [SerializeField] [Min(0f)] private float minimumDistanceFromPlayer = 2.8f;
        [SerializeField] [Min(0f)] private float preferredSpawnSeparation = 2.2f;
        [SerializeField] [Min(1)] private int roomCandidateSamples = 8;
        [SerializeField] [Range(0f, 0.45f)] private float roomBoundsInsetRatio = 0.16f;
        [SerializeField] [Min(0f)] private float firstAttackDelayBonus = 0.2f;
        [SerializeField] [Range(0.5f, 2f)] private float telegraphDurationMultiplier = 1f;
        [SerializeField] [Range(1, 3)] private int challengeWaveCount = 2;
        [SerializeField] [Range(0.25f, 1.25f)] private float challengeReinforcementMultiplier = 0.7f;
        [SerializeField] [Range(0.25f, 1.5f)] private float challengeFollowupAggroDelayMultiplier = 0.55f;
        [SerializeField] [Range(0f, 0.5f)] private float challengeFollowupChampionChanceBonus = 0.12f;
        [SerializeField] [Range(0f, 0.35f)] private float challengeFollowupChampionChanceBonusPerWave = 0.05f;
        [SerializeField] [Range(0f, 0.75f)] private float challengeFollowupChampionChanceBonusCap = 0.24f;
        [SerializeField] [Min(0)] private int challengeFollowupGuaranteedChampionCount = 1;
        [SerializeField] [Min(0)] private int challengeFollowupGuaranteedChampionCountPerWave = 1;
        [SerializeField] [Min(0)] private int challengeFollowupGuaranteedChampionCountCap = 2;

        public float EncounterStartAggroDelay => encounterStartAggroDelay;
        public float EncounterStartAggroDelayJitter => encounterStartAggroDelayJitter;
        public float MinimumDistanceFromPlayer => minimumDistanceFromPlayer;
        public float PreferredSpawnSeparation => preferredSpawnSeparation;
        public int RoomCandidateSamples => Mathf.Max(1, roomCandidateSamples);
        public float RoomBoundsInsetRatio => Mathf.Clamp(roomBoundsInsetRatio, 0f, 0.45f);
        public float FirstAttackDelayBonus => firstAttackDelayBonus;
        public float TelegraphDurationMultiplier => Mathf.Clamp(telegraphDurationMultiplier, 0.5f, 2f);
        public int ChallengeWaveCount => Mathf.Clamp(challengeWaveCount, 1, 3);
        public float ChallengeReinforcementMultiplier => Mathf.Clamp(challengeReinforcementMultiplier, 0.25f, 1.25f);
        public float ChallengeFollowupAggroDelayMultiplier => Mathf.Clamp(challengeFollowupAggroDelayMultiplier, 0.25f, 1.5f);
        public float ChallengeFollowupChampionChanceBonus => Mathf.Clamp01(challengeFollowupChampionChanceBonus);
        public float ChallengeFollowupChampionChanceBonusPerWave => Mathf.Clamp01(challengeFollowupChampionChanceBonusPerWave);
        public float ChallengeFollowupChampionChanceBonusCap => Mathf.Clamp(challengeFollowupChampionChanceBonusCap, 0f, 0.75f);
        public int ChallengeFollowupGuaranteedChampionCount => Mathf.Max(0, challengeFollowupGuaranteedChampionCount);
        public int ChallengeFollowupGuaranteedChampionCountPerWave => Mathf.Max(0, challengeFollowupGuaranteedChampionCountPerWave);
        public int ChallengeFollowupGuaranteedChampionCountCap => Mathf.Max(0, challengeFollowupGuaranteedChampionCountCap);

        public float EvaluateChallengeFollowupChampionChanceBonus(int waveIndex)
        {
            if (waveIndex <= 0)
            {
                return 0f;
            }

            float bonus = ChallengeFollowupChampionChanceBonus;
            bonus += Mathf.Max(0, waveIndex - 1) * ChallengeFollowupChampionChanceBonusPerWave;
            return Mathf.Min(ChallengeFollowupChampionChanceBonusCap, bonus);
        }

        public int EvaluateChallengeFollowupGuaranteedChampionCount(int totalEnemyCount, int waveIndex)
        {
            if (waveIndex <= 0 || totalEnemyCount <= 0)
            {
                return 0;
            }

            int guaranteedCount = ChallengeFollowupGuaranteedChampionCount;
            guaranteedCount += Mathf.Max(0, waveIndex - 1) * ChallengeFollowupGuaranteedChampionCountPerWave;
            guaranteedCount = Mathf.Min(ChallengeFollowupGuaranteedChampionCountCap, guaranteedCount);
            return Mathf.Clamp(guaranteedCount, 0, totalEnemyCount);
        }

        public static EncounterPacingSettings CreateChallengeDefault()
        {
            return new EncounterPacingSettings
            {
                encounterStartAggroDelay = 0.55f,
                encounterStartAggroDelayJitter = 0.2f,
                minimumDistanceFromPlayer = 3.15f,
                preferredSpawnSeparation = 2.5f,
                roomCandidateSamples = 11,
                roomBoundsInsetRatio = 0.12f,
                firstAttackDelayBonus = 0.05f,
                telegraphDurationMultiplier = 0.82f,
                challengeWaveCount = 2,
                challengeReinforcementMultiplier = 0.72f,
                challengeFollowupAggroDelayMultiplier = 0.45f,
                challengeFollowupChampionChanceBonus = 0.18f,
                challengeFollowupChampionChanceBonusPerWave = 0.07f,
                challengeFollowupChampionChanceBonusCap = 0.32f,
                challengeFollowupGuaranteedChampionCount = 1,
                challengeFollowupGuaranteedChampionCountPerWave = 1,
                challengeFollowupGuaranteedChampionCountCap = 2
            };
        }
    }
}
