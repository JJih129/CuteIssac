using UnityEngine;

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

        private Vector3 _baseScale = Vector3.one;
        private float _attackFlashRemaining;

        private void Awake()
        {
            CacheScale();
            ApplyColor(idleColor);
        }

        private void Update()
        {
            if (_attackFlashRemaining > 0f)
            {
                _attackFlashRemaining = Mathf.Max(0f, _attackFlashRemaining - Time.deltaTime * 6f);
                float t = _attackFlashRemaining;
                ApplyColor(Color.Lerp(idleColor, attackFlashColor, t));
                ApplyScale(Mathf.Lerp(1f, pulseScale, t));
                return;
            }

            ApplyColor(idleColor);
            ApplyScale(1f);
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
            if (bodySpriteRenderer == null || Mathf.Abs(moveDirection.x) <= 0.01f)
            {
                return;
            }

            bodySpriteRenderer.flipX = moveDirection.x < 0f;
        }

        public void ResetPresentation()
        {
            _attackFlashRemaining = 0f;
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
    }
}
