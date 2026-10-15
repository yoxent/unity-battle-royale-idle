using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [CreateAssetMenu(
        fileName = "BattleRoyaleModeConfiguration",
        menuName = "Battle Royale/Maps/Battle Royale Mode")]
    public sealed class BattleRoyaleModeConfiguration : MapModeConfiguration
    {
        public List<MapModePoint> spawnPoints = new();
    }
}
