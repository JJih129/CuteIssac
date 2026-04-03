using System.Collections.Generic;
using CuteIssac.Core.Feedback;
using CuteIssac.Core.Gameplay;
using CuteIssac.Room;
using UnityEngine;

namespace CuteIssac.Enemy
{
    public enum EnemyFormationRole
    {
        None = 0,
        Frontline = 1,
        Support = 2,
        Controller = 3,
        Ranged = 4,
        Siege = 5
    }

    public enum EnemyFormationCueType
    {
        None = 0,
        EscortSurge = 1,
        CrossfireLock = 2,
        SiegePressure = 3
    }

    public enum EnemyFormationPriorityLevel
    {
        None = 0,
        Focus = 1,
        Critical = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyFormationModifier : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private RoomEnemyMember roomEnemyMember;

        private string _formationId = string.Empty;
        private EnemyFormationRole _formationRole;
        private Color _accentColor = Color.white;
        private bool _isApplied;
        private Transform _markerRoot;
        private SpriteRenderer _ringRenderer;
        private static Sprite s_CircleSprite;
        private static Sprite s_WhiteSprite;
        private static readonly List<EnemyFormationModifier> s_ActiveModifiers = new();
        private static int s_CueSerialCounter;
        private Vector3 _markerBaseLocalPosition;
        private Vector3 _ringBaseScale;
        private float _phaseOffset;
        private EnemyFormationCueType _activeCueType;
        private Vector2 _activeCueAnchor;
        private float _activeCueExpiresAt;
        private float _activeCueIntensity = 1f;
        private int _activeCueSerial;
        private float _formationSpeedMultiplier = 1f;
        private float _formationContactDamageMultiplier = 1f;
        private float _breakWindowExpiresAt;
        private float _breakSpeedMultiplier = 1f;
        private float _breakContactDamageMultiplier = 1f;
        private EnemyFormationPriorityLevel _priorityLevel;
        private Color _priorityColor = Color.white;
        private Transform _priorityRoot;
        private SpriteRenderer _priorityPulseRenderer;
        private SpriteRenderer _priorityCoreRenderer;
        private Vector3 _priorityBaseScale = Vector3.one;
        private bool _isSubscribedToHealthEvents;

        public string FormationId => _formationId;
        public EnemyFormationRole FormationRole => _formationRole;
        public EnemyFormationPriorityLevel PriorityLevel => _priorityLevel;
        public Color AccentColor => _accentColor;
        public Color PriorityColor => _priorityColor;
        public bool IsApplied => _isApplied;
        public Vector2 Position => transform.position;
        public float StableSideSign => (GetInstanceID() & 1) == 0 ? 1f : -1f;

        private void Awake()
        {
            ResolveReferences();
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            ResolveReferences();
            RegisterActiveModifier();
            SubscribeToHealthEvents();
        }

