using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Settings
{
    public sealed class SettingsQuitButton : MonoBehaviour
    {
        [SerializeField] Button button;

        bool subscribed;

        void OnEnable()
        {
            if (button == null)
            {
                Debug.LogError("SettingsQuitButton: assign a button.", this);
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

        void HandleClick()
        {
            Application.Quit();
        }
    }
}
