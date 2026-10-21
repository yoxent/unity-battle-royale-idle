using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Settings
{
    public sealed class SettingsTabButton : MonoBehaviour
    {
        [SerializeField] SettingsTab tab;
        [SerializeField] SettingsPage page;
        [SerializeField] Button button;
        [SerializeField] GameObject selectedMarker;

        bool subscribed;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (button == null)
            {
                Debug.LogError("SettingsTabButton: assign a button.", this);
                return;
            }

            button.onClick.AddListener(HandleClick);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            subscribed = false;
        }

        public void SetSelected(bool selected)
        {
            if (selectedMarker == null)
            {
                Debug.LogError("SettingsTabButton: assign a selected marker.", this);
                return;
            }

            selectedMarker.SetActive(selected);
        }

        void HandleClick()
        {
            if (page == null)
            {
                Debug.LogError("SettingsTabButton: assign a settings page.", this);
                return;
            }

            page.Show(tab);
        }
    }
}
