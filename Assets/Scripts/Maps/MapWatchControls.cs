using BattleRoyale.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Maps
{
    [DisallowMultipleComponent]
    public sealed class MapWatchControls : MonoBehaviour
    {
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] SimulationStage stage;
        [SerializeField] bool useFade = true;

        void OnEnable()
        {
            if (previousButton == null || nextButton == null || stage == null)
            {
                Debug.LogError(
                    "Map watch controls need both buttons and the simulation stage.",
                    this);
                return;
            }

            previousButton.onClick.AddListener(ShowPrevious);
            nextButton.onClick.AddListener(ShowNext);
            EventBus.Subscribe<MapPlacementChanged>(HandlePlacementChanged);
            ApplyNavigation(stage.OccupiedMapCount);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<MapPlacementChanged>(HandlePlacementChanged);
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(ShowPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(ShowNext);
            }
        }

        void HandlePlacementChanged(MapPlacementChanged change)
        {
            if (previousButton == null || nextButton == null)
            {
                return;
            }

            ApplyNavigation(change.OccupiedCount);
        }

        void ApplyNavigation(int occupiedCount)
        {
            var show = occupiedCount > 1;
            previousButton.gameObject.SetActive(show);
            nextButton.gameObject.SetActive(show);
        }

        void ShowPrevious()
        {
            EventBus.Publish(new MapViewStepRequested(-1, useFade));
        }

        void ShowNext()
        {
            EventBus.Publish(new MapViewStepRequested(1, useFade));
        }
    }
}
