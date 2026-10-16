using UnityEngine;

namespace BattleRoyale.Maps
{
    public readonly struct MapViewStepRequested
    {
        public MapViewStepRequested(int direction, bool useFade)
        {
            Direction = direction;
            UseFade = useFade;
        }

        public int Direction { get; }
        public bool UseFade { get; }
    }
}
