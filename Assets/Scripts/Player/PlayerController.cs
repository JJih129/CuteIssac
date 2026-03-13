using CuteIssac.Common.Input;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Translates player input into movement commands.
    /// Gameplay decisions stay here while physics execution stays in PlayerMovement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private MonoBehaviour inputReaderSource;
        [SerializeField] private PlayerVisual playerVisual;

        private IPlayerInputReader _inputReader;

        private void Awake()
        {
            if (!TryResolvePlayerMovement() || !TryResolveInputReader())
            {
                enabled = false;
            }
        }

        private void Update()
        {
            PlayerGameplayInputState inputState = _inputReader.ReadState();
            playerMovement.SetMoveInput(inputState.Move);
            playerVisual?.SetMoveInput(inputState.Move);
        }

        private void OnDisable()
        {
            playerMovement?.Stop();
        }

        private bool TryResolvePlayerMovement()
        {
            if (playerMovement != null)
            {
                return true;
            }

            return TryGetComponent(out playerMovement);
        }

        private void Reset()
        {
            TryGetComponent(out playerMovement);
            TryGetComponent(out playerVisual);
        }

        private void OnValidate()
        {
            if (playerMovement == null)
            {
                TryGetComponent(out playerMovement);
            }

            if (playerVisual == null)
            {
                TryGetComponent(out playerVisual);
            }
        }

        private bool TryResolveInputReader()
        {
            if (inputReaderSource is IPlayerInputReader serializedReader)
            {
                _inputReader = serializedReader;
                return true;
            }

            // This scan only runs during initialization, so it keeps scene wiring flexible without frame-time cost.
            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < sceneBehaviours.Length; i++)
            {
                if (sceneBehaviours[i] is IPlayerInputReader sceneReader)
                {
                    _inputReader = sceneReader;
                    return true;
                }
            }

            Debug.LogError("PlayerController could not find an IPlayerInputReader in the scene.", this);
            return false;
        }
    }
}
