using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Presentation-only screen feedback for player hits.
    /// Uses an orthographic size punch so camera position systems can keep ownership of camera movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerScreenFeedback : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Optional. When empty, Camera.main is used. Only orthographic cameras are affected.")]
        [SerializeField] private Camera targetCamera;

        [Header("Hit Zoom")]
        [SerializeField] [Min(0f)] private float hitZoomDelta = 0.18f;
        [SerializeField] [Min(0f)] private float maxZoomDelta = 0.32f;
        [SerializeField] [Min(0f)] private float zoomRecoverSpeed = 1.8f;

        private float _baseOrthographicSize;
        private float _activeZoomDelta;
        private bool _initialized;

        private void Awake()
        {
            ResolveCamera();
            CacheBaseOrthographicSize();
        }

        private void Update()
        {
            if (!_initialized || _activeZoomDelta <= 0f)
            {
                return;
            }

            _activeZoomDelta = Mathf.MoveTowards(_activeZoomDelta, 0f, zoomRecoverSpeed * Time.deltaTime);
            ApplyZoom();
        }

        public void PlayHitFeedback(float scale = 1f)
        {
            ResolveCamera();
            CacheBaseOrthographicSize();

            if (!_initialized)
            {
                return;
            }

            _activeZoomDelta = Mathf.Min(maxZoomDelta, _activeZoomDelta + (hitZoomDelta * Mathf.Max(0f, scale)));
            ApplyZoom();
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void CacheBaseOrthographicSize()
        {
            if (targetCamera == null || !targetCamera.orthographic)
            {
                _initialized = false;
                return;
            }

            if (!_initialized)
            {
                _baseOrthographicSize = targetCamera.orthographicSize;
                _initialized = true;
            }
        }

        private void ApplyZoom()
        {
            if (!_initialized || targetCamera == null)
            {
                return;
            }

            targetCamera.orthographicSize = Mathf.Max(0.01f, _baseOrthographicSize - _activeZoomDelta);
        }

        private void OnDisable()
        {
            if (_initialized && targetCamera != null)
            {
                targetCamera.orthographicSize = _baseOrthographicSize;
            }

            _activeZoomDelta = 0f;
        }

        private void Reset()
        {
            ResolveCamera();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ResolveCamera();
            }
        }
    }
}
