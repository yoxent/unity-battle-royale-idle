using System.Collections.Generic;
using BattleRoyale.CameraSystems;
using BattleRoyale.Events;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class ActiveMapSyncTests
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
        public void PublishedEventReachesSubscriber()
        {
            ActiveMapChanged received = null;
            void Handler(ActiveMapChanged change) => received = change;
            EventBus.Subscribe<ActiveMapChanged>(Handler);
            try
            {
                var payload = new ActiveMapChanged(null, null);
                EventBus.Publish(payload);
                Assert.AreSame(payload, received);
            }
            finally
            {
                EventBus.Unsubscribe<ActiveMapChanged>(Handler);
            }
        }

        [Test]
        public void UnsubscribedHandlerDoesNotReceiveLaterEvents()
        {
            var count = 0;
            void Handler(ActiveMapChanged change) => count++;
            EventBus.Subscribe<ActiveMapChanged>(Handler);
            EventBus.Unsubscribe<ActiveMapChanged>(Handler);
            EventBus.Publish(new ActiveMapChanged(null, null));
            Assert.AreEqual(0, count);
        }

        [Test]
        public void EventTypesStaySeparate()
        {
            var mapCount = 0;
            var probeCount = 0;
            void MapHandler(ActiveMapChanged change) => mapCount++;
            void ProbeHandler(ProbeEvent change) => probeCount++;
            EventBus.Subscribe<ActiveMapChanged>(MapHandler);
            EventBus.Subscribe<ProbeEvent>(ProbeHandler);
            try
            {
                EventBus.Publish(new ProbeEvent { Value = 3 });
                Assert.AreEqual(0, mapCount);
                Assert.AreEqual(1, probeCount);
            }
            finally
            {
                EventBus.Unsubscribe<ActiveMapChanged>(MapHandler);
                EventBus.Unsubscribe<ProbeEvent>(ProbeHandler);
            }
        }

        [Test]
        public void SettingTheSameMapDoesNotPublishAgain()
        {
            var definition = CreateDefinition();
            var map = CreateMap("Map", definition);
            var manager = Create<ActiveMapManager>("Active Map");
            var count = 0;
            void Handler(ActiveMapChanged change) => count++;
            EventBus.Subscribe<ActiveMapChanged>(Handler);
            try
            {
                manager.SetActiveMap(map);
                manager.SetActiveMap(map);
                Assert.AreEqual(1, count);
                Assert.AreSame(map, manager.ActiveMap);
            }
            finally
            {
                EventBus.Unsubscribe<ActiveMapChanged>(Handler);
            }
        }

        [Test]
        public void CameraAndGameModeFollowManagerMap()
        {
            var definition = CreateDefinition();
            var first = CreateMap("Older Map", definition);
            var second = CreateMap("New Map", definition);
            var manager = Create<ActiveMapManager>("Active Map");
            var camera = CreateCamera();
            var mode = Create<BattleRoyaleGameMode>("Battle Royale");

            manager.SetActiveMap(first);
            Assert.AreSame(first, camera.ActiveMap);
            Assert.AreSame(first, mode.ActiveMap);

            manager.SetActiveMap(second);
            Assert.AreSame(second, camera.ActiveMap);
            Assert.AreSame(second, mode.ActiveMap);
        }

        [Test]
        public void ActivateBakedMapReplacesThePreviousInstance()
        {
            var firstTemplate = CreateMap("First Template", CreateDefinition());
            var secondTemplate = CreateMap("Second Template", CreateDefinition());
            var manager = Create<ActiveMapManager>("Active Map");

            var first = manager.ActivateBakedMap(firstTemplate.gameObject);
            created.Add(first.gameObject);
            var second = manager.ActivateBakedMap(secondTemplate.gameObject);
            created.Add(second.gameObject);

            Assert.AreSame(second, manager.ActiveMap);
            Assert.AreSame(secondTemplate.definition, second.definition);
            Assert.IsTrue(first == null);
            Assert.AreSame(second, manager.ActivateBakedMap(secondTemplate.gameObject));
        }

        [Test]
        public void ActivateForModeKeepsAReadyMapThatSupportsIt()
        {
            var battle = CreateReadyMap(MapGameMode.BattleRoyale);
            var team = CreateReadyMap(MapGameMode.Deathmatch);
            var manager = CreateManager(battle.definition, team.definition);

            var placed = manager.ActivatePlayableMap(battle.definition);
            created.Add(placed.gameObject);

            Assert.AreSame(placed, manager.ActivateForMode(MapGameMode.BattleRoyale));
        }

        [Test]
        public void ActivateForModeSwitchesWhenTheCurrentMapDoesNotSupportIt()
        {
            var battle = CreateReadyMap(MapGameMode.BattleRoyale);
            var team = CreateReadyMap(MapGameMode.Deathmatch);
            var manager = CreateManager(battle.definition, team.definition);
            var placed = manager.ActivatePlayableMap(battle.definition);
            created.Add(placed.gameObject);

            var switched = manager.ActivateForMode(MapGameMode.Deathmatch);
            created.Add(switched.gameObject);

            Assert.AreSame(team.definition, switched.definition);
            Assert.IsTrue(placed == null);
        }

        [Test]
        public void EnsureLoadedKeepsTheSceneMapAndDoesNotSpawnASecondCopy()
        {
            var definition = CreateDefinition();
            var sceneMap = CreateMap("Scene Map", definition);
            definition.bakedPrefab = sceneMap.gameObject;
            var manager = CreateManager(definition);
            manager.SetActiveMap(sceneMap);

            manager.EnsureLoaded();
            manager.EnsureLoaded();

            Assert.AreSame(sceneMap, manager.ActiveMap);
            Assert.AreEqual(1, CountInstances(definition));
        }

        [Test]
        public void ActivateBakedMapReusesAPreloadedCatalogMap()
        {
            var battle = CreateReadyMap(MapGameMode.BattleRoyale);
            var team = CreateReadyMap(MapGameMode.Deathmatch);
            var manager = CreateManager(battle.definition, team.definition);
            manager.SetActiveMap(battle);
            manager.EnsureLoaded();
            var loadedCopies = CountInstances(team.definition);

            var shown = manager.ActivateBakedMap(team.definition.bakedPrefab);

            Assert.AreSame(team.definition, shown.definition);
            Assert.AreNotSame(team, shown);
            Assert.AreEqual(loadedCopies, CountInstances(team.definition));
            Assert.IsFalse(battle == null);
            Assert.IsFalse(battle.gameObject.activeSelf);
            Assert.AreSame(shown, manager.ActivateBakedMap(team.definition.bakedPrefab));
        }

        [Test]
        public void UseAuthoredMapsDropsTheTemporaryCatalog()
        {
            var authored = ScriptableObject.CreateInstance<PlayableMapCatalog>();
            authored.hideFlags = HideFlags.HideAndDontSave;
            created.Add(authored);
            authored.maps.Add(CreateDefinition());
            var temporary = ScriptableObject.CreateInstance<PlayableMapCatalog>();
            temporary.hideFlags = HideFlags.HideAndDontSave;
            created.Add(temporary);
            temporary.maps.Add(CreateDefinition());
            var manager = Create<ActiveMapManager>("Active Map");
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("playableMaps").objectReferenceValue = authored;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            manager.SetPlayableMaps(temporary);
            Assert.AreSame(temporary, manager.PlayableMaps);

            manager.UseAuthoredMaps();
            Assert.AreSame(authored, manager.PlayableMaps);
        }

        [Test]
        public void ActivatePlayableMapIgnoresAMapThatIsNotReady()
        {
            var battle = CreateReadyMap(MapGameMode.BattleRoyale);
            var manager = CreateManager(battle.definition);
            var placed = manager.ActivatePlayableMap(battle.definition);
            created.Add(placed.gameObject);

            var outsider = CreateReadyMap(MapGameMode.BattleRoyale);
            LogAssert.Expect(
                LogType.Error,
                "Choose a map from the playable catalog. It needs a baked prefab.");
            Assert.AreSame(placed, manager.ActivatePlayableMap(outsider.definition));
            Assert.AreSame(placed, manager.ActiveMap);
        }

        private MapInstance CreateReadyMap(MapGameMode mode)
        {
            var definition = CreateDefinition();
            definition.supportedGameModes.Add(mode);
            var map = CreateMap(mode.ToString(), definition);
            definition.bakedPrefab = map.gameObject;
            return map;
        }

        private ActiveMapManager CreateManager(params MapDefinition[] definitions)
        {
            var catalog = ScriptableObject.CreateInstance<PlayableMapCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            created.Add(catalog);
            catalog.maps.AddRange(definitions);
            var manager = Create<ActiveMapManager>("Active Map");
            manager.SetPlayableMaps(catalog);
            return manager;
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

        private static int CountInstances(MapDefinition definition)
        {
            var maps = Object.FindObjectsByType<MapInstance>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var count = 0;
            for (var i = 0; i < maps.Length; i++)
            {
                if (maps[i] != null && maps[i].definition == definition)
                {
                    count++;
                }
            }

            return count;
        }

        private MapInstance CreateMap(string name, MapDefinition definition)
        {
            var map = Create<MapInstance>(name);
            map.definition = definition;
            return map;
        }

        private SimulationCameraController CreateCamera()
        {
            var cameraObject = new GameObject("Simulation Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            created.Add(cameraObject);
            cameraObject.AddComponent<Camera>();
            return cameraObject.AddComponent<SimulationCameraController>();
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

        private sealed class ProbeEvent
        {
            public int Value;
        }
    }
}
