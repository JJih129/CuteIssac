using UnityEngine;

namespace CuteIssac.Data.Visual
{
    /// <summary>
    /// 런타임으로 생성되는 드랍 아이템의 스프라이트 참조를 한곳에서 관리합니다.
    /// Resources 경로의 단일 카탈로그를 교체하면 코드 수정 없이 드랍 외형을 바꿀 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourcePickupSpriteCatalog", menuName = "CuteIssac/Visual/Resource Pickup Sprite Catalog")]
    public sealed class ResourcePickupSpriteCatalog : ScriptableObject
    {
        [Header("Runtime Drop Sprites")]
        [Tooltip("맵에 떨어지는 코인 드랍에 사용할 실제 스프라이트입니다. 비워두면 런타임 임시 아이콘으로 대체됩니다.")]
        [SerializeField] private Sprite coinSprite;

        public Sprite CoinSprite => coinSprite;
    }
}
