using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public readonly struct MuzzleFlashRequested
    {
        public MuzzleFlashRequested(Vector3 position, float scale)
        {
            Position = position;
            Scale = scale;
        }

        public Vector3 Position { get; }
        public float Scale { get; }
    }
}
