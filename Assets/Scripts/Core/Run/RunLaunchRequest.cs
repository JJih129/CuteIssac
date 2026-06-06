namespace CuteIssac.Core.Run
{
    /// <summary>
    /// Carries one-shot launch intent between the title scene and gameplay bootstrap.
    /// </summary>
    public static class RunLaunchRequest
    {
        private static bool s_StartNewRunFromFirstFloor;

        public static void RequestNewRunFromFirstFloor()
        {
            s_StartNewRunFromFirstFloor = true;
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
    }
}
