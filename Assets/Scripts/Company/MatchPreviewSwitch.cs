using BattleRoyale.CameraSystems;
using BattleRoyale.Events;
using UnityEngine;

namespace BattleRoyale.Company
{
    public sealed class MatchPreviewSwitch : MonoBehaviour
    {
        [SerializeField] GameObject activeMatch;
        [SerializeField] GameObject matchInfo;
        [SerializeField] SimulationMapView mapView;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EventBus.Subscribe<MatchCardViewed>(Apply);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<MatchCardViewed>(Apply);
        }

        void Apply(MatchCardViewed viewed)
        {
            if (activeMatch == null || matchInfo == null || mapView == null)
            {
                Debug.LogError(
                    "Match preview needs the active match, the match info, and the map view.",
                    this);
                return;
            }

            activeMatch.SetActive(viewed.MapStarted);
            matchInfo.SetActive(!viewed.MapStarted);
            mapView.SetShown(viewed.MapStarted);
        }
    }
}
