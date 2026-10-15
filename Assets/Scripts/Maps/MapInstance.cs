using UnityEngine;

namespace BattleRoyale.Maps
{
    [DisallowMultipleComponent]
    public sealed class MapInstance : MonoBehaviour
    {
        public MapDefinition definition;

        public Vector3 WorldPoint(Vector3 definitionPoint)
        {
            return transform.TransformPoint(definitionPoint);
        }
    }
}
