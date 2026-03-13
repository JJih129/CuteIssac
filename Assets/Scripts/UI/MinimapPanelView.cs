using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    /// <summary>
    /// Minimap presentation view.
    /// This class only manages layout and visuals for room nodes. It should receive already-prepared room state snapshots from a controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapPanelView : MonoBehaviour
    {
        [Header("Optional Root")]
        [Tooltip("Optional. Root object for the minimap panel.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [Tooltip("Optional minimap background image.")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("Optional minimap frame image.")]
        [SerializeField] private Image frameImage;
        [Tooltip("Parent for future minimap room nodes, icons, and overlays.")]
        [SerializeField] private RectTransform contentRoot;
        [Tooltip("Optional placeholder label shown before a real minimap presenter is connected.")]
        [SerializeField] private Text placeholderText;

        [Header("Node Presentation")]
        [Tooltip("Optional. Reusable node prefab root. If empty, the view creates a simple fallback node at runtime.")]
        [SerializeField] private MinimapNodeView nodeTemplate;
        [Tooltip("Spacing between grid-based room nodes in the minimap UI.")]
        [SerializeField] private float nodeSpacing = 26f;
        [Tooltip("Fallback node size when no node prefab is supplied.")]
        [SerializeField] private Vector2 fallbackNodeSize = new(24f, 24f);
        [Tooltip("Current room nodes are slightly larger to make navigation easier to read.")]
        [SerializeField] private float currentNodeScaleMultiplier = 1.3f;
        [Tooltip("Unknown rooms stay hidden until visited. Disable this to show a faint full-layout map.")]
        [SerializeField] private bool hideUnknownRooms = true;

        [Header("State Colors")]
        [SerializeField] private Color unknownStateColor = new(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color visitedStateColor = new(0.24f, 0.30f, 0.42f, 0.95f);
        [SerializeField] private Color currentStateColor = new(0.38f, 0.62f, 0.92f, 1f);
        [SerializeField] private Color clearedStateColor = new(0.29f, 0.55f, 0.34f, 1f);
        [SerializeField] private Color currentOutlineColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color clearedMarkerColor = new(1f, 1f, 1f, 0.92f);

        [Header("Type Colors")]
        [SerializeField] private Color startRoomColor = new(0.85f, 0.92f, 1f, 1f);
        [SerializeField] private Color normalRoomColor = new(0.88f, 0.88f, 0.88f, 1f);
        [SerializeField] private Color bossRoomColor = new(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color treasureRoomColor = new(1f, 0.85f, 0.28f, 1f);
        [SerializeField] private Color shopRoomColor = new(0.35f, 0.95f, 0.85f, 1f);
        [SerializeField] private Color secretRoomColor = new(0.82f, 0.55f, 1f, 1f);
        [SerializeField] private Color challengeRoomColor = new(1f, 0.64f, 0.22f, 1f);

        [Header("Type Icons")]
        [SerializeField] private Sprite startRoomIcon;
        [SerializeField] private Sprite normalRoomIcon;
        [SerializeField] private Sprite bossRoomIcon;
        [SerializeField] private Sprite treasureRoomIcon;
        [SerializeField] private Sprite shopRoomIcon;
        [SerializeField] private Sprite secretRoomIcon;
        [SerializeField] private Sprite challengeRoomIcon;

        public RectTransform ContentRoot => contentRoot;

        private bool _warnedMissingPlaceholderText;
        private bool _warnedMissingContentRoot;
        private readonly List<MinimapNodeView> _spawnedNodes = new();
        private bool _isVisible = true;

        public void ConfigureDebugView(Text placeholder, RectTransform minimapContent = null)
        {
            placeholderText = placeholder;
            contentRoot = minimapContent;
            panelRoot = placeholder != null && placeholder.transform.parent != null
                ? placeholder.transform.parent.gameObject
                : panelRoot;
        }

        public void ShowPlaceholder()
        {
            if (placeholderText == null && !_warnedMissingPlaceholderText)
            {
                Debug.LogWarning("MinimapPanelView has no placeholderText assigned. The panel space still exists, but the prototype label cannot be shown.", this);
                _warnedMissingPlaceholderText = true;
            }

            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(true);
                placeholderText.text = "MINIMAP";
            }

            ApplyPanelVisibility();
        }

        public void RenderRooms(IReadOnlyList<MinimapRoomViewData> roomStates)
        {
            if (!TryEnsureContentRoot())
            {
                return;
            }

            if (roomStates == null || roomStates.Count == 0)
            {
                ShowPlaceholder();
                HideUnusedNodes(0);
                return;
            }

            EnsureNodeCapacity(roomStates.Count);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < roomStates.Count; i++)
            {
                GridPosition gridPosition = roomStates[i].GridPosition;
                minX = Mathf.Min(minX, gridPosition.X);
                minY = Mathf.Min(minY, gridPosition.Y);
                maxX = Mathf.Max(maxX, gridPosition.X);
                maxY = Mathf.Max(maxY, gridPosition.Y);
            }

            Vector2 center = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            for (int i = 0; i < roomStates.Count; i++)
            {
                MinimapRoomViewData roomState = roomStates[i];
                MinimapNodeView nodeView = _spawnedNodes[i];
                RoomExplorationState explorationState = roomState.ExplorationState;
                bool isUnknown = explorationState == RoomExplorationState.Unknown;
                bool visible = !hideUnknownRooms || !isUnknown;
                Vector2 anchoredPosition = new(
                    (roomState.GridPosition.X - center.x) * nodeSpacing,
                    (roomState.GridPosition.Y - center.y) * nodeSpacing);

                nodeView.SetAnchoredPosition(anchoredPosition);
                nodeView.SetVisible(visible);

                if (!visible)
                {
                    continue;
                }

                Vector2 nodeSize = fallbackNodeSize;

                if (explorationState == RoomExplorationState.Current)
                {
                    nodeSize *= currentNodeScaleMultiplier;
                }

                nodeView.Present(
                    nodeSize,
                    GetStateColor(explorationState),
                    GetTypeColor(roomState.RoomType),
                    GetTypeIcon(roomState.RoomType),
                    explorationState == RoomExplorationState.Current,
                    currentOutlineColor,
                    roomState.IsCleared,
                    clearedMarkerColor);
            }

            HideUnusedNodes(roomStates.Count);

            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(false);
            }

            ApplyPanelVisibility();
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            ApplyPanelVisibility();
        }

        private bool TryEnsureContentRoot()
        {
            if (contentRoot != null)
            {
                return true;
            }

            if (!_warnedMissingContentRoot)
            {
                Debug.LogWarning("MinimapPanelView requires a contentRoot RectTransform to render room nodes.", this);
                _warnedMissingContentRoot = true;
            }

            return false;
        }

        private void EnsureNodeCapacity(int requiredCount)
        {
            while (_spawnedNodes.Count < requiredCount)
            {
                _spawnedNodes.Add(CreateNodeInstance(_spawnedNodes.Count));
            }
        }

        private MinimapNodeView CreateNodeInstance(int index)
        {
            if (nodeTemplate != null)
            {
                MinimapNodeView instance = Instantiate(nodeTemplate, contentRoot);
                instance.gameObject.name = $"{nodeTemplate.name}_{index}";
                instance.gameObject.SetActive(true);
                return instance;
            }

            GameObject nodeRoot = new($"MinimapNode_{index}");
            nodeRoot.layer = contentRoot.gameObject.layer;
            RectTransform rootRect = nodeRoot.AddComponent<RectTransform>();
            rootRect.SetParent(contentRoot, false);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = fallbackNodeSize;

            Image background = nodeRoot.AddComponent<Image>();
            background.raycastTarget = false;

            GameObject currentObject = new("CurrentHighlight");
            currentObject.layer = nodeRoot.layer;
            RectTransform currentRect = currentObject.AddComponent<RectTransform>();
            currentRect.SetParent(rootRect, false);
            currentRect.anchorMin = Vector2.zero;
            currentRect.anchorMax = Vector2.one;
            currentRect.offsetMin = new Vector2(-4f, -4f);
            currentRect.offsetMax = new Vector2(4f, 4f);
            Image currentImage = currentObject.AddComponent<Image>();
            currentImage.raycastTarget = false;
            currentImage.gameObject.SetActive(false);

            GameObject iconObject = new("TypeIcon");
            iconObject.layer = nodeRoot.layer;
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.SetParent(rootRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = fallbackNodeSize * 0.45f;
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.raycastTarget = false;

            GameObject clearedObject = new("ClearedMarker");
            clearedObject.layer = nodeRoot.layer;
            RectTransform clearedRect = clearedObject.AddComponent<RectTransform>();
            clearedRect.SetParent(rootRect, false);
            clearedRect.anchorMin = new Vector2(1f, 0f);
            clearedRect.anchorMax = new Vector2(1f, 0f);
            clearedRect.pivot = new Vector2(1f, 0f);
            clearedRect.anchoredPosition = new Vector2(1f, 1f);
            clearedRect.sizeDelta = fallbackNodeSize * 0.26f;
            Image clearedImage = clearedObject.AddComponent<Image>();
            clearedImage.raycastTarget = false;
            clearedImage.gameObject.SetActive(false);

            MinimapNodeView nodeView = nodeRoot.AddComponent<MinimapNodeView>();
            nodeView.ConfigureFallback(rootRect, background, iconImage, currentImage, clearedImage);
            return nodeView;
        }

        private void HideUnusedNodes(int usedCount)
        {
            for (int i = usedCount; i < _spawnedNodes.Count; i++)
            {
                if (_spawnedNodes[i] != null)
                {
                    _spawnedNodes[i].SetVisible(false);
                }
            }
        }

        private Color GetStateColor(RoomExplorationState explorationState)
        {
            return explorationState switch
            {
                RoomExplorationState.Current => currentStateColor,
                RoomExplorationState.Cleared => clearedStateColor,
                RoomExplorationState.Visited => visitedStateColor,
                _ => unknownStateColor
            };
        }

        private Color GetTypeColor(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => startRoomColor,
                RoomType.Boss => bossRoomColor,
                RoomType.Treasure => treasureRoomColor,
                RoomType.Shop => shopRoomColor,
                RoomType.Secret => secretRoomColor,
                RoomType.Challenge => challengeRoomColor,
                _ => normalRoomColor
            };
        }

        private Sprite GetTypeIcon(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Start => startRoomIcon,
                RoomType.Boss => bossRoomIcon,
                RoomType.Treasure => treasureRoomIcon,
                RoomType.Shop => shopRoomIcon,
                RoomType.Secret => secretRoomIcon,
                RoomType.Challenge => challengeRoomIcon,
                _ => normalRoomIcon
            };
        }

        private void ApplyPanelVisibility()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(_isVisible);
            }
        }
    }
}
