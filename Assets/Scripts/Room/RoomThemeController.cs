using System.Collections.Generic;
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
        [Header("Fallback Renderers")]
        [Tooltip("Fallback floor renderer tinted when no floor prefab override is assigned.")]
        [SerializeField] private SpriteRenderer floorRenderer;
        [Tooltip("Fallback wall renderers tinted when no wall prefab override is assigned.")]
        [SerializeField] private SpriteRenderer[] wallRenderers;
        [Tooltip("Fallback door renderers tinted when no door prefab override is assigned.")]
        [SerializeField] private SpriteRenderer[] doorRenderers;

        [Header("Visual Anchors")]
        [Tooltip("Anchor used for themed floor prefab overrides.")]
        [SerializeField] private Transform floorAnchor;
        [Tooltip("Anchors used for themed wall prefab overrides.")]
        [SerializeField] private Transform[] wallAnchors;
        [Tooltip("Anchors used for themed door prefab overrides.")]
        [SerializeField] private Transform[] doorAnchors;
        [Tooltip("Anchor used for optional decoration prefabs.")]
        [SerializeField] private Transform decorationAnchor;
        [Tooltip("Optional parent used to keep spawned theme objects grouped under the room.")]
        [SerializeField] private Transform spawnedThemeParent;

        private readonly List<GameObject> _spawnedThemeObjects = new();
        private RoomThemeData _appliedTheme;

        public RoomThemeData AppliedTheme => _appliedTheme;

        /// <summary>
        /// Generated-room setup calls this after instantiation so floors can reskin rooms per theme asset.
        /// </summary>
        public void ApplyTheme(RoomThemeData roomTheme)
        {
            _appliedTheme = roomTheme;
            ClearSpawnedThemeObjects();

            ApplyFallbackColors(roomTheme);
            ApplyPrefabOverride(roomTheme != null ? roomTheme.FloorVisualPrefab : null, floorAnchor, floorRenderer);
            ApplyPrefabOverrides(roomTheme != null ? roomTheme.WallVisualPrefab : null, wallAnchors, wallRenderers);
            ApplyPrefabOverrides(roomTheme != null ? roomTheme.DoorVisualPrefab : null, doorAnchors, doorRenderers);
            SpawnDecorations(roomTheme);
        }

        private void ApplyFallbackColors(RoomThemeData roomTheme)
        {
            if (floorRenderer != null)
            {
                floorRenderer.enabled = true;
            }

            if (roomTheme == null)
            {
                return;
            }

            if (floorRenderer != null)
            {
                floorRenderer.color = roomTheme.FloorColor;
            }

            ApplyRendererColors(wallRenderers, roomTheme.WallColor);
            ApplyRendererColors(doorRenderers, roomTheme.DoorColor);
        }

        private static void ApplyRendererColors(SpriteRenderer[] renderers, Color color)
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
                renderer.color = color;
            }
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

            Transform parent = spawnedThemeParent != null ? spawnedThemeParent : anchor;
            GameObject spawnedObject = Instantiate(prefab, anchor.position, anchor.rotation, parent);
            spawnedObject.name = $"{prefab.name}_ThemeVisual";
            spawnedObject.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
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
    }
}
