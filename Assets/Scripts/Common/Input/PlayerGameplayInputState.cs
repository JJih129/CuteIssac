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
            bool minimapTogglePressed,
            bool reloadPressed,
            bool cycleWeaponPressed,
            bool dropWeaponPressed,
            bool weaponCarouselHeld,
            int weaponCarouselSelectionDelta,
            bool weaponCarouselDropPressed)
        {
            Move = move;
            Aim = aim;
            HasAimInput = hasAimInput;
            BombPressed = bombPressed;
            ActiveItemPressed = activeItemPressed;
            MinimapTogglePressed = minimapTogglePressed;
            ReloadPressed = reloadPressed;
            CycleWeaponPressed = cycleWeaponPressed;
            DropWeaponPressed = dropWeaponPressed;
            WeaponCarouselHeld = weaponCarouselHeld;
            WeaponCarouselSelectionDelta = Mathf.Clamp(weaponCarouselSelectionDelta, -1, 1);
            WeaponCarouselDropPressed = weaponCarouselDropPressed;
        }

        public Vector2 Move { get; }
        public Vector2 Aim { get; }
        public bool HasAimInput { get; }
        public bool BombPressed { get; }
        public bool ActiveItemPressed { get; }
        public bool MinimapTogglePressed { get; }
        public bool ReloadPressed { get; }
        public bool CycleWeaponPressed { get; }
        public bool DropWeaponPressed { get; }
        public bool WeaponCarouselHeld { get; }
        public int WeaponCarouselSelectionDelta { get; }
        public bool WeaponCarouselDropPressed { get; }
    }
}
