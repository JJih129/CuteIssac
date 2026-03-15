using UnityEngine;

namespace CuteIssac.Data.Visual
{
    [CreateAssetMenu(menuName = "CuteIssac/Visual/Pickup Visual Set", fileName = "PickupVisualSet")]
    public sealed class PickupVisualSet : ScriptableObject
    {
        [Header("Sprites")]
        [SerializeField] private Sprite bodySprite;

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("Colors")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color collectedColor = new(1f, 1f, 1f, 0.35f);

        public Sprite BodySprite => bodySprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public Color BaseColor => baseColor;
        public Color CollectedColor => collectedColor;
    }
}
