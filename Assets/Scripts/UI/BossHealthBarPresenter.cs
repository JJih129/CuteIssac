using UnityEngine;

namespace CuteIssac.UI
{
    /// <summary>
    /// Subscribes to boss HUD events and forwards the latest state to the replaceable HUD view layer.
    /// Keep this on the HUD root so designers can swap the boss panel art without touching boss logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossHealthBarPresenter : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Required. Existing HUD controller that owns the boss panel view.")]
        [SerializeField] private HUDController hudController;

        private int _activeBossSourceId = -1;

        private void Awake()
        {
            ResolveReferences();
            hudController?.HideBoss();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BossHudEvents.BossShownOrUpdated += HandleBossShownOrUpdated;
            BossHudEvents.BossHidden += HandleBossHidden;
            hudController?.HideBoss();
        }

        private void OnDisable()
        {
            BossHudEvents.BossShownOrUpdated -= HandleBossShownOrUpdated;
            BossHudEvents.BossHidden -= HandleBossHidden;
            _activeBossSourceId = -1;
            hudController?.HideBoss();
        }

        private void HandleBossShownOrUpdated(BossHudState hudState)
        {
            if (hudController == null)
            {
                return;
            }

            _activeBossSourceId = hudState.SourceId;
            string subtitle = string.Empty;

            if (!string.IsNullOrWhiteSpace(hudState.PhaseLabel) && !string.IsNullOrWhiteSpace(hudState.PatternLabel))
            {
                subtitle = $"{hudState.PhaseLabel}  ·  {hudState.PatternLabel}";
            }
            else if (!string.IsNullOrWhiteSpace(hudState.PhaseLabel))
            {
                subtitle = hudState.PhaseLabel;
            }
            else if (!string.IsNullOrWhiteSpace(hudState.PatternLabel))
            {
                subtitle = hudState.PatternLabel;
            }

            Color phaseAccent = GetPhaseAccent(hudState.Phase);
            Color patternAccent = GetPatternAccent(hudState.Pattern);
            bool hasPattern = !string.IsNullOrWhiteSpace(hudState.PatternLabel);
            float patternBlend = hasPattern ? 0.34f : 0f;
            float transitionBoost = hudState.IsPhaseTransitioning ? 0.28f : 0f;

            Color backgroundAccent = Color.Lerp(new Color(0.12f, 0.06f, 0.08f, 0.94f), phaseAccent, 0.24f + transitionBoost);
            Color fillAccent = Color.Lerp(phaseAccent, patternAccent, patternBlend + transitionBoost);
            Color nameAccent = Color.Lerp(new Color(1f, 0.96f, 0.92f, 1f), phaseAccent, 0.22f + (transitionBoost * 0.4f));
            Color subtitleAccent = hasPattern
                ? Color.Lerp(phaseAccent, patternAccent, 0.68f + (transitionBoost * 0.4f))
                : Color.Lerp(new Color(1f, 0.9f, 0.66f, 1f), phaseAccent, 0.2f);
            string badgeText = ResolveBadgeText(hudState);
            string statusText = ResolveStatusText(hudState);

            hudController.ShowBoss(
                hudState.BossName,
                hudState.NormalizedHealth,
                subtitle,
                backgroundAccent,
                fillAccent,
                nameAccent,
                subtitleAccent,
                badgeText,
                statusText);
        }

        private void HandleBossHidden(int sourceId)
        {
            if (hudController == null)
            {
                return;
            }

            if (_activeBossSourceId != -1 && sourceId != _activeBossSourceId)
            {
                return;
            }

            _activeBossSourceId = -1;
            hudController.HideBoss();
        }

        private void ResolveReferences()
        {
            if (hudController == null)
            {
                hudController = GetComponent<HUDController>();
            }

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<HUDController>(FindObjectsInactive.Exclude);
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private static Color GetPhaseAccent(CuteIssac.Enemy.BossPhaseType phase)
        {
            return phase switch
            {
                CuteIssac.Enemy.BossPhaseType.PhaseTwo => new Color(1f, 0.66f, 0.24f, 1f),
                CuteIssac.Enemy.BossPhaseType.PhaseThree => new Color(1f, 0.34f, 0.46f, 1f),
                _ => new Color(1f, 0.82f, 0.34f, 1f)
            };
        }

        private static Color GetPatternAccent(CuteIssac.Enemy.BossPatternType pattern)
        {
            return pattern switch
            {
                CuteIssac.Enemy.BossPatternType.Charge => new Color(1f, 0.3f, 0.3f, 1f),
                CuteIssac.Enemy.BossPatternType.Volley => new Color(0.42f, 0.92f, 1f, 1f),
                CuteIssac.Enemy.BossPatternType.Sweep => new Color(0.84f, 0.48f, 1f, 1f),
                CuteIssac.Enemy.BossPatternType.Spiral => new Color(0.58f, 1f, 0.42f, 1f),
                CuteIssac.Enemy.BossPatternType.Fan => new Color(1f, 0.7f, 0.26f, 1f),
                CuteIssac.Enemy.BossPatternType.Shockwave => new Color(1f, 0.42f, 0.22f, 1f),
                CuteIssac.Enemy.BossPatternType.Crossfire => new Color(1f, 0.54f, 0.3f, 1f),
                _ => new Color(1f, 0.76f, 0.32f, 1f)
            };
        }

        private static string ResolveBadgeText(BossHudState hudState)
        {
            if (hudState.IsPhaseTransitioning)
            {
                return "<b>페이즈 전환</b>";
            }

            return hudState.Phase switch
            {
                CuteIssac.Enemy.BossPhaseType.PhaseThree => "<b>최종 국면</b>",
                CuteIssac.Enemy.BossPhaseType.PhaseTwo => "<b>격화 국면</b>",
                _ => "<b>보스전</b>"
            };
        }

        private static string ResolveStatusText(BossHudState hudState)
        {
            if (hudState.IsPhaseTransitioning)
            {
                return "<size=14>상태</size>\n<b>변이</b>";
            }

            return hudState.Phase switch
            {
                CuteIssac.Enemy.BossPhaseType.PhaseThree => "<size=14>국면</size>\n<b>최종</b>",
                CuteIssac.Enemy.BossPhaseType.PhaseTwo => "<size=14>국면</size>\n<b>격화</b>",
                _ => "<size=14>국면</size>\n<b>개시</b>"
            };
        }
    }
}