        private void Update()
        {
            if (!_isApplied || _markerRoot == null)
            {
                return;
            }

            bool breakActive = HasActiveFormationBreak();
            float pulseTime = (Time.time * 5.2f) + _phaseOffset;
            float pulse = 0.5f + (0.5f * Mathf.Sin(pulseTime));
            _markerRoot.localPosition = _markerBaseLocalPosition + new Vector3(0f, Mathf.Sin(pulseTime * 0.42f) * 0.035f, 0f);

            if (_ringRenderer != null)
            {
                _ringRenderer.transform.localScale = _ringBaseScale * Mathf.Lerp(0.94f, 1.08f, pulse);
                Color ringColor = breakActive
                    ? Color.Lerp(_accentColor, Color.white, 0.42f)
                    : _accentColor;
                _ringRenderer.color = new Color(
                    ringColor.r,
                    ringColor.g,
                    ringColor.b,
                    breakActive
                        ? Mathf.Lerp(0.08f, 0.2f, pulse)
                        : Mathf.Lerp(0.18f, 0.32f, pulse));
            }

            if (_priorityRoot != null)
            {
                bool cueRelevant = !breakActive && IsCueRelevantToPriority();
                float urgency = cueRelevant ? 1.35f : 1f;
                float priorityPulse = 0.5f + (0.5f * Mathf.Sin((Time.time * (6.6f * urgency)) + (_phaseOffset * 1.6f)));
                float priorityScaleMax = _priorityLevel == EnemyFormationPriorityLevel.Critical ? 1.18f : 1.08f;
                _priorityRoot.localScale = _priorityBaseScale * Mathf.Lerp(0.92f, priorityScaleMax, priorityPulse);

                if (_priorityPulseRenderer != null)
                {
                    float baseAlpha = breakActive
                        ? 0.08f
                        : (_priorityLevel == EnemyFormationPriorityLevel.Critical ? 0.2f : 0.14f);
                    float maxAlpha = cueRelevant
                        ? (_priorityLevel == EnemyFormationPriorityLevel.Critical ? 0.48f : 0.32f)
                        : breakActive
                            ? 0.16f
                            : (_priorityLevel == EnemyFormationPriorityLevel.Critical ? 0.34f : 0.22f);
                    _priorityPulseRenderer.color = new Color(
                        _priorityColor.r,
                        _priorityColor.g,
                        _priorityColor.b,
                        Mathf.Lerp(baseAlpha, maxAlpha, priorityPulse));
                }

                if (_priorityCoreRenderer != null)
                {
                    Color coreColor = Color.Lerp(_priorityColor, Color.white, _priorityLevel == EnemyFormationPriorityLevel.Critical ? 0.38f : 0.18f);
                    coreColor.a = cueRelevant
                        ? Mathf.Lerp(0.86f, 1f, priorityPulse)
                        : breakActive
                            ? Mathf.Lerp(0.48f, 0.66f, priorityPulse)
                            : Mathf.Lerp(0.72f, 0.9f, priorityPulse);
                    _priorityCoreRenderer.color = coreColor;
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromHealthEvents();

            if (Application.isPlaying)
            {
                ClearFormation();
            }

            UnregisterActiveModifier();
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealthEvents();
            UnregisterActiveModifier();
        }

        public void PrepareForSpawn()
        {
            ClearFormation();
        }

        public void ApplyFormation(string formationId, EnemyFormationRole formationRole, Color accentColor, float moveSpeedMultiplier, float contactDamageMultiplier)
        {
            ResolveReferences();
            _formationId = formationId ?? string.Empty;
            _formationRole = formationRole;
            _accentColor = accentColor;
            _formationSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier);
            _formationContactDamageMultiplier = Mathf.Max(0f, contactDamageMultiplier);
            _isApplied = true;
            ApplyResolvedFormationMultipliers();
            BuildMarker();
        }

        public void SetPriorityHint(EnemyFormationPriorityLevel priorityLevel, Color priorityColor)
        {
            if (priorityLevel == EnemyFormationPriorityLevel.None)
            {
                ClearPriorityHint();
                return;
            }

            _priorityLevel = priorityLevel;
            _priorityColor = priorityColor;
            BuildPriorityHintVisual();
        }

        public void ClearPriorityHint()
        {
            _priorityLevel = EnemyFormationPriorityLevel.None;
            _priorityColor = Color.white;
            ClearPriorityHintVisual();
        }

        public void ClearFormation()
        {
            if (!_isApplied && _markerRoot == null)
            {
                return;
            }

            _isApplied = false;
            _formationId = string.Empty;
            _formationRole = EnemyFormationRole.None;
            ClearFormationBreakState();
            _formationSpeedMultiplier = 1f;
            _formationContactDamageMultiplier = 1f;
            ClearPriorityHint();
            ClearCueState();
            enemyController?.SetFormationSpeedMultiplier(1f);
            enemyController?.SetFormationContactDamageMultiplier(1f);
            ClearMarker();
        }

        public bool HasFormation(string formationId)
        {
            return _isApplied && !string.IsNullOrWhiteSpace(formationId) && _formationId == formationId;
        }

        public void BroadcastCue(EnemyFormationCueType cueType, Vector2 anchorPosition, float windowDuration, float intensity = 1f)
        {
            if (!_isApplied || HasActiveFormationBreak() || cueType == EnemyFormationCueType.None)
            {
                return;
            }

            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;
            int cueSerial = ++s_CueSerialCounter;
            float cueLifetime = Mathf.Max(0.05f, windowDuration);
            float cueIntensity = Mathf.Clamp(intensity, 0.25f, 2f);

            for (int index = 0; index < s_ActiveModifiers.Count; index++)
            {
                EnemyFormationModifier candidate = s_ActiveModifiers[index];

                if (candidate == null
                    || !candidate._isApplied
                    || candidate._formationId != _formationId
                    || candidate.enemyHealth == null
                    || candidate.enemyHealth.IsDead)
                {
                    continue;
                }

                RoomController candidateRoom = candidate.roomEnemyMember != null ? candidate.roomEnemyMember.AssignedRoom : null;

                if (assignedRoom != null && candidateRoom != assignedRoom)
                {
                    continue;
                }

                candidate.ReceiveCue(cueType, anchorPosition, cueLifetime, cueIntensity, cueSerial);
            }
        }

        public bool TryConsumeCue(EnemyFormationCueType cueType, ref int lastConsumedCueSerial, out Vector2 cueAnchor, out float cueIntensity)
        {
            cueAnchor = Position;
            cueIntensity = 0f;

            if (!_isApplied
                || HasActiveFormationBreak()
                || cueType == EnemyFormationCueType.None
                || _activeCueType != cueType
                || _activeCueSerial <= lastConsumedCueSerial
                || Time.time > _activeCueExpiresAt)
            {
                return false;
            }

            lastConsumedCueSerial = _activeCueSerial;
            cueAnchor = _activeCueAnchor;
            cueIntensity = _activeCueIntensity;
            return true;
        }

        private void ApplyFormationBreakWindow(float duration, float freezeDuration, float speedMultiplier, float contactDamageMultiplier)
        {
            if (!_isApplied || enemyHealth == null || enemyHealth.IsDead)
            {
                return;
            }

            if (!HasActiveFormationBreak())
            {
                _breakSpeedMultiplier = 1f;
                _breakContactDamageMultiplier = 1f;
            }

            _breakWindowExpiresAt = Mathf.Max(_breakWindowExpiresAt, Time.time + Mathf.Max(0.15f, duration));
            _breakSpeedMultiplier = Mathf.Min(_breakSpeedMultiplier, Mathf.Clamp(speedMultiplier, 0.1f, 1f));
            _breakContactDamageMultiplier = Mathf.Min(_breakContactDamageMultiplier, Mathf.Clamp(contactDamageMultiplier, 0.1f, 1f));
            ClearCueState();
            enemyController?.ApplyFreeze(Mathf.Max(0f, freezeDuration));
            ApplyResolvedFormationMultipliers();
        }

        public bool TryGetNearestAllyPosition(EnemyFormationRole role, out Vector2 allyPosition)
        {
            allyPosition = Vector2.zero;

            if (!_isApplied || role == EnemyFormationRole.None)
            {
                return false;
            }

            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;
            float bestDistanceSq = float.MaxValue;
            bool found = false;

            for (int index = 0; index < s_ActiveModifiers.Count; index++)
            {
                EnemyFormationModifier candidate = s_ActiveModifiers[index];

                if (candidate == null
                    || candidate == this
                    || !candidate._isApplied
                    || candidate._formationRole != role
                    || candidate._formationId != _formationId
                    || candidate.enemyHealth == null
                    || candidate.enemyHealth.IsDead)
                {
                    continue;
                }

                if (assignedRoom != null)
                {
                    RoomController candidateRoom = candidate.roomEnemyMember != null ? candidate.roomEnemyMember.AssignedRoom : null;

                    if (candidateRoom != assignedRoom)
                    {
                        continue;
                    }
                }

                float distanceSq = (candidate.Position - Position).sqrMagnitude;

                if (distanceSq >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                allyPosition = candidate.Position;
                found = true;
            }

            return found;
        }

        public bool TryGetRoomBacklineAnchor(Vector2 targetPosition, float inset, out Vector2 anchorPosition)
        {
            anchorPosition = Position;
            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;

            if (assignedRoom == null)
            {
                return false;
            }

            Bounds roomBounds = assignedRoom.RoomBounds;

            if (roomBounds.size.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector2 roomCenter = roomBounds.center;
            Vector2 targetDirection = targetPosition - roomCenter;

            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                targetDirection = Position - roomCenter;
            }

            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                targetDirection = Vector2.up;
            }

            float backlineDistance = Mathf.Min(roomBounds.extents.x, roomBounds.extents.y) * 0.55f;
            anchorPosition = ClampPointToAssignedRoom(roomCenter - (targetDirection.normalized * backlineDistance), inset);
            return true;
        }

        public Vector2 ClampPointToAssignedRoom(Vector2 point, float inset)
        {
            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;

            if (assignedRoom == null)
            {
                return point;
            }

            Bounds roomBounds = assignedRoom.RoomBounds;
            Vector3 min = roomBounds.min + new Vector3(inset, inset, 0f);
            Vector3 max = roomBounds.max - new Vector3(inset, inset, 0f);

            if (min.x > max.x)
            {
                min.x = max.x = roomBounds.center.x;
            }

            if (min.y > max.y)
            {
                min.y = max.y = roomBounds.center.y;
            }

            return new Vector2(
                Mathf.Clamp(point.x, min.x, max.x),
                Mathf.Clamp(point.y, min.y, max.y));
        }

        private void SubscribeToHealthEvents()
        {
            if (_isSubscribedToHealthEvents || enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died += HandleEnemyDied;
            _isSubscribedToHealthEvents = true;
        }

        private void UnsubscribeFromHealthEvents()
        {
            if (!_isSubscribedToHealthEvents || enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died -= HandleEnemyDied;
            _isSubscribedToHealthEvents = false;
        }

        private void HandleEnemyDied()
        {
            if (!_isApplied || _priorityLevel != EnemyFormationPriorityLevel.Critical || string.IsNullOrWhiteSpace(_formationId))
            {
                return;
            }

            BroadcastFormationBreak();
        }

        private void BroadcastFormationBreak()
        {
            RoomController assignedRoom = roomEnemyMember != null ? roomEnemyMember.AssignedRoom : null;
            ResolveFormationBreakProfile(
                out float freezeDuration,
                out float breakDuration,
                out float speedMultiplier,
                out float contactDamageMultiplier,
                out Color accentColor,
                out string title,
                out string subtitle);

            int affectedCount = 0;

            for (int index = 0; index < s_ActiveModifiers.Count; index++)
            {
                EnemyFormationModifier candidate = s_ActiveModifiers[index];

                if (candidate == null
                    || candidate == this
                    || !candidate._isApplied
                    || candidate._formationId != _formationId
                    || candidate.enemyHealth == null
                    || candidate.enemyHealth.IsDead)
                {
                    continue;
                }

                RoomController candidateRoom = candidate.roomEnemyMember != null ? candidate.roomEnemyMember.AssignedRoom : null;

                if (assignedRoom != null && candidateRoom != assignedRoom)
                {
                    continue;
                }

                candidate.ApplyFormationBreakWindow(breakDuration, freezeDuration, speedMultiplier, contactDamageMultiplier);
                affectedCount++;
            }

            if (affectedCount <= 0)
            {
                return;
            }

            GameplayFeedbackEvents.RaiseBannerFeedback(new BannerFeedbackRequest(
                title,
                subtitle,
                accentColor,
                1.35f));

            GameplayFeedbackEvents.RaiseThreatFlash(new ThreatFlashRequest(
                accentColor,
                0.06f,
                0.28f,
                1,
                0.2f,
                1.08f,
                0.52f));

            GameplayRuntimeEvents.RaiseFormationBroken(new FormationBreakSignal(
                assignedRoom,
                _formationId,
                transform.position,
                breakDuration,
                affectedCount,
                accentColor));
        }

        private void ResolveFormationBreakProfile(
            out float freezeDuration,
            out float breakDuration,
            out float speedMultiplier,
            out float contactDamageMultiplier,
            out Color accentColor,
            out string title,
            out string subtitle)
        {
            freezeDuration = 0.28f;
            breakDuration = 1.85f;
            speedMultiplier = 0.76f;
            contactDamageMultiplier = 0.84f;
            accentColor = Color.Lerp(_accentColor, Color.white, 0.18f);
            title = "FORMATION BROKEN";
            subtitle = "Their timing slipped. Step in now.";

            switch (_formationId)
            {
                case "escort":
                    freezeDuration = 0.3f;
                    breakDuration = 1.9f;
                    speedMultiplier = 0.78f;
                    contactDamageMultiplier = 0.82f;
                    title = "ESCORT SCREEN BROKEN";
                    subtitle = "Backline is exposed. Collapse the front.";
                    break;
                case "crossfire":
                    freezeDuration = 0.34f;
                    breakDuration = 2f;
                    speedMultiplier = 0.72f;
                    contactDamageMultiplier = 0.8f;
                    title = "CROSSFIRE SHATTERED";
                    subtitle = "Kill lane is gone. Push through the gap.";
                    break;
                case "siege":
                    freezeDuration = 0.38f;
                    breakDuration = 2.15f;
                    speedMultiplier = 0.7f;
                    contactDamageMultiplier = 0.78f;
                    title = "SIEGE NEST BROKEN";
                    subtitle = "The nest is staggered. Take ground now.";
                    break;
            }
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (roomEnemyMember == null)
            {
                roomEnemyMember = GetComponent<RoomEnemyMember>();
            }
        }

        private void ReceiveCue(EnemyFormationCueType cueType, Vector2 anchorPosition, float cueLifetime, float cueIntensity, int cueSerial)
        {
            _activeCueType = cueType;
            _activeCueAnchor = anchorPosition;
            _activeCueExpiresAt = Time.time + cueLifetime;
            _activeCueIntensity = cueIntensity;
            _activeCueSerial = cueSerial;
        }

        private void ClearCueState()
        {
            _activeCueType = EnemyFormationCueType.None;
            _activeCueAnchor = Vector2.zero;
            _activeCueExpiresAt = 0f;
            _activeCueIntensity = 1f;
            _activeCueSerial = 0;
        }

        private void ApplyResolvedFormationMultipliers()
        {
            float effectiveSpeedMultiplier = _formationSpeedMultiplier * _breakSpeedMultiplier;
            float effectiveContactDamageMultiplier = _formationContactDamageMultiplier * _breakContactDamageMultiplier;
            enemyController?.SetFormationSpeedMultiplier(effectiveSpeedMultiplier);
            enemyController?.SetFormationContactDamageMultiplier(effectiveContactDamageMultiplier);
        }

        private void ClearFormationBreakState()
        {
            _breakWindowExpiresAt = 0f;
            _breakSpeedMultiplier = 1f;
            _breakContactDamageMultiplier = 1f;
        }

        private bool HasActiveFormationBreak()
        {
            if (_breakWindowExpiresAt <= 0f)
            {
                return false;
            }

            if (Time.time <= _breakWindowExpiresAt)
            {
                return true;
            }

            ClearFormationBreakState();
            ApplyResolvedFormationMultipliers();
            return false;
        }

        private void RegisterActiveModifier()
        {
            if (!s_ActiveModifiers.Contains(this))
            {
                s_ActiveModifiers.Add(this);
            }
        }

        private void UnregisterActiveModifier()
        {
            s_ActiveModifiers.Remove(this);
        }

        private void BuildMarker()
        {
            ClearMarker();

            GameObject root = new("FormationMarker");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            _markerBaseLocalPosition = new Vector3(0f, 0.72f, 0f);
            root.transform.localPosition = _markerBaseLocalPosition;
            _markerRoot = root.transform;
            _ringBaseScale = new Vector3(0.74f, 0.18f, 1f);

            _ringRenderer = CreatePart(
                "Ring",
                GetCircleSprite(),
                new Vector3(0f, -0.52f, 0f),
                _ringBaseScale,
                new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.24f),
                1);

            BuildFormationGlyph();
            BuildPriorityHintVisual();
        }

        private void BuildPriorityHintVisual()
        {
            ClearPriorityHintVisual();

            if (_markerRoot == null || _priorityLevel == EnemyFormationPriorityLevel.None)
            {
                return;
            }

            GameObject root = new("PriorityHint");
            root.layer = gameObject.layer;
            root.transform.SetParent(_markerRoot, false);
            root.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            _priorityRoot = root.transform;

            bool isCritical = _priorityLevel == EnemyFormationPriorityLevel.Critical;
            _priorityBaseScale = isCritical
                ? new Vector3(1.04f, 1.04f, 1f)
                : new Vector3(0.96f, 0.96f, 1f);

            Color pulseColor = new(_priorityColor.r, _priorityColor.g, _priorityColor.b, isCritical ? 0.28f : 0.18f);
            _priorityPulseRenderer = CreatePriorityPart(
                "PriorityPulse",
                GetCircleSprite(),
                Vector3.zero,
                isCritical ? new Vector3(0.72f, 0.72f, 1f) : new Vector3(0.58f, 0.58f, 1f),
                pulseColor,
                2);

            if (isCritical)
            {
                _priorityCoreRenderer = CreatePriorityPart(
                    "PriorityCore",
                    GetWhiteSprite(),
                    Vector3.zero,
                    new Vector3(0.18f, 0.18f, 1f),
                    Color.Lerp(_priorityColor, Color.white, 0.38f),
                    6,
                    45f);

                CreatePriorityPart(
                    "PriorityBarA",
                    GetWhiteSprite(),
                    Vector3.zero,
                    new Vector3(0.34f, 0.038f, 1f),
                    new Color(_priorityColor.r, _priorityColor.g, _priorityColor.b, 0.84f),
                    5,
                    45f);

                CreatePriorityPart(
                    "PriorityBarB",
                    GetWhiteSprite(),
                    Vector3.zero,
                    new Vector3(0.34f, 0.038f, 1f),
                    new Color(_priorityColor.r, _priorityColor.g, _priorityColor.b, 0.84f),
                    5,
                    -45f);
            }
            else
            {
                _priorityCoreRenderer = CreatePriorityPart(
                    "PriorityCore",
                    GetCircleSprite(),
                    Vector3.zero,
                    new Vector3(0.2f, 0.2f, 1f),
                    Color.Lerp(_priorityColor, Color.white, 0.18f),
                    5);

                CreatePriorityPart(
                    "PriorityUnderline",
                    GetWhiteSprite(),
                    new Vector3(0f, -0.18f, 0f),
                    new Vector3(0.22f, 0.032f, 1f),
                    new Color(_priorityColor.r, _priorityColor.g, _priorityColor.b, 0.74f),
                    4);
            }
        }

        private SpriteRenderer CreatePart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder, float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_markerRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private SpriteRenderer CreatePriorityPart(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder, float rotationZ = 0f)
        {
            GameObject child = new(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(_priorityRoot, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void BuildFormationGlyph()
        {
            Color primaryColor = Color.Lerp(_accentColor, Color.white, 0.24f);
            Color secondaryColor = new(_accentColor.r, _accentColor.g, _accentColor.b, 0.92f);

            switch (_formationId)
            {
                case "escort":
                    CreatePart("EscortCore", GetWhiteSprite(), Vector3.zero, new Vector3(0.16f, 0.16f, 1f), primaryColor, 4, 45f);
                    CreatePart("EscortWingLeft", GetWhiteSprite(), new Vector3(-0.16f, -0.02f, 0f), new Vector3(0.14f, 0.05f, 1f), secondaryColor, 3, 24f);
                    CreatePart("EscortWingRight", GetWhiteSprite(), new Vector3(0.16f, -0.02f, 0f), new Vector3(0.14f, 0.05f, 1f), secondaryColor, 3, -24f);
                    break;
                case "crossfire":
                    CreatePart("CrossfireSlashA", GetWhiteSprite(), Vector3.zero, new Vector3(0.36f, 0.045f, 1f), secondaryColor, 3, 42f);
                    CreatePart("CrossfireSlashB", GetWhiteSprite(), Vector3.zero, new Vector3(0.36f, 0.045f, 1f), secondaryColor, 3, -42f);
                    CreatePart("CrossfireCore", GetCircleSprite(), Vector3.zero, new Vector3(0.14f, 0.14f, 1f), primaryColor, 4);
                    break;
                case "siege":
                    CreatePart("SiegeTop", GetWhiteSprite(), new Vector3(0f, 0.08f, 0f), new Vector3(0.34f, 0.055f, 1f), secondaryColor, 3);
                    CreatePart("SiegeLeft", GetWhiteSprite(), new Vector3(-0.13f, -0.04f, 0f), new Vector3(0.06f, 0.22f, 1f), secondaryColor, 3);
                    CreatePart("SiegeRight", GetWhiteSprite(), new Vector3(0.13f, -0.04f, 0f), new Vector3(0.06f, 0.22f, 1f), secondaryColor, 3);
                    CreatePart("SiegeCore", GetWhiteSprite(), new Vector3(0f, -0.05f, 0f), new Vector3(0.12f, 0.08f, 1f), primaryColor, 4);
                    break;
                default:
                    CreatePart("Badge", GetWhiteSprite(), Vector3.zero, new Vector3(0.22f, 0.22f, 1f), primaryColor, 3);
                    break;
            }
        }

        private void ClearMarker()
        {
            if (_markerRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_markerRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_markerRoot.gameObject);
            }

            _markerRoot = null;
            _ringRenderer = null;
            _priorityRoot = null;
            _priorityPulseRenderer = null;
            _priorityCoreRenderer = null;
            _priorityBaseScale = Vector3.one;
        }

        private void ClearPriorityHintVisual()
        {
            if (_priorityRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_priorityRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_priorityRoot.gameObject);
            }

            _priorityRoot = null;
            _priorityPulseRenderer = null;
            _priorityCoreRenderer = null;
            _priorityBaseScale = Vector3.one;
        }

        private bool IsCueRelevantToPriority()
        {
            if (_priorityLevel == EnemyFormationPriorityLevel.None
                || _activeCueType == EnemyFormationCueType.None
                || Time.time > _activeCueExpiresAt)
            {
                return false;
            }

            return _formationId switch
            {
                "escort" => _activeCueType == EnemyFormationCueType.EscortSurge,
                "crossfire" => _activeCueType == EnemyFormationCueType.CrossfireLock,
                "siege" => _activeCueType == EnemyFormationCueType.SiegePressure,
                _ => false
            };
        }

        private static Sprite GetWhiteSprite()
        {
            if (s_WhiteSprite != null)
            {
                return s_WhiteSprite;
            }

            s_WhiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_WhiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null)
            {
                return s_CircleSprite;
            }

            const int size = 48;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeFormationCircle"
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - normalizedDistance);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            s_CircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_CircleSprite;
        }
    }

