using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// 2층 벽타기 보스 전용 스프라이트 교체기.
    /// 상단/하단 이동 프레임을 방 중앙 기준으로 고르고, 중앙을 넘으면 좌우 반전한다.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public sealed class WallCrawlerBossSpriteAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private WallCrawlerBossBrain wallCrawlerBrain;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite[] upperLaneMoveSprites = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] lowerLaneMoveSprites = System.Array.Empty<Sprite>();

        [Header("Timing")]
        [SerializeField] [Min(0.1f)] private float moveFramesPerSecond = 7f;
        [SerializeField] [Min(0.1f)] private float telegraphFramesPerSecond = 9f;

        [Header("Facing")]
        [Tooltip("켜면 방 중앙 기준 왼쪽에 있을 때 X축 반전한다. 원본 스프라이트 머리가 왼쪽을 보는 경우에 맞춘 기본값이다.")]
        [SerializeField] private bool flipWhenLeftOfCenter = true;
        [Tooltip("타깃이 있으면 중앙선 대신 플레이어 X 위치 기준으로 바라보게 한다.")]
        [SerializeField] private bool preferTargetFacing = true;
        [SerializeField] [Min(0f)] private float facingDeadZone = 0.08f;

        private bool _lastFlipX;

        private void Awake()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (bodySpriteRenderer == null)
            {
                return;
            }

            Sprite[] frames = ResolveFrames();
            if (frames != null && frames.Length > 0)
            {
                float fps = wallCrawlerBrain != null && wallCrawlerBrain.IsTelegraphing
                    ? telegraphFramesPerSecond
                    : moveFramesPerSecond;
                int frameIndex = frames.Length == 1 ? 0 : Mathf.FloorToInt(Time.time * fps) % frames.Length;
                Sprite frame = frames[frameIndex];
                if (frame != null)
                {
                    bodySpriteRenderer.sprite = frame;
                }
            }

            bodySpriteRenderer.flipX = ResolveFlipX();
        }

        private Sprite[] ResolveFrames()
        {
            bool upperLane = wallCrawlerBrain == null || wallCrawlerBrain.IsOnUpperLane;
            Sprite[] laneFrames = upperLane ? upperLaneMoveSprites : lowerLaneMoveSprites;
            if (laneFrames != null && laneFrames.Length > 0)
            {
                return laneFrames;
            }

            Sprite[] fallbackFrames = upperLane ? lowerLaneMoveSprites : upperLaneMoveSprites;
            return fallbackFrames != null ? fallbackFrames : System.Array.Empty<Sprite>();
        }

        private bool ResolveFlipX()
        {
            if (enemyController == null)
            {
                return _lastFlipX;
            }

            float referenceX = 0f;
            if (preferTargetFacing && enemyController.HasTarget)
            {
                referenceX = enemyController.TargetPosition.x;
            }

            float deltaX = enemyController.Position.x - referenceX;
            if (Mathf.Abs(deltaX) <= facingDeadZone)
            {
                return _lastFlipX;
            }

            bool leftOfReference = deltaX < 0f;
            _lastFlipX = flipWhenLeftOfCenter ? leftOfReference : !leftOfReference;
            return _lastFlipX;
        }

        private void ResolveReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (wallCrawlerBrain == null)
            {
                wallCrawlerBrain = GetComponent<WallCrawlerBossBrain>();
            }

            if (bodySpriteRenderer == null)
            {
                EnemyVisual enemyVisual = enemyController != null ? enemyController.EnemyVisual : GetComponent<EnemyVisual>();
                bodySpriteRenderer = enemyVisual != null ? enemyVisual.BodySpriteRenderer : GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
