using System;

namespace CuteIssac.UI
{
    /// <summary>
    /// Event bridge between boss gameplay objects and the HUD layer.
    /// This keeps boss encounter logic decoupled from specific HUD scene objects.
    /// </summary>
    public static class BossHudEvents
    {
        public static event Action<BossHudState> BossShownOrUpdated;
        public static event Action<int> BossHidden;

        public static void RaiseBossShownOrUpdated(BossHudState hudState)
        {
            BossShownOrUpdated?.Invoke(hudState);
        }

        public static void RaiseBossHidden(int sourceId)
        {
            BossHidden?.Invoke(sourceId);
        }
    }
}
