using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class AuthoringMarkerPlayModeTests
    {
        [Test]
        public void HideTurnsOffWallRenderersAndAuthoringMarkers()
        {
            var root = new GameObject("Map");
            var walls = new GameObject("DungeonWalls");
            walls.transform.SetParent(root.transform, false);
            var wallMesh = new GameObject("DungeonWallMesh");
            wallMesh.transform.SetParent(walls.transform, false);
            var wallRenderer = wallMesh.AddComponent<MeshRenderer>();
            var wallCollider = wallMesh.AddComponent<BoxCollider>();

            var boundary = new GameObject("BoundaryWalls");
            boundary.transform.SetParent(root.transform, false);
            var boundaryRenderer = boundary.AddComponent<MeshRenderer>();

            var visual = new GameObject("VisualMap");
            visual.transform.SetParent(root.transform, false);
            var visualRenderer = visual.AddComponent<MeshRenderer>();

            var building = new GameObject("BuildingWallMesh");
            building.transform.SetParent(root.transform, false);
            var buildingRenderer = building.AddComponent<MeshRenderer>();

            var marker = new GameObject("DeathmatchSpawn");
            marker.transform.SetParent(root.transform, false);
            marker.AddComponent<MapModePointMarker>();
            var anchor = new GameObject("Anchor");
            anchor.transform.SetParent(root.transform, false);
            anchor.AddComponent<MapTacticalAnchorMarker>();
            var corner = new GameObject("ImageCornerUpperLeft");
            corner.transform.SetParent(root.transform, false);

            try
            {
                AuthoringMarkerPlayMode.Hide(root);

                Assert.IsFalse(wallRenderer.enabled);
                Assert.IsTrue(wallCollider.enabled);
                Assert.IsFalse(boundaryRenderer.enabled);
                Assert.IsTrue(visualRenderer.enabled);
                Assert.IsTrue(buildingRenderer.enabled);
                Assert.IsFalse(marker.activeSelf);
                Assert.IsFalse(anchor.activeSelf);
                Assert.IsFalse(corner.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ViewConeLengthStaysInWorldUnitsWhenTheAgentIsScaled()
        {
            var unscaled = BattleRoyaleViewCone.LocalScaleForWorldLength(10f, 0.5f, 1f);
            var scaled = BattleRoyaleViewCone.LocalScaleForWorldLength(10f, 0.5f, 10f);

            Assert.AreEqual(5f, unscaled.x, 0.0001f);
            Assert.AreEqual(10f, unscaled.z, 0.0001f);
            Assert.AreEqual(0.5f, scaled.x, 0.0001f);
            Assert.AreEqual(1f, scaled.z, 0.0001f);
            Assert.AreEqual(unscaled, scaled * 10f);
        }
    }
}
