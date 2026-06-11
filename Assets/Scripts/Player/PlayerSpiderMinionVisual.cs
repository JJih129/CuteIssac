using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CuteIssac.Player
{
    /// <summary>
    /// Presentation-only view for summoned spider minions.
    /// Designers can swap sprite, colors, and scale without touching minion logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpiderMinionVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Color idleColor = new(0.88f, 0.94f, 1f, 1f);
        [SerializeField] private Color attackFlashColor = new(1f, 0.86f, 0.42f, 1f);
        [SerializeField] [Min(0f)] private float pulseScale = 1.12f;
        [SerializeField] private Sprite[] moveFrames = new Sprite[6];
        [SerializeField] [Min(1f)] private float moveFramesPerSecond = 10f;
        [SerializeField] [Min(0f)] private float moveAnimationThreshold = 0.02f;
#if UNITY_EDITOR
        [SerializeField] private bool autoAssignMoveFramesInEditor = true;
        [SerializeField] private string autoMoveFramePrefix = "Mech4_Move";
#endif

        private Vector3 _baseScale = Vector3.one;
        private Vector2 _moveDirection;
        private float _attackFlashRemaining;
        private float _moveAnimationTime;
        private int _lastAppliedMoveFrameIndex = -1;

        private void Awake()
        {
            CacheScale();
            ApplyMoveFrame(0, force: true);
            ApplyColor(idleColor);
        }

        private void Update()
        {
            TickMoveAnimation();

            if (_attackFlashRemaining > 0f)
            {
                _attackFlashRemaining = Mathf.Max(0f, _attackFlashRemaining - Time.deltaTime * 6f);
                float t = _attackFlashRemaining;
                ApplyColor(Color.Lerp(idleColor, attackFlashColor, t));
                ApplyScale(Mathf.Lerp(1f, pulseScale, t));
            }
            else
            {
                ApplyColor(idleColor);
                ApplyScale(1f);
            }
        }

        public void HandleSpawned()
        {
            _attackFlashRemaining = 0f;
            ApplyColor(idleColor);
            ApplyScale(1f);
        }

        public void HandleAttack()
        {
            _attackFlashRemaining = 1f;
        }

        public void SetMoveDirection(Vector2 moveDirection)
        {
            _moveDirection = moveDirection;

            if (bodySpriteRenderer == null || Mathf.Abs(moveDirection.x) <= 0.01f)
            {
                return;
            }

            bodySpriteRenderer.flipX = moveDirection.x < 0f;
        }

        public void ResetPresentation()
        {
            _attackFlashRemaining = 0f;
            _moveDirection = Vector2.zero;
            _moveAnimationTime = 0f;
            _lastAppliedMoveFrameIndex = -1;
            ApplyMoveFrame(0, force: true);
            ApplyColor(idleColor);
            ApplyScale(1f);
        }

        private void Reset()
        {
            bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            visualRoot = bodySpriteRenderer != null ? bodySpriteRenderer.transform : transform;
        }

        private void OnValidate()
        {
            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (visualRoot == null)
            {
                visualRoot = bodySpriteRenderer != null ? bodySpriteRenderer.transform : transform;
            }

            CacheScale();
#if UNITY_EDITOR
            AutoAssignMoveFramesInEditor();
#endif
            ApplyDefaultSpriteFromMoveFrames();
        }

        private void CacheScale()
        {
            if (visualRoot != null)
            {
                _baseScale = visualRoot.localScale;
            }
        }

        private void ApplyColor(Color color)
        {
            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.color = color;
            }
        }

        private void ApplyScale(float multiplier)
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = _baseScale * Mathf.Max(0.01f, multiplier);
            }
        }

        private void TickMoveAnimation()
        {
            int frameCount = CountMoveFrames();

            if (frameCount <= 0)
            {
                return;
            }

            if (!ShouldPlayMoveAnimation())
            {
                _moveAnimationTime = 0f;
                ApplyMoveFrame(0, force: false);
                return;
            }

            _moveAnimationTime += Time.deltaTime * Mathf.Max(1f, moveFramesPerSecond);
            int frameIndex = Mathf.FloorToInt(_moveAnimationTime) % frameCount;
            ApplyMoveFrame(frameIndex, force: false);
        }

        private bool ShouldPlayMoveAnimation()
        {
            float threshold = Mathf.Max(0f, moveAnimationThreshold);
            return _moveDirection.sqrMagnitude > threshold * threshold;
        }

        private int CountMoveFrames()
        {
            if (moveFrames == null)
            {
                return 0;
            }

            int count = 0;

            for (int index = 0; index < moveFrames.Length; index++)
            {
                if (moveFrames[index] == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private void ApplyMoveFrame(int frameIndex, bool force)
        {
            Sprite sprite = ResolveMoveFrame(frameIndex);

            if (bodySpriteRenderer == null || sprite == null)
            {
                return;
            }

            if (!force && _lastAppliedMoveFrameIndex == frameIndex && bodySpriteRenderer.sprite == sprite)
            {
                return;
            }

            bodySpriteRenderer.sprite = sprite;
            _lastAppliedMoveFrameIndex = frameIndex;
        }

        private Sprite ResolveMoveFrame(int frameIndex)
        {
            if (moveFrames == null || moveFrames.Length == 0)
            {
                return null;
            }

            int liveIndex = 0;

            for (int index = 0; index < moveFrames.Length; index++)
            {
                Sprite sprite = moveFrames[index];

                if (sprite == null)
                {
                    continue;
                }

                if (liveIndex == frameIndex)
                {
                    return sprite;
                }

                liveIndex++;
            }

            return null;
        }

        private void ApplyDefaultSpriteFromMoveFrames()
        {
            Sprite defaultSprite = ResolveMoveFrame(0);

            if (bodySpriteRenderer != null && defaultSprite != null && bodySpriteRenderer.sprite != defaultSprite)
            {
                bodySpriteRenderer.sprite = defaultSprite;
            }
        }

#if UNITY_EDITOR
        private void AutoAssignMoveFramesInEditor()
        {
            if (!autoAssignMoveFramesInEditor || string.IsNullOrWhiteSpace(autoMoveFramePrefix))
            {
                return;
            }

            if (moveFrames == null || moveFrames.Length != 6)
            {
                moveFrames = new Sprite[6];
            }

            for (int frameNumber = 1; frameNumber <= moveFrames.Length; frameNumber++)
            {
                if (moveFrames[frameNumber - 1] != null)
                {
                    continue;
                }

                Sprite sprite = FindSpriteByAssetName($"{autoMoveFramePrefix}{frameNumber}");

                if (sprite != null)
                {
                    moveFrames[frameNumber - 1] = sprite;
                }
            }
        }

        private static Sprite FindSpriteByAssetName(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:Texture2D");

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);

                if (string.IsNullOrEmpty(path)
                    || !string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), assetName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return null;
        }
#endif
    }
}
