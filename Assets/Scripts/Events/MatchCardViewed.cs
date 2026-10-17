using UnityEngine;

namespace BattleRoyale.Events
{
    public readonly struct MatchCardViewed
    {
        public MatchCardViewed(int card, bool mapStarted, string mapName, Sprite mapPicture)
        {
            Card = card;
            MapStarted = mapStarted;
            MapName = mapName ?? string.Empty;
            MapPicture = mapPicture;
        }

        public int Card { get; }
        public bool MapStarted { get; }
        public string MapName { get; }
        public Sprite MapPicture { get; }
    }
}
