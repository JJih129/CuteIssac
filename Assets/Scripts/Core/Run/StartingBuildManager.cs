using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Data.Run;
using CuteIssac.Player;
using CuteIssac.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Handles starting build selection and applies the selected loadout when a run begins.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StartingBuildManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunManager runManager;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private StartingBuildPanelView panelView;

        [Header("Catalog")]
        [SerializeField] private StartingBuildCatalog startingBuildCatalog;
        [SerializeField] private string resourcesCatalogPath = "StartingBuilds/DefaultStartingBuildCatalog";
        [SerializeField] private bool requireSelectionOnStartup = true;

        private readonly List<StatModifier> _buildStatModifierBuffer = new();
        private readonly List<ProjectileModifier> _buildProjectileModifierBuffer = new();
        private Action _pendingStartupAction;
        private StartingBuildData _selectedBuild;
        private bool _suppressNextRunStartLoadout;

        public StartingBuildData SelectedBuild => _selectedBuild;

        private void Awake()
        {
            ResolveReferences();
            ResolveCatalog();
            ResolveDefaultBuild();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveCatalog();
            ResolveDefaultBuild();

            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
            }
        }

        private void OnDisable()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
            }
        }

        public bool TryBeginStartupSelection(Action onComplete)
        {
            ResolveCatalog();
            ResolveDefaultBuild();

            if (startingBuildCatalog == null || CountValidBuilds() == 0)
            {
                onComplete?.Invoke();
                return false;
            }

            if (!requireSelectionOnStartup || CountValidBuilds() == 1)
            {
                onComplete?.Invoke();
                return false;
            }

            _pendingStartupAction = onComplete;
            ShowSelectionPanel();
            return true;
        }

        private void HandleRunStarted(RunContext _)
        {
            if (_suppressNextRunStartLoadout)
            {
                _suppressNextRunStartLoadout = false;
                return;
            }

            ApplySelectedBuildToPlayer();
        }

        public void SuppressNextRunStartLoadout()
        {
            _suppressNextRunStartLoadout = true;
        }

        private void ApplySelectedBuildToPlayer()
        {
            ResolveReferences();
            ResolveDefaultBuild();

            if (_selectedBuild == null)
            {
                return;
            }

            playerInventory?.ApplyStartingLoadout(
                _selectedBuild.StartingCoins,
                Mathf.Max(1, _selectedBuild.StartingKeys),
                _selectedBuild.StartingBombs,
                _selectedBuild.StartingPassiveItems);

            _buildStatModifierBuffer.Clear();
            _buildProjectileModifierBuffer.Clear();
            CopyStatModifiers(_selectedBuild.StatModifiers, _buildStatModifierBuffer);
            CopyProjectileModifiers(_selectedBuild.ProjectileModifiers, _buildProjectileModifierBuffer);
            playerStats?.SetStartingBuildModifiers(_buildStatModifierBuffer, _buildProjectileModifierBuffer);
            playerHealth?.RestoreToFull();
        }

        private void ShowSelectionPanel()
        {
            ResolvePanelView();

            if (panelView == null || startingBuildCatalog == null)
            {
                _pendingStartupAction?.Invoke();
                _pendingStartupAction = null;
                return;
            }

            panelView.Present(startingBuildCatalog.Builds, _selectedBuild, HandleBuildSelected);
        }

        private void HandleBuildSelected(StartingBuildData buildData)
        {
            _selectedBuild = buildData != null ? buildData : _selectedBuild;
            panelView?.Hide();

            Action pendingAction = _pendingStartupAction;
            _pendingStartupAction = null;
            pendingAction?.Invoke();
        }

        private void ResolveReferences()
        {
            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }
        }

        private void ResolveCatalog()
        {
            if (startingBuildCatalog == null && !string.IsNullOrWhiteSpace(resourcesCatalogPath))
            {
                startingBuildCatalog = Resources.Load<StartingBuildCatalog>(resourcesCatalogPath);
            }
        }

        private void ResolveDefaultBuild()
        {
            if (_selectedBuild == null && startingBuildCatalog != null)
            {
                _selectedBuild = startingBuildCatalog.DefaultBuild;
            }
        }

        private void ResolvePanelView()
        {
            if (panelView != null)
            {
                return;
            }

            panelView = FindFirstObjectByType<StartingBuildPanelView>(FindObjectsInactive.Include);

            if (panelView != null)
            {
                return;
            }

            InputSystemEventSystemBootstrap.EnsureReady();
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

            if (canvas == null)
            {
                GameObject canvasObject = new("StartingBuildCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            panelView = StartingBuildPanelView.CreateRuntime(canvas);
        }

        private int CountValidBuilds()
        {
            if (startingBuildCatalog == null)
            {
                return 0;
            }

            int count = 0;

            for (int index = 0; index < startingBuildCatalog.Builds.Count; index++)
            {
                if (startingBuildCatalog.Builds[index] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void CopyStatModifiers(IReadOnlyList<StatModifier> source, List<StatModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }

        private static void CopyProjectileModifiers(IReadOnlyList<ProjectileModifier> source, List<ProjectileModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }
    }
}
