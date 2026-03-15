using UnityEngine;

namespace CuteIssac.Data.Visual
{
    [CreateAssetMenu(menuName = "CuteIssac/Visual/Player Visual Set", fileName = "PlayerVisualSet")]
    public sealed class PlayerVisualSet : ScriptableObject
    {
        [Header("Sprites")]
        [SerializeField] private Sprite bodySprite;
        [SerializeField] private Sprite shadowSprite;

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("Colors")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color damagedColor = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 0.28f);

        public Sprite BodySprite => bodySprite;
        public Sprite ShadowSprite => shadowSprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public Color BaseColor => baseColor;
        public Color HitFlashColor => hitFlashColor;
        public Color DamagedColor => damagedColor;
        public Color DeadColor => deadColor;
        public Color ShadowColor => shadowColor;
    }
}
