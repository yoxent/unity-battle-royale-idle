using System.Collections.Generic;
using BattleRoyale.Maps;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class VillageWaterGeometryTests
    {
        [Test]
        public void ARoadThatCrossesTheRiverBecomesOneDeck()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var roads = new List<VillageRoadLine>
            {
                new(
                    new List<Vector2> { new(-5f, 10f), new(25f, 10f) },
                    1.4f)
            };

            var decks = VillageWaterGeometry.FindBridgeDecks(water, roads, 4f, 6f);

            Assert.AreEqual(1, decks.Count);
            Assert.IsTrue(VillageWaterGeometry.TopCovers(
                DeckVertices(decks[0]),
                DeckTriangles(decks[0]),
                new Vector2(10f, 10f)));
        }

        [Test]
        public void ARoadThatOnlyNicksTheBankIsNotABridge()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var roads = new List<VillageRoadLine>
            {
                new(
                    new List<Vector2> { new(-1f, 1f), new(1f, -2f) },
                    1.4f)
            };

            var decks = VillageWaterGeometry.FindBridgeDecks(water, roads, 4f, 6f);

            Assert.AreEqual(0, decks.Count);
        }

        [Test]
        public void TheWaterVolumeLeavesTheBridgeHoleOpen()
        {
            var water = Square(0f, 0f, 20f, 20f);
            var hole = Square(8f, 8f, 12f, 12f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var built = VillageWaterGeometry.TryBuildVolume(
                water,
                new List<List<Vector2>> { hole },
                VillageWaterGeometry.BlockHeight,
                vertices,
                triangles);

            Assert.IsTrue(built);
            Assert.IsTrue(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(2f, 2f)));
            Assert.IsFalse(VillageWaterGeometry.TopCovers(vertices, triangles, new Vector2(10f, 10f)));
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

        static List<Vector3> DeckVertices(List<Vector2> deck)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            VillageWaterGeometry.TryBuildDeck(deck, 0.05f, vertices, triangles);
            return vertices;
        }

        static List<int> DeckTriangles(List<Vector2> deck)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            VillageWaterGeometry.TryBuildDeck(deck, 0.05f, vertices, triangles);
            return triangles;
        }
    }
}
