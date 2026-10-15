namespace BattleRoyale.Maps
{
    public readonly struct MapViewChanged
    {
        public MapViewChanged(int previousSlot, int slot, MapInstance map)
        {
            PreviousSlot = previousSlot;
            Slot = slot;
            Map = map;
        }

        public int PreviousSlot { get; }
        public int Slot { get; }
        public MapInstance Map { get; }
    }
}
