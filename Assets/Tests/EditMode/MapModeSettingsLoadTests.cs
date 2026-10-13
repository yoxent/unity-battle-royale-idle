using System.Collections.Generic;
using BattleRoyale.Maps;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapModeSettingsLoadTests
    {
        [Test]
        public void NewMapHasEveryGameModeDisabled()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            var panel = new MapModeAuthoringPanel();
            try
            {
                Assert.NotNull(definition.supportedGameModes);
                Assert.AreEqual(0, definition.supportedGameModes.Count);

                panel.BindSavedSettings(definition);

                Assert.AreEqual(0, definition.supportedGameModes.Count);
                Assert.IsNull(panel.BoundConfiguration);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SavedModeSettingsLoadInsteadOfDraftDefaults()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            var configuration = ScriptableObject.CreateInstance<MapModeConfiguration>();
            var panel = new MapModeAuthoringPanel();
            try
            {
                definition.supportedGameModes = new List<MapGameMode>
                {
                    MapGameMode.Deathmatch
                };
                definition.editPointsInSceneView = false;
                configuration.map = definition;
                configuration.mode = MapGameMode.Deathmatch;
                configuration.variantName = "Skirmish";
                configuration.teamCount = 7;
                configuration.teamSize = 4;
                configuration.scoreLimit = 40;
                configuration.respawnSeconds = 9f;
                definition.modeConfigurations = new List<MapModeConfiguration>
                {
                    configuration
                };

                panel.BindSavedSettings(definition);

                Assert.AreEqual(MapGameMode.Deathmatch, panel.BoundMode);
                Assert.AreEqual("Skirmish", panel.BoundVariantName);
                Assert.AreEqual(7, panel.BoundTeamCount);
                Assert.AreEqual(4, panel.BoundTeamSize);
                Assert.AreEqual(40, panel.BoundScoreLimit);
                Assert.AreEqual(9f, panel.BoundRespawnSeconds);
                Assert.AreEqual(7, configuration.teamCount);
                Assert.AreEqual("Skirmish", configuration.variantName);
                Assert.IsFalse(panel.BoundEditPointsInSceneView);
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SavedEliminationSettingsLoadInsteadOfDraftDefaults()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            var configuration = ScriptableObject.CreateInstance<MapModeConfiguration>();
            var panel = new MapModeAuthoringPanel();
            var spawn = new Vector3(4f, 0f, 9f);
            try
            {
                definition.supportedGameModes = new List<MapGameMode>
                {
                    MapGameMode.Elimination
                };
                definition.editPointsInSceneView = false;
                configuration.map = definition;
                configuration.mode = MapGameMode.Elimination;
                configuration.variantName = "Short";
                configuration.teamCount = 2;
                configuration.teamSize = 5;
                configuration.roundWinLimit = 9;
                configuration.roundSeconds = 80f;
                configuration.intermissionSeconds = 4f;
                configuration.eliminationTeams = new List<MapModeTeamSpawn>
                {
                    new MapModeTeamSpawn
                    {
                        teamId = "Team 1",
                        spawnPosition = spawn,
                        radius = 6f
                    },
                    new MapModeTeamSpawn
                    {
                        teamId = "Team 2",
                        spawnPosition = spawn + new Vector3(20f, 0f, 0f),
                        radius = 6f
                    }
                };
                definition.modeConfigurations = new List<MapModeConfiguration>
                {
                    configuration
                };

                panel.BindSavedSettings(definition);

                Assert.AreEqual(MapGameMode.Elimination, panel.BoundMode);
                Assert.AreEqual("Short", panel.BoundVariantName);
                Assert.AreEqual(2, panel.BoundTeamCount);
                Assert.AreEqual(5, panel.BoundTeamSize);
                Assert.AreEqual(9, panel.BoundRoundWinLimit);
                Assert.AreEqual(80f, panel.BoundRoundSeconds);
                Assert.AreEqual(4f, panel.BoundIntermissionSeconds);
                Assert.AreEqual(spawn, configuration.eliminationTeams[0].spawnPosition);
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SavedSpawnPointsStayWhereTheyWere()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            var configuration = ScriptableObject.CreateInstance<MapModeConfiguration>();
            var panel = new MapModeAuthoringPanel();
            var spawn = new Vector3(12f, 0f, -4f);
            try
            {
                definition.supportedGameModes = new List<MapGameMode>
                {
                    MapGameMode.BattleRoyale
                };
                configuration.map = definition;
                configuration.mode = MapGameMode.BattleRoyale;
                configuration.variantName = "Duos";
                configuration.teamCount = 8;
                configuration.teamSize = 2;
                configuration.battleRoyaleSpawnPoints = new List<MapModePoint>();
                for (var i = 0; i < 8; i++)
                {
                    configuration.battleRoyaleSpawnPoints.Add(new MapModePoint
                    {
                        id = "Keep " + (i + 1),
                        position = spawn + new Vector3(i, 0f, 0f),
                        radius = 3f
                    });
                }

                definition.modeConfigurations = new List<MapModeConfiguration>
                {
                    configuration
                };

                panel.BindSavedSettings(definition);

                Assert.AreEqual("Duos", panel.BoundVariantName);
                Assert.AreEqual(8, panel.BoundTeamCount);
                Assert.AreEqual(2, panel.BoundTeamSize);
                Assert.AreEqual(spawn, configuration.battleRoyaleSpawnPoints[0].position);
                Assert.AreEqual(3f, configuration.battleRoyaleSpawnPoints[0].radius);
                Assert.AreEqual("Keep 1", configuration.battleRoyaleSpawnPoints[0].id);
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(definition);
            }
        }
    }
}
