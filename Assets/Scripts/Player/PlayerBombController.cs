using CuteIssac.Combat;
using CuteIssac.Common.Input;
using UnityEngine;

namespace CuteIssac.Player
{
    /// <summary>
    /// Owns bomb placement requests, inventory consumption, and bomb prefab spawning.
    /// The controller depends on PlayerInventory and shared input, while the bomb prefab owns the explosion logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBombController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private Collider2D ownerCollider;
        [SerializeField] private MonoBehaviour inputReaderSource;
        [SerializeField] private BombController bombPrefab;
        [SerializeField] private Transform bombSpawnAnchor;

        [Header("Placement")]
        [SerializeField] [Min(0f)] private float placementOffset;
        [SerializeField] [Min(0f)] private float placementCooldown = 0.15f;

        private IPlayerInputReader _inputReader;
        private float _placementCooldownRemaining;

        private void Awake()
        {
            if (!ResolveDependencies())
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (_placementCooldownRemaining > 0f)
            {
                _placementCooldownRemaining -= Time.deltaTime;
            }

            if (_inputReader == null || !_inputReader.ReadState().BombPressed)
            {
                return;
            }

            TryPlaceBomb();
        }

        public bool TryPlaceBomb()
        {
            if (_placementCooldownRemaining > 0f || playerInventory == null || bombPrefab == null)
            {
                return false;
            }

            if (!playerInventory.TrySpendBombs(1))
            {
                return false;
            }

            Vector3 spawnPosition = ResolveSpawnPosition();
            BombController spawnedBomb = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
            spawnedBomb.Initialize(transform, ownerCollider);
            _placementCooldownRemaining = placementCooldown;
            return true;
        }

        public bool TrySpawnBombBurst(int bombCount, float spawnRadius)
        {
            if (bombPrefab == null || bombCount <= 0)
            {
                return false;
            }

            Vector3 center = ResolveSpawnPosition();
            float radius = Mathf.Max(0f, spawnRadius);

            for (int index = 0; index < bombCount; index++)
            {
                float angle = bombCount == 1 ? 0f : (360f / bombCount) * index;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 offset = radius <= 0f
                    ? Vector3.zero
                    : new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
                BombController spawnedBomb = Instantiate(bombPrefab, center + offset, Quaternion.identity);
                spawnedBomb.Initialize(transform, ownerCollider);
            }

            _placementCooldownRemaining = Mathf.Max(_placementCooldownRemaining, placementCooldown);
            return true;
        }

        private Vector3 ResolveSpawnPosition()
        {
            Vector3 basePosition = bombSpawnAnchor != null ? bombSpawnAnchor.position : transform.position;

            if (placementOffset <= 0f)
            {
                return basePosition;
            }

            return basePosition + (Vector3.up * placementOffset);
        }

        private bool ResolveDependencies()
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }

            if (bombSpawnAnchor == null)
            {
                bombSpawnAnchor = transform;
            }

            if (playerInventory == null)
            {
                Debug.LogError("PlayerBombController requires PlayerInventory.", this);
                return false;
            }

            if (bombPrefab == null)
            {
                Debug.LogError("PlayerBombController requires a BombController prefab reference.", this);
                return false;
            }

            if (inputReaderSource is IPlayerInputReader serializedReader)
            {
                _inputReader = serializedReader;
                return true;
            }

            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int index = 0; index < sceneBehaviours.Length; index++)
            {
                if (sceneBehaviours[index] is IPlayerInputReader sceneReader)
                {
                    _inputReader = sceneReader;
                    return true;
                }
            }

            Debug.LogError("PlayerBombController could not find an IPlayerInputReader in the scene.", this);
            return false;
        }

        private void Reset()
        {
            playerInventory = GetComponent<PlayerInventory>();
            ownerCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }

            if (ownerCollider == null)
            {
                ownerCollider = GetComponent<Collider2D>();
            }

            if (bombSpawnAnchor == null)
            {
                bombSpawnAnchor = transform;
            }
        }
    }
}
