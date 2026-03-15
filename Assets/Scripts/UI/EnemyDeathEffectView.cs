using UnityEngine;
using CuteIssac.Core.Pooling;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyDeathEffectView : MonoBehaviour, IUiModalDismissible
    {
        private static Sprite s_fallbackSprite;

        [SerializeField] [Min(0.05f)] private float duration = 0.32f;
        [SerializeField] [Range(3, 8)] private int shardCount = 5;
        [SerializeField] [Min(0.1f)] private float burstRadius = 0.8f;
        [SerializeField] [Min(0f)] private float startScale = 0.18f;
        [SerializeField] [Min(0f)] private float endScale = 0.04f;
        [SerializeField] [Min(0)] private int sortingOrder = 260;

        private SpriteRenderer[] _shards;
        private Vector3[] _directions;
        private float _elapsed;
        private Color _baseColor = Color.white;

        private void Awake()
        {
            EnsureShards();
        }

        private void OnEnable()
        {
            UiModalDismissRegistry.Register(this);
        }

        private void OnDisable()
        {
            UiModalDismissRegistry.Unregister(this);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(_elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);
            float scale = Mathf.Lerp(startScale, endScale, normalized);

            for (int i = 0; i < _shards.Length; i++)
            {
                SpriteRenderer shard = _shards[i];

                if (shard == null)
                {
                    continue;
                }

                shard.transform.localPosition = _directions[i] * (burstRadius * eased);
                shard.transform.localScale = Vector3.one * scale;
                shard.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_directions[i].y, _directions[i].x) * Mathf.Rad2Deg);

                Color color = _baseColor;
                color.a *= 1f - normalized;
                shard.color = color;
            }

            if (normalized >= 1f)
            {
                PrefabPoolService.Return(gameObject);
            }
        }

        public void Initialize(Color burstColor)
        {
            EnsureShards();
            _elapsed = 0f;
            _baseColor = burstColor;

            for (int i = 0; i < _shards.Length; i++)
            {
                float angle = (360f / _shards.Length) * i + Random.Range(-14f, 14f);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                _directions[i] = direction.normalized;
                _shards[i].transform.localPosition = Vector3.zero;
                _shards[i].transform.localScale = Vector3.one * startScale;
                _shards[i].color = _baseColor;
            }
        }

        public void DismissForModal()
        {
            PrefabPoolService.Return(gameObject);
        }

        private void EnsureShards()
        {
            if (_shards != null && _shards.Length == shardCount)
            {
                return;
            }

            _shards = new SpriteRenderer[shardCount];
            _directions = new Vector3[shardCount];

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject childObject = transform.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(childObject);
                }
                else
                {
                    DestroyImmediate(childObject);
                }
            }

            Sprite sprite = GetFallbackSprite();

            for (int i = 0; i < shardCount; i++)
            {
                GameObject shardObject = new($"Shard{i + 1}");
                shardObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = shardObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder + i;
                _shards[i] = renderer;
            }
        }

        private static Sprite GetFallbackSprite()
        {
            if (s_fallbackSprite != null)
            {
                return s_fallbackSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            s_fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            return s_fallbackSprite;
        }
    }
}
