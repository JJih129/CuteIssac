using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.UI
{
    internal interface IUiModalDismissible
    {
        void DismissForModal();
    }

    internal static class UiModalDismissRegistry
    {
        private static readonly HashSet<IUiModalDismissible> ActiveDismissibles = new();
        private static readonly List<IUiModalDismissible> Scratch = new();

        public static void Register(IUiModalDismissible dismissible)
        {
            if (dismissible != null)
            {
                ActiveDismissibles.Add(dismissible);
            }
        }

        public static void Unregister(IUiModalDismissible dismissible)
        {
            if (dismissible != null)
            {
                ActiveDismissibles.Remove(dismissible);
            }
        }

        public static void DismissAll()
        {
            Scratch.Clear();
            Scratch.AddRange(ActiveDismissibles);

            for (int index = 0; index < Scratch.Count; index++)
            {
                Scratch[index]?.DismissForModal();
            }

            Scratch.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnSubsystemRegistration()
        {
            ActiveDismissibles.Clear();
            Scratch.Clear();
        }
    }
}
