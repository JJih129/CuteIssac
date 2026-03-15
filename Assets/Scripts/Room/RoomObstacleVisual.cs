using UnityEngine;

namespace CuteIssac.Room
{
    /// <summary>
    /// Presentation-only layer for room obstacles.
    /// Artists can swap body/highlight renderers without touching hazard or projectile logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomObstacleVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer accentRenderer;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color projectileHitColor = new(1f, 0.92f, 0.6f, 1f);
        [SerializeField] private Color hazardActiveColor = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField] [Min(0f)] private float flashDuration = 0.08f;

        private float _flashRemaining;
        private Color _currentFlashColor;

        private void Awake()
        {
            ResolveReferences();
            ApplyColor(idleColor);
        }

        private void Update()
        {
            if (_flashRemaining <= 0f)
            {
                return;
            }

            _flashRemaining -= Time.deltaTime;

            if (_flashRemaining <= 0f)
            {
                ApplyColor(idleColor);
            }
        }

        public void HandleProjectileImpact()
        {
            Flash(projectileHitColor);
        }

        public void HandleHazardTriggered()
        {
            Flash(hazardActiveColor);
        }

        private void Flash(Color color)
        {
            _currentFlashColor = color;
            _flashRemaining = flashDuration;
            ApplyColor(_currentFlashColor);
        }

        private void ApplyColor(Color color)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }

            if (accentRenderer != null)
            {
                accentRenderer.color = color;
            }
        }

        private void ResolveReferences()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
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