    public static class EnemyFormationTactics
    {
        public static bool TryPrimeCrossfireCue(
            EnemyFormationModifier modifier,
            ref int lastConsumedCueSerial,
            ref float cueWindowRemaining,
            ref Vector2 cueAnchor,
            ref float cooldown,
            float fixedDeltaTime,
            float cooldownClamp = 0.12f)
        {
            cueWindowRemaining = Mathf.Max(0f, cueWindowRemaining - fixedDeltaTime);

            if (modifier == null
                || !modifier.TryConsumeCue(EnemyFormationCueType.CrossfireLock, ref lastConsumedCueSerial, out cueAnchor, out float cueIntensity))
            {
                return false;
            }

            cueWindowRemaining = Mathf.Max(cueWindowRemaining, Mathf.Lerp(0.4f, 0.82f, Mathf.Clamp01(cueIntensity)));
            cooldown = Mathf.Min(cooldown, Mathf.Max(0f, cooldownClamp));
            return true;
        }

        public static bool TryPrimeSiegeCue(
            EnemyFormationModifier modifier,
            ref int lastConsumedCueSerial,
            ref float cueWindowRemaining,
            ref Vector2 cueAnchor,
            ref float cooldown,
            float fixedDeltaTime,
            float cooldownClamp = 0.22f)
        {
            cueWindowRemaining = Mathf.Max(0f, cueWindowRemaining - fixedDeltaTime);

            if (modifier == null
                || !modifier.TryConsumeCue(EnemyFormationCueType.SiegePressure, ref lastConsumedCueSerial, out cueAnchor, out float cueIntensity))
            {
                return false;
            }

            cueWindowRemaining = Mathf.Max(cueWindowRemaining, Mathf.Lerp(0.45f, 0.9f, Mathf.Clamp01(cueIntensity)));
            cooldown = Mathf.Min(cooldown, Mathf.Max(0f, cooldownClamp));
            return true;
        }

