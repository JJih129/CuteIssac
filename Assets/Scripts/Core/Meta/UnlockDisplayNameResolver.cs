using System;
using System.Collections.Generic;
using CuteIssac.Data.Dungeon;
using CuteIssac.Data.Item;
using CuteIssac.Data.Run;
using CuteIssac.Data.Unlock;
using UnityEngine;

namespace CuteIssac.Core.Meta
{
    public static class UnlockDisplayNameResolver
    {
        private static readonly Dictionary<string, string> DisplayNamesByUnlockKey = new(StringComparer.OrdinalIgnoreCase);
        private static bool s_cacheBuilt;

        public static string Resolve(string unlockKey)
        {
            if (string.IsNullOrWhiteSpace(unlockKey))
            {
                return "Meta progression";
            }

            EnsureCacheBuilt();

            return DisplayNamesByUnlockKey.TryGetValue(unlockKey, out string displayName)
                ? displayName
                : unlockKey;
        }

        public static string ResolveUnlockedLabel(string unlockKey)
        {
            return $"Unlocked {Resolve(unlockKey)}";
        }

        public static void ClearCache()
        {
            DisplayNamesByUnlockKey.Clear();
            s_cacheBuilt = false;
        }

        private static void EnsureCacheBuilt()
        {
            if (s_cacheBuilt)
            {
                return;
            }

            s_cacheBuilt = true;
            CacheItemUnlocks();
            CacheCharacterUnlocks();
            CacheUnlockDefinitions();
        }

        private static void CacheItemUnlocks()
        {
            ItemData[] items = Resources.LoadAll<ItemData>(string.Empty);
            for (int index = 0; index < items.Length; index++)
            {
                ItemData item = items[index];
                if (item == null || string.IsNullOrWhiteSpace(item.UnlockKey))
                {
                    continue;
                }

                AddDisplayName(item.UnlockKey, item.DisplayName, item.ItemId);
            }
        }

        private static void CacheCharacterUnlocks()
        {
            CharacterProfileData[] profiles = Resources.LoadAll<CharacterProfileData>("Characters");
            for (int index = 0; index < profiles.Length; index++)
            {
                CharacterProfileData profile = profiles[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.UnlockKey))
                {
                    continue;
                }

                AddDisplayName(profile.UnlockKey, profile.DisplayName, profile.CharacterId);
            }
        }

        private static void CacheUnlockDefinitions()
        {
            UnlockData[] unlocks = Resources.LoadAll<UnlockData>("Unlocks");
            for (int index = 0; index < unlocks.Length; index++)
            {
                UnlockData unlock = unlocks[index];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.TargetKey))
                {
                    continue;
                }

                string fallback = unlock.TargetType == UnlockTargetType.RoomType
                    ? FormatRoomType(unlock.TargetRoomType)
                    : unlock.UnlockId;
                AddDisplayName(unlock.TargetKey, unlock.DisplayName, fallback);
            }
        }

        private static string FormatRoomType(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Secret => "Secret Rooms",
                RoomType.Challenge => "Challenge Rooms",
                RoomType.MiniBoss => "Mini Boss Rooms",
                RoomType.Trap => "Trap Rooms",
                RoomType.Curse => "Curse Rooms",
                _ => $"{roomType} Rooms"
            };
        }

        private static void AddDisplayName(string unlockKey, string displayName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(unlockKey) || DisplayNamesByUnlockKey.ContainsKey(unlockKey))
            {
                return;
            }

            DisplayNamesByUnlockKey.Add(unlockKey, !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : fallback);
        }
    }
}
