using CuteIssac.UI;
using CuteIssac.Core.Audio;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Enemy;
using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Boss-specific orchestration that stays separate from generic enemy movement/combat code.
    /// It drives HUD updates, enrage state, and forwards damage/death feedback to BossVisual.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(BossEnemyBrain))]
    [RequireComponent(typeof(BossVisual))]
    public sealed class BossEnemyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private BossEnemyBrain bossEnemyBrain;
        [SerializeField] private BossVisual bossVisual;
        [SerializeField] private BossPhaseProfile bossPhaseProfile;
        [Header("Boss Setup")]
        [SerializeField] private string bossDisplayName = "감시 코어";
        [SerializeField] [Range(0.05f, 0.95f)] private float enrageThreshold = 0.22f;

        private bool _isEnraged;
        private BossPhaseType _currentPhase = BossPhaseType.PhaseOne;
        private bool _encounterAnnounced;
        private BossPhaseProfile _resolvedPhaseProfile;

        private void Awake()
        {
            ResolveReferences();

            if (enemyHealth == null || bossEnemyBrain == null || bossVisual == null)
            {
                Debug.LogError("BossEnemyController requires EnemyHealth, BossEnemyBrain, and BossVisual.", this);
                enabled = false;
                return;
            }

            _resolvedPhaseProfile = ResolvePhaseProfile();
            bossEnemyBrain.SetPhaseProfile(_resolvedPhaseProfile);

            enemyHealth.Damaged += HandleDamaged;
            enemyHealth.Died += HandleDied;
            bossEnemyBrain.TelegraphStarted += HandleBossTelegraphStarted;
            bossEnemyBrain.TelegraphEnded += HandleBossTelegraphEnded;
            bossEnemyBrain.PhaseTransitionStateChanged += HandleBossPhaseTransitionStateChanged;
        }

        private void Start()
        {
            AnnounceEncounterStart();
            EvaluatePhaseState(true);
            UpdateBossHud();
            EvaluateEnrageState();
        }

        private void OnDestroy()
        {
            if (enemyHealth != null)
            {
                enemyHealth.Damaged -= HandleDamaged;
                enemyHealth.Died -= HandleDied;
            }

            if (bossEnemyBrain != null)
            {
                bossEnemyBrain.TelegraphStarted -= HandleBossTelegraphStarted;
                bossEnemyBrain.TelegraphEnded -= HandleBossTelegraphEnded;
                bossEnemyBrain.PhaseTransitionStateChanged -= HandleBossPhaseTransitionStateChanged;
            }

            BossHudEvents.RaiseBossHidden(GetInstanceID());
        }

        private void HandleDamaged()
        {
            bossVisual.HandleDamaged();
            EvaluatePhaseState(false);
            EvaluateEnrageState();
            UpdateBossHud();
        }

        private void HandleDied()
        {
            bossVisual.HandleDied();
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "보스 격파",
                bossDisplayName,
                new Color(1f, 0.38f, 0.28f, 1f),
                2.2f));
            BossHudEvents.RaiseBossHidden(GetInstanceID());
        }

        private void HandleBossTelegraphStarted(BossPatternType _)
        {
            UpdateBossHud();
        }

        private void HandleBossTelegraphEnded()
        {
            UpdateBossHud();
        }

        private void HandleBossPhaseTransitionStateChanged(bool isTransitioning)
        {
            if (isTransitioning)
            {
                bossVisual?.BeginPhaseTransition(ResolvePhaseTransitionDuration(_currentPhase));
            }

            UpdateBossHud();
        }

        private void AnnounceEncounterStart()
        {
            if (_encounterAnnounced)
            {
                return;
            }

            _encounterAnnounced = true;
            GameAudioEvents.Raise(GameAudioEventType.BossAppeared, transform.position);
            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                "보스 등장",
                bossDisplayName,
                new Color(1f, 0.32f, 0.24f, 1f),
                2.1f));
        }

        private void EvaluatePhaseState(bool force)
        {
            if (enemyHealth == null)
            {
                return;
            }

            float normalizedHealth = enemyHealth.MaxHealth > 0f ? enemyHealth.CurrentHealth / enemyHealth.MaxHealth : 0f;
            BossPhaseType nextPhase = _resolvedPhaseProfile != null
                ? _resolvedPhaseProfile.EvaluatePhase(normalizedHealth)
                : BossPhaseType.PhaseOne;

            if (!force && nextPhase == _currentPhase)
            {
                return;
            }

            _currentPhase = nextPhase;
            bossVisual.SetPhase(_currentPhase);

            if (!force)
            {
                float transitionDuration = ResolvePhaseTransitionDuration(_currentPhase);
                bossEnemyBrain.BeginPhaseTransition(_currentPhase, transitionDuration);
                RaiseBossPhasePresentation(_currentPhase, transitionDuration);
                GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                    ResolvePhaseBannerTitle(_currentPhase),
                    ResolvePhaseBannerSubtitle(_currentPhase),
                    ResolvePhaseAccent(_currentPhase),
                    ResolvePhaseBannerDuration(_currentPhase)));
                return;
            }

            bossEnemyBrain.SetPhase(_currentPhase);
        }

        private void EvaluateEnrageState()
        {
            if (enemyHealth == null)
            {
                return;
            }

            float normalizedHealth = enemyHealth.MaxHealth > 0f ? enemyHealth.CurrentHealth / enemyHealth.MaxHealth : 0f;
            bool shouldEnrage = normalizedHealth <= enrageThreshold && !enemyHealth.IsDead;

            if (_isEnraged == shouldEnrage)
            {
                return;
            }

            _isEnraged = shouldEnrage;
            bossEnemyBrain.SetEnraged(_isEnraged);
            bossVisual.SetEnraged(_isEnraged);
        }

        private void UpdateBossHud()
        {
            if (enemyHealth == null || enemyHealth.IsDead)
            {
                return;
            }

            float normalizedHealth = enemyHealth.MaxHealth > 0f ? enemyHealth.CurrentHealth / enemyHealth.MaxHealth : 0f;
            BossPatternType patternType = bossEnemyBrain.CurrentTelegraphedPattern ?? BossPatternType.Burst;
            BossHudEvents.RaiseBossShownOrUpdated(new BossHudState(
                GetInstanceID(),
                bossDisplayName,
                normalizedHealth,
                _currentPhase,
                GetPhaseLabel(_currentPhase),
                patternType,
                GetPatternLabel(bossEnemyBrain.CurrentTelegraphedPattern, bossEnemyBrain.IsPhaseTransitioning),
                bossEnemyBrain.IsPhaseTransitioning));
        }

        private string GetPhaseLabel(BossPhaseType phase)
        {
            if (_resolvedPhaseProfile != null && _resolvedPhaseProfile.TryGetDefinition(phase, out BossPhaseProfile.BossPhaseDefinition definition))
            {
                return definition.DisplayLabel;
            }

            return phase switch
            {
                BossPhaseType.PhaseTwo => "2페이즈",
                BossPhaseType.PhaseThree => "3페이즈",
                _ => "1페이즈"
            };
        }

        private float ResolvePhaseTransitionDuration(BossPhaseType phase)
        {
            if (_resolvedPhaseProfile != null &&
                _resolvedPhaseProfile.TryGetDefinition(phase, out BossPhaseProfile.BossPhaseDefinition definition))
            {
                return definition.TransitionDuration;
            }

            return 0f;
        }

        private void RaiseBossPhasePresentation(BossPhaseType phase, float transitionDuration)
        {
            GameplayFeedbackEvents.RaiseThreatFlash(new ThreatFlashRequest(
                ResolvePhaseFlashColor(phase),
                ResolvePhaseFlashOpacity(phase),
                Mathf.Max(0.32f, transitionDuration + 0.18f),
                ResolvePhaseFlashPulseCount(phase),
                ResolvePhaseFlashPulseStrength(phase),
                ResolvePhaseFlashFrequencyScale(phase),
                ResolvePhaseFlashDecaySoftness(phase)));
        }

        private string ResolvePhaseBannerTitle(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => "최종 국면 돌입",
                BossPhaseType.PhaseTwo => "격화 국면 돌입",
                _ => "보스 국면 전환"
            };
        }

        private string ResolvePhaseBannerSubtitle(BossPhaseType phase)
        {
            string phaseLabel = GetPhaseLabel(phase);
            return string.IsNullOrWhiteSpace(phaseLabel)
                ? bossDisplayName
                : $"{bossDisplayName}  ·  {phaseLabel}";
        }

        private static Color ResolvePhaseAccent(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => new Color(1f, 0.34f, 0.46f, 1f),
                BossPhaseType.PhaseTwo => new Color(1f, 0.68f, 0.24f, 1f),
                _ => new Color(1f, 0.82f, 0.34f, 1f)
            };
        }

        private static float ResolvePhaseBannerDuration(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => 1.95f,
                BossPhaseType.PhaseTwo => 1.7f,
                _ => 1.5f
            };
        }

        private static Color ResolvePhaseFlashColor(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => new Color(1f, 0.26f, 0.36f, 1f),
                BossPhaseType.PhaseTwo => new Color(1f, 0.62f, 0.22f, 1f),
                _ => new Color(1f, 0.8f, 0.3f, 1f)
            };
        }

        private static float ResolvePhaseFlashOpacity(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => 0.24f,
                BossPhaseType.PhaseTwo => 0.18f,
                _ => 0.14f
            };
        }

        private static int ResolvePhaseFlashPulseCount(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => 3,
                BossPhaseType.PhaseTwo => 2,
                _ => 2
            };
        }

        private static float ResolvePhaseFlashPulseStrength(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => 0.42f,
                BossPhaseType.PhaseTwo => 0.34f,
                _ => 0.28f
            };
        }

        private static float ResolvePhaseFlashFrequencyScale(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => 1.18f,
                BossPhaseType.PhaseTwo => 1.06f,
                _ => 1f
            };
        }

        private static float ResolvePhaseFlashDecaySoftness(BossPhaseType phase)
        {
            return phase switch
            {
                BossPhaseType.PhaseThree => 0.52f,
                BossPhaseType.PhaseTwo => 0.44f,
                _ => 0.38f
            };
        }

        private static string GetPatternLabel(BossPatternType? patternType, bool isPhaseTransitioning)
        {
            if (isPhaseTransitioning)
            {
                return "전환 중";
            }

            return patternType switch
            {
                BossPatternType.Charge => "돌진",
                BossPatternType.Volley => "연사",
                BossPatternType.Sweep => "휩쓸기",
                BossPatternType.Spiral => "나선탄",
                BossPatternType.Fan => "부채탄",
                BossPatternType.Shockwave => "충격파",
                BossPatternType.Crossfire => "십자탄",
                BossPatternType.Burst => "폭발탄",
                _ => string.Empty
            };
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (bossEnemyBrain == null)
            {
                bossEnemyBrain = GetComponent<BossEnemyBrain>();
            }

            if (bossVisual == null)
            {
                bossVisual = GetComponent<BossVisual>();
            }
        }

        private BossPhaseProfile ResolvePhaseProfile()
        {
            return bossPhaseProfile != null ? bossPhaseProfile : BossPhaseProfile.CreateRuntimeDefault();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