        public static void BroadcastCrossfireCue(EnemyFormationModifier modifier, Vector2 anchorPosition, float duration = 0.72f, float intensity = 1f)
        {
            modifier?.BroadcastCue(EnemyFormationCueType.CrossfireLock, anchorPosition, duration, intensity);
        }

        public static void BroadcastSiegeCue(EnemyFormationModifier modifier, Vector2 anchorPosition, float duration = 0.84f, float intensity = 1f)
        {
            modifier?.BroadcastCue(EnemyFormationCueType.SiegePressure, anchorPosition, duration, intensity);
        }

        public static Vector2 ResolveCueAimDirection(Vector2 currentPosition, Vector2 fallbackAimDirection, float cueWindowRemaining, Vector2 cueAnchor)
        {
            if (cueWindowRemaining <= 0.001f)
            {
                return NormalizeOrFallback(fallbackAimDirection, Vector2.right, Vector2.up);
            }

            Vector2 cueDirection = NormalizeOrFallback(cueAnchor - currentPosition, fallbackAimDirection, Vector2.right);
            return BlendNormalized(fallbackAimDirection, cueDirection, Mathf.Lerp(0.35f, 0.82f, Mathf.Clamp01(cueWindowRemaining / 0.8f)));
        }

        public static float ResolveCueTelegraphDuration(float baseDuration, float cueWindowRemaining, float quickMultiplier = 0.62f)
        {
            return cueWindowRemaining > 0.001f
                ? baseDuration * Mathf.Clamp(quickMultiplier, 0.2f, 1f)
                : baseDuration;
        }

