using CuteIssac.Common.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CuteIssac.Core.Input
{
    /// <summary>
    /// Reads the project's Input System action asset and exposes a compact gameplay-facing interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputSystemPlayerInputReader : MonoBehaviour, IPlayerInputReader
    {
        private const string FallbackInputActionsJson = "{\n" +
            "  \"name\": \"EmbeddedPlayerInput\",\n" +
            "  \"maps\": [\n" +
            "    {\n" +
            "      \"name\": \"Player\",\n" +
            "      \"id\": \"96fd18d4-4667-4f9f-a451-fb5d7247bba2\",\n" +
            "      \"actions\": [\n" +
            "        { \"name\": \"Move\", \"type\": \"Value\", \"id\": \"d7f957c9-b034-42d6-aa8f-36a087b93c0d\", \"expectedControlType\": \"Vector2\", \"processors\": \"\", \"interactions\": \"\", \"initialStateCheck\": true },\n" +
            "        { \"name\": \"Aim\", \"type\": \"Value\", \"id\": \"6dd16b54-ac8e-4c64-ac35-cba5cdbba93d\", \"expectedControlType\": \"Vector2\", \"processors\": \"\", \"interactions\": \"\", \"initialStateCheck\": true },\n" +
            "        { \"name\": \"Bomb\", \"type\": \"Button\", \"id\": \"01ce467b-58cd-4242-9a8d-8bc10f5f73e0\", \"expectedControlType\": \"Button\", \"processors\": \"\", \"interactions\": \"\", \"initialStateCheck\": false },\n" +
            "        { \"name\": \"ActiveItem\", \"type\": \"Button\", \"id\": \"f317e723-592c-4b0e-b67a-e0cc1c9a5f74\", \"expectedControlType\": \"Button\", \"processors\": \"\", \"interactions\": \"\", \"initialStateCheck\": false },\n" +
            "        { \"name\": \"ToggleMinimap\", \"type\": \"Button\", \"id\": \"8a80785a-b0da-4ed8-9649-c936669ebd17\", \"expectedControlType\": \"Button\", \"processors\": \"\", \"interactions\": \"\", \"initialStateCheck\": false }\n" +
            "      ],\n" +
            "      \"bindings\": [\n" +
            "        { \"name\": \"\", \"id\": \"0e4e45fa-4461-4628-8706-fcd40fb18631\", \"path\": \"<Gamepad>/leftStick\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Gamepad\", \"action\": \"Move\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"WASD\", \"id\": \"2dd1d719-57f7-4fc0-bfcd-ee46adb3e31f\", \"path\": \"2DVector\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"\", \"action\": \"Move\", \"isComposite\": true, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"up\", \"id\": \"5346ef4a-31bd-4c62-b2d8-d0a9c7554149\", \"path\": \"<Keyboard>/w\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Move\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"down\", \"id\": \"0f8f6f50-f8b5-49a8-8f67-c31cad4d8648\", \"path\": \"<Keyboard>/s\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Move\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"left\", \"id\": \"de320f50-8ca7-479f-a0b0-d332c75b9f3f\", \"path\": \"<Keyboard>/a\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Move\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"right\", \"id\": \"f95b3fb7-2a4a-4f72-ac4e-a1b3abf3ae1c\", \"path\": \"<Keyboard>/d\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Move\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"\", \"id\": \"48aa6f78-c772-4382-86c2-cc11f22e93c3\", \"path\": \"<Gamepad>/rightStick\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Gamepad\", \"action\": \"Aim\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"ArrowKeys\", \"id\": \"c3f2cb98-2df8-40ef-93d0-3a5d478eaafd\", \"path\": \"2DVector\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"\", \"action\": \"Aim\", \"isComposite\": true, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"up\", \"id\": \"936bc09a-d855-4ba2-b55c-5b2ae96e38c4\", \"path\": \"<Keyboard>/upArrow\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Aim\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"down\", \"id\": \"b9ed4846-379e-44f5-8eda-a8185bc7dfe7\", \"path\": \"<Keyboard>/downArrow\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Aim\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"left\", \"id\": \"807174ef-4de9-4f91-bd50-7000f60ee3fe\", \"path\": \"<Keyboard>/leftArrow\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Aim\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"right\", \"id\": \"91a7a94d-0b69-4937-af4e-bb36af0035c5\", \"path\": \"<Keyboard>/rightArrow\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Aim\", \"isComposite\": false, \"isPartOfComposite\": true },\n" +
            "        { \"name\": \"\", \"id\": \"9034f4e4-ae23-4745-b4d4-da4084b3040b\", \"path\": \"<Keyboard>/space\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"Bomb\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"\", \"id\": \"5da1a1ea-f22f-4d4c-abca-bba2dcf2316e\", \"path\": \"<Gamepad>/buttonSouth\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Gamepad\", \"action\": \"Bomb\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"\", \"id\": \"b8eb44e0-3c14-4d23-b8d3-44fdb7171242\", \"path\": \"<Keyboard>/leftShift\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"ActiveItem\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"\", \"id\": \"ff845a52-c93d-46a3-ad39-90e5de91bc9a\", \"path\": \"<Keyboard>/e\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"ActiveItem\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"\", \"id\": \"58fb0e27-f405-4ff0-82fe-a27d8303488a\", \"path\": \"<Gamepad>/leftShoulder\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Gamepad\", \"action\": \"ActiveItem\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"\", \"id\": \"f4c69bc2-0b68-4c9f-89d2-96fb9466580a\", \"path\": \"<Keyboard>/tab\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Keyboard&Mouse\", \"action\": \"ToggleMinimap\", \"isComposite\": false, \"isPartOfComposite\": false },\n" +
            "        { \"name\": \"\", \"id\": \"b1985a1d-9337-4b6d-97d8-c7b6614f4c09\", \"path\": \"<Gamepad>/select\", \"interactions\": \"\", \"processors\": \"\", \"groups\": \"Gamepad\", \"action\": \"ToggleMinimap\", \"isComposite\": false, \"isPartOfComposite\": false }\n" +
            "      ]\n" +
            "    }\n" +
            "  ],\n" +
            "  \"controlSchemes\": []\n" +
            "}";
        private const string PlayerMapName = "Player";
        private const string MoveActionName = "Move";
        private const string AimActionName = "Aim";
        private const string BombActionName = "Bomb";
        private const string ActiveItemActionName = "ActiveItem";
        private const string ToggleMinimapActionName = "ToggleMinimap";

        [Header("Input Asset")]
        [SerializeField] private InputActionAsset inputActionsAsset;
        [SerializeField] private bool useEmbeddedFallbackWhenAssetMissing = true;

        private InputActionAsset _runtimeInputActions;
        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _bombAction;
        private InputAction _activeItemAction;
        private InputAction _toggleMinimapAction;
        private bool _initializationFailed;
        private bool _warnedMissingAsset;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            _playerMap?.Enable();
        }

        private void OnDisable()
        {
            _playerMap?.Disable();
        }

        private void OnDestroy()
        {
            if (_runtimeInputActions != null)
            {
                Destroy(_runtimeInputActions);
            }
        }

        public PlayerGameplayInputState ReadState()
        {
            if (!EnsureInitialized())
            {
                return default;
            }

            Vector2 move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 aim = _aimAction?.ReadValue<Vector2>() ?? Vector2.zero;
            bool hasAimInput = aim.sqrMagnitude > 0.0001f;

            if (!hasAimInput)
            {
                aim = Vector2.zero;
            }

            return new PlayerGameplayInputState(
                move,
                aim,
                hasAimInput,
                WasPressedThisFrame(_bombAction),
                WasPressedThisFrame(_activeItemAction),
                WasPressedThisFrame(_toggleMinimapAction));
        }

        [ContextMenu("Enable Gameplay Input")]
        public void EnableGameplayInput()
        {
            if (EnsureInitialized())
            {
                _playerMap.Enable();
            }
        }

        [ContextMenu("Disable Gameplay Input")]
        public void DisableGameplayInput()
        {
            _playerMap?.Disable();
        }

        private bool EnsureInitialized()
        {
            if (_initializationFailed)
            {
                return false;
            }

            if (_runtimeInputActions == null)
            {
                Initialize();
            }

            return _runtimeInputActions != null;
        }

        private void Initialize()
        {
            if (_runtimeInputActions != null || _initializationFailed)
            {
                return;
            }

            try
            {
                if (inputActionsAsset != null)
                {
                    // Clone the asset so scene-local enable/disable calls do not mutate the shared project asset state.
                    _runtimeInputActions = Instantiate(inputActionsAsset);
                }
                else if (useEmbeddedFallbackWhenAssetMissing)
                {
                    _runtimeInputActions = InputActionAsset.FromJson(FallbackInputActionsJson);

                    if (!_warnedMissingAsset)
                    {
                        UnityEngine.Debug.Log(
                            "InputSystemPlayerInputReader is using its embedded fallback bindings because no InputActionAsset is assigned.",
                            this);
                        _warnedMissingAsset = true;
                    }
                }
                else
                {
                    FailInitialization("InputSystemPlayerInputReader requires an InputActionAsset reference.", null);
                    return;
                }

                _playerMap = _runtimeInputActions.FindActionMap(PlayerMapName, true);
                _moveAction = _playerMap.FindAction(MoveActionName, true);
                _aimAction = _playerMap.FindAction(AimActionName, true);
                _bombAction = _playerMap.FindAction(BombActionName, true);
                _activeItemAction = _playerMap.FindAction(ActiveItemActionName, true);
                _toggleMinimapAction = _playerMap.FindAction(ToggleMinimapActionName, true);

                if (isActiveAndEnabled)
                {
                    _playerMap.Enable();
                }
            }
            catch (Exception exception)
            {
                FailInitialization("InputSystemPlayerInputReader failed to initialize gameplay input.", exception);
            }
        }

        private static bool WasPressedThisFrame(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

        private void FailInitialization(string message, Exception exception)
        {
            _initializationFailed = true;

            if (exception == null)
            {
                UnityEngine.Debug.LogError(message, this);
            }
            else
            {
                UnityEngine.Debug.LogError($"{message}\n{exception}", this);
            }

            enabled = false;
        }
    }
}
