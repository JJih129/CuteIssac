using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Data.Item
{
    /// <summary>
    /// Base active item effect asset. Concrete effects execute through the player active item controller.
    /// </summary>
    public abstract class ActiveItemEffectData : ScriptableObject
    {
        public abstract bool TryApply(PlayerActiveItemController controller);

        public virtual bool TryRestore(PlayerActiveItemController controller, float remainingSeconds)
        {
            return false;
        }
    }
}
