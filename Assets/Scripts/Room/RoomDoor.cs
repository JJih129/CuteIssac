using CuteIssac.Player;
using CuteIssac.Dungeon;
using CuteIssac.Core.Feedback;
using System.Collections.Generic;
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
        public bool HasUnpaidHealthEntryCost => HasPendingHealthEntryCost();
        public float RequiredHealthToEnter => requiredHealthToEnter;
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
        private bool _isShowingHealthCostWarning;
        private Vector3 _healthCostPromptBaseScale = Vector3.one;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();

            if (playerController == null)
            {
                return;
            }

            TryEnter(playerController);
        }

        private void Update()
        {
            if (_isShowingSecretHint)
            {
                ApplySecretHintColors(true);
            }

            if (_isShowingHealthCostWarning)
            {
                ApplyHealthCostWarningVisuals(true);
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
            if (playerController == null || IsLocked)
            {
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

            TryConsumeEntryCost(playerController);
            if (!TryConsumeHealthEntryCost(playerController))
            {
                return false;
            }

            RoomNavigationController navigationController = FindFirstObjectByType<RoomNavigationController>(FindObjectsInactive.Exclude);

            if (navigationController != null)
            {
                return navigationController.TryTraverse(this, playerController);
            }

            if (connectedRoom != null && connectedRoom.State == RoomState.Idle)
            {
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

        public void ConfigureEntryCost(int keyCost, bool consumeOnce)
        {
            requiredKeysToEnter = Mathf.Max(0, keyCost);
            consumeKeysOnFirstEntry = consumeOnce;
            _entryCostPaid = requiredKeysToEnter <= 0;
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

        public void RevealSecretAccess()
        {
            if (!_requiresReveal || _isRevealed)
            {
                return;
            }

            _isRevealed = true;
            RefreshDoorState();
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

            ResolveSecretHintTargets();
            ResolveHealthCostTargets();

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
            bool showHealthCostWarning = canTraverse && HasPendingHealthEntryCost();

            IsLocked = !canTraverse;

            if (passageTrigger != null)
            {
                passageTrigger.enabled = canTraverse;
            }

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isRevealedAndAvailable || (showSecretHint && ShouldUseRootSpriteForHint(spriteRenderer));
            }

            SetCollidersEnabled(blockingColliders, shouldBlock);
            SetObjectsActive(lockedStateObjects, isRevealedAndAvailable && _combatLocked);
            SetObjectsActive(unlockedStateObjects, isRevealedAndAvailable && !_combatLocked);
            ApplySecretHintState(showSecretHint);
            ApplyHealthCostWarningState(showHealthCostWarning);
        }

        private void OnValidate()
        {
            ResolveReferences();
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
            return requiredHealthToEnter > 0f && (!consumeHealthOnFirstEntry || !_healthEntryCostPaid);
        }

        private bool CanPayEntryCost(PlayerController playerController)
        {
            if (requiredKeysToEnter <= 0 || (consumeKeysOnFirstEntry && _entryCostPaid))
            {
                return true;
            }

            PlayerInventory playerInventory = playerController.GetComponentInParent<PlayerInventory>();
            return playerInventory != null && playerInventory.Keys >= requiredKeysToEnter;
        }

        private void ShowDeniedEntryFeedback(PlayerController playerController)
        {
            if (playerController == null || requiredKeysToEnter <= 0)
            {
                return;
            }

            if (Time.unscaledTime - _lastDeniedFeedbackTime < deniedEntryFeedbackCooldown)
            {
                return;
            }

            _lastDeniedFeedbackTime = Time.unscaledTime;

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                playerController.transform.position + Vector3.up * 0.9f,
                $"KEY x{requiredKeysToEnter} NEEDED",
                new Color(0.7f, 0.9f, 1f, 1f),
                1.05f,
                0.85f,
                1.34f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private void ShowDeniedHealthEntryFeedback(PlayerController playerController)
        {
            if (playerController == null || requiredHealthToEnter <= 0f)
            {
                return;
            }

            if (Time.unscaledTime - _lastDeniedFeedbackTime < deniedEntryFeedbackCooldown)
            {
                return;
            }

            _lastDeniedFeedbackTime = Time.unscaledTime;

            GameplayFeedbackEvents.RaiseFloatingFeedback(new FloatingFeedbackRequest(
                playerController.transform.position + Vector3.up * 0.9f,
                $"HP {Mathf.CeilToInt(requiredHealthToEnter)} NEEDED",
                new Color(1f, 0.5f, 0.72f, 1f),
                1.05f,
                0.85f,
                1.34f,
                visualProfile: FloatingFeedbackVisualProfile.EventLabel));
        }

        private void TryConsumeEntryCost(PlayerController playerController)
        {
            if (requiredKeysToEnter <= 0 || (consumeKeysOnFirstEntry && _entryCostPaid))
            {
                return;
            }

            PlayerInventory playerInventory = playerController.GetComponentInParent<PlayerInventory>();

            if (playerInventory == null)
            {
                return;
            }

            if (playerInventory.TrySpendKeys(requiredKeysToEnter) && consumeKeysOnFirstEntry)
            {
                _entryCostPaid = true;
            }
        }

        private bool CanPayHealthEntryCost(PlayerController playerController)
        {
            if (requiredHealthToEnter <= 0f || (consumeHealthOnFirstEntry && _healthEntryCostPaid))
            {
                return true;
            }

            PlayerHealth playerHealth = playerController.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                return false;
            }

            return !denyLethalHealthEntry || playerHealth.CurrentHealth > requiredHealthToEnter;
        }

        private bool TryConsumeHealthEntryCost(PlayerController playerController)
        {
            if (requiredHealthToEnter <= 0f || (consumeHealthOnFirstEntry && _healthEntryCostPaid))
            {
                return true;
            }

            PlayerHealth playerHealth = playerController.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                return false;
            }

            bool paid = playerHealth.TrySpendHealth(requiredHealthToEnter, transform, !denyLethalHealthEntry);
            if (paid && consumeHealthOnFirstEntry)
            {
                _healthEntryCostPaid = true;
                RefreshDoorState();
            }

            return paid;
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

            if (!HasPendingHealthEntryCost())
            {
                healthCostPromptText.text = string.Empty;
                return;
            }

            healthCostPromptText.text = $"HP -{Mathf.CeilToInt(requiredHealthToEnter)}";
        }
    }
}
