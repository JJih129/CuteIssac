using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Room;
using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Applies floor-driven room visuals without touching room gameplay logic.
    /// Existing SpriteRenderers remain as safe fallbacks when no replacement prefabs are assigned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomThemeController : MonoBehaviour
    {
        private const float WholeRoomVisualViewportPadding = 1.1f;
        private const float WholeRoomGeometryViewportPadding = 1.0f;

        [Header("Fallback Renderers")]
        [Tooltip("Fallback floor renderer tinted when no floor prefab override is assigned.")]
        [SerializeField] private SpriteRenderer floorRenderer;
        [Tooltip("Fallback wall renderers tinted when no wall prefab override is assigned.")]
        [SerializeField] private SpriteRenderer[] wallRenderers;
        [Tooltip("Fallback door renderers tinted when no door prefab override is assigned.")]
        [SerializeField] private SpriteRenderer[] doorRenderers;

        [Header("Visual Anchors")]
        [Tooltip("Anchor used for a whole-room theme visual. Falls back to the floor anchor when empty.")]
        [SerializeField] private Transform roomVisualAnchor;
        [Tooltip("Anchor used for themed floor prefab overrides.")]
        [SerializeField] private Transform floorAnchor;
        [Tooltip("Anchors used for themed wall prefab overrides.")]
        [SerializeField] private Transform[] wallAnchors;
        [Tooltip("Anchors used for themed door prefab overrides.")]
        [SerializeField] private Transform[] doorAnchors;
        [Tooltip("Anchor used for optional decoration prefabs.")]
        [SerializeField] private Transform decorationAnchor;
        [Tooltip("Anchor used for special room-type landmarks such as treasure pedestals or shop signs.")]
        [SerializeField] private Transform landmarkAnchor;
        [Tooltip("Optional parent used to keep spawned theme objects grouped under the room.")]
        [SerializeField] private Transform spawnedThemeParent;

        [Header("Visibility")]
        [Tooltip("When enabled, spawned theme visuals are only shown for the current room so enlarged room art does not bleed in from adjacent rooms.")]
        [SerializeField] private bool hideThemeVisualsWhenRoomNotCurrent = true;

        private readonly List<GameObject> _spawnedThemeObjects = new();
        private readonly List<RoomThemeData.RoomTypeLandmarkEntry> _landmarkEntryBuffer = new();
        private readonly List<Transform> _spawnedLandmarkTransforms = new();
        private RoomThemeData _appliedTheme;
        private RoomController _roomController;

        public RoomThemeData AppliedTheme => _appliedTheme;

        public bool TryResolveLandmarkFocusTarget(out Vector3 focusPosition, out float focusRadius)
        {
            Vector3 accumulatedPosition = Vector3.zero;
            int activeLandmarkCount = 0;

            for (int i = 0; i < _spawnedLandmarkTransforms.Count; i++)
            {
                Transform landmarkTransform = _spawnedLandmarkTransforms[i];

                if (landmarkTransform == null)
                {
                    continue;
                }

                accumulatedPosition += landmarkTransform.position;
                activeLandmarkCount++;
            }

            if (activeLandmarkCount > 0)
            {
                focusPosition = accumulatedPosition / activeLandmarkCount;
                focusRadius = Mathf.Max(1.1f, 0.84f + (activeLandmarkCount * 0.18f));
                return true;
            }

            Transform anchor = landmarkAnchor != null
                ? landmarkAnchor
                : decorationAnchor != null
                    ? decorationAnchor
                    : floorAnchor != null
                        ? floorAnchor
                        : transform;
            focusPosition = anchor.position;
            focusRadius = 1.02f;
            return landmarkAnchor != null || decorationAnchor != null || floorAnchor != null;
        }

        public void CollectLandmarkTargets(List<Transform> targetBuffer)
        {
            if (targetBuffer == null)
            {
                return;
            }

            for (int i = 0; i < _spawnedLandmarkTransforms.Count; i++)
            {
                Transform landmarkTransform = _spawnedLandmarkTransforms[i];

                if (landmarkTransform == null)
                {
                    continue;
                }

                targetBuffer.Add(landmarkTransform);
            }
        }

        private void Awake()
        {
            ResolveRoomController();
        }

        private void OnEnable()
        {
            ResolveRoomController();
            RefreshSpawnedThemeVisibility();
        }

        private void LateUpdate()
        {
            RefreshSpawnedThemeVisibility();
        }

        /// <summary>
        /// Generated-room setup calls this after instantiation so floors can reskin rooms per theme asset.
        /// </summary>
        public void ApplyTheme(RoomThemeData roomTheme)
        {
            ResolveRoomController();
            _appliedTheme = roomTheme;
            ClearDoorRuntimeStateObjects();
            ClearSpawnedThemeObjects();

            bool useWholeRoomVisual = roomTheme != null && roomTheme.RoomVisualPrefab != null;

            ApplyFallbackColors(roomTheme, useWholeRoomVisual);

            if (useWholeRoomVisual)
            {
                ApplyWholeRoomPrefabOverride(roomTheme);
                ApplyPrefabOverrides(roomTheme != null ? roomTheme.DoorVisualPrefab : null, doorAnchors, doorRenderers);
            }
            else
            {
                ApplyPrefabOverride(roomTheme != null ? roomTheme.FloorVisualPrefab : null, floorAnchor, floorRenderer);
                ApplyPrefabOverrides(roomTheme != null ? roomTheme.WallVisualPrefab : null, wallAnchors, wallRenderers);
                ApplyPrefabOverrides(roomTheme != null ? roomTheme.DoorVisualPrefab : null, doorAnchors, doorRenderers);
            }

            SpawnDecorations(roomTheme);
            SpawnRoomTypeLandmarks(roomTheme);
            RefreshSpawnedThemeVisibility();
        }

        private void ResolveRoomController()
        {
            if (_roomController == null)
            {
                _roomController = GetComponent<RoomController>();
            }
        }

        private void ApplyFallbackColors(RoomThemeData roomTheme, bool useWholeRoomVisual)
        {
            if (floorRenderer != null)
            {
                floorRenderer.enabled = !useWholeRoomVisual || roomTheme == null || !roomTheme.HideFallbackGeometryWhenRoomVisualIsPresent;
            }

            if (roomTheme == null)
            {
                ReactivateFallbacks(wallRenderers);
                ReactivateFallbacks(doorRenderers);
                return;
            }

            if (floorRenderer != null)
            {
                floorRenderer.color = roomTheme.FloorColor;
            }

            bool showFallbackGeometry = !useWholeRoomVisual || !roomTheme.HideFallbackGeometryWhenRoomVisualIsPresent;
            ApplyRendererColors(wallRenderers, roomTheme.WallColor, showFallbackGeometry);
            ApplyRendererColors(doorRenderers, roomTheme.DoorColor, showFallbackGeometry);
        }

        private static void ApplyRendererColors(SpriteRenderer[] renderers, Color color, bool enabled)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = enabled;
                renderer.color = color;
            }
        }

        private void ApplyWholeRoomPrefabOverride(RoomThemeData roomTheme)
        {
            if (roomTheme == null || roomTheme.RoomVisualPrefab == null)
            {
                return;
            }

            Transform anchor = roomVisualAnchor != null
                ? roomVisualAnchor
                : floorAnchor != null
                    ? floorAnchor
                    : transform;
            Transform parent = spawnedThemeParent != null ? spawnedThemeParent : anchor;
            GameObject spawnedObject = Instantiate(roomTheme.RoomVisualPrefab, anchor.position, anchor.rotation, parent);
            spawnedObject.name = $"{roomTheme.RoomVisualPrefab.name}_RoomThemeVisual";
            spawnedObject.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            spawnedObject.transform.localScale = Vector3.one;
            FitWholeRoomGeometry();
            FitWholeRoomVisual(spawnedObject);
            _spawnedThemeObjects.Add(spawnedObject);
        }

        private void FitWholeRoomGeometry()
        {
            if (!TryGetComponent(out RoomController roomController))
            {
                return;
            }

            if (!TryGetComponent(out BoxCollider2D roomBoundsTrigger))
            {
                return;
            }

            Camera mainCamera = Camera.main;

            if (mainCamera == null || !mainCamera.orthographic)
            {
                return;
            }

            Vector2 originalSize = roomBoundsTrigger.size;

            if (originalSize.x <= 0.001f || originalSize.y <= 0.001f)
            {
                return;
            }

            float viewportHeight = mainCamera.orthographicSize * 2f;
            float viewportWidth = viewportHeight * mainCamera.aspect;
            Vector2 targetSize = new Vector2(
                Mathf.Max(originalSize.x, viewportWidth * WholeRoomGeometryViewportPadding),
                Mathf.Max(originalSize.y, viewportHeight * WholeRoomGeometryViewportPadding));

            float scaleX = targetSize.x / originalSize.x;
            float scaleY = targetSize.y / originalSize.y;

            roomBoundsTrigger.size = targetSize;
            roomBoundsTrigger.offset = new Vector2(roomBoundsTrigger.offset.x * scaleX, roomBoundsTrigger.offset.y * scaleY);

            BoxCollider2D[] rootColliders = GetComponents<BoxCollider2D>();

            for (int i = 0; i < rootColliders.Length; i++)
            {
                BoxCollider2D collider = rootColliders[i];

                if (collider == null || collider == roomBoundsTrigger)
                {
                    continue;
                }

                collider.offset = new Vector2(collider.offset.x * scaleX, collider.offset.y * scaleY);
                collider.size = new Vector2(collider.size.x * scaleX, collider.size.y * scaleY);
            }

            IReadOnlyList<RoomDoor> roomDoors = roomController.RoomDoors;

            for (int i = 0; i < roomDoors.Count; i++)
            {
                RoomDoor roomDoor = roomDoors[i];

                if (roomDoor == null)
                {
                    continue;
                }

                Transform doorTransform = roomDoor.transform;
                Vector3 localPosition = doorTransform.localPosition;
                doorTransform.localPosition = new Vector3(localPosition.x * scaleX, localPosition.y * scaleY, localPosition.z);

                Collider2D[] doorColliders = roomDoor.GetComponents<Collider2D>();

                for (int colliderIndex = 0; colliderIndex < doorColliders.Length; colliderIndex++)
                {
                    if (doorColliders[colliderIndex] is not BoxCollider2D boxCollider)
                    {
                        continue;
                    }

                    boxCollider.offset = new Vector2(boxCollider.offset.x * scaleX, boxCollider.offset.y * scaleY);
                    boxCollider.size = new Vector2(boxCollider.size.x * scaleX, boxCollider.size.y * scaleY);
                }
            }
        }

        private void FitWholeRoomVisual(GameObject spawnedObject)
        {
            if (spawnedObject == null)
            {
                return;
            }

            SpriteRenderer[] renderers = spawnedObject.GetComponentsInChildren<SpriteRenderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Bounds visualBounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                visualBounds.Encapsulate(renderers[i].bounds);
            }

            if (visualBounds.size.x <= 0.001f || visualBounds.size.y <= 0.001f)
            {
                return;
            }

            Vector3 targetCenter = transform.position;
            Vector2 targetSize = new Vector2(visualBounds.size.x, visualBounds.size.y);

            if (TryGetComponent(out RoomController roomController))
            {
                Bounds roomBounds = roomController.RoomBounds;
                targetCenter = roomBounds.center;
                targetSize = new Vector2(roomBounds.size.x, roomBounds.size.y);
            }

            Camera mainCamera = Camera.main;

            if (mainCamera != null && mainCamera.orthographic)
            {
                float viewportHeight = mainCamera.orthographicSize * 2f;
                float viewportWidth = viewportHeight * mainCamera.aspect;
                targetSize.x = Mathf.Max(targetSize.x, viewportWidth * WholeRoomVisualViewportPadding);
                targetSize.y = Mathf.Max(targetSize.y, viewportHeight * WholeRoomVisualViewportPadding);
            }

            float widthScale = targetSize.x / visualBounds.size.x;
            float heightScale = targetSize.y / visualBounds.size.y;
            float scale = Mathf.Max(widthScale, heightScale);

            spawnedObject.transform.position = new Vector3(targetCenter.x, targetCenter.y, spawnedObject.transform.position.z);
            spawnedObject.transform.localScale *= scale;
        }

        private void ApplyPrefabOverrides(GameObject prefab, Transform[] anchors, SpriteRenderer[] fallbackRenderers)
        {
            if (anchors == null || anchors.Length == 0)
            {
                return;
            }

            for (int i = 0; i < anchors.Length; i++)
            {
                Transform anchor = anchors[i];
                SpriteRenderer fallbackRenderer = fallbackRenderers != null && i < fallbackRenderers.Length ? fallbackRenderers[i] : null;
                ApplyPrefabOverride(prefab, anchor, fallbackRenderer);
            }
        }

        private void ApplyPrefabOverride(GameObject prefab, Transform anchor, SpriteRenderer fallbackRenderer)
        {
            if (anchor == null)
            {
                return;
            }

            if (prefab == null)
            {
                if (fallbackRenderer != null)
                {
                    fallbackRenderer.enabled = true;
                }

                return;
            }

            GameObject spawnedObject = Instantiate(prefab, anchor, false);
            spawnedObject.name = $"{prefab.name}_ThemeVisual";
            _spawnedThemeObjects.Add(spawnedObject);

            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled = false;
            }

        }

        private void SpawnDecorations(RoomThemeData roomTheme)
        {
            if (roomTheme == null || decorationAnchor == null)
            {
                return;
            }

            IReadOnlyList<GameObject> decorationPrefabs = roomTheme.DecorationPrefabs;

            for (int i = 0; i < decorationPrefabs.Count; i++)
            {
                GameObject decorationPrefab = decorationPrefabs[i];

                if (decorationPrefab == null)
                {
                    continue;
                }

                Transform parent = spawnedThemeParent != null ? spawnedThemeParent : decorationAnchor;
                GameObject decorationObject = Instantiate(decorationPrefab, decorationAnchor.position, decorationAnchor.rotation, parent);
                decorationObject.name = $"{decorationPrefab.name}_Decoration";
                _spawnedThemeObjects.Add(decorationObject);
            }
        }

        private void SpawnRoomTypeLandmarks(RoomThemeData roomTheme)
        {
            if (roomTheme == null)
            {
                return;
            }

            ResolveRoomController();

            if (_roomController == null)
            {
                return;
            }

            _landmarkEntryBuffer.Clear();
            roomTheme.CollectLandmarkEntries(_roomController.RoomType, _landmarkEntryBuffer);

            if (_landmarkEntryBuffer.Count == 0)
            {
                return;
            }

            Transform anchor = landmarkAnchor != null
                ? landmarkAnchor
                : decorationAnchor != null
                    ? decorationAnchor
                    : floorAnchor != null
                        ? floorAnchor
                        : transform;

            for (int index = 0; index < _landmarkEntryBuffer.Count; index++)
            {
                SpawnRoomTypeLandmark(anchor, roomTheme, _roomController.RoomType, _landmarkEntryBuffer[index]);
            }
        }

        private void SpawnRoomTypeLandmark(
            Transform anchor,
            RoomThemeData roomTheme,
            RoomType roomType,
            RoomThemeData.RoomTypeLandmarkEntry landmarkEntry)
        {
            if (anchor == null)
            {
                return;
            }

            GameObject spawnedObject = null;

            if (landmarkEntry.LandmarkPrefab != null)
            {
                spawnedObject = Instantiate(landmarkEntry.LandmarkPrefab, anchor, false);
                spawnedObject.name = $"{roomType}_{landmarkEntry.LandmarkPrefab.name}_Landmark";
            }
            else if (landmarkEntry.UseRuntimeFallback)
            {
                spawnedObject = new GameObject($"{roomType}_RuntimeLandmark");
                spawnedObject.transform.SetParent(anchor, false);
                RoomLandmarkVisual roomLandmarkVisual = spawnedObject.AddComponent<RoomLandmarkVisual>();
                ApplyLandmarkTransform(spawnedObject.transform, landmarkEntry);
                roomLandmarkVisual.Configure(roomType, roomTheme);
            }

            if (spawnedObject == null)
            {
                return;
            }

            ApplyLandmarkTransform(spawnedObject.transform, landmarkEntry);

            _spawnedThemeObjects.Add(spawnedObject);
            _spawnedLandmarkTransforms.Add(spawnedObject.transform);
        }

        private static void ApplyLandmarkTransform(Transform targetTransform, RoomThemeData.RoomTypeLandmarkEntry landmarkEntry)
        {
            if (targetTransform == null)
            {
                return;
            }

            targetTransform.localPosition = new Vector3(landmarkEntry.LocalOffset.x, landmarkEntry.LocalOffset.y, 0f);
            targetTransform.localRotation = Quaternion.Euler(0f, 0f, landmarkEntry.RotationZ);
            targetTransform.localScale = landmarkEntry.Scale;
        }

        private void ClearSpawnedThemeObjects()
        {
            for (int i = _spawnedThemeObjects.Count - 1; i >= 0; i--)
            {
                GameObject spawnedObject = _spawnedThemeObjects[i];

                if (spawnedObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(spawnedObject);
                }
                else
                {
                    DestroyImmediate(spawnedObject);
                }
            }

            _spawnedThemeObjects.Clear();
            _spawnedLandmarkTransforms.Clear();
            ClearDoorRuntimeStateObjects();

            ReactivateFallbacks(wallRenderers);
            ReactivateFallbacks(doorRenderers);

            if (floorRenderer != null)
            {
                floorRenderer.enabled = true;

                if (floorRenderer.transform != null)
                {
                    floorRenderer.transform.gameObject.SetActive(true);
                }
            }
        }

        private void RefreshSpawnedThemeVisibility()
        {
            if (_spawnedThemeObjects.Count == 0)
            {
                return;
            }

            bool shouldShow = !hideThemeVisualsWhenRoomNotCurrent || _roomController == null || _roomController.IsCurrentRoom;

            for (int i = 0; i < _spawnedThemeObjects.Count; i++)
            {
                GameObject spawnedObject = _spawnedThemeObjects[i];
                if (spawnedObject == null)
                {
                    continue;
                }

                if (spawnedObject.activeSelf != shouldShow)
                {
                    spawnedObject.SetActive(shouldShow);
                }
            }
        }

        private static void ReactivateFallbacks(SpriteRenderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = true;
                renderer.gameObject.SetActive(true);
            }
        }

        private void ClearDoorRuntimeStateObjects()
        {
            if (!TryGetComponent(out RoomController roomController))
            {
                return;
            }

            IReadOnlyList<RoomDoor> roomDoors = roomController.RoomDoors;

            for (int i = 0; i < roomDoors.Count; i++)
            {
                roomDoors[i]?.ClearRuntimeStateObjects();
            }
        }
    }
}
