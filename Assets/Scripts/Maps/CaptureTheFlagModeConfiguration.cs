using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    [CreateAssetMenu(
        fileName = "CaptureTheFlagModeConfiguration",
        menuName = "Battle Royale/Maps/Capture The Flag Mode")]
    public sealed class CaptureTheFlagModeConfiguration : MapModeConfiguration
    {
        public List<MapModeTeamPoints> teams = new();
    }
}
