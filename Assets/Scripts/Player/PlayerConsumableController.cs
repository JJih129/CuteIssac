using CuteIssac.Common.Input;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Reads the shared gameplay input backend and requests consumable usage from the holder.
    /// This stays separate from PlayerController so movement logic and active-slot logic do not get mixed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerConsumableHolder))]
    public sealed class PlayerConsumableController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerConsumableHolder consumableHolder;
        [SerializeField] private PlayerActiveItemController activeItemController;
        [SerializeField] private MonoBehaviour inputReaderSource;

        private IPlayerInputReader _inputReader;

        private void Awake()
        {
            if (!TryResolveDependencies())
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (activeItemController != null && activeItemController.HasEquippedItem)
            {
                return;
            }

            if (_inputReader.ReadState().ActiveItemPressed)
            {
                consumableHolder.TryUseHeldConsumable();
            }
        }

        private bool TryResolveDependencies()
        {
            if (consumableHolder == null)
            {
                consumableHolder = GetComponent<PlayerConsumableHolder>();
            }

            if (activeItemController == null)
            {
                activeItemController = GetComponent<PlayerActiveItemController>();
            }

            if (consumableHolder == null)
            {
                Debug.LogError("PlayerConsumableController requires PlayerConsumableHolder.", this);
                return false;
            }

            if (inputReaderSource is IPlayerInputReader serializedReader)
            {
                _inputReader = serializedReader;
                return true;
            }

            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < sceneBehaviours.Length; i++)
            {
                if (sceneBehaviours[i] is IPlayerInputReader sceneReader)
                {
                    _inputReader = sceneReader;
                    return true;
                }
            }

            Debug.LogError("PlayerConsumableController could not find an IPlayerInputReader in the scene.", this);
            return false;
        }

        private void Reset()
        {
            consumableHolder = GetComponent<PlayerConsumableHolder>();
        }

        private void OnValidate()
        {
            if (consumableHolder == null)
            {
                consumableHolder = GetComponent<PlayerConsumableHolder>();
            }

            if (activeItemController == null)
            {
                activeItemController = GetComponent<PlayerActiveItemController>();
            }
        }
    }
}
