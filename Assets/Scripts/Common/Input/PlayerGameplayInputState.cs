using UnityEngine;

namespace CuteIssac.Common.Input
{
    /// <summary>
    /// Value-type snapshot of player gameplay input. Read this from controllers without knowing the input backend.
    /// </summary>
    public readonly struct PlayerGameplayInputState
    {
        public PlayerGameplayInputState(
            Vector2 move,
            Vector2 aim,
            bool hasAimInput,
            bool bombPressed,
            bool activeItemPressed,
            bool minimapTogglePressed)
        {
            Move = move;
            Aim = aim;
            HasAimInput = hasAimInput;
            BombPressed = bombPressed;
            ActiveItemPressed = activeItemPressed;
            MinimapTogglePressed = minimapTogglePressed;
        }

        public Vector2 Move { get; }
        public Vector2 Aim { get; }
        public bool HasAimInput { get; }
        public bool BombPressed { get; }
        public bool ActiveItemPressed { get; }
        public bool MinimapTogglePressed { get; }
    }
}
