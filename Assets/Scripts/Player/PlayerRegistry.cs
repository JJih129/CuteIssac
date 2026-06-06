namespace CuteIssac.Player
{
    public static class PlayerRegistry
    {
        public static PlayerController ActiveController { get; private set; }
        public static PlayerHealth ActiveHealth { get; private set; }

        public static void Register(PlayerController playerController)
        {
            if (playerController != null)
            {
                ActiveController = playerController;
            }
        }

        public static void Register(PlayerHealth playerHealth)
        {
            if (playerHealth != null)
            {
                ActiveHealth = playerHealth;
            }
        }

        public static void Unregister(PlayerController playerController)
        {
            if (ActiveController == playerController)
            {
                ActiveController = null;
            }
        }

        public static void Unregister(PlayerHealth playerHealth)
        {
            if (ActiveHealth == playerHealth)
            {
                ActiveHealth = null;
            }
        }
    }
}
