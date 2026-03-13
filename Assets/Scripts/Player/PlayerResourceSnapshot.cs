using System;

namespace CuteIssac.Player
{
    /// <summary>
    /// Lightweight read model for HUD-facing player resources.
    /// A snapshot keeps UI code simple and avoids exposing inventory internals just to show counts.
    /// </summary>
    [Serializable]
    public readonly struct PlayerResourceSnapshot
    {
        public PlayerResourceSnapshot(int coins, int keys, int bombs)
        {
            Coins = coins;
            Keys = keys;
            Bombs = bombs;
        }

        public int Coins { get; }
        public int Keys { get; }
        public int Bombs { get; }
    }
}
