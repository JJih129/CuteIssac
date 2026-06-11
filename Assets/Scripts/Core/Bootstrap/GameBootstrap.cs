using CuteIssac.Core.Meta;
using CuteIssac.Core.Run;
using CuteIssac.Core.Save;
using CuteIssac.Core.Settings;
using CuteIssac.Core.Debug;
using CuteIssac.Item;
using UnityEngine;

namespace CuteIssac.Core.Bootstrap
{
    /// <summary>
    /// Scene entry point. Attach this to a root object such as "__App" and wire a RunManager.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private CuteIssac.Data.Run.RunConfiguration startupRunConfiguration;

        [Header("Startup")]
        [SerializeField] private bool bootstrapOnAwake = true;
        [SerializeField] private bool autoStartRunOnAwake = true;
        [SerializeField] private bool preferCharacterSelectionBeforeRunRestore = true;
        [SerializeField] [Min(0)] private int prewarmCoinPickupCount = 48;
        [SerializeField] [Min(0)] private int prewarmBombPickupCount = 8;
        [SerializeField] [Min(0)] private int prewarmAmmoPickupCount = 12;
        [SerializeField] [Min(0)] private int prewarmKeyPickupCount = 8;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool enableDevelopmentDebugController = true;
#endif

        private bool _hasBootstrapped;
        private bool _startPhaseReached;
        private bool _hasAutoStartedRun;

        private void Awake()
        {
            if (!bootstrapOnAwake)
            {
                return;
            }

            Bootstrap();
        }

        [ContextMenu("Bootstrap Game")]
        public void Bootstrap()
        {
            if (_hasBootstrapped)
            {
                return;
            }

            if (!TryResolveRunManager())
            {
                enabled = false;
                return;
            }

            _hasBootstrapped = true;
            EnsureFloorTransitionController();
            RuntimePickupFactory.PrewarmDefaultPickups(
                prewarmCoinPickupCount,
                prewarmBombPickupCount,
                prewarmAmmoPickupCount,
                prewarmKeyPickupCount);
            runManager.Bootstrap(startupRunConfiguration);

            if (_startPhaseReached)
            {
                TryAutoStartRun();
            }
        }

        private void Start()
        {
            _startPhaseReached = true;
            TryAutoStartRun();
        }

        private void TryAutoStartRun()
        {
            if (!autoStartRunOnAwake || _hasAutoStartedRun || !_hasBootstrapped || runManager == null)
            {
                return;
            }

            _hasAutoStartedRun = true;
            RunRestoreController runRestoreController = GetComponent<RunRestoreController>();
            CharacterProfileManager characterProfileManager = GetComponent<CharacterProfileManager>();

            if (RunLaunchRequest.ConsumeNewRunFromFirstFloorRequest())
            {
                // Title "new game" must not resume a stale run snapshot from a later floor.
                GetComponent<RunSaveSystem>()?.DeleteRunSave();
                characterProfileManager?.ConsumeLaunchCharacterIfAny();
                runManager.StartNewRunAtFloor(1);
                return;
            }

            if (!preferCharacterSelectionBeforeRunRestore && runRestoreController != null && runRestoreController.TryResumeLatestRun())
            {
                return;
            }

            characterProfileManager?.ConsumeLaunchCharacterIfAny();

            if (preferCharacterSelectionBeforeRunRestore && runRestoreController != null && runRestoreController.TryResumeLatestRun())
            {
                return;
            }

            runManager.StartNewRun();
        }

        private bool TryResolveRunManager()
        {
            if (runManager != null)
            {
                return true;
            }

            if (TryGetComponent(out runManager))
            {
                return true;
            }

            UnityEngine.Debug.LogError("GameBootstrap requires a RunManager reference on the same object or in the inspector.", this);
            return false;
        }

        private void EnsureFloorTransitionController()
        {
            if (GetComponent<FloorTransitionController>() == null)
            {
                gameObject.AddComponent<FloorTransitionController>();
            }

            if (GetComponent<RunItemPoolService>() == null)
            {
                gameObject.AddComponent<RunItemPoolService>();
            }

            if (GetComponent<UnlockManager>() == null)
            {
                gameObject.AddComponent<UnlockManager>();
            }

            if (GetComponent<CharacterProfileManager>() == null)
            {
                gameObject.AddComponent<CharacterProfileManager>();
            }

            if (GetComponent<MetaProgressionManager>() == null)
            {
                gameObject.AddComponent<MetaProgressionManager>();
            }

            if (GetComponent<GameOptionsService>() == null)
            {
                gameObject.AddComponent<GameOptionsService>();
            }

            if (GetComponent<RunSaveSystem>() == null)
            {
                gameObject.AddComponent<RunSaveSystem>();
            }

            if (GetComponent<GameSaveSystem>() == null)
            {
                gameObject.AddComponent<GameSaveSystem>();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enableDevelopmentDebugController && GetComponent<DevelopmentDebugController>() == null)
            {
                gameObject.AddComponent<DevelopmentDebugController>();
            }
#endif

            if (GetComponent<RunRestoreController>() == null)
            {
                gameObject.AddComponent<RunRestoreController>();
            }
        }
    }
}