        public static Vector2 ResolveEscortSupportMove(
            EnemyFormationModifier modifier,
            Vector2 currentPosition,
            Vector2 targetPosition,
            Vector2 fallbackMoveDirection,
            float behindDistance = 1.2f,
            float lateralOffset = 0.7f)
        {
            if (modifier == null
                || modifier.FormationRole != EnemyFormationRole.Support
                || !modifier.HasFormation("escort")
                || !modifier.TryGetNearestAllyPosition(EnemyFormationRole.Frontline, out Vector2 frontlinePosition))
            {
                return fallbackMoveDirection;
            }

            Vector2 screenDirection = NormalizeOrFallback(targetPosition - frontlinePosition, targetPosition - currentPosition, Vector2.up);
            Vector2 lateralDirection = Perpendicular(screenDirection, modifier.StableSideSign);
            Vector2 anchorPosition = modifier.ClampPointToAssignedRoom(
                frontlinePosition - (screenDirection * behindDistance) + (lateralDirection * lateralOffset),
                0.35f);
            return ResolveAnchorMove(currentPosition, targetPosition, anchorPosition, fallbackMoveDirection, modifier.StableSideSign, 0.42f, 0.78f);
        }

        public static Vector2 ResolveEscortFrontlineMove(
            EnemyFormationModifier modifier,
            Vector2 currentPosition,
            Vector2 targetPosition,
            Vector2 fallbackMoveDirection,
            float screenDistance = 1.1f,
            float lateralOffset = 0.42f)
        {
            if (modifier == null
                || modifier.FormationRole != EnemyFormationRole.Frontline
                || !modifier.HasFormation("escort")
                || !modifier.TryGetNearestAllyPosition(EnemyFormationRole.Support, out Vector2 supportPosition))
            {
                return fallbackMoveDirection;
            }

            Vector2 screenDirection = NormalizeOrFallback(targetPosition - supportPosition, targetPosition - currentPosition, Vector2.right);
            Vector2 lateralDirection = Perpendicular(screenDirection, modifier.StableSideSign);
            Vector2 anchorPosition = modifier.ClampPointToAssignedRoom(
                supportPosition + (screenDirection * screenDistance) + (lateralDirection * lateralOffset),
                0.3f);
            Vector2 anchorMove = ResolveAnchorMove(currentPosition, targetPosition, anchorPosition, fallbackMoveDirection, modifier.StableSideSign, 0.45f, 0.52f);
            return BlendNormalized(fallbackMoveDirection, anchorMove, 0.58f);
        }

