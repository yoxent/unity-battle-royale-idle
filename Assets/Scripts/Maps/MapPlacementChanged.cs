namespace BattleRoyale.Maps
{
    public readonly struct MapPlacementChanged
    {
        public MapPlacementChanged(int occupiedCount)
        {
            OccupiedCount = occupiedCount;
        }

        public int OccupiedCount { get; }
    }
}
