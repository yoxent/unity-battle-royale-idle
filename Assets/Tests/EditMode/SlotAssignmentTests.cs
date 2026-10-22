using System.Collections.Generic;
using System.Reflection;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.GameModes.Elimination;
using BattleRoyale.Maps;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Stage = BattleRoyale.Maps.SimulationStage;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SlotAssignmentTests
    {
        private readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in created)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            created.Clear();
        }

        [Test]
        public void AssignSlot_PutsTheMapOnTheChosenIndex()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var eliminationMap = ReadyDefinition(MapGameMode.Elimination);
            var stage = CreateStage(
                Create<DeathmatchGameMode>("Deathmatch"),
                Create<EliminationGameMode>("Elimination"));
            var manager = CreateManager(stage, deathmatchMap, eliminationMap);

            manager.SlotIndex = 0;
            Assert.IsTrue(manager.AssignSlot(deathmatchMap, MapGameMode.Deathmatch));
            manager.SlotIndex = 1;
            Assert.IsTrue(manager.AssignSlot(eliminationMap, MapGameMode.Elimination));

            Assert.AreSame(deathmatchMap, SlotConfiguration(stage, 0).map);
            Assert.AreSame(eliminationMap, SlotConfiguration(stage, 1).map);
            Assert.AreEqual(MapGameMode.Deathmatch, SlotConfiguration(stage, 0).mode);
            Assert.AreEqual(MapGameMode.Elimination, SlotConfiguration(stage, 1).mode);
        }

        [Test]
        public void UseModeOnSlot_ReplacesTheMatchWithTheChosenMode()
        {
            var agents = new GameObject("Agents");
            created.Add(agents);
            var stage = CreateStage(
                Create<DeathmatchGameMode>("Deathmatch"),
                Create<DeathmatchGameMode>("Other"),
                Create<EliminationGameMode>("Elimination"));
            var elimination = SlotMatch(stage, 2);
            var serialized = new SerializedObject(elimination);
            serialized.FindProperty("agentParent").objectReferenceValue = agents.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(MapBakeOutput.UseModeOnSlot(stage, 2, MapGameMode.Deathmatch));

            var replacement = SlotMatch(stage, 2);
            Assert.IsInstanceOf<DeathmatchGameMode>(replacement);
            Assert.AreEqual("Deathmatch", replacement.gameObject.name);
            Assert.IsNull(replacement.GetComponent<EliminationGameMode>());
            Assert.AreSame(
                agents.transform,
                new SerializedObject(replacement).FindProperty("agentParent").objectReferenceValue);
            Assert.IsInstanceOf<DeathmatchGameMode>(SlotMatch(stage, 0));
        }

        [Test]
        public void ClearSlot_KeepsTheMatchAndStopsIt()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var match = Create<DeathmatchGameMode>("Deathmatch");
            var stage = CreateStage(match);
            var manager = CreateManager(stage, deathmatchMap);
            manager.SlotIndex = 0;
            Assert.IsTrue(manager.AssignSlot(deathmatchMap, MapGameMode.Deathmatch));

            Assert.IsTrue(manager.ClearSlot());

            Assert.IsFalse(match.gameObject.activeSelf);
            Assert.IsNull(match.ModeConfiguration);
            Assert.IsNull(match.ActiveMap);
            Assert.AreSame(match, SlotMatch(stage, 0));
        }

        [Test]
        public void AssignSlot_FillsAClearedSlot()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var match = Create<DeathmatchGameMode>("Deathmatch");
            var stage = CreateStage(match);
            var manager = CreateManager(stage, deathmatchMap);
            manager.SlotIndex = 0;
            Assert.IsTrue(manager.ClearSlot());

            Assert.IsTrue(manager.AssignSlot(deathmatchMap, MapGameMode.Deathmatch));

            Assert.IsTrue(match.gameObject.activeSelf);
            Assert.AreSame(deathmatchMap, match.ModeConfiguration.map);
            Assert.AreEqual(MapGameMode.Deathmatch, match.ModeConfiguration.mode);
        }

        [Test]
        public void ClearSlot_RefusesAnIndexThatDoesNotExist()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var stage = CreateStage(Create<DeathmatchGameMode>("Deathmatch"));
            var manager = CreateManager(stage, deathmatchMap);
            manager.SlotIndex = 3;

            LogAssert.Expect(LogType.Error, "Simulation slot 3 does not exist.");
            Assert.IsFalse(manager.ClearSlot());
            Assert.IsTrue(SlotMatch(stage, 0).gameObject.activeSelf);
        }

        [Test]
        public void PlaceMaps_ReusesTheLoadedCatalogMap()
        {
            var definition = ReadyDefinition(MapGameMode.Deathmatch);
            var sceneMap = definition.bakedPrefab.GetComponent<MapInstance>();
            sceneMap.definition = definition;
            var match = Create<DeathmatchGameMode>("Deathmatch");
            match.UseConfiguration(definition.modeConfigurations[0]);
            var stage = CreateStage(match);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            LogAssert.Expect(LogType.Error, "The simulation stage needs the active map's playable catalog.");
            stage.GetType().GetMethod("Awake", flags).Invoke(stage, null);

            var manager = CreateManager(stage, definition);
            manager.SetActiveMap(sceneMap);
            stage.GetType().GetField("mapManager", flags).SetValue(stage, manager);

            LogAssert.Expect(LogType.Error, "The simulation stage needs a watch camera.");
            stage.GetType().GetMethod("Awake", flags).Invoke(stage, null);

            var placed = (MapInstance[])stage.GetType().GetField("placedMaps", flags).GetValue(stage);
            Assert.AreSame(sceneMap, placed[0]);
            Assert.AreEqual(1, CountMaps(definition));
        }

        private static int CountMaps(MapDefinition definition)
        {
            var maps = Resources.FindObjectsOfTypeAll<MapInstance>();
            var count = 0;
            for (var index = 0; index < maps.Length; index++)
            {
                var map = maps[index];
                if (map == null ||
                    map.definition != definition ||
                    EditorUtility.IsPersistent(map))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        [Test]
        public void PlaceMaps_SkipsAnInactiveSlot()
        {
            var runningMap = ReadyDefinition(MapGameMode.Deathmatch);
            var running = Create<DeathmatchGameMode>("Running");
            running.UseConfiguration(Configuration(runningMap, MapGameMode.Deathmatch));
            var idle = Create<DeathmatchGameMode>("Idle");
            idle.gameObject.SetActive(false);
            var stage = CreateStage(idle, running);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            LogAssert.Expect(LogType.Error, "The simulation stage needs the active map's playable catalog.");
            stage.GetType().GetMethod("Awake", flags).Invoke(stage, null);

            var manager = CreateManager(stage, runningMap);
            stage.GetType().GetField("mapManager", flags).SetValue(stage, manager);

            LogAssert.Expect(LogType.Error, "The simulation stage needs a watch camera.");
            stage.GetType().GetMethod("Awake", flags).Invoke(stage, null);

            var placed = (MapInstance[])stage.GetType().GetField("placedMaps", flags).GetValue(stage);
            Assert.IsNull(placed[0]);
            Assert.IsNotNull(placed[1]);
            Assert.AreEqual(1, stage.WatchedSlot);
            created.Add(placed[1].gameObject);
        }

        [Test]
        public void AssignSlot_RefusesAnIndexThatDoesNotExist()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var stage = CreateStage(Create<DeathmatchGameMode>("Deathmatch"));
            var manager = CreateManager(stage, deathmatchMap);
            manager.SlotIndex = 3;

            LogAssert.Expect(LogType.Error, "Simulation slot 3 does not exist.");
            Assert.IsFalse(manager.AssignSlot(deathmatchMap, MapGameMode.Deathmatch));
            Assert.IsNull(SlotMatch(stage, 0).ModeConfiguration);
        }

        [Test]
        public void AssignSlot_RefusesAnEmptySlot()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var stage = CreateStage(Create<DeathmatchGameMode>("Deathmatch"));
            var manager = CreateManager(stage, deathmatchMap);
            manager.SlotIndex = 2;

            LogAssert.Expect(LogType.Error, "Simulation slot 2 has no match.");
            Assert.IsFalse(manager.AssignSlot(deathmatchMap, MapGameMode.Deathmatch));
            Assert.IsNull(SlotMatch(stage, 0).ModeConfiguration);
        }

        [Test]
        public void AssignSlot_UsesTheIndexWhenNoMapIsPlacedYet()
        {
            var deathmatchMap = ReadyDefinition(MapGameMode.Deathmatch);
            var eliminationMap = ReadyDefinition(MapGameMode.Elimination);
            var stage = CreateStage(
                Create<DeathmatchGameMode>("Deathmatch"),
                Create<EliminationGameMode>("Elimination"));
            var manager = CreateManager(stage, deathmatchMap, eliminationMap);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            stage.GetType().GetField("placedMaps", flags)
                .SetValue(stage, System.Array.Empty<MapInstance>());
            stage.GetType().GetField("slotCenters", flags)
                .SetValue(stage, System.Array.Empty<Vector3>());
            manager.SlotIndex = 0;

            Assert.IsTrue(manager.AssignSlot(eliminationMap, MapGameMode.Elimination));

            Assert.AreSame(eliminationMap, SlotConfiguration(stage, 0).map);
            Assert.IsNull(SlotConfiguration(stage, 1));
        }

        [Test]
        public void TryGetSingleConfiguration_RefusesTwoAssetsForOneMode()
        {
            var definition = CreateDefinition();
            definition.modeConfigurations.Add(Configuration(definition, MapGameMode.Deathmatch));
            definition.modeConfigurations.Add(Configuration(definition, MapGameMode.Deathmatch));

            Assert.IsFalse(definition.TryGetSingleConfiguration(
                MapGameMode.Deathmatch,
                out var configuration));
            Assert.IsNull(configuration);
        }

        private ActiveMapManager CreateManager(Stage stage, params MapDefinition[] definitions)
        {
            var catalog = ScriptableObject.CreateInstance<PlayableMapCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            created.Add(catalog);
            catalog.maps.AddRange(definitions);
            var manager = Create<ActiveMapManager>("Active Map");
            manager.SetPlayableMaps(catalog);
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("stage").objectReferenceValue = stage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private Stage CreateStage(params SimulationMatch[] matches)
        {
            var stage = Create<Stage>("Simulation Stage");
            var serialized = new SerializedObject(stage);
            var slots = serialized.FindProperty("slots");
            slots.arraySize = 3;
            for (var index = 0; index < matches.Length; index++)
            {
                slots.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("match").objectReferenceValue = matches[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private MapDefinition ReadyDefinition(MapGameMode mode)
        {
            var definition = CreateDefinition();
            definition.supportedGameModes.Add(mode);
            definition.bakedPrefab = Create<MapInstance>(mode + " Prefab").gameObject;
            definition.modeConfigurations.Add(Configuration(definition, mode));
            return definition;
        }

        private MapModeConfiguration Configuration(MapDefinition definition, MapGameMode mode)
        {
            MapModeConfiguration configuration = mode switch
            {
                MapGameMode.Elimination => ScriptableObject.CreateInstance<EliminationModeConfiguration>(),
                _ => ScriptableObject.CreateInstance<DeathmatchModeConfiguration>()
            };
            configuration.hideFlags = HideFlags.HideAndDontSave;
            configuration.map = definition;
            configuration.mode = mode;
            created.Add(configuration);
            return configuration;
        }

        private MapDefinition CreateDefinition()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.sourceMin = Vector2.zero;
            definition.sourceMax = new Vector2(20f, 20f);
            definition.worldScale = 1f;
            created.Add(definition);
            return definition;
        }

        private static MapModeConfiguration SlotConfiguration(Stage stage, int index)
        {
            return SlotMatch(stage, index).ModeConfiguration;
        }

        private static SimulationMatch SlotMatch(Stage stage, int index)
        {
            var slots = new SerializedObject(stage).FindProperty("slots");
            return slots.GetArrayElementAtIndex(index)
                .FindPropertyRelative("match").objectReferenceValue as SimulationMatch;
        }

        private T Create<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            created.Add(gameObject);
            return gameObject.AddComponent<T>();
        }
    }
}
