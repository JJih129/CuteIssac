using UnityEngine;

namespace CuteIssac.Enemy
{
    /// <summary>
    /// Adds a lightweight visual-only hop arc to Jelly Bear dash attacks without changing AI or collider movement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DasherEnemyBrain))]
    public sealed class JellyBearPounceVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DasherEnemyBrain dasherBrain;
        [SerializeField] private Transform visualRoot;

        [Header("Pounce Arc")]
        [SerializeField] [Min(0f)] private float jumpHeight = 0.22f;
        [SerializeField] [Min(0f)] private float windupCrouchDepth = 0.06f;
        [SerializeField] [Min(0f)] private float recoveryDipDepth = 0.025f;
        [SerializeField] [Min(0.1f)] private float returnSpeed = 18f;

        private Vector3 _baseLocalPosition;
        private bool _hasBasePosition;

        private void Awake()
        {
            ResolveReferences();
            CacheBasePosition();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheBasePosition();
        }

        private void OnDisable()
        {
            RestoreBasePosition();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            if (visualRoot == null || dasherBrain == null)
            {
                return;
            }

            if (!_hasBasePosition)
            {
                CacheBasePosition();
            }

            Vector3 targetPosition = _baseLocalPosition;

            if (dasherBrain.IsDashingWindup)
            {
                float progress = Mathf.Clamp01(dasherBrain.DashWindupProgressNormalized);
                targetPosition.y -= Mathf.SmoothStep(0f, windupCrouchDepth, progress);
            }
            else if (dasherBrain.IsDashing)
            {
                float progress = Mathf.Clamp01(dasherBrain.DashProgressNormalized);
                targetPosition.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            }
            else if (dasherBrain.IsDashingRecovery)
            {
                float progress = Mathf.Clamp01(dasherBrain.DashRecoveryProgressNormalized);
                targetPosition.y -= Mathf.Lerp(recoveryDipDepth, 0f, progress);
            }

            float step = returnSpeed * Time.deltaTime;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, step);
        }

        private void ResolveReferences()
        {
            if (dasherBrain == null)
            {
                dasherBrain = GetComponent<DasherEnemyBrain>();
            }
        }

        private void CacheBasePosition()
        {
            if (visualRoot == null)
            {
                return;
            }

            _baseLocalPosition = visualRoot.localPosition;
            _hasBasePosition = true;
        }

        private void RestoreBasePosition()
        {
            if (visualRoot == null || !_hasBasePosition)
            {
                return;
            }

            visualRoot.localPosition = _baseLocalPosition;
        }
    }
}
