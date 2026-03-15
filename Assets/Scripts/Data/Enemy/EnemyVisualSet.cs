using UnityEngine;

namespace CuteIssac.Data.Enemy
{
    [CreateAssetMenu(fileName = "EnemyVisualSet", menuName = "CuteIssac/Data/Enemy/Enemy Visual Set")]
    public sealed class EnemyVisualSet : ScriptableObject
    {
        [SerializeField] private Sprite bodySprite;
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color damagedColor = new(1f, 0.48f, 0.48f, 1f);
        [SerializeField] private Color deadColor = new(1f, 1f, 1f, 0.55f);

        public Sprite BodySprite => bodySprite;
        public Color BaseColor => baseColor;
        public Color HitFlashColor => hitFlashColor;
        public Color DamagedColor => damagedColor;
        public Color DeadColor => deadColor;
    }
}
