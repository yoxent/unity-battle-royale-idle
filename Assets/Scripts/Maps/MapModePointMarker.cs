using UnityEngine;

namespace BattleRoyale.Maps
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class MapModePointMarker : MonoBehaviour
    {
        public MapModeConfiguration configuration;
        public MapModePointType pointType;
        public int index;
        public int teamIndex = -1;

        private void OnDrawGizmos()
        {
            Gizmos.color = GetColor(pointType);
            Gizmos.DrawSphere(transform.position, 1.25f);
        }

        public static Color GetColor(MapModePointType type)
        {
            return type switch
            {
                MapModePointType.BattleRoyaleSpawn => new Color(0.2f, 0.8f, 1f, 0.9f),
                MapModePointType.TeamSpawn => new Color(0.2f, 1f, 0.3f, 0.9f),
                MapModePointType.TeamBase => new Color(1f, 0.8f, 0.1f, 0.9f),
                MapModePointType.TeamFlag => new Color(1f, 0.25f, 0.2f, 0.9f),
                MapModePointType.DeathmatchSpawn => new Color(0.65f, 0.35f, 1f, 0.9f),
                MapModePointType.EliminationSpawn => new Color(1f, 0.72f, 0.15f, 0.9f),
                _ => Color.white
            };
        }
    }
}
