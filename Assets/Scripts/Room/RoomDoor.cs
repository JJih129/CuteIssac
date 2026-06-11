using CuteIssac.Player;
using CuteIssac.Dungeon;
using CuteIssac.Core.Feedback;
using CuteIssac.Data.Dungeon;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Door endpoint controlled by a room.
    /// It knows whether passage is currently allowed and optionally references the next room for future dungeon wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomDoor : MonoBehaviour
    {
        [Header("Room Link")]
        [SerializeField] private RoomController ownerRoom;
        [SerializeField] private RoomDirection doorDirection = RoomDirection.Up;
        [SerializeField] private RoomController connectedRoom;
        [SerializeField] private RoomDoor connectedDoor;
        [SerializeField] private Transform arrivalPoint;
        [SerializeField] [Min(0f)] private float arrivalInsetDistance = 1.1f;

        [Header("Blocking")]
        [SerializeField] private Collider2D[] blockingColliders;
        [SerializeField] private Collider2D passageTrigger;
        [SerializeField] private bool matchBlockingColliderShapeToPassageTrigger = true;

        [Header("Visuals")]
        [SerializeField] private GameObject[] lockedStateObjects;
        [SerializeField] private GameObject[] unlockedStateObjects;
        [SerializeField] private GameObject[] secretHintStateObjects;
        [SerializeField] private SpriteRenderer[] secretHintTintTargets;
        [SerializeField] private Color secretHintColor = new(0.78f, 0.72f, 0.62f, 0.24f);
        [SerializeField] [Min(0f)] private float secretHintPulseAmplitude = 0.08f;
        [SerializeField] [Min(0.05f)] private float secretHintPulseSpeed = 1.6f;
        [SerializeField] private GameObject[] healthCostWarningStateObjects;
        [SerializeField] private SpriteRenderer[] healthCostTintTargets;
        [SerializeField] private Transform healthCostPromptRoot;
        [SerializeField] private TextMesh healthCostPromptText;
        [SerializeField] private bool showWorldHealthCostPrompt;
        [SerializeField] private Color healthCostWarningColor = new(0.96f, 0.34f, 0.48f, 0.92f);
        [SerializeField] [Min(0f)] private float healthCostPulseAmplitude = 0.11f;
        [SerializeField] [Min(0.05f)] private float healthCostPulseSpeed = 2.3f;
        [SerializeField] private Vector3 healthCostPromptLocalOffset = new(0f, 1.25f, 0f);
        [SerializeField] [Min(0.05f)] private float healthCostPromptCharacterSize = 0.18f;
        [SerializeField] [Min(1)] private int healthCostPromptFontSize = 64;
        [SerializeField] private Transform keyCostPromptRoot;
        [SerializeField] private TextMesh keyCostPromptText;
        [SerializeField] private bool showWorldKeyCostPrompt = true;
        [SerializeField] private Color keyCostWarningColor = new(0.45f, 0.82f, 1f, 0.96f);
        [SerializeField] [Min(0f)] private float keyCostPulseAmplitude = 0.08f;
        [SerializeField] [Min(0.05f)] private float keyCostPulseSpeed = 2.1f;
        [SerializeField] private Vector3 keyCostPromptLocalOffset = new(0f, 0.95f, 0f);
        [SerializeField] [Min(0.05f)] private float keyCostPromptCharacterSize = 0.16f;
        [SerializeField] [Min(1)] private int keyCostPromptFontSize = 64;

        [Header("Entry Cost")]
        [SerializeField] [Min(0)] private int requiredKeysToEnter;
        [SerializeField] private bool consumeKeysOnFirstEntry = true;
        [SerializeField] [Min(0f)] private float requiredHealthToEnter;
        [SerializeField] private bool consumeHealthOnFirstEntry = true;
        [SerializeField] private bool denyLethalHealthEntry = true;
        [SerializeField] [Min(0f)] private float deniedEntryFeedbackCooldown = 0.4f;

        public RoomController OwnerRoom => ownerRoom;
        public RoomDirection DoorDirection => doorDirection;
        public RoomController ConnectedRoom => connectedRoom;
        public RoomDoor ConnectedDoor => connectedDoor;
        public bool IsLocked { get; private set; }
        public bool RequiresReveal => _requiresReveal;
        public bool IsRevealed => !_requiresReveal || _isRevealed;
        public bool HasUnrevealedSecretAccess => _isAvailable && _requiresReveal && !_isRevealed && connectedRoom != null;
        public bool HasUnpaidKeyEntryCost => HasPendingKeyEntryCost();
        public bool HasUnpaidHealthEntryCost => HasPendingHealthEntryCost();
        public int RequiredKeysToEnter => GetRequiredKeyCost();
        public float RequiredHealthToEnter => GetRequiredHealthCost();
        public Color HealthEntryWarningColor => healthCostWarningColor;

        private bool _isAvailable = true;
        private bool _combatLocked;
        private bool _requiresReveal;
        private bool _isRevealed = true;
        private bool _entryCostPaid;
        private bool _healthEntryCostPaid;
        private float _lastDeniedFeedbackTime = float.NegativeInfinity;
        private SpriteRenderer[] _resolvedSecretHintTintTargets;
        private Color[] _resolvedSecretHintBaseColors;
        private SpriteRenderer[] _resolvedHealthCostTintTargets;
        private Color[] _resolvedHealthCostBaseColors;
        private bool _isShowingSecretHint;
        private bool _isShowingKeyCostWarning;
        private bool _isShowingHealthCostWarning;
        private Vector3 _keyCostPromptBaseScale = Vector3.one;
        private Vector3 _healthCostPromptBaseScale = Vector3.one;
        private int _lastDisplayedKeyCost = int.MinValue;
        private int _lastDisplayedHealthCost = int.MinValue;
        private GameObject[] _runtimeLockedStateObjects = Array.Empty<GameObject>();
        private GameObject[] _runtimeUnlockedStateObjects = Array.Empty<GameObject>();
        private PlayerController _scenePlayerController;
        private PlayerItemManager _scenePlayerItemManager;
        private PlayerInventory _scenePlayerInventory;
        private PlayerHealth _scenePlayerHealth;
        private bool _hasCachedPlayerComponents;
        private RoomNavigationController _navigationController;

        private void Awake()
        {
            ValidateDirectionFromTransform(false);
            ResolveReferences();
        }

        private void Reset()
        {
            ValidateDirectionFromTransform(false);
            ResolveReferences();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = ResolvePlayerControllerFromCollider(other);

            if (playerController == null)
            {
                return;
            }

            TryEnter(playerController);
        }

        private static PlayerController ResolvePlayerControllerFromCollider(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            PlayerController playerController = PlayerRegistry.ActiveController;
            if (playerController == null)
            {
                return other.GetComponentInParent<PlayerController>();
            }

            Transform playerTransform = playerController.transform;
            Transform otherTransform = other.transform;
            return otherTransform == playerTransform || otherTransform.IsChildOf(playerTransform)
                ? playerController
                : null;
        }

        private void Update()
        {
            bool shouldShowKeyCostWarning = !_combatLocked
                && _isAvailable
                && (!_requiresReveal || _isRevealed)
                && HasPendingKeyEntryCost();
            bool shouldShowHealthCostWarning = !_combatLocked
                && _isAvailable
                && (!_requiresReveal || _isRevealed)
                && HasPendingHealthEntryCost();

            if (shouldShowKeyCostWarning != _isShowingKeyCostWarning
                || shouldShowHealthCostWarning != _isShowingHealthCostWarning)
            {
                RefreshDoorState();
            }

            if (_isShowingSecretHint)
            {
                ApplySecretHintColors(true);
            }

            if (_isShowingHealthCostWarning)
            {
                UpdateHealthCostPromptText();
                ApplyHealthCostWarningVisuals(true);
            }

            if (_isShowingKeyCostWarning)
            {
                UpdateKeyCostPromptText();
                ApplyKeyCostWarningVisuals(true);
            }
        }

        [ContextMenu("Lock")]
        public void Lock()
        {
            _combatLocked = true;
            RefreshDoorState();
        }

        [ContextMenu("Unlock")]
        public void Unlock()
        {
            _combatLocked = false;
            RefreshDoorState();
        }

        /// <summary>
        /// Called when a player attempts to traverse the door.
        /// Current prototype delegates room traversal to RoomNavigationController so manual layouts and future generators use one path.
        /// </summary>
        public bool TryEnter(PlayerController playerController)
        {
            if (playerController == null)
            {
                return false;
            }

            if (IsLocked)
            {
                ShowTraversalBlockedFeedback(playerController, "ENTRY LOCKED");
                return false;
            }

            if (!CanPayEntryCost(playerController))
            {
                ShowDeniedEntryFeedback(playerController);
                return false;
            }

            if (!CanPayHealthEntryCost(playerController))
            {
                ShowDeniedHealthEntryFeedback(playerController);
                return false;
            }

            RoomNavigationController navigationController = ResolveNavigationController();

            if (navigationController != null)
            {
                if (!navigationController.TryTraverse(this, playerController))
                {
                    ShowTraversalBlockedFeedback(playerController, "ROUTE BLOCKED");
                    return false;
                }

                TryConsumeEntryCost(playerController);
                return TryConsumeHealthEntryCost(playerController);
            }

            if (connectedRoom != null && connectedRoom.State == RoomState.Idle)
            {
                TryConsumeEntryCost(playerController);
                if (!TryConsumeHealthEntryCost(playerController))
                {
                    return false;
                }

                connectedRoom.EnterRoom();
                return true;
            }

            return connectedRoom != null;
        }

        public void BindOwner(RoomController roomController)
        {
            ownerRoom = roomController;
        }

        /// <summary>
        /// Future dungeon generation can call this once rooms are laid out to wire neighboring doors.
        /// </summary>
        public void SetConnection(RoomController nextRoom, RoomDoor nextDoor = null)
        {
            connectedRoom = nextRoom;
            connectedDoor = nextDoor;
            SetDoorAvailable(nextRoom != null);
        }

        public void ConfigureRevealRequirement(bool requiresReveal, bool startsRevealed = false)
        {
            _requiresReveal = requiresReveal;
            _isRevealed = !requiresReveal || startsRevealed;
            RefreshDoorState();
        }

        public void SetRuntimeStateObjects(GameObject[] lockedObjects, GameObject[] unlockedObjects)
        {
            _runtimeLockedStateObjects = lockedObjects ?? Array.Empty<GameObject>();
            _runtimeUnlockedStateObjects = unlockedObjects ?? Array.Empty<GameObject>();
            RefreshDoorState();
        }

        public void ClearRuntimeStateObjects()
        {
            _runtimeLockedStateObjects = Array.Empty<GameObject>();
            _runtimeUnlockedStateObjects = Array.Empty<GameObject>();
            RefreshDoorState();
        }

        public void ConfigureEntryCost(int keyCost, bool consumeOnce)
        {
            requiredKeysToEnter = Mathf.Max(0, keyCost);
            consumeKeysOnFirstEntry = consumeOnce;
            _entryCostPaid = requiredKeysToEnter <= 0;
            if (requiredKeysToEnter > 0 || keyCostPromptRoot != null || keyCostPromptText != null)
            {
                EnsureKeyCostPrompt();
            }

            RefreshDoorState();
        }

        public void ConfigureHealthEntryCost(float healthCost, bool consumeOnce, bool denyLethal)
        {
            requiredHealthToEnter = Mathf.Max(0f, healthCost);
            consumeHealthOnFirstEntry = consumeOnce;
            denyLethalHealthEntry = denyLethal;
            _healthEntryCostPaid = requiredHealthToEnter <= 0f;
            EnsureHealthCostPrompt();
            RefreshDoorState();
        }

        public bool RevealSecretAccess()
        {
            if (!_requiresReveal || _isRevealed)
            {
                return false;
            }

            _isRevealed = true;
            RefreshDoorState();
            return true;
        }

        public Vector3 GetArrivalPosition()
        {
            Vector3 basePosition = arrivalPoint != null ? arrivalPoint.position : transform.position;
            Vector2 inwardOffset = GetInwardOffset(doorDirection) * arrivalInsetDistance;
            return basePosition + new Vector3(inwardOffset.x, inwardOffset.y, 0f);
        }

        private void SetDoorAvailable(bool available)
        {
            _isAvailable = available;
            RefreshDoorState();
        }

        private void ResolveReferences()
        {
            if (ownerRoom == null)
            {
                ownerRoom = GetComponentInParent<RoomController>();
            }

            if (passageTrigger == null)
            {
                passageTrigger = GetComponent<Collider2D>();
            }

            SyncBlockingColliderShape();
            ResolveSecretHintTargets();
            ResolveHealthCostTargets();

            if (requiredKeysToEnter > 0 || keyCostPromptRoot != null || keyCostPromptText != null)
            {
                EnsureKeyCostPrompt();
            }

            if (requiredHealthToEnter > 0f || healthCostPromptRoot != null || healthCostPromptText != null)
            {
                EnsureHealthCostPrompt();
            }
        }

        private static void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(active);
                }
            }
        }

        private void SyncBlockingColliderShape()
        {
            if (!matchBlockingColliderShapeToPassageTrigger || blockingColliders == null || passageTrigger is not BoxCollider2D passageBox)
            {
                return;
            }

            for (int i = 0; i < blockingColliders.Length; i++)
            {
                if (blockingColliders[i] is not BoxCollider2D blockingBox || blockingBox == passageTrigger)
                {
                    continue;
                }

                // Closed doors should block the same physical lane that the open-door trigger uses for traversal.
                blockingBox.offset = passageBox.offset;
                blockingBox.size = passageBox.size;
                blockingBox.isTrigger = false;
            }
        }

        private static Vector2 GetInwardOffset(RoomDirection direction)
        {
            return direction switch
            {
                RoomDirection.Up => Vector2.down,
                RoomDirection.Right => Vector2.left,
                RoomDirection.Down => Vector2.up,
                RoomDirection.Left => Vector2.right,
                _ => Vector2.zero
            };
        }

        private void RefreshDoorState()
        {
            bool isRevealedAndAvailable = _isAvailable && (!_requiresReveal || _isRevealed);
            bool canTraverse = isRevealedAndAvailable && !_combatLocked;
            bool shouldBlock = !_isAvailable || (_requiresReveal && !_isRevealed) || _combatLocked;
            bool showSecretHint = _isAvailable && _requiresReveal && !_isRevealed;
            bool showKeyCostWarning = canTraverse && HasPendingKeyEntryCost();
            bool showHealthCostWarning = canTraverse && HasPendingHealthEntryCost();
            bool hasStateVisualObjects =
                (lockedStateObjects != null && lockedStateObjects.Length > 0) ||
                (unlockedStateObjects != null && unlockedStateObjects.Length > 0) ||
                (_runtimeLockedStateObjects != null && _runtimeLockedStateObjects.Length > 0) ||
                (_runtimeUnlockedStateObjects != null && _runtimeUnlockedStateObjects.Length > 0);

            IsLocked = !canTraverse;

            if (passageTrigger != null)
            {
                passageTrigger.enabled = canTraverse;
            }

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                bool shouldUseRootSprite = !hasStateVisualObjects || (showSecretHint && ShouldUseRootSpriteForHint(spriteRenderer));
                spriteRenderer.enabled = shouldUseRootSprite && (isRevealedAndAvailable || (showSecretHint && ShouldUseRootSpriteForHint(spriteRenderer)));
            }

            SetCollidersEnabled(blockingColliders, shouldBlock);
            SetObjectsActive(lockedStateObjects, isRevealedAndAvailable && _combatLocked);
            SetObjectsActive(unlockedStateObjects, isRevealedAndAvailable && !_combatLocked);
            SetObjectsActive(_runtimeLockedStateObjects, isRevealedAndAvailable && _combatLocked);
            SetObjectsActive(_runtimeUnlockedStateObjects, isRevealedAndAvailable && !_combatLocked);
            ApplySecretHintState(showSecretHint);
            ApplyKeyCostWarningState(showKeyCostWarning);
            ApplyHealthCostWarningState(showHealthCostWarning);
        }

        private void OnValidate()
        {
            ValidateDirectionFromTransform(true);
            ResolveReferences();
        }

        /// <summary>
        /// Ensures the serialized door direction matches the door's local placement.
        /// This guards against prefab overrides or rotated authoring setups that would otherwise desync navigation and visuals.
        /// </summary>
        public void ValidateDirectionFromTransform(bool logCorrection)
        {
            if (!TryResolveDirectionFromLocalPosition(transform.localPosition, out RoomDirection resolvedDirection))
            {
                return;
            }

            if (doorDirection == resolvedDirection)
            {
                return;
            }

            RoomDirection previousDirection = doorDirection;
            doorDirection = resolvedDirection;

            if (logCorrection)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
                Debug.LogWarning(
                    $"RoomDoor '{name}' direction corrected from {previousDirection} to {resolvedDirection} based on local position {transform.localPosition}.",
                    this);
            }
        }

        private static bool TryResolveDirectionFromLocalPosition(Vector3 localPosition, out RoomDirection resolvedDirection)
        {
            float absX = Mathf.Abs(localPosition.x);
            float absY = Mathf.Abs(localPosition.y);

            if (absX < 0.001f && absY < 0.001f)
            {
                resolvedDirection = RoomDirection.Up;
                return false;
            }

            if (absX >= absY)
            {
                resolvedDirection = localPosition.x >= 0f ? RoomDirection.Right : RoomDirection.Left;
                return true;
            }

            resolvedDirection = localPosition.y >= 0f ? RoomDirection.Up : RoomDirection.Down;
            return true;
        }

        private void ResolveSecretHintTargets()
        {
            List<SpriteRenderer> resolvedTargets = new();

            CollectHintTargets(secretHintTintTargets, resolvedTargets);
            CollectHintTargets(secretHintStateObjects, resolvedTargets);

            if (resolvedTargets.Count == 0)
            {
                CollectHintTargets(unlockedStateObjects, resolvedTargets);
            }

            if (resolvedTargets.Count == 0)
            {
                CollectHintTargets(lockedStateObjects, resolvedTargets);
            }

            SpriteRenderer rootSpriteRenderer = GetComponent<SpriteRenderer>();
            if (rootSpriteRenderer != null && !resolvedTargets.Contains(rootSpriteRenderer))
            {
                resolvedTargets.Add(rootSpriteRenderer);
            }

            _resolvedSecretHintTintTargets = resolvedTargets.ToArray();
            _resolvedSecretHintBaseColors = new Color[_resolvedSecretHintTintTargets.Length];

            for (int i = 0; i < _resolvedSecretHintTintTargets.Length; i++)
            {
                _resolvedSecretHintBaseColors[i] = _resolvedSecretHintTintTargets[i] != null
                    ? _resolvedSecretHintTintTargets[i].color
                    : Color.white;
            }
        }

        private void ResolveHealthCostTargets()
        {
            List<SpriteRenderer> resolvedTargets = new();

            CollectHintTargets(healthCostTintTargets, resolvedTargets);
            CollectHintTargets(healthCostWarningStateObjects, resolvedTargets);

            if (resolvedTargets.Count == 0)
            {
                CollectHintTargets(unlockedStateObjects, resolvedTargets);
            }

            SpriteRenderer rootSpriteRenderer = GetComponent<SpriteRenderer>();
            if (rootSpriteRenderer != null && !resolvedTargets.Contains(rootSpriteRenderer))
            {
                resolvedTargets.Add(rootSpriteRenderer);
            }

            _resolvedHealthCostTintTargets = resolvedTargets.ToArray();
            _resolvedHealthCostBaseColors = new Color[_resolvedHealthCostTintTargets.Length];

            for (int i = 0; i < _resolvedHealthCostTintTargets.Length; i++)
            {
                _resolvedHealthCostBaseColors[i] = _resolvedHealthCostTintTargets[i] != null
                    ? _resolvedHealthCostTintTargets[i].color
                    : Color.white;
            }
        }

        private void ApplySecretHintState(bool visible)
        {
            _isShowingSecretHint = visible;

            if (secretHintStateObjects != null && secretHintStateObjects.Length > 0)
            {
                SetObjectsActive(secretHintStateObjects, visible);
            }
            else if (visible)
            {
                if (unlockedStateObjects != null && unlockedStateObjects.Length > 0)
                {
                    SetObjectsActive(unlockedStateObjects, true);
                }
                else if (lockedStateObjects != null && lockedStateObjects.Length > 0)
                {
                    SetObjectsActive(lockedStateObjects, true);
                }
            }

            if (!visible)
            {
                RestoreSecretHintColors();
                return;
            }

            ApplySecretHintColors(true);
        }

        private void ApplyHealthCostWarningState(bool visible)
        {
            _isShowingHealthCostWarning = visible;

            if (healthCostWarningStateObjects != null && healthCostWarningStateObjects.Length > 0)
            {
                SetObjectsActive(healthCostWarningStateObjects, visible);
            }

            if (healthCostPromptRoot != null)
            {
                healthCostPromptRoot.gameObject.SetActive(visible && showWorldHealthCostPrompt);
            }

            if (healthCostPromptText != null)
            {
                healthCostPromptText.gameObject.SetActive(visible && showWorldHealthCostPrompt);
            }

            if (!visible)
            {
                RestoreHealthCostWarningVisuals();
                return;
            }

            UpdateHealthCostPromptText();
            ApplyHealthCostWarningVisuals(true);
        }

        private void ApplyKeyCostWarningState(bool visible)
        {
            _isShowingKeyCostWarning = visible;

            if (keyCostPromptRoot != null)
            {
                keyCostPromptRoot.gameObject.SetActive(visible && showWorldKeyCostPrompt);
            }

            if (keyCostPromptText != null)
            {
                keyCostPromptText.gameObject.SetActive(visible && showWorldKeyCostPrompt);
            }

            if (!visible)
            {
                if (keyCostPromptRoot != null)
                {
                    keyCostPromptRoot.localScale = _keyCostPromptBaseScale;
                }

                return;
            }

            UpdateKeyCostPromptText();
            ApplyKeyCostWarningVisuals(true);
        }

        private void ApplySecretHintColors(bool usePulse)
        {
            if (_resolvedSecretHintTintTargets == null || _resolvedSecretHintBaseColors == null)
            {
                return;
            }

            float pulse = usePulse
                ? 1f + Mathf.Sin(Time.unscaledTime * Mathf.Max(0.05f, secretHintPulseSpeed)) * secretHintPulseAmplitude
                : 1f;
            float alpha = Mathf.Clamp01(secretHintColor.a * pulse);

            for (int i = 0; i < _resolvedSecretHintTintTargets.Length; i++)
            {
                SpriteRenderer target = _resolvedSecretHintTintTargets[i];
                if (target == null)
                {
                    continue;
                }

                Color baseColor = i < _resolvedSecretHintBaseColors.Length ? _resolvedSecretHintBaseColors[i] : target.color;
                target.enabled = true;
                target.color = new Color(
                    Mathf.Lerp(baseColor.r, secretHintColor.r, 0.6f),
                    Mathf.Lerp(baseColor.g, secretHintColor.g, 0.6f),
                    Mathf.Lerp(baseColor.b, secretHintColor.b, 0.6f),
                    alpha);
            }
        }

        private void RestoreSecretHintColors()
        {
            if (_resolvedSecretHintTintTargets == null || _resolvedSecretHintBaseColors == null)
            {
                return;
            }

            for (int i = 0; i < _resolvedSecretHintTintTargets.Length; i++)
            {
                SpriteRenderer target = _resolvedSecretHintTintTargets[i];
                if (target == null)
                {
                    continue;
                }

                target.color = i < _resolvedSecretHintBaseColors.Length
                    ? _resolvedSecretHintBaseColors[i]
                    : Color.white;
            }
        }

        private void ApplyHealthCostWarningVisuals(bool usePulse)
        {
            if (_resolvedHealthCostTintTargets == null || _resolvedHealthCostBaseColors == null)
            {
                return;
            }

            float pulse = usePulse
                ? 1f + Mathf.Sin(Time.unscaledTime * Mathf.Max(0.05f, healthCostPulseSpeed)) * healthCostPulseAmplitude
                : 1f;
            float alpha = Mathf.Clamp01(healthCostWarningColor.a * pulse);

            for (int i = 0; i < _resolvedHealthCostTintTargets.Length; i++)
            {
                SpriteRenderer target = _resolvedHealthCostTintTargets[i];
                if (target == null)
                {
                    continue;
                }

                Color baseColor = i < _resolvedHealthCostBaseColors.Length ? _resolvedHealthCostBaseColors[i] : target.color;
                target.enabled = true;
                target.color = new Color(
                    Mathf.Lerp(baseColor.r, healthCostWarningColor.r, 0.72f),
                    Mathf.Lerp(baseColor.g, healthCostWarningColor.g, 0.72f),
                    Mathf.Lerp(baseColor.b, healthCostWarningColor.b, 0.72f),
                    Mathf.Max(baseColor.a, alpha));
            }

            if (healthCostPromptRoot != null)
            {
                healthCostPromptRoot.localScale = _healthCostPromptBaseScale * Mathf.Lerp(1f, 1.08f, Mathf.Clamp01((pulse - 1f) + 0.5f));
            }

            if (healthCostPromptText != null)
            {
                healthCostPromptText.color = Color.Lerp(healthCostWarningColor, Color.white, Mathf.Clamp01((pulse - 1f) + 0.5f) * 0.4f);
            }
        }

        private void ApplyKeyCostWarningVisuals(bool usePulse)
        {
            float pulse = usePulse
                ? 1f + Mathf.Sin(Time.unscaledTime * Mathf.Max(0.05f, keyCostPulseSpeed)) * keyCostPulseAmplitude
                : 1f;

            if (keyCostPromptRoot != null)
            {
                keyCostPromptRoot.localScale = _keyCostPromptBaseScale * Mathf.Lerp(1f, 1.06f, Mathf.Clamp01((pulse - 1f) + 0.5f));
            }

            if (keyCostPromptText != null)
            {
                keyCostPromptText.color = Color.Lerp(keyCostWarningColor, Color.white, Mathf.Clamp01((pulse - 1f) + 0.5f) * 0.35f);
            }
        }

        private void RestoreHealthCostWarningVisuals()
        {
            if (_resolvedHealthCostTintTargets != null && _resolvedHealthCostBaseColors != null)
            {
                for (int i = 0; i < _resolvedHealthCostTintTargets.Length; i++)
                {
                    SpriteRenderer target = _resolvedHealthCostTintTargets[i];
                    if (target == null)
                    {
                        continue;
                    }

                    target.color = i < _resolvedHealthCostBaseColors.Length
                        ? _resolvedHealthCostBaseColors[i]
                        : Color.white;
                }
            }

            if (healthCostPromptRoot != null)
            {
                healthCostPromptRoot.localScale = _healthCostPromptBaseScale;
            }
        }

        private static void CollectHintTargets(SpriteRenderer[] explicitTargets, List<SpriteRenderer> results)
        {
            if (explicitTargets == null || results == null)
            {
                return;
            }

            for (int i = 0; i < explicitTargets.Length; i++)
            {
                SpriteRenderer target = explicitTargets[i];
                if (target != null && !results.Contains(target))
                {
                    results.Add(target);
                }
            }
        }

        private static void CollectHintTargets(GameObject[] sourceObjects, List<SpriteRenderer> results)
        {
            if (sourceObjects == null || results == null)
            {
                return;
            }

            for (int i = 0; i < sourceObjects.Length; i++)
            {
                GameObject sourceObject = sourceObjects[i];
                if (sourceObject == null)
                {
                    continue;
                }

                SpriteRenderer[] renderers = sourceObject.GetComponentsInChildren<SpriteRenderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    SpriteRenderer renderer = renderers[rendererIndex];
                    if (renderer != null && !results.Contains(renderer))
                    {
                        results.Add(renderer);
                    }
                }
            }
        }

        private bool ShouldUseRootSpriteForHint(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
            {
                return false;
            }

            if (_resolvedSecretHintTintTargets == null || _resolvedSecretHintTintTargets.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < _resolvedSecretHintTintTargets.Length; i++)
            {
                if (_resolvedSecretHintTintTargets[i] == spriteRenderer)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasPendingHealthEntryCost()
        {
            return GetRequiredHealthCost() > 0f;
        }

        private bool HasPendingKeyEntryCost()
        {
            return GetRequiredKeyCost() > 0;
        }

        private bool CanPayEntryCost(PlayerController playerController)
        {
            int requiredKeyCost = GetRequiredKeyCost(playerController);

            if (requiredKeyCost <= 0)
            {
                return true;
            }

            PlayerInventory playerInventory = ResolvePlayerInventory(playerController);
            return playerInventory != null && playerInventory.Keys >= requiredKeyCost;
        }

        private void ShowDeniedEntryFeedback(PlayerController playerController)
        {
            int requiredKeyCost = GetRequiredKeyCost(playerController);

            if (playerController == null || requiredKeyCost <= 0)
            {
                return;
            }

            if (Time.unscaledTime - _lastDeniedFeedbackTime < deniedEntryFeedbackCooldown)
            {
                return;
            }

            _lastDeniedFeedbackTime = Time.unscaledTime;
            Color accentColor = ResolveKeyDeniedAccent();

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                playerController.transform.position + Vector3.up * 0.9f,
                $"KEY x{requiredKeyCost} NEEDED",
                accentColor,
                1.05f,
                0.85f,
                1.34f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
            PresentCommitRejectedFeedback(accentColor, TraversalDoorGuidanceStyle.Guided);
        }

        private void ShowDeniedHealthEntryFeedback(PlayerController playerController)
        {
            float requiredHealthCost = GetRequiredHealthCost(playerController);

            if (playerController == null || requiredHealthCost <= 0f)
            {
                return;
            }

            if (Time.unscaledTime - _lastDeniedFeedbackTime < deniedEntryFeedbackCooldown)
            {
                return;
            }

            _lastDeniedFeedbackTime = Time.unscaledTime;
            Color accentColor = ResolveHealthDeniedAccent();

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                playerController.transform.position + Vector3.up * 0.9f,
                $"HP {Mathf.CeilToInt(requiredHealthCost)} NEEDED",
                accentColor,
                1.05f,
                0.85f,
                1.34f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
            PresentCommitRejectedFeedback(accentColor, TraversalDoorGuidanceStyle.GuidedRisk);
        }

        private void TryConsumeEntryCost(PlayerController playerController)
        {
            int requiredKeyCost = GetRequiredKeyCost(playerController);

            if (requiredKeyCost <= 0)
            {
                if (consumeKeysOnFirstEntry && requiredKeysToEnter > 0)
                {
                    _entryCostPaid = true;
                }

                return;
            }

            PlayerInventory playerInventory = ResolvePlayerInventory(playerController);

            if (playerInventory == null)
            {
                return;
            }

            if (playerInventory.TrySpendKeys(requiredKeyCost) && consumeKeysOnFirstEntry)
            {
                _entryCostPaid = true;
                RefreshDoorState();
            }
        }

        private bool CanPayHealthEntryCost(PlayerController playerController)
        {
            float requiredHealthCost = GetRequiredHealthCost(playerController);

            if (requiredHealthCost <= 0f)
            {
                return true;
            }

            PlayerHealth playerHealth = ResolvePlayerHealth(playerController);
            if (playerHealth == null)
            {
                return false;
            }

            return !denyLethalHealthEntry || playerHealth.CurrentHealth > requiredHealthCost;
        }

        private bool TryConsumeHealthEntryCost(PlayerController playerController)
        {
            float requiredHealthCost = GetRequiredHealthCost(playerController);

            if (requiredHealthCost <= 0f)
            {
                if (consumeHealthOnFirstEntry && requiredHealthToEnter > 0f)
                {
                    _healthEntryCostPaid = true;
                    RefreshDoorState();
                }

                return true;
            }

            PlayerHealth playerHealth = ResolvePlayerHealth(playerController);
            if (playerHealth == null)
            {
                return false;
            }

            bool paid = playerHealth.TrySpendHealth(requiredHealthCost, transform, !denyLethalHealthEntry);
            if (paid && consumeHealthOnFirstEntry)
            {
                _healthEntryCostPaid = true;
                RefreshDoorState();
            }

            return paid;
        }

        private void ShowTraversalBlockedFeedback(PlayerController playerController, string label)
        {
            if (playerController == null)
            {
                return;
            }

            if (Time.unscaledTime - _lastDeniedFeedbackTime < deniedEntryFeedbackCooldown)
            {
                return;
            }

            _lastDeniedFeedbackTime = Time.unscaledTime;
            Color accentColor = ResolveTraversalBlockedAccent();
            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                playerController.transform.position + Vector3.up * 0.9f,
                string.IsNullOrWhiteSpace(label) ? "ROUTE BLOCKED" : label,
                accentColor,
                1.05f,
                0.85f,
                1.34f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
            PresentCommitRejectedFeedback(accentColor, ResolveTraversalBlockedStyle());
        }

        private void PresentCommitRejectedFeedback(Color accentColor, TraversalDoorGuidanceStyle style)
        {
            TraversalGuidanceBeacon beacon = GetComponent<TraversalGuidanceBeacon>();
            if (beacon == null)
            {
                beacon = gameObject.AddComponent<TraversalGuidanceBeacon>();
            }

            float feedbackDuration = Mathf.Max(1.05f, deniedEntryFeedbackCooldown * 3.4f);
            if (style == TraversalDoorGuidanceStyle.RiskWarning)
            {
                beacon.ShowDoorRiskWarning(accentColor, doorDirection, feedbackDuration);
            }
            else
            {
                beacon.ShowDoorGuidance(accentColor, doorDirection, feedbackDuration, style);
            }

            beacon.PlayCommitRejected();
        }

        private Color ResolveKeyDeniedAccent()
        {
            return Color.Lerp(ResolveConnectedRoomAccent(), new Color(0.7f, 0.9f, 1f, 1f), 0.46f);
        }

        private Color ResolveHealthDeniedAccent()
        {
            return Color.Lerp(ResolveConnectedRoomAccent(), healthCostWarningColor, 0.72f);
        }

        private Color ResolveTraversalBlockedAccent()
        {
            if (HasPendingHealthEntryCost())
            {
                return ResolveHealthDeniedAccent();
            }

            if (GetRequiredKeyCost() > 0)
            {
                return ResolveKeyDeniedAccent();
            }

            return ResolveConnectedRoomAccent();
        }

        private TraversalDoorGuidanceStyle ResolveTraversalBlockedStyle()
        {
            if (HasPendingHealthEntryCost())
            {
                return TraversalDoorGuidanceStyle.GuidedRisk;
            }

            return IsLocked
                ? TraversalDoorGuidanceStyle.RiskWarning
                : TraversalDoorGuidanceStyle.Guided;
        }

        private Color ResolveConnectedRoomAccent()
        {
            RoomType roomType = connectedRoom != null ? connectedRoom.RoomType : RoomType.Normal;
            return RoomTraversalGuidanceController.ResolveRoomAccent(roomType);
        }

        private void EnsureKeyCostPrompt()
        {
            if (!showWorldKeyCostPrompt && keyCostPromptRoot == null && keyCostPromptText == null)
            {
                return;
            }

            if (keyCostPromptRoot == null)
            {
                GameObject promptRootObject = new("KeyCostPromptRoot");
                promptRootObject.transform.SetParent(transform, false);
                promptRootObject.transform.localPosition = keyCostPromptLocalOffset;
                keyCostPromptRoot = promptRootObject.transform;
            }

            keyCostPromptRoot.localPosition = keyCostPromptLocalOffset;
            _keyCostPromptBaseScale = keyCostPromptRoot.localScale;

            if (keyCostPromptText == null)
            {
                keyCostPromptText = keyCostPromptRoot.GetComponentInChildren<TextMesh>(true);
            }

            if (showWorldKeyCostPrompt && keyCostPromptText == null)
            {
                GameObject textObject = new("KeyCostPromptText");
                textObject.transform.SetParent(keyCostPromptRoot, false);
                keyCostPromptText = textObject.AddComponent<TextMesh>();
                keyCostPromptText.anchor = TextAnchor.MiddleCenter;
                keyCostPromptText.alignment = TextAlignment.Center;
            }

            if (keyCostPromptText != null)
            {
                keyCostPromptText.fontSize = keyCostPromptFontSize;
                keyCostPromptText.characterSize = keyCostPromptCharacterSize;
                keyCostPromptText.color = keyCostWarningColor;
                keyCostPromptText.transform.localPosition = Vector3.zero;
                keyCostPromptText.transform.localRotation = Quaternion.identity;
                keyCostPromptText.gameObject.layer = gameObject.layer;
                CuteIssac.UI.LocalizedUiFontProvider.Apply(keyCostPromptText);
                UpdateKeyCostPromptText();

                if (!showWorldKeyCostPrompt)
                {
                    keyCostPromptText.text = string.Empty;
                    keyCostPromptText.gameObject.SetActive(false);
                }
            }

            if (keyCostPromptRoot != null)
            {
                keyCostPromptRoot.gameObject.SetActive(false);
            }
        }

        private void UpdateKeyCostPromptText()
        {
            if (keyCostPromptText == null)
            {
                return;
            }

            int requiredKeyCost = GetRequiredKeyCost();

            if (requiredKeyCost <= 0)
            {
                if (_lastDisplayedKeyCost != 0)
                {
                    keyCostPromptText.text = string.Empty;
                    _lastDisplayedKeyCost = 0;
                }

                return;
            }

            if (_lastDisplayedKeyCost == requiredKeyCost)
            {
                return;
            }

            keyCostPromptText.text = $"KEY x{requiredKeyCost}";
            _lastDisplayedKeyCost = requiredKeyCost;
        }

        private void EnsureHealthCostPrompt()
        {
            if (healthCostPromptRoot == null)
            {
                GameObject promptRootObject = new("HealthCostPromptRoot");
                promptRootObject.transform.SetParent(transform, false);
                promptRootObject.transform.localPosition = healthCostPromptLocalOffset;
                healthCostPromptRoot = promptRootObject.transform;
            }

            healthCostPromptRoot.localPosition = healthCostPromptLocalOffset;
            _healthCostPromptBaseScale = healthCostPromptRoot.localScale;

            if (healthCostPromptText == null)
            {
                healthCostPromptText = healthCostPromptRoot.GetComponentInChildren<TextMesh>(true);
            }

            if (showWorldHealthCostPrompt && healthCostPromptText == null)
            {
                GameObject textObject = new("HealthCostPromptText");
                textObject.transform.SetParent(healthCostPromptRoot, false);
                healthCostPromptText = textObject.AddComponent<TextMesh>();
                healthCostPromptText.anchor = TextAnchor.MiddleCenter;
                healthCostPromptText.alignment = TextAlignment.Center;
            }

            if (healthCostPromptText != null)
            {
                healthCostPromptText.fontSize = healthCostPromptFontSize;
                healthCostPromptText.characterSize = healthCostPromptCharacterSize;
                healthCostPromptText.color = healthCostWarningColor;
                healthCostPromptText.transform.localPosition = Vector3.zero;
                healthCostPromptText.transform.localRotation = Quaternion.identity;
                healthCostPromptText.gameObject.layer = gameObject.layer;
                CuteIssac.UI.LocalizedUiFontProvider.Apply(healthCostPromptText);
                UpdateHealthCostPromptText();

                if (!showWorldHealthCostPrompt)
                {
                    healthCostPromptText.text = string.Empty;
                    healthCostPromptText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateHealthCostPromptText()
        {
            if (healthCostPromptText == null)
            {
                return;
            }

            int requiredHealthCost = Mathf.CeilToInt(GetRequiredHealthCost());

            if (requiredHealthCost <= 0)
            {
                if (_lastDisplayedHealthCost != 0)
                {
                    healthCostPromptText.text = string.Empty;
                    _lastDisplayedHealthCost = 0;
                }

                return;
            }

            if (_lastDisplayedHealthCost == requiredHealthCost)
            {
                return;
            }

            healthCostPromptText.text = $"HP -{requiredHealthCost}";
            _lastDisplayedHealthCost = requiredHealthCost;
        }

        private int GetRequiredKeyCost(PlayerController playerController = null)
        {
            int baseCost = Mathf.Max(0, requiredKeysToEnter);
            if (baseCost <= 0 || (consumeKeysOnFirstEntry && _entryCostPaid))
            {
                return 0;
            }

            PlayerItemManager itemManager = ResolvePlayerItemManager(playerController);
            return itemManager != null
                ? itemManager.ResolveEffectiveDoorKeyCost(baseCost, ResolveEntryCostRoomType())
                : baseCost;
        }

        private float GetRequiredHealthCost(PlayerController playerController = null)
        {
            float baseCost = Mathf.Max(0f, requiredHealthToEnter);
            if (baseCost <= 0f || (consumeHealthOnFirstEntry && _healthEntryCostPaid))
            {
                return 0f;
            }

            PlayerItemManager itemManager = ResolvePlayerItemManager(playerController);
            return itemManager != null
                ? itemManager.ResolveEffectiveDoorHealthCost(baseCost, ResolveEntryCostRoomType())
                : baseCost;
        }

        private PlayerItemManager ResolvePlayerItemManager(PlayerController playerController)
        {
            if (playerController != null)
            {
                CachePlayerComponents(playerController);
                return _scenePlayerItemManager;
            }

            if (_scenePlayerItemManager != null)
            {
                return _scenePlayerItemManager;
            }

            PlayerController resolvedPlayerController = ResolveScenePlayerController();
            CachePlayerComponents(resolvedPlayerController);
            return _scenePlayerItemManager;
        }

        private PlayerInventory ResolvePlayerInventory(PlayerController playerController)
        {
            if (playerController != null)
            {
                CachePlayerComponents(playerController);
                return _scenePlayerInventory;
            }

            if (_scenePlayerInventory != null)
            {
                return _scenePlayerInventory;
            }

            PlayerController resolvedPlayerController = ResolveScenePlayerController();
            CachePlayerComponents(resolvedPlayerController);
            return _scenePlayerInventory;
        }

        private PlayerHealth ResolvePlayerHealth(PlayerController playerController)
        {
            if (playerController != null)
            {
                CachePlayerComponents(playerController);
                return _scenePlayerHealth;
            }

            if (_scenePlayerHealth != null)
            {
                return _scenePlayerHealth;
            }

            PlayerController resolvedPlayerController = ResolveScenePlayerController();
            CachePlayerComponents(resolvedPlayerController);
            return _scenePlayerHealth;
        }

        private void CachePlayerComponents(PlayerController playerController)
        {
            if (playerController == null)
            {
                return;
            }

            if (_hasCachedPlayerComponents && _scenePlayerController == playerController)
            {
                return;
            }

            _scenePlayerController = playerController;
            _scenePlayerItemManager = playerController.GetComponentInParent<PlayerItemManager>();
            _scenePlayerInventory = playerController.GetComponentInParent<PlayerInventory>();
            _scenePlayerHealth = playerController.GetComponentInParent<PlayerHealth>();
            _hasCachedPlayerComponents = true;
        }

        private PlayerController ResolveScenePlayerController()
        {
            if (_scenePlayerController == null)
            {
                _scenePlayerController = PlayerRegistry.ActiveController != null
                    ? PlayerRegistry.ActiveController
                    : FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            }

            return _scenePlayerController;
        }

        private RoomNavigationController ResolveNavigationController()
        {
            if (_navigationController == null)
            {
                _navigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);
            }

            return _navigationController;
        }

        private RoomType ResolveEntryCostRoomType()
        {
            if (connectedRoom != null)
            {
                return connectedRoom.RoomType;
            }

            if (ownerRoom != null)
            {
                return ownerRoom.RoomType;
            }

            return RoomType.Normal;
        }
    }
}
