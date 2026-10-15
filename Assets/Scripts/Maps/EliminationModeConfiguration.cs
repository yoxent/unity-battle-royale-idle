using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [CreateAssetMenu(
        fileName = "EliminationModeConfiguration",
        menuName = "Battle Royale/Maps/Elimination Mode")]
    public sealed class EliminationModeConfiguration : MapModeConfiguration
    {
        public List<MapTeamSpawn> teams = new();

        [Min(1)]
        public int roundWinLimit = 13;

        [Min(1f)]
        public float roundSeconds = 115f;

        [Min(0f)]
        public float intermissionSeconds = 7f;
    }
}
