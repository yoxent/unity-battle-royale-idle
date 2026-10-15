using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [CreateAssetMenu(
        fileName = "DeathmatchModeConfiguration",
        menuName = "Battle Royale/Maps/Deathmatch Mode")]
    public sealed class DeathmatchModeConfiguration : MapModeConfiguration
    {
        public List<MapTeamSpawn> teams = new();

        [Min(1)]
        public int scoreLimit = 50;

        [Min(0f)]
        public float respawnSeconds = 3f;
    }
}
