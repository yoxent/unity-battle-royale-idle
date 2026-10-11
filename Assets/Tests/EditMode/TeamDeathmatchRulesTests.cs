using System;
using System.Collections.Generic;
using BattleRoyale.GameModes.TeamDeathmatch;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class TeamDeathmatchRulesTests
    {
        private readonly List<UnityEngine.Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in created)
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            created.Clear();
        }

        [Test]
        public void GameModeListIncludesThePlayableModes()
        {
            var modes = (MapGameMode[])Enum.GetValues(typeof(MapGameMode));
            CollectionAssert.Contains(modes, MapGameMode.BattleRoyale);
            CollectionAssert.Contains(modes, MapGameMode.CaptureTheFlag);
            CollectionAssert.Contains(modes, MapGameMode.TeamDeathmatch);
        }

        [Test]
        public void GameModeListIncludesTheReservedModes()
        {
            var modes = (MapGameMode[])Enum.GetValues(typeof(MapGameMode));
            CollectionAssert.Contains(modes, MapGameMode.Control);
            CollectionAssert.Contains(modes, MapGameMode.KingOfTheHill);
            CollectionAssert.Contains(modes, MapGameMode.Assault);
            CollectionAssert.Contains(modes, MapGameMode.Extraction);
            CollectionAssert.Contains(modes, MapGameMode.Oddball);
            CollectionAssert.Contains(modes, MapGameMode.Infection);
            CollectionAssert.Contains(modes, MapGameMode.Firefight);
            CollectionAssert.Contains(modes, MapGameMode.BountyHunt);
            CollectionAssert.Contains(modes, MapGameMode.Payload);
            CollectionAssert.Contains(modes, MapGameMode.Survival);
            CollectionAssert.Contains(modes, MapGameMode.Extermination);
            CollectionAssert.Contains(modes, MapGameMode.Raid);
            Assert.IsFalse(MapGameModes.HasPlacement(MapGameMode.Control));
            Assert.IsTrue(MapGameModes.HasPlacement(MapGameMode.TeamDeathmatch));
        }

        [Test]
        public void OppositionFollowsWhoTheModeFights()
        {
            Assert.AreEqual(
                GameModeOpposition.PvP,
                MapGameModes.Opposition(MapGameMode.Infection));
            Assert.AreEqual(
                GameModeOpposition.PvP,
                MapGameModes.Opposition(MapGameMode.BountyHunt));
            Assert.AreEqual(
                GameModeOpposition.PvE,
                MapGameModes.Opposition(MapGameMode.Firefight));
            Assert.AreEqual(
                GameModeOpposition.PvE,
                MapGameModes.Opposition(MapGameMode.Survival));
            Assert.AreEqual(
                GameModeOpposition.PvPvE,
                MapGameModes.Opposition(MapGameMode.Extraction));
        }

        [Test]
        public void TeamSpawnsFollowTheTeamCount()
        {
            var map = Create<MapDefinition>("Map");
            map.sourceMin = Vector2.zero;
            map.sourceMax = new Vector2(200f, 200f);
            var configuration = Create<MapModeConfiguration>("Team Deathmatch");
            configuration.map = map;
            configuration.mode = MapGameMode.TeamDeathmatch;
            configuration.teamCount = 2;
            configuration.teamSize = TeamDeathmatchRules.DefaultTeamSize;

            Assert.IsTrue(configuration.IsTeamDeathmatch);
            Assert.IsTrue(TeamDeathmatchRules.EnsureTeamSpawns(configuration));
            Assert.AreEqual(2, configuration.teamDeathmatchTeams.Count);
            Assert.AreNotEqual(
                configuration.teamDeathmatchTeams[0].spawnPosition,
                configuration.teamDeathmatchTeams[1].spawnPosition);
            Assert.IsFalse(TeamDeathmatchRules.EnsureTeamSpawns(configuration));

            configuration.teamCount = 1;
            Assert.IsTrue(TeamDeathmatchRules.EnsureTeamSpawns(configuration));
            Assert.AreEqual(
                TeamDeathmatchRules.MinimumTeamCount,
                configuration.teamDeathmatchTeams.Count);
        }

        [Test]
        public void KillsScoreUntilATeamReachesTheLimit()
        {
            var scores = new List<int> { 0, 0 };
            Assert.IsFalse(TeamDeathmatchRules.SharesTeam(-1, -1));
            Assert.IsFalse(TeamDeathmatchRules.SharesTeam(0, 1));
            Assert.IsTrue(TeamDeathmatchRules.SharesTeam(1, 1));

            TeamDeathmatchRules.AddKill(scores, 1);
            Assert.AreEqual(-1, TeamDeathmatchRules.FindWinner(scores, 2));
            TeamDeathmatchRules.AddKill(scores, 1);
            TeamDeathmatchRules.AddKill(scores, -1);

            Assert.AreEqual(0, scores[0]);
            Assert.AreEqual(2, scores[1]);
            Assert.AreEqual(1, TeamDeathmatchRules.FindWinner(scores, 2));
        }

        private T Create<T>(string name) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = name;
            created.Add(instance);
            return instance;
        }
    }
}
