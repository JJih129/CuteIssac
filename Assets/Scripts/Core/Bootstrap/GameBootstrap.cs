using CuteIssac.Core.Run;
using CuteIssac.Data.Run;
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
        [SerializeField] private RunConfiguration startupRunConfiguration;

        [Header("Startup")]
        [SerializeField] private bool bootstrapOnAwake = true;
        [SerializeField] private bool autoStartRunOnAwake = true;

        private bool _hasBootstrapped;

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
            runManager.Bootstrap(startupRunConfiguration);

            if (autoStartRunOnAwake)
            {
                runManager.StartNewRun();
            }
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

            Debug.LogError("GameBootstrap requires a RunManager reference on the same object or in the inspector.", this);
            return false;
        }
    }
}
