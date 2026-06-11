namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Carries one-shot launch intent between the title scene and gameplay bootstrap.
    /// </summary>
    public static class RunLaunchRequest
    {
        private static bool s_StartNewRunFromFirstFloor;
        private static bool s_HardModeRequested;

        public static void RequestNewRunFromFirstFloor(bool hardMode = false)
        {
            s_StartNewRunFromFirstFloor = true;
            s_HardModeRequested = hardMode;
        }

        public static bool ConsumeNewRunFromFirstFloorRequest()
        {
            if (!s_StartNewRunFromFirstFloor)
            {
                return false;
            }

            s_StartNewRunFromFirstFloor = false;
            return true;
        }

        public static bool ConsumeHardModeRequest()
        {
            bool requested = s_HardModeRequested;
            s_HardModeRequested = false;
            return requested;
        }
    }
}
