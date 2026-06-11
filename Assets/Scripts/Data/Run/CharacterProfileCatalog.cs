using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.Data.Run
{
    [CreateAssetMenu(fileName = "CharacterProfileCatalog", menuName = "CuteIssac/Data/Run/Character Profile Catalog")]
    public sealed class CharacterProfileCatalog : ScriptableObject
    {
        [SerializeField] private List<CharacterProfileData> profiles = new();
        [SerializeField] private CharacterProfileData defaultProfile;

        public IReadOnlyList<CharacterProfileData> Profiles => profiles;
        public CharacterProfileData DefaultProfile => defaultProfile != null ? defaultProfile : GetFirstValidProfile();

        public CharacterProfileData FindProfile(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return DefaultProfile;
            }

            for (int index = 0; index < profiles.Count; index++)
            {
                CharacterProfileData profile = profiles[index];

                if (profile != null && string.Equals(profile.CharacterId, characterId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }

            return DefaultProfile;
        }

        private CharacterProfileData GetFirstValidProfile()
        {
            for (int index = 0; index < profiles.Count; index++)
            {
                if (profiles[index] != null)
                {
                    return profiles[index];
                }
            }

            return null;
        }
    }
}
