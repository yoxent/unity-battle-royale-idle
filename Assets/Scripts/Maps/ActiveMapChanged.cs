using UnityEngine;

namespace BattleRoyale.Maps
{
    public sealed class ActiveMapChanged
    {
        public ActiveMapChanged(MapInstance previous, MapInstance current)
        {
            Previous = previous;
            Current = current;
        }

        public MapInstance Previous { get; }
        public MapInstance Current { get; }

        public bool CurrentIsTemporary =>
            Current != null &&
            (Current.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0;
    }
}