        public static Vector2 ResolveCrossfireControllerMove(
            EnemyFormationModifier modifier,
            Vector2 currentPosition,
            Vector2 targetPosition,
            Vector2 fallbackMoveDirection,
            float desiredRadius = 1.95f,
            float lateralOffset = 0.6f)
        {
            if (modifier == null
                || modifier.FormationRole != EnemyFormationRole.Controller
                || !modifier.HasFormation("crossfire")
                || !modifier.TryGetNearestAllyPosition(EnemyFormationRole.Ranged, out Vector2 rangedPosition))
            {
                return fallbackMoveDirection;
            }

            Vector2 anchorDirection = NormalizeOrFallback(targetPosition - rangedPosition, currentPosition - targetPosition, Vector2.right);
            Vector2 lateralDirection = Perpendicular(anchorDirection, modifier.StableSideSign);
            Vector2 anchorPosition = modifier.ClampPointToAssignedRoom(
                targetPosition + (anchorDirection * desiredRadius) + (lateralDirection * lateralOffset),
                0.4f);
            return ResolveAnchorMove(currentPosition, targetPosition, anchorPosition, fallbackMoveDirection, modifier.StableSideSign, 0.48f, 0.72f);
        }

        public static Vector2 ResolveCrossfireRangedMove(
            EnemyFormationModifier modifier,
            Vector2 currentPosition,
            Vector2 targetPosition,
            Vector2 fallbackMoveDirection,
            float desiredRadius = 3.2f,
            float lateralOffset = 0.85f)
        {
            if (modifier == null
                || modifier.FormationRole != EnemyFormationRole.Ranged
                || !modifier.HasFormation("crossfire")
                || !modifier.TryGetNearestAllyPosition(EnemyFormationRole.Controller, out Vector2 controllerPosition))
            {
                return fallbackMoveDirection;
            }

            Vector2 anchorDirection = NormalizeOrFallback(targetPosition - controllerPosition, currentPosition - targetPosition, Vector2.left);
            Vector2 lateralDirection = Perpendicular(anchorDirection, modifier.StableSideSign);
            Vector2 anchorPosition = modifier.ClampPointToAssignedRoom(
                targetPosition + (anchorDirection * desiredRadius) + (lateralDirection * lateralOffset),
                0.45f);
            Vector2 anchorMove = ResolveAnchorMove(currentPosition, targetPosition, anchorPosition, fallbackMoveDirection, modifier.StableSideSign, 0.55f, 0.86f);
            return BlendNormalized(fallbackMoveDirection, anchorMove, 0.72f);
        }

