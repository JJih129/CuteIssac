using System.Collections.Generic;
using UnityEngine;

namespace CuteIssac.UI
{
    /// <summary>
    /// Temporarily hides world-space TextMesh renderers while a blocking modal is open.
    /// This is intentionally narrow so regular gameplay resumes untouched when the modal closes.
    /// </summary>
    internal static class WorldTextModalSuppressor
    {
        private static readonly Dictionary<MeshRenderer, bool> CachedStates = new();
        private static bool _isSuppressed;

        public static void SetSuppressed(bool suppressed)
        {
            if (_isSuppressed == suppressed)
            {
                return;
            }

            _isSuppressed = suppressed;

            if (_isSuppressed)
            {
                SuppressAllVisibleWorldText();
                return;
            }

            RestoreAll();
        }

        public static void SuppressNonFeedbackWorldText()
        {
            TextMesh[] textMeshes = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int index = 0; index < textMeshes.Length; index++)
            {
                TextMesh textMesh = textMeshes[index];

                if (textMesh == null
                    || textMesh.GetComponentInParent<FloatingFeedbackView>() != null
                    || textMesh.GetComponentInParent<Room.FloorExitVisual>() != null)
                {
                    continue;
                }

                MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }

                if (!string.IsNullOrEmpty(textMesh.text))
                {
                    textMesh.text = string.Empty;
                }
            }
        }

        private static void SuppressAllVisibleWorldText()
        {
            CachedStates.Clear();

            TextMesh[] textMeshes = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int index = 0; index < textMeshes.Length; index++)
            {
                TextMesh textMesh = textMeshes[index];

                if (textMesh == null)
                {
                    continue;
                }

                if (textMesh.GetComponentInParent<Room.FloorExitVisual>() != null)
                {
                    continue;
                }

                MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();

                if (renderer == null || CachedStates.ContainsKey(renderer))
                {
                    continue;
                }

                CachedStates.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
        }

        private static void RestoreAll()
        {
            foreach (KeyValuePair<MeshRenderer, bool> pair in CachedStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = pair.Value;
                }
            }

            CachedStates.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnSubsystemRegistration()
        {
            CachedStates.Clear();
            _isSuppressed = false;
        }
    }
}
