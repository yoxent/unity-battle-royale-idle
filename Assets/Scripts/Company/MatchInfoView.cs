using BattleRoyale.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class MatchInfoView : MonoBehaviour
    {
        [SerializeField] Image mapPicture;
        [SerializeField] TMP_Text mapName;

        void OnEnable()
        {
            EventBus.Subscribe<MatchCardViewed>(Apply);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<MatchCardViewed>(Apply);
        }

        void Apply(MatchCardViewed viewed)
        {
            if (mapPicture == null || mapName == null)
            {
                Debug.LogError("Match info needs the map picture and the map name.", this);
                return;
            }

            mapName.text = string.IsNullOrEmpty(viewed.MapName) ? "Map" : viewed.MapName;
            mapPicture.sprite = viewed.MapPicture;
            mapPicture.enabled = viewed.MapPicture != null;
        }
    }
}
