using BattleRoyale.Movement;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [DisallowMultipleComponent]
    public sealed class MapInstance : MonoBehaviour
    {
        public MapDefinition definition;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            AuthoringMarkerPlayMode.Hide(gameObject);
        }

        public Vector3 WorldPoint(Vector3 definitionPoint)
        {
            return transform.TransformPoint(definitionPoint);
        }

        public void PublishNavigation()
        {
            var navigation = GetComponent<MapNavigationController>();
            if (navigation == null)
            {
                return;
            }

            navigation.PublishPose();
        }
    }
}