        public static Vector2 ResolveSiegeBacklineMove(
            EnemyFormationModifier modifier,
            Vector2 currentPosition,
            Vector2 targetPosition,
            Vector2 fallbackMoveDirection,
            float inset = 0.6f)
        {
            if (modifier == null
                || modifier.FormationRole != EnemyFormationRole.Siege
                || !modifier.HasFormation("siege")
                || !modifier.TryGetRoomBacklineAnchor(targetPosition, inset, out Vector2 backlineAnchor))
            {
                return fallbackMoveDirection;
            }

            Vector2 anchorMove = ResolveAnchorMove(currentPosition, targetPosition, backlineAnchor, fallbackMoveDirection, modifier.StableSideSign, 0.62f, 0.58f);
            return BlendNormalized(fallbackMoveDirection, anchorMove, 0.8f);
        }

        public static Vector3 ResolveSiegeSummonPosition(
            EnemyFormationModifier modifier,
            Transform summonAnchor,
            Vector2 targetPosition,
            int summonIndex,
            int summonCount,
            float summonScatterRadius)
        {
            Vector2 anchorPosition = summonAnchor != null ? (Vector2)summonAnchor.position : Vector2.zero;
            float fallbackAngle = ((360f / Mathf.Max(1, summonCount)) * summonIndex) * Mathf.Deg2Rad;
            Vector2 fallbackPosition = anchorPosition + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * summonScatterRadius;

            if (modifier == null
                || modifier.FormationRole != EnemyFormationRole.Siege
                || !modifier.HasFormation("siege")
                || !modifier.TryGetRoomBacklineAnchor(targetPosition, 0.65f, out Vector2 backlineAnchor))
            {
                return fallbackPosition;
            }

            Vector2 forwardDirection = NormalizeOrFallback(targetPosition - backlineAnchor, targetPosition - (Vector2)anchorPosition, Vector2.up);
            Vector2 lateralDirection = Perpendicular(forwardDirection, 1f);
            float normalizedSlot = summonCount <= 1
                ? 0f
                : (summonIndex / (float)(summonCount - 1) - 0.5f) * 2f;
            Vector2 lanePosition = backlineAnchor
                + (forwardDirection * Mathf.Max(0.45f, summonScatterRadius * 0.55f))
                + (lateralDirection * summonScatterRadius * (normalizedSlot + (modifier.StableSideSign * 0.2f)));
            return modifier.ClampPointToAssignedRoom(lanePosition, 0.45f);
        }

