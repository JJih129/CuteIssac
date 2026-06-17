using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    public enum TitleMenuButtonAction
    {
        NewRun = 0,
        Continue = 1,
        Character = 2,
        Options = 3,
        Collection = 4,
        Achievements = 5,
        Stats = 6,
        Credits = 7,
        ResetData = 8,
        Quit = 9
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class TitleMenuButtonId : MonoBehaviour
    {
        [SerializeField] private TitleMenuButtonAction actionId;
        [SerializeField] private Button button;

        public TitleMenuButtonAction ActionId => actionId;
        public Button Button => button != null ? button : GetComponent<Button>();

        public void Configure(TitleMenuButtonAction action)
        {
            actionId = action;
            button = GetComponent<Button>();
        }

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnValidate()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }
    }
}
