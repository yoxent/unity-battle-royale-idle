using System.Collections.Generic;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class WallBoxMeshTests
    {
        [Test]
        public void OneEdgeBecomesOneBoxInASingleMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            WallBoxMesh.AppendBox(
                vertices,
                triangles,
                new Vector3(1f, 1f, 0f),
                new Vector3(2f, 2f, 1f),
                Quaternion.identity);

            var mesh = WallBoxMesh.Build("Wall", vertices, triangles);

            Assert.AreEqual(24, mesh.vertexCount);
            Assert.AreEqual(new Vector3(1f, 1f, 0f), mesh.bounds.center);
            Assert.AreEqual(new Vector3(2f, 2f, 1f), mesh.bounds.size);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void RotatedEdgeTurnsTheBoxWithTheEdge()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            WallBoxMesh.AppendBox(
                vertices,
                triangles,
                Vector3.zero,
                new Vector3(4f, 2f, 1f),
                Quaternion.Euler(0f, 90f, 0f));

            var mesh = WallBoxMesh.Build("Wall", vertices, triangles);

            Assert.AreEqual(4f, mesh.bounds.size.z, 0.001f);
            Assert.AreEqual(1f, mesh.bounds.size.x, 0.001f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void AppendedCopiesStayInOneMesh()
        {
            var source = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
                triangles = new[] { 0, 1, 2 }
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            WallBoxMesh.AppendMesh(
                vertices,
                triangles,
                source,
                new Vector3(10f, 0f, 0f),
                new Vector3(2f, 1f, 2f));
            WallBoxMesh.AppendMesh(
                vertices,
                triangles,
                source,
                new Vector3(0f, 3f, 0f),
                Vector3.one);

            var mesh = WallBoxMesh.Build("Trees", vertices, triangles);

            Assert.AreEqual(6, mesh.vertexCount);
            Assert.AreEqual(6, mesh.triangles.Length);
            Assert.AreEqual(new Vector3(10f, 0f, 0f), mesh.vertices[0]);
            Assert.AreEqual(new Vector3(12f, 0f, 0f), mesh.vertices[1]);
            Assert.AreEqual(new Vector3(0f, 4f, 0f), mesh.vertices[5]);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(source);
        }
    }
}
