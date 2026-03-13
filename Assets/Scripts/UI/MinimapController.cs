using System.Collections.Generic;
using CuteIssac.Common.Input;
using CuteIssac.Dungeon;
using UnityEngine;

namespace CuteIssac.UI
{
    /// <summary>
    /// Bridges generated dungeon exploration data to the minimap view.
    /// The controller listens to dungeon/navigation events and forwards pure room snapshots to the UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapController : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Optional. Generated dungeon source. If empty, the controller searches the scene.")]
        [SerializeField] private DungeonInstantiator dungeonInstantiator;
        [Tooltip("Optional. Room navigation source. If empty, the controller searches the scene.")]
        [SerializeField] private RoomNavigationController roomNavigationController;
        [Tooltip("Optional. Minimap panel view. If empty, the controller searches the scene.")]
        [SerializeField] private MinimapPanelView minimapPanelView;
        [Tooltip("Optional. Gameplay input reader used for the Tab minimap toggle.")]
        [SerializeField] private MonoBehaviour inputReaderSource;

        [Header("Behavior")]
        [SerializeField] private bool minimapStartsVisible = true;
        [SerializeField] private bool listenForToggleInput = true;

        private readonly DungeonExplorationTracker _explorationTracker = new();
        private readonly List<MinimapRoomViewData> _roomBuffer = new();
        private IPlayerInputReader _inputReader;
        private bool _isMinimapVisible = true;
        private bool _warnedMissingInputReader;

        private void Awake()
        {
            ResolveReferences();
            _isMinimapVisible = minimapStartsVisible;
            minimapPanelView?.SetVisible(_isMinimapVisible);
        }

        private void OnEnable()
        {
            ResolveReferences();
            _explorationTracker.Changed += HandleExplorationChanged;

            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
                dungeonInstantiator.DungeonInstantiated += HandleDungeonInstantiated;
            }
        }

        private void Start()
        {
            ResolveReferences();
            minimapPanelView?.SetVisible(_isMinimapVisible);

            if (dungeonInstantiator != null && dungeonInstantiator.CurrentInstance != null)
            {
                ConnectDungeon(dungeonInstantiator.CurrentInstance);
            }
            else
            {
                minimapPanelView?.ShowPlaceholder();
            }
        }

        private void Update()
        {
            if (!listenForToggleInput)
            {
                return;
            }

            if (_inputReader == null)
            {
                ResolveInputReader();
            }

            if (_inputReader == null)
            {
                return;
            }

            if (_inputReader.ReadState().MinimapTogglePressed)
            {
                _isMinimapVisible = !_isMinimapVisible;
                minimapPanelView?.SetVisible(_isMinimapVisible);
            }
        }

        private void OnDisable()
        {
            if (dungeonInstantiator != null)
            {
                dungeonInstantiator.DungeonInstantiated -= HandleDungeonInstantiated;
            }

            _explorationTracker.Changed -= HandleExplorationChanged;
            _explorationTracker.Reset();
        }

        private void ResolveReferences()
        {
            if (dungeonInstantiator == null)
            {
                dungeonInstantiator = FindFirstObjectByType<DungeonInstantiator>(FindObjectsInactive.Exclude);
            }

            if (roomNavigationController == null)
            {
                roomNavigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);
            }

            if (minimapPanelView == null)
            {
                minimapPanelView = FindFirstObjectByType<MinimapPanelView>(FindObjectsInactive.Exclude);
            }

            ResolveInputReader();
        }

        private void ResolveInputReader()
        {
            if (inputReaderSource == null)
            {
                inputReaderSource = FindFirstObjectByType<CuteIssac.Core.Input.InputSystemPlayerInputReader>(FindObjectsInactive.Exclude);
            }

            _inputReader = inputReaderSource as IPlayerInputReader;

            if (_inputReader == null && inputReaderSource != null && !_warnedMissingInputReader)
            {
                Debug.LogWarning("MinimapController received an inputReaderSource that does not implement IPlayerInputReader.", this);
                _warnedMissingInputReader = true;
            }
        }

        private void HandleDungeonInstantiated(DungeonInstantiationResult instantiationResult)
        {
            ConnectDungeon(instantiationResult);
        }

        private void ConnectDungeon(DungeonInstantiationResult instantiationResult)
        {
            if (minimapPanelView == null)
            {
                return;
            }

            _explorationTracker.Initialize(instantiationResult, roomNavigationController);
            RenderMinimap();
            minimapPanelView.SetVisible(_isMinimapVisible);
        }

        private void HandleExplorationChanged()
        {
            RenderMinimap();
        }

        private void RenderMinimap()
        {
            if (minimapPanelView == null)
            {
                return;
            }

            _explorationTracker.BuildViewData(_roomBuffer);

            if (_roomBuffer.Count == 0)
            {
                minimapPanelView.ShowPlaceholder();
                return;
            }

            minimapPanelView.RenderRooms(_roomBuffer);
        }
    }
}
