using System;
using System.Collections.Generic;
using CuteIssac.Common.Stats;
using CuteIssac.Core.Meta;
using CuteIssac.Core.Scene;
using CuteIssac.Data.Run;
using CuteIssac.Player;
using UnityEngine;

namespace CuteIssac.Core.Run
{
    [DefaultExecutionOrder(-90)]
    [DisallowMultipleComponent]
    public sealed class CharacterProfileManager : MonoBehaviour
    {
        private const string SelectedCharacterPrefKey = "meta.selected_character";

        [Header("References")]
        [Tooltip("씬에 배치된 GameplaySceneContext입니다. 비워두면 Active Context를 먼저 사용하고, 마지막에만 씬 검색으로 보정합니다.")]
        [SerializeField] private GameplaySceneContext sceneContext;
        [SerializeField] private RunManager runManager;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerWeaponLoadout playerWeaponLoadout;
        [SerializeField] private PlayerActiveItemController playerActiveItemController;

        [Header("Catalog")]
        [SerializeField] private CharacterProfileCatalog profileCatalog;
        [SerializeField] private string resourcesCatalogPath = "Characters/DefaultCharacterProfileCatalog";

        private readonly List<StatModifier> _statModifierBuffer = new();
        private readonly List<ProjectileModifier> _projectileModifierBuffer = new();
        private CharacterProfileData _selectedProfile;
        private bool _suppressNextRunStartProfile;

        public CharacterProfileData SelectedProfile => _selectedProfile;
        public string SelectedCharacterId => _selectedProfile != null ? _selectedProfile.CharacterId : "default";
        public string SelectedDisplayName => _selectedProfile != null ? _selectedProfile.DisplayName : "Default";

        private void Awake()
        {
            ResolveReferences();
            ResolveCatalog();
            ResolveSelectedProfile();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveCatalog();
            ResolveSelectedProfile();

            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
                runManager.RunStarted += HandleRunStarted;
            }
        }

        private void OnDisable()
        {
            if (runManager != null)
            {
                runManager.RunStarted -= HandleRunStarted;
            }
        }

        public bool TrySelectCharacter(string characterId)
        {
            ResolveCatalog();

            CharacterProfileData nextProfile = profileCatalog != null
                ? profileCatalog.FindProfile(characterId)
                : null;

            if (nextProfile == null)
            {
                return false;
            }

            _selectedProfile = nextProfile;
            PlayerPrefs.SetString(SelectedCharacterPrefKey, nextProfile.CharacterId);
            PlayerPrefs.Save();
            return true;
        }

        public void ConsumeLaunchCharacterIfAny()
        {
            if (CharacterProfileLaunchRequest.TryConsumeRequestedCharacter(out string requestedCharacterId))
            {
                TrySelectCharacter(requestedCharacterId);
            }
        }

        public void SuppressNextRunStartProfile()
        {
            _suppressNextRunStartProfile = true;
        }

        private void HandleRunStarted(RunContext _)
        {
            if (_suppressNextRunStartProfile)
            {
                _suppressNextRunStartProfile = false;
                return;
            }

            ApplySelectedProfile();
        }

        private void ApplySelectedProfile()
        {
            ResolveReferences();
            ResolveSelectedProfile();

            if (_selectedProfile == null)
            {
                return;
            }

            EnsureSelectedProfilePlayable();

            playerInventory?.ApplyStartingLoadout(
                _selectedProfile.StartingCoins,
                _selectedProfile.StartingKeys,
                _selectedProfile.StartingBombs,
                _selectedProfile.StartingPassiveItems);

            _statModifierBuffer.Clear();
            _projectileModifierBuffer.Clear();
            CopyStatModifiers(_selectedProfile.StatModifiers, _statModifierBuffer);
            CopyProjectileModifiers(_selectedProfile.ProjectileModifiers, _projectileModifierBuffer);
            playerStats?.SetStartingBuildModifiers(_statModifierBuffer, _projectileModifierBuffer);
            playerHealth?.RestoreToFull();

            playerWeaponLoadout?.ApplyStartingWeapon(_selectedProfile.StartingWeaponItem);
            playerActiveItemController?.EquipActiveItem(_selectedProfile.StartingActiveItem);
        }

