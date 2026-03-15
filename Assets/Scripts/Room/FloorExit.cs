using System;
using CuteIssac.Common.Input;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Input;
using CuteIssac.Core.Run;
using UnityEngine;

namespace CuteIssac.Room
{
    [DisallowMultipleComponent]
    public sealed class FloorExit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Collider2D interactionTrigger;
        [SerializeField] private MonoBehaviour inputReaderSource;
        [SerializeField] private FloorExitVisual floorExitVisual;

        [Header("Behavior")]
        [SerializeField] [Min(0.5f)] private float interactionDistance = 1.8f;

        public event Action<FloorExit> Activated;

        public int TargetFloorIndex { get; private set; }

        private IPlayerInputReader _inputReader;
        private RunManager _runManager;
        private Transform _playerTransform;
        private bool _isActivated;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (_isActivated || _inputReader == null)
            {
                floorExitVisual?.SetPromptVisible(false);
                return;
            }

            if (_playerTransform == null)
            {
                floorExitVisual?.SetPromptVisible(false);
                return;
            }

            bool inRange = Vector2.Distance(_playerTransform.position, transform.position) <= interactionDistance;
            floorExitVisual?.SetPromptVisible(inRange);

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
            floorExitVisual?.SetWorldPromptEnabled(true);
            floorExitVisual?.Configure(targetFloorIndex, accentColor);
            floorExitVisual?.SetPromptVisible(false);
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
        }

        private void TryActivate()
        {
            if (_isActivated || _runManager == null || !_runManager.CurrentContext.HasActiveRun)
            {
                return;
            }

            _isActivated = true;
            floorExitVisual?.PlayActivateFeedback();
            Activated?.Invoke(this);
        }
    }
}