        private static Vector2 ResolveAnchorMove(
            Vector2 currentPosition,
            Vector2 targetPosition,
            Vector2 anchorPosition,
            Vector2 fallbackMoveDirection,
            float sideSign,
            float arriveDistance,
            float orbitBlend)
        {
            Vector2 toAnchor = anchorPosition - currentPosition;

            if (toAnchor.sqrMagnitude > arriveDistance * arriveDistance)
            {
                return toAnchor.normalized;
            }

            Vector2 fallbackDirection = NormalizeOrFallback(fallbackMoveDirection, targetPosition - currentPosition, Vector2.up);
            Vector2 aimDirection = NormalizeOrFallback(targetPosition - currentPosition, fallbackDirection, Vector2.up);
            Vector2 strafeDirection = Perpendicular(aimDirection, sideSign);
            return BlendNormalized(fallbackDirection, strafeDirection, orbitBlend);
        }

        private static Vector2 BlendNormalized(Vector2 primary, Vector2 secondary, float blend)
        {
            Vector2 primaryDirection = NormalizeOrFallback(primary, secondary, Vector2.zero);
            Vector2 secondaryDirection = NormalizeOrFallback(secondary, primaryDirection, Vector2.zero);
            Vector2 blendedDirection = Vector2.Lerp(primaryDirection, secondaryDirection, Mathf.Clamp01(blend));
            return blendedDirection.sqrMagnitude <= 0.0001f ? primaryDirection : blendedDirection.normalized;
        }

        private static Vector2 NormalizeOrFallback(Vector2 value, Vector2 fallback, Vector2 secondaryFallback)
        {
            if (value.sqrMagnitude > 0.0001f)
            {
                return value.normalized;
            }

            if (fallback.sqrMagnitude > 0.0001f)
            {
                return fallback.normalized;
            }

            return secondaryFallback.sqrMagnitude > 0.0001f
                ? secondaryFallback.normalized
                : Vector2.zero;
        }

        private static Vector2 Perpendicular(Vector2 direction, float sideSign)
        {
            Vector2 normalizedDirection = NormalizeOrFallback(direction, Vector2.right, Vector2.up);
            return new Vector2(-normalizedDirection.y, normalizedDirection.x * Mathf.Sign(sideSign));
        }
    }
}
