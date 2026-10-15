using UnityEngine;

namespace BattleRoyale.Maps
{
    public readonly struct MapViewStepRequested
    {
        public MapViewStepRequested(int direction)
        {
            Direction = direction;
        }

        public int Direction { get; }
    }
}
