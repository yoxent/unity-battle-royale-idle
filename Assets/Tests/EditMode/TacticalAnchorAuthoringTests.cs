using System.Collections.Generic;
using BattleRoyale.Maps;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class TacticalAnchorAuthoringTests
    {
        private static readonly string[] MapPaths =
        {
            "Assets/Maps/blackwing_temple/Definitions/blackwing_temple_Map.asset",
            "Assets/Maps/hall_of_the_raven_savant/Definitions/hall_of_the_raven_savant_Map.asset",
            "Assets/Maps/sunken_temple_of_mande/Definitions/sunken_temple_of_mande_Map.asset"
        };

        [Test]
        public void SaveCopiesMarkerLocalPositionWeightAndId()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.tacticalAnchors = new List<MapTacticalAnchor>
            {
                new MapTacticalAnchor
                {
                    id = "Center",
                    position = new Vector3(1f, 0f, 2f),
                    weight = 5f
                }
            };
            var mapObject = new GameObject("Tactical Anchor Map");
            var map = mapObject.AddComponent<MapInstance>();
            map.definition = definition;

            try
            {
                TacticalAnchorAuthoring.Show(definition);
                var marker = mapObject.GetComponentInChildren<MapTacticalAnchorMarker>();
                Assert.IsNotNull(marker);
                Assert.IsNotNull(marker.transform.Find("MarkerDisc"));
                AssertDiscColor(marker, TacticalAnchorAuthoring.MarkerColor);
                marker.transform.localPosition = new Vector3(4f, 0f, 5f);
                marker.weight = 2f;
                marker.anchorId = "Gate";

                TacticalAnchorAuthoring.Save(definition);

                Assert.AreEqual(1, definition.tacticalAnchors.Count);
                Assert.AreEqual("Gate", definition.tacticalAnchors[0].id);
                Assert.AreEqual(2f, definition.tacticalAnchors[0].weight, 0.001f);
                Assert.AreEqual(
                    new Vector3(4f, 0f, 5f),
                    definition.tacticalAnchors[0].position);
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SyncDropsASecondAnchorRootAndARemovedAnchor()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.tacticalAnchors = new List<MapTacticalAnchor>
            {
                new MapTacticalAnchor
                {
                    id = "Center",
                    position = new Vector3(9.5f, 0f, 2.4f),
                    weight = 5f
                },
                new MapTacticalAnchor
                {
                    id = "Side1",
                    position = new Vector3(4f, 0f, 5f),
                    weight = 2f
                }
            };
            var mapObject = new GameObject("Tactical Anchor Map");
            mapObject.AddComponent<MapInstance>().definition = definition;

            try
            {
                TacticalAnchorAuthoring.Show(definition);
                var stale = new GameObject(TacticalAnchorAuthoring.RootName);
                stale.transform.SetParent(mapObject.transform, false);
                var staleMarker = new GameObject("Gate");
                staleMarker.transform.SetParent(stale.transform, false);
                staleMarker.AddComponent<MapTacticalAnchorMarker>().anchorId = "Gate";
                definition.tacticalAnchors.RemoveAt(1);
                TacticalAnchorAuthoring.Show(definition);

                var roots = 0;
                for (var index = 0; index < mapObject.transform.childCount; index++)
                {
                    if (mapObject.transform.GetChild(index).name == TacticalAnchorAuthoring.RootName)
                    {
                        roots++;
                    }
                }

                var markers = mapObject.GetComponentsInChildren<MapTacticalAnchorMarker>(true);
                Assert.AreEqual(1, roots);
                Assert.AreEqual(1, markers.Length);
                Assert.AreEqual("Center", markers[0].anchorId);
                Assert.AreEqual(new Vector3(9.5f, 0f, 2.4f), markers[0].transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SyncFollowsAnchorListOrder()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            var center = new MapTacticalAnchor
            {
                id = "Center",
                position = new Vector3(1f, 0f, 2f),
                weight = 5f
            };
            var gate = new MapTacticalAnchor
            {
                id = "Gate",
                position = new Vector3(8f, 0f, 3f),
                weight = 3f
            };
            definition.tacticalAnchors = new List<MapTacticalAnchor> { center, gate };
            var mapObject = new GameObject("Tactical Anchor Order Map");
            mapObject.AddComponent<MapInstance>().definition = definition;

            try
            {
                TacticalAnchorAuthoring.Show(definition);
                definition.tacticalAnchors.Remove(gate);
                definition.tacticalAnchors.Insert(0, gate);
                TacticalAnchorAuthoring.Show(definition);

                var markers = mapObject.GetComponentsInChildren<MapTacticalAnchorMarker>(true);
                Assert.AreEqual(2, markers.Length);
                Assert.AreEqual("Gate", markers[0].anchorId);
                Assert.AreEqual(new Vector3(8f, 0f, 3f), markers[0].transform.localPosition);
                Assert.AreEqual("Center", markers[1].anchorId);
                Assert.AreEqual(new Vector3(1f, 0f, 2f), markers[1].transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SyncKeepsAMovedMarkerPosition()
        {
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.tacticalAnchors = new List<MapTacticalAnchor>
            {
                new MapTacticalAnchor
                {
                    id = "Center",
                    position = new Vector3(1f, 0f, 2f),
                    weight = 5f
                }
            };
            var mapObject = new GameObject("Tactical Anchor Move Map");
            mapObject.AddComponent<MapInstance>().definition = definition;

            try
            {
                TacticalAnchorAuthoring.Show(definition);
                var marker = mapObject.GetComponentInChildren<MapTacticalAnchorMarker>();
                marker.transform.localPosition = new Vector3(6f, 0f, 7f);
                TacticalAnchorAuthoring.Show(definition);

                Assert.AreEqual(new Vector3(6f, 0f, 7f), marker.transform.localPosition);
                Assert.AreEqual(new Vector3(6f, 0f, 7f), definition.tacticalAnchors[0].position);
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void DefaultMarkerColorIsAnchorPurple()
        {
            Assert.AreEqual(
                "A659FF",
                ColorUtility.ToHtmlStringRGB(MapTacticalAnchorMarker.DefaultColor));
        }

        [Test]
        public void MarkerColorPaintsTheDisc()
        {
            var previous = TacticalAnchorAuthoring.MarkerColor;
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.tacticalAnchors = new List<MapTacticalAnchor>
            {
                new MapTacticalAnchor { id = "Center", weight = 1f }
            };
            var mapObject = new GameObject("Tactical Anchor Color Map");
            var map = mapObject.AddComponent<MapInstance>();
            map.definition = definition;
            var picked = new Color(0.1f, 0.8f, 0.3f, 1f);

            try
            {
                TacticalAnchorAuthoring.SetMarkerColor(picked);
                TacticalAnchorAuthoring.Show(definition);
                var marker = mapObject.GetComponentInChildren<MapTacticalAnchorMarker>();
                Assert.AreEqual(picked, marker.color);
                AssertDiscColor(marker, picked);
            }
            finally
            {
                TacticalAnchorAuthoring.SetMarkerColor(previous);
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(definition);
            }
        }

        private static void AssertDiscColor(MapTacticalAnchorMarker marker, Color expected)
        {
            var renderer = marker.transform.Find("MarkerDisc").GetComponent<MeshRenderer>();
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var actual = block.GetColor("_BaseColor");
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }

        [Test]
        public void PlayableTempleMapsHaveWeightedAnchors()
        {
            for (var index = 0; index < MapPaths.Length; index++)
            {
                var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(MapPaths[index]);
                Assert.IsNotNull(map, MapPaths[index]);
                Assert.GreaterOrEqual(map.tacticalAnchors.Count, 3, map.name);
                for (var anchorIndex = 0; anchorIndex < map.tacticalAnchors.Count; anchorIndex++)
                {
                    var anchor = map.tacticalAnchors[anchorIndex];
                    Assert.IsNotNull(anchor, map.name);
                    Assert.Greater(anchor.weight, 0f, map.name);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(anchor.id), map.name);
                }
            }
        }
    }
}
