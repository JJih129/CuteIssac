using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace CuteIssac.UI
{
    /// <summary>
    /// Ensures uGUI uses InputSystemUIInputModule when the project runs with the new Input System only.
    /// This runs at scene load so runtime-generated UI does not depend on scene-authored EventSystem setup.
    /// </summary>
    public static class InputSystemEventSystemBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureReady();
        }

        private static void HandleSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnsureReady();
        }

        public static void EnsureReady()
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (eventSystems.Length == 0)
            {
                GameObject eventSystemObject = new("EventSystem");
                EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();
                SanitizeEventSystem(eventSystem);
                return;
            }

            for (int i = 0; i < eventSystems.Length; i++)
            {
                SanitizeEventSystem(eventSystems[i]);
            }
        }

        private static void SanitizeEventSystem(EventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return;
            }

            InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputSystemModule == null)
            {
                inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputSystemModule.enabled = true;

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            List<BaseInputModule> legacyModules = new();

            for (int i = 0; i < modules.Length; i++)
            {
                BaseInputModule module = modules[i];

                if (module == null || ReferenceEquals(module, inputSystemModule))
                {
                    continue;
                }

                legacyModules.Add(module);
            }

            for (int i = 0; i < legacyModules.Count; i++)
            {
                BaseInputModule legacyModule = legacyModules[i];
                legacyModule.enabled = false;
                Object.DestroyImmediate(legacyModule);
            }

            eventSystem.enabled = false;
            eventSystem.enabled = true;
        }
    }
}
