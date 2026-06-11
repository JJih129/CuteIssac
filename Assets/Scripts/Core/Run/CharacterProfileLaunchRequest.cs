namespace CuteIssac.Core.Run
{
    public static class CharacterProfileLaunchRequest
    {
        private static string s_RequestedCharacterId;

        public static void RequestCharacter(string characterId)
        {
            s_RequestedCharacterId = characterId ?? string.Empty;
        }

        public static bool TryPeekRequestedCharacter(out string characterId)
        {
            characterId = s_RequestedCharacterId;
            return !string.IsNullOrWhiteSpace(characterId);
        }

        public static bool TryConsumeRequestedCharacter(out string characterId)
        {
            characterId = s_RequestedCharacterId;
            s_RequestedCharacterId = string.Empty;
            return !string.IsNullOrWhiteSpace(characterId);
        }
    }
}
