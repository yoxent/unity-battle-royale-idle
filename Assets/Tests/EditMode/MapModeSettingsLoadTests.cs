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
                    MapGameMode.TeamDeathmatch
                };
                definition.editPointsInSceneView = false;
                configuration.map = definition;
                configuration.mode = MapGameMode.TeamDeathmatch;
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

                Assert.AreEqual(MapGameMode.TeamDeathmatch, panel.BoundMode);
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
