using System;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.Company
{
    public enum TournamentDifficulty
    {
        Easy,
        Normal,
        Hard,
    }

    [Serializable]
    public sealed class TournamentOffer
    {
        public string Name;
        public TournamentDifficulty Difficulty;
        public int TeamCount = 2;
        public int TeamSize = 5;
        public int EntryFee;
        public int PrizePool;
        public int SuggestedRating;
        public MapModeConfiguration Configuration;
    }

    [CreateAssetMenu(fileName = "Tournament Catalog", menuName = "Battle Royale/Tournament Catalog")]
    public sealed class TournamentCatalog : ScriptableObject
    {
        public TournamentOffer[] Offers;
    }
}
