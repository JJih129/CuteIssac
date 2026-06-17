using System;
using CuteIssac.Common.Input;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Input;
using CuteIssac.Core.Run;
using CuteIssac.Core.Scene;
using UnityEngine;

namespace CuteIssac.Room
{
    [DisallowMultipleComponent]
    public sealed class FloorExit : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("씬에 배치된 GameplaySceneContext입니다. 비워두면 Active Context를 먼저 사용하고, 마지막에만 씬 검색으로 보정합니다.")]
        [SerializeField] private GameplaySceneContext sceneContext;
        [SerializeField] private Collider2D interactionTrigger;
        [SerializeField] private MonoBehaviour inputReaderSource;
        [SerializeField] private FloorExitVisual floorExitVisual;

        [Header("Behavior")]
        [SerializeField] [Min(0.5f)] private float interactionDistance = 1.8f;
        [SerializeField] private bool activateOnTouch = true;
        [SerializeField] [Min(0f)] private float touchActivationDelay = 0.35f;

        public event Action<FloorExit> Activated;

        public int TargetFloorIndex { get; private set; }

        private IPlayerInputReader _inputReader;
        private RunManager _runManager;
        private Transform _playerTransform;
        private bool _isActivated;
        private float _activationReadyAt;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (_isActivated || (!activateOnTouch && _inputReader == null))
            {
                floorExitVisual?.SetPromptVisible(false);
                return;
            }

            if (_playerTransform == null)
            {
                floorExitVisual?.SetPromptVisible(false);
                return;
            }

            float interactionDistanceSqr = interactionDistance * interactionDistance;
            bool inRange = ((Vector2)_playerTransform.position - (Vector2)transform.position).sqrMagnitude <= interactionDistanceSqr;
            floorExitVisual?.SetPromptVisible(!activateOnTouch && inRange);

            if (activateOnTouch)
            {
                if (inRange && Time.time >= _activationReadyAt)
                {
                    TryActivate();
                }

                return;
            }

            if (!inRange || !_inputReader.ReadState().ActiveItemPressed)
            {
                return;
            }

            TryActivate();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBindPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBindPlayer(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_playerTransform == null || other.transform.root != _playerTransform)
            {
                return;
            }

            _playerTransform = null;
        }

        public void Configure(RunManager runManager, int targetFloorIndex, Color accentColor)
        {
            ResolveReferences();
            _runManager = runManager;
            TargetFloorIndex = targetFloorIndex;
            _isActivated = false;
            _activationReadyAt = Time.time + touchActivationDelay;
            _playerTransform = ResolveActivePlayerTransform();
            floorExitVisual?.SetWorldPromptEnabled(true);
            floorExitVisual?.Configure(targetFloorIndex, accentColor);
            floorExitVisual?.SetPromptVisible(false);
            GetOrAddGuidanceBeacon().ShowPortalGuidance(accentColor, 0f);
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                transform.position + Vector3.up * 1.2f,
                $"FLOOR {targetFloorIndex} PORTAL",
                accentColor,
                1.7f,
                1.05f,
                1.4f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private void ResolveReferences()
        {
            ResolveReferencesFromSceneContext();

            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<Collider2D>();
            }

            if (interactionTrigger == null)
            {
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector2(2f, 1.4f);
                interactionTrigger = boxCollider;
            }

            if (inputReaderSource == null)
            {
                inputReaderSource = FindFirstObjectByType<InputSystemPlayerInputReader>(FindObjectsInactive.Exclude);
            }

            _inputReader = inputReaderSource as IPlayerInputReader;

            if (floorExitVisual == null)
            {
                floorExitVisual = GetComponent<FloorExitVisual>();
            }

            if (floorExitVisual == null)
            {
                floorExitVisual = gameObject.AddComponent<FloorExitVisual>();
            }
        }

        private void TryBindPlayer(Collider2D other)
        {
            Player.PlayerController playerController = other.GetComponentInParent<Player.PlayerController>();

            if (playerController == null)
            {
                return;
            }

            _playerTransform = playerController.transform;

            if (activateOnTouch && Time.time >= _activationReadyAt)
            {
                TryActivate();
            }
        }

        private void TryActivate()
        {
            if (_isActivated || _runManager == null || !_runManager.CurrentContext.HasActiveRun)
            {
                return;
            }

            _isActivated = true;
            GetOrAddGuidanceBeacon().Hide();
            floorExitVisual?.PlayActivateFeedback();
            Activated?.Invoke(this);
        }

        private TraversalGuidanceBeacon GetOrAddGuidanceBeacon()
        {
            TraversalGuidanceBeacon beacon = GetComponent<TraversalGuidanceBeacon>();

            if (beacon == null)
            {
                beacon = gameObject.AddComponent<TraversalGuidanceBeacon>();
            }

            return beacon;
        }

        private Transform ResolveActivePlayerTransform()
        {
            ResolveReferencesFromSceneContext();

            if (_playerTransform != null)
            {
                return _playerTransform;
            }

            Player.PlayerController playerController = Player.PlayerRegistry.ActiveController != null
                ? Player.PlayerRegistry.ActiveController
                : FindFirstObjectByType<Player.PlayerController>(FindObjectsInactive.Exclude);

            return playerController != null ? playerController.transform : null;
        }

        private void ResolveReferencesFromSceneContext()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext == null)
            {
                return;
            }

            sceneContext.ResolveMissingReferences();

            if (inputReaderSource == null)
            {
                inputReaderSource = sceneContext.PlayerInputReader;
            }

            if (_playerTransform == null && sceneContext.PlayerController != null)
            {
                _playerTransform = sceneContext.PlayerController.transform;
            }
        }
    }
}
