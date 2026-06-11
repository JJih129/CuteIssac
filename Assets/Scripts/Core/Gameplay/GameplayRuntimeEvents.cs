using System;
using CuteIssac.Enemy;

namespace CuteIssac.Core.Gameplay
{
    public static class GameplayRuntimeEvents
    {
        public static event Action<PlayerDamagedSignal> PlayerDamaged;
        public static event Action<EnemyKilledSignal> EnemyKilled;
        public static event Action<PlayerProjectileHitSignal> PlayerProjectileHit;
        public static event Action<ProjectileFiredSignal> ProjectileFired;
        public static event Action<RoomResolvedSignal> RoomResolved;
        public static event Action<RoomClearSignal> RoomCleared;
        public static event Action<RoomRewardPhaseSignal> RoomRewardPhaseCompleted;
        public static event Action<RoomRewardCollectedSignal> RoomRewardCollected;
        public static event Action<CurseRewardManifestedSignal> CurseRewardManifested;
        public static event Action<ChampionEnemyPromotedSignal> ChampionEnemyPromoted;
        public static event Action<SecretRoomRevealedSignal> SecretRoomRevealed;
        public static event Action<FormationBreakSignal> FormationBroken;
        public static event Action<MomentumExecutionSignal> MomentumExecuted;
        public static event Action<PlayerLoadoutDeltaSignal> PlayerLoadoutDelta;
        public static event Action<PlayerInteractionOutcomeSignal> PlayerInteractionOutcome;
        public static event Action<ChoiceRouteResolvedSignal> ChoiceRouteResolved;
        public static event Action<ShopPurchaseFailedSignal> ShopPurchaseFailed;
        public static event Action<EnemyHealth> EnemyDied;

        public static void RaisePlayerDamaged(PlayerDamagedSignal signal)
        {
            PlayerDamaged?.Invoke(signal);
        }

        public static void RaiseEnemyKilled(EnemyKilledSignal signal)
        {
            EnemyKilled?.Invoke(signal);
        }

        public static void RaisePlayerProjectileHit(PlayerProjectileHitSignal signal)
        {
            PlayerProjectileHit?.Invoke(signal);
        }

        public static void RaiseProjectileFired(ProjectileFiredSignal signal)
        {
            ProjectileFired?.Invoke(signal);
        }

        public static void RaiseRoomResolved(RoomResolvedSignal signal)
        {
            RoomResolved?.Invoke(signal);
        }

        public static void RaiseRoomCleared(RoomClearSignal signal)
        {
            RoomCleared?.Invoke(signal);
        }

        public static void RaiseRoomRewardPhaseCompleted(RoomRewardPhaseSignal signal)
        {
            RoomRewardPhaseCompleted?.Invoke(signal);
        }

        public static void RaiseRoomRewardCollected(RoomRewardCollectedSignal signal)
        {
            RoomRewardCollected?.Invoke(signal);
        }

        public static void RaiseCurseRewardManifested(CurseRewardManifestedSignal signal)
        {
            CurseRewardManifested?.Invoke(signal);
        }

        public static void RaiseChampionEnemyPromoted(ChampionEnemyPromotedSignal signal)
        {
            ChampionEnemyPromoted?.Invoke(signal);
        }

        public static void RaiseSecretRoomRevealed(SecretRoomRevealedSignal signal)
        {
            SecretRoomRevealed?.Invoke(signal);
        }

        public static void RaiseFormationBroken(FormationBreakSignal signal)
        {
            FormationBroken?.Invoke(signal);
        }

        public static void RaiseMomentumExecuted(MomentumExecutionSignal signal)
        {
            MomentumExecuted?.Invoke(signal);
        }

        public static void RaisePlayerLoadoutDelta(PlayerLoadoutDeltaSignal signal)
        {
            PlayerLoadoutDelta?.Invoke(signal);
        }

        public static void RaisePlayerInteractionOutcome(PlayerInteractionOutcomeSignal signal)
        {
            PlayerInteractionOutcome?.Invoke(signal);
        }

        public static void RaiseChoiceRouteResolved(ChoiceRouteResolvedSignal signal)
        {
            ChoiceRouteResolved?.Invoke(signal);
        }

        public static void RaiseShopPurchaseFailed(ShopPurchaseFailedSignal signal)
        {
            ShopPurchaseFailed?.Invoke(signal);
        }

        public static void RaiseEnemyDied(EnemyHealth enemyHealth)
        {
            EnemyDied?.Invoke(enemyHealth);
        }
    }
}
