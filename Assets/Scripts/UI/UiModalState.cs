using System;
using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.UI
{
    /// <summary>
    /// Shared gameplay-presentation modal gate.
    /// Systems that should quiet down while blocking modals are open can listen here.
    /// </summary>
    public static class UiModalState
    {
        private static readonly HashSet<string> ActiveScopes = new();

        public static event Action<bool> GameplayModalStateChanged;

        public static bool IsGameplayModalActive => ActiveScopes.Count > 0;

        public static void SetScopeActive(string scopeId, bool active)
        {
            if (string.IsNullOrWhiteSpace(scopeId))
            {
                return;
            }

            bool changed = active
                ? ActiveScopes.Add(scopeId)
                : ActiveScopes.Remove(scopeId);

            if (!changed)
            {
                return;
            }

            GameplayModalStateChanged?.Invoke(IsGameplayModalActive);
        }

        public static void ResetAll()
        {
            if (ActiveScopes.Count == 0)
            {
                return;
            }

            ActiveScopes.Clear();
            GameplayModalStateChanged?.Invoke(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnSubsystemRegistration()
        {
            ActiveScopes.Clear();
            GameplayModalStateChanged = null;
        }
    }
}
