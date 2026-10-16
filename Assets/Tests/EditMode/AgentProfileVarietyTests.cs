using System.Collections.Generic;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEditor;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AgentProfileVarietyTests
    {
        private static readonly string[] ProfilePaths =
        {
            "Assets/GameModes/BattleRoyale/HunterAgentProfile.asset",
            "Assets/GameModes/BattleRoyale/ScoutAgentProfile.asset",
            "Assets/GameModes/BattleRoyale/AnchorAgentProfile.asset",
            "Assets/GameModes/BattleRoyale/GunnerAgentProfile.asset",
            "Assets/GameModes/BattleRoyale/SurvivorAgentProfile.asset"
        };

        private static readonly string[] MapPaths =
        {
            "Assets/Maps/plaguecrown_chambers/Definitions/plaguecrown_chambers_Map.asset",
            "Assets/Maps/maze_of_the_cursed_messiah/Definitions/maze_of_the_cursed_messiah_Map.asset"
        };

        [Test]
        public void AuthoredProfilesHaveMeaningfullyDifferentBehavior()
        {
            var names = new HashSet<string>();
            var aggressionMinimum = 1f;
            var aggressionMaximum = 0f;
            var resilienceMinimum = 1f;
            var resilienceMaximum = 0f;
            var awarenessMinimum = 1f;
            var awarenessMaximum = 0f;

            foreach (var path in ProfilePaths)
            {
                var profile =
                    AssetDatabase.LoadAssetAtPath<BattleRoyaleAgentProfile>(
                        path);
                Assert.IsNotNull(profile, $"Missing profile asset: {path}");
                Assert.IsNotNull(profile.stats, $"Missing stats: {path}");
                Assert.IsNotNull(profile.ai, $"Missing AI settings: {path}");
                Assert.IsNotNull(
                    profile.defaultWeapon,
                    $"Missing weapon: {path}");
                Assert.IsTrue(names.Add(profile.displayName));

                aggressionMinimum =
                    System.Math.Min(
                        aggressionMinimum,
                        profile.ai.aggression);
                aggressionMaximum =
                    System.Math.Max(
                        aggressionMaximum,
                        profile.ai.aggression);
                resilienceMinimum =
                    System.Math.Min(
                        resilienceMinimum,
                        profile.stats.resilience);
                resilienceMaximum =
                    System.Math.Max(
                        resilienceMaximum,
                        profile.stats.resilience);
                awarenessMinimum =
                    System.Math.Min(
                        awarenessMinimum,
                        profile.stats.awareness);
                awarenessMaximum =
                    System.Math.Max(
                        awarenessMaximum,
                        profile.stats.awareness);
            }

            Assert.Greater(aggressionMaximum - aggressionMinimum, 0.5f);
            Assert.Greater(resilienceMaximum - resilienceMinimum, 0.3f);
            Assert.Greater(awarenessMaximum - awarenessMinimum, 0.3f);
        }

        [Test]
        public void AuthoredMapsHaveTacticalAnchors()
        {
            foreach (var path in MapPaths)
            {
                var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(path);
                Assert.IsNotNull(map, $"Missing map definition: {path}");
                Assert.IsNotNull(map.tacticalAnchors);
                Assert.Greater(map.tacticalAnchors.Count, 0);
                Assert.IsTrue(
                    map.tacticalAnchors.TrueForAll(
                        anchor => anchor != null && anchor.weight > 0f));
            }
        }
    }
}
