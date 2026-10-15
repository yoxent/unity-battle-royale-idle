using BattleRoyale.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Maps
{
    // Temporary play-mode control for switching the watched map.
    // Delete this component with Assets/UI/MapWatchTest when that check is done.
    public sealed class MapWatchTestButtons : MonoBehaviour
    {
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;

        void OnEnable()
        {
            if (previousButton == null || nextButton == null)
            {
                Debug.LogError("Map watch test buttons need both buttons.", this);
                return;
            }

            previousButton.onClick.AddListener(ShowPrevious);
            nextButton.onClick.AddListener(ShowNext);
        }

        void OnDisable()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(ShowPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(ShowNext);
            }
        }

        void ShowPrevious()
        {
            EventBus.Publish(new MapViewStepRequested(-1));
        }

        void ShowNext()
        {
            EventBus.Publish(new MapViewStepRequested(1));
        }
    }
}
