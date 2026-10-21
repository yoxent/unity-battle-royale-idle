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
        public void ColumnIsACylinderOfTheGivenDiameterAndHeight()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            WallBoxMesh.AppendCylinder(
                vertices,
                triangles,
                new Vector3(3f, 5f, 1f),
                diameter: 2f,
                height: 4f,
                sides: WallBoxMesh.ColumnSides);

            var mesh = WallBoxMesh.Build("Column", vertices, triangles);
            var points = mesh.vertices;
            var normals = mesh.normals;
            var sawRim = false;

            Assert.AreEqual(new Vector3(3f, 5f, 1f), mesh.bounds.center);
            Assert.AreEqual(2f, mesh.bounds.size.x, 0.001f);
            Assert.AreEqual(4f, mesh.bounds.size.y, 0.001f);
            Assert.AreEqual(2f, mesh.bounds.size.z, 0.001f);
            for (var i = 0; i < points.Length; i++)
            {
                var offset = points[i] - mesh.bounds.center;
                var horizontal = new Vector2(offset.x, offset.z).magnitude;
                Assert.LessOrEqual(horizontal, 1.001f);
                var horizontalNormal = new Vector2(normals[i].x, normals[i].z);
                if (horizontal > 0.9f && horizontalNormal.sqrMagnitude > 0.25f)
                {
                    sawRim = true;
                    Assert.Greater(
                        Vector2.Dot(horizontalNormal, new Vector2(offset.x, offset.z)),
                        0f);
                }

                if (horizontal < 0.001f)
                {
                    Assert.Greater(normals[i].y * Mathf.Sign(offset.y), 0.9f);
                }
            }

            Assert.IsTrue(sawRim);
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