        private void EnsureSelectedProfilePlayable()
        {
            if (IsProfilePlayable(_selectedProfile))
            {
                return;
            }

            CharacterProfileData fallbackProfile = ResolveFallbackPlayableProfile();

            if (fallbackProfile == null || fallbackProfile == _selectedProfile)
            {
                return;
            }

            _selectedProfile = fallbackProfile;
            PlayerPrefs.SetString(SelectedCharacterPrefKey, fallbackProfile.CharacterId);
            PlayerPrefs.Save();
        }

        private CharacterProfileData ResolveFallbackPlayableProfile()
        {
            ResolveCatalog();

            if (IsProfilePlayable(profileCatalog != null ? profileCatalog.DefaultProfile : null))
            {
                return profileCatalog.DefaultProfile;
            }

            if (profileCatalog == null)
            {
                return null;
            }

            IReadOnlyList<CharacterProfileData> profiles = profileCatalog.Profiles;

            for (int index = 0; index < profiles.Count; index++)
            {
                CharacterProfileData profile = profiles[index];

                if (IsProfilePlayable(profile))
                {
                    return profile;
                }
            }

            return profileCatalog.DefaultProfile;
        }

        private static bool IsProfilePlayable(CharacterProfileData profile)
        {
            if (profile == null)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(profile.UnlockKey) ||
                UnlockManager.IsUnlocked(profile.UnlockKey, profile.UnlockedByDefault);
        }

        private void ResolveReferences()
        {
            ResolveReferencesFromSceneContext();

            if (runManager == null)
            {
                runManager = GetComponent<RunManager>();
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            }

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Exclude);
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
            }

            if (playerWeaponLoadout == null)
            {
                playerWeaponLoadout = FindFirstObjectByType<PlayerWeaponLoadout>(FindObjectsInactive.Exclude);
            }

            if (playerActiveItemController == null)
            {
                playerActiveItemController = FindFirstObjectByType<PlayerActiveItemController>(FindObjectsInactive.Exclude);
            }
        }

        private void ResolveReferencesFromSceneContext()
        {
            if (sceneContext == null)
            {
                sceneContext = GameplaySceneContext.Active;
            }

            if (sceneContext == null)
            {
                return;
            }

            sceneContext.ResolveMissingReferences();
            runManager ??= sceneContext.RunManager;
            playerInventory ??= sceneContext.PlayerInventory;
            playerStats ??= sceneContext.PlayerStats;
            playerHealth ??= sceneContext.PlayerHealth;
            playerWeaponLoadout ??= sceneContext.PlayerWeaponLoadout;
            playerActiveItemController ??= sceneContext.PlayerActiveItemController;
        }

        private void ResolveCatalog()
        {
            if (profileCatalog == null && !string.IsNullOrWhiteSpace(resourcesCatalogPath))
            {
                profileCatalog = Resources.Load<CharacterProfileCatalog>(resourcesCatalogPath);
            }
        }

        private void ResolveSelectedProfile()
        {
            if (_selectedProfile != null)
            {
                return;
            }

            ResolveCatalog();

            if (CharacterProfileLaunchRequest.TryPeekRequestedCharacter(out string launchCharacterId))
            {
                TrySelectCharacter(launchCharacterId);
                return;
            }

            string savedCharacterId = PlayerPrefs.GetString(SelectedCharacterPrefKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedCharacterId) && TrySelectCharacter(savedCharacterId))
            {
                return;
            }

            _selectedProfile = profileCatalog != null ? profileCatalog.DefaultProfile : null;
        }

        private static void CopyStatModifiers(IReadOnlyList<StatModifier> source, List<StatModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }

        private static void CopyProjectileModifiers(IReadOnlyList<ProjectileModifier> source, List<ProjectileModifier> destination)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }
    }
}
