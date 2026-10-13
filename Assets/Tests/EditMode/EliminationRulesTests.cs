using System;
using System.Collections.Generic;
using BattleRoyale.GameModes.Elimination;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class EliminationRulesTests
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
        public void EliminationIsAPlayablePvpMode()
        {
            var modes = (MapGameMode[])Enum.GetValues(typeof(MapGameMode));
            CollectionAssert.Contains(modes, MapGameMode.Elimination);
            Assert.IsTrue(MapGameModes.HasPlacement(MapGameMode.Elimination));
            Assert.AreEqual(
                GameModeOpposition.PvP,
                MapGameModes.Opposition(MapGameMode.Elimination));
            Assert.IsFalse(MapGameModes.HasPlacement(MapGameMode.Assault));
        }

        [Test]
        public void TeamSpawnsFollowTheTeamCount()
        {
            var map = Create<MapDefinition>("Map");
            map.sourceMin = Vector2.zero;
            map.sourceMax = new Vector2(200f, 200f);
            var configuration = Create<MapModeConfiguration>("Elimination");
            configuration.map = map;
            configuration.mode = MapGameMode.Elimination;
            configuration.teamCount = 2;
            configuration.teamSize = EliminationRules.DefaultTeamSize;

            Assert.IsTrue(configuration.IsElimination);
            Assert.IsTrue(EliminationRules.EnsureTeamSpawns(configuration));
            Assert.AreEqual(2, configuration.eliminationTeams.Count);
            Assert.AreNotEqual(
                configuration.eliminationTeams[0].spawnPosition,
                configuration.eliminationTeams[1].spawnPosition);
            Assert.IsFalse(EliminationRules.EnsureTeamSpawns(configuration));

            configuration.teamCount = 1;
            Assert.IsTrue(EliminationRules.EnsureTeamSpawns(configuration));
            Assert.AreEqual(
                EliminationRules.MinimumTeamCount,
                configuration.eliminationTeams.Count);
            Assert.IsFalse(
                EliminationRules.EnsureTeamSpawns(
                    Create<MapModeConfiguration>("Other")));
        }

        [Test]
        public void ARoundEndsWhenOnlyOneTeamIsAlive()
        {
            Assert.IsFalse(EliminationRules.SharesTeam(0, 1));
            Assert.IsTrue(EliminationRules.SharesTeam(1, 1));
            Assert.IsFalse(EliminationRules.IsRoundOver(new[] { 1, 1 }));
            Assert.AreEqual(-1, EliminationRules.SoleSurvivor(new[] { 1, 1, 2 }));

            Assert.IsTrue(EliminationRules.IsRoundOver(new[] { 3, 0 }));
            Assert.AreEqual(0, EliminationRules.SoleSurvivor(new[] { 3, 0 }));
            Assert.IsTrue(EliminationRules.IsRoundOver(new[] { 0, 0, 1 }));
            Assert.AreEqual(2, EliminationRules.SoleSurvivor(new[] { 0, 0, 1 }));
        }

        [Test]
        public void AMutualWipeScoresNoRound()
        {
            Assert.IsTrue(EliminationRules.IsRoundOver(new[] { 0, 0 }));
            Assert.AreEqual(-1, EliminationRules.SoleSurvivor(new[] { 0, 0 }));
            Assert.AreEqual(-1, EliminationRules.TimeExpiryWinner(new[] { 0, 0 }));
        }

        [Test]
        public void TimeExpiryAwardsTheTeamWithMorePlayersAlive()
        {
            Assert.AreEqual(1, EliminationRules.TimeExpiryWinner(new[] { 1, 4 }));
            Assert.AreEqual(0, EliminationRules.TimeExpiryWinner(new[] { 2, 0, 1 }));
            Assert.AreEqual(-1, EliminationRules.TimeExpiryWinner(new[] { 2, 2 }));
            Assert.AreEqual(-1, EliminationRules.TimeExpiryWinner(null));
        }

        [Test]
        public void RoundWinsReachTheMatchLimit()
        {
            var wins = new List<int> { 0, 0 };
            EliminationRules.AddRoundWin(wins, 0);
            EliminationRules.AddRoundWin(wins, -1);
            EliminationRules.AddRoundWin(wins, 3);
            Assert.AreEqual(-1, EliminationRules.FindMatchWinner(wins, 2));

            EliminationRules.AddRoundWin(wins, 0);
            Assert.AreEqual(2, wins[0]);
            Assert.AreEqual(0, wins[1]);
            Assert.AreEqual(0, EliminationRules.FindMatchWinner(wins, 2));
            Assert.AreEqual(-1, EliminationRules.FindMatchWinner(wins, 0));
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
