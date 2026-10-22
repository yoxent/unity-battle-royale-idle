using System.Collections.Generic;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class DungeonFloorFacingTests
    {
        [Test]
        public void UpwardTriangleIsAccepted()
        {
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f)
            };

            Assert.AreEqual(0, DungeonFloorFacing.CountDownward(vertices, new[] { 0, 1, 2 }));
        }

        [Test]
        public void DownwardTriangleIsRejected()
        {
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f)
            };

            Assert.AreEqual(1, DungeonFloorFacing.CountDownward(vertices, new[] { 0, 2, 1 }));
        }

        [Test]
        public void DegenerateTriangleIsIgnored()
        {
            var vertices = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(2f, 0f, 0f)
            };

            Assert.AreEqual(0, DungeonFloorFacing.CountDownward(vertices, new[] { 0, 1, 2 }));
        }

        [Test]
        public void GridFanFacesUpAfterTheWorldFlip()
        {
            var vertices = new List<Vector3> { new Vector3(0f, 0f, 0f) };
            const int segments = 8;
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle)));
            }

            var upward = new List<int>();
            DungeonFloorFacing.AddGridFan(upward, 0, 1, segments);
            Assert.AreEqual(0, DungeonFloorFacing.CountDownward(vertices, upward));

            var reversed = new List<int>();
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                reversed.Add(0);
                reversed.Add(1 + next);
                reversed.Add(1 + i);
            }

            Assert.AreEqual(segments, DungeonFloorFacing.CountDownward(vertices, reversed));
        }

        [Test]
        public void DownwardMessageNamesTheBakeHole()
        {
            Assert.That(
                DungeonFloorFacing.DescribeDownward(1),
                Does.Contain("faces down").And.Contain("navigation bake"));
            Assert.That(
                DungeonFloorFacing.DescribeDownward(2),
                Does.Contain("2 floor triangles").And.Contain("leaves a hole"));
        }
    }
}
