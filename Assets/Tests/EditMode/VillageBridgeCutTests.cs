using System.Collections.Generic;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class VillageBridgeCutTests
    {
        [Test]
        public void AQuadAcrossTheRiverOpensBothBanks()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var bridges = new List<List<Vector2>>
            {
                Quad(-2f, 8f, 22f, 8f, 22f, 12f, -2f, 12f)
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var decks = new List<List<Vector2>>();

            var built = VillageBridgeCut.TryBuildOpenedVolume(
                water,
                bridges,
                VillageWaterGeometry.BlockHeight,
                vertices,
                triangles,
                decks,
                out var skipped);

            Assert.IsTrue(built);
            Assert.AreEqual(0, skipped);
            Assert.AreEqual(1, decks.Count);
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 10f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(0.2f, 10f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(19.8f, 10f)));
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 2f)));
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 18f)));
        }

        [Test]
        public void AShortQuadIsExtendedUntilTheBanksOpen()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var bridges = new List<List<Vector2>>
            {
                Quad(8f, 9f, 12f, 9f, 12f, 11f, 8f, 11f)
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var built = VillageBridgeCut.TryBuildOpenedVolume(
                water,
                bridges,
                2f,
                vertices,
                triangles,
                new List<List<Vector2>>(),
                out var skipped);

            Assert.IsTrue(built);
            Assert.AreEqual(0, skipped);
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 10f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(0.2f, 10f)));
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 2f)));
        }

        [Test]
        public void AQuadThatMissesTheRiverIsSkipped()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var bridges = new List<List<Vector2>>
            {
                Quad(-10f, -6f, -4f, -6f, -4f, -2f, -10f, -2f)
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var built = VillageBridgeCut.TryBuildOpenedVolume(
                water,
                bridges,
                2f,
                vertices,
                triangles,
                new List<List<Vector2>>(),
                out var skipped);

            Assert.IsTrue(built);
            Assert.AreEqual(1, skipped);
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 10f)));
        }

        [Test]
        public void ABentRiverStaysOpenBetweenTheBanks()
        {
            var water = new List<Vector2>
            {
                new(0f, 0f),
                new(30f, 0f),
                new(30f, 30f),
                new(20f, 30f),
                new(20f, 10f),
                new(10f, 10f),
                new(10f, 30f),
                new(0f, 30f)
            };
            var bridges = new List<List<Vector2>>
            {
                Quad(-2f, 18f, 32f, 18f, 32f, 22f, -2f, 22f)
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var built = VillageBridgeCut.TryBuildOpenedVolume(
                water,
                bridges,
                2f,
                vertices,
                triangles,
                new List<List<Vector2>>(),
                out var skipped);

            Assert.IsTrue(built);
            Assert.AreEqual(0, skipped);
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(5f, 28f)));
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(15f, 5f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(15f, 25f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(5f, 20f)));
        }

        [Test]
        public void ARoadCrossingOpensBothBanks()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var roads = new List<VillageRoadLine>
            {
                new(
                    new List<Vector2> { new(-5f, 10f), new(25f, 10f) },
                    1.4f)
            };
            var openings = VillageWaterGeometry.FindBridgeOpenings(water, roads, 4f, 6f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var built = VillageBridgeCut.TryBuildOpenedVolume(
                water,
                openings,
                2f,
                vertices,
                triangles,
                new List<List<Vector2>>(),
                out var skipped);

            Assert.IsTrue(built);
            Assert.AreEqual(1, openings.Count);
            Assert.AreEqual(0, skipped);
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 10f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(0.2f, 10f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(19.8f, 10f)));
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 2f)));
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 18f)));
        }

        [Test]
        public void ARoadThatOnlyNicksTheBankDoesNotOpenTheWater()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var roads = new List<VillageRoadLine>
            {
                new(
                    new List<Vector2> { new(-1f, 1f), new(1f, -2f) },
                    1.4f)
            };

            var openings = VillageWaterGeometry.FindBridgeOpenings(water, roads, 4f, 6f);

            Assert.AreEqual(0, openings.Count);
        }

        [Test]
        public void ARoadThatStopsInsideTheBankIsNotABridge()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var roads = new List<VillageRoadLine>
            {
                new(
                    new List<Vector2> { new(-5f, 10f), new(5f, 10f) },
                    1.4f)
            };

            var openings = VillageWaterGeometry.FindBridgeOpenings(water, roads, 4f, 4f);

            Assert.AreEqual(0, openings.Count);
        }

        [Test]
        public void ARoadThatEndsOnTheFarBankIsStillABridge()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var roads = new List<VillageRoadLine>
            {
                new(
                    new List<Vector2> { new(-5f, 10f), new(20f, 10f) },
                    1.4f)
            };

            var openings = VillageWaterGeometry.FindBridgeOpenings(water, roads, 4f, 6f);

            Assert.AreEqual(1, openings.Count);
        }

        static List<Vector2> Quad(
            float ax,
            float ay,
            float bx,
            float by,
            float cx,
            float cy,
            float dx,
            float dy)
        {
            return new List<Vector2>
            {
                new(ax, ay),
                new(bx, by),
                new(cx, cy),
                new(dx, dy)
            };
        }

        static List<Vector2> Square(float minX, float minY, float maxX, float maxY)
        {
            return new List<Vector2>
            {
                new(minX, minY),
                new(maxX, minY),
                new(maxX, maxY),
                new(minX, maxY)
            };
        }
    }
}
