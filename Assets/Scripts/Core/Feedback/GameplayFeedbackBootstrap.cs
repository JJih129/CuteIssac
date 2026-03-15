using CuteIssac.UI;
using UnityEngine;

namespace CuteIssac.Core.Feedback
{
    public static class GameplayFeedbackBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePresenterExistsAfterSceneLoad()
        {
            EnsurePresenterExists();
        }

        public static void EnsurePresenterExists()
        {
            if (Object.FindFirstObjectByType<GameplayFeedbackPresenter>(FindObjectsInactive.Exclude) != null)
            {
                return;
            }

            GameObject presenterObject = new("GameplayFeedbackPresenter");
            presenterObject.AddComponent<GameplayFeedbackPresenter>();
        }
    }
}
