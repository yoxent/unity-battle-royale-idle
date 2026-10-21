#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class WallBoxMesh
    {
        public const int ColumnSides = 12;

        private const string MaterialPath = "Assets/Materials/MarkerCube.mat";
        private const string FallbackMaterialPath = "Assets/Materials/MapPlane.mat";

        public static Material SharedMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            return AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
        }

        public static void AppendBox(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            Vector3 size,
            Quaternion rotation)
        {
            var half = size * 0.5f;
            var min = -half;
            var max = half;
            AddFace(
                vertices,
                triangles,
                center,
                rotation,
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, max.y, min.z));
            AddFace(
                vertices,
                triangles,
                center,
                rotation,
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, min.y, max.z));
            AddFace(
                vertices,
                triangles,
                center,
                rotation,
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, min.y, max.z));
            AddFace(
                vertices,
                triangles,
                center,
                rotation,
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, max.y, min.z));
            AddFace(
                vertices,
                triangles,
                center,
                rotation,
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(min.x, max.y, max.z));
            AddFace(
                vertices,
                triangles,
                center,
                rotation,
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, min.y, min.z));
        }

        public static void AppendCylinder(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            float diameter,
            float height,
            int sides)
        {
            if (vertices == null ||
                triangles == null ||
                diameter <= 0.001f ||
                height <= 0.001f ||
                sides < 3)
            {
                return;
            }

            var radius = diameter * 0.5f;
            var halfHeight = height * 0.5f;
            var ring = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = Mathf.PI * 2f * i / sides;
                ring[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
            }

            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                AddFace(
                    vertices,
                    triangles,
                    center,
                    Quaternion.identity,
                    ring[i] + Vector3.down * halfHeight,
                    ring[i] + Vector3.up * halfHeight,
                    ring[next] + Vector3.up * halfHeight,
                    ring[next] + Vector3.down * halfHeight);
            }

            AppendCap(vertices, triangles, center, ring, halfHeight, true);
            AppendCap(vertices, triangles, center, ring, halfHeight, false);
        }

        private static void AppendCap(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            Vector3[] ring,
            float halfHeight,
            bool top)
        {
            var y = top ? halfHeight : -halfHeight;
            var centerIndex = vertices.Count;
            vertices.Add(center + Vector3.up * y);
            var ringStart = vertices.Count;
            for (var i = 0; i < ring.Length; i++)
            {
                vertices.Add(center + ring[i] + Vector3.up * y);
            }

            for (var i = 0; i < ring.Length; i++)
            {
                var next = (i + 1) % ring.Length;
                triangles.Add(centerIndex);
                if (top)
                {
                    triangles.Add(ringStart + next);
                    triangles.Add(ringStart + i);
                }
                else
                {
                    triangles.Add(ringStart + i);
                    triangles.Add(ringStart + next);
                }
            }
        }

        public static void AppendMesh(
            List<Vector3> vertices,
            List<int> triangles,
            Mesh source,
            Vector3 position,
            Vector3 scale)
        {
            if (source == null)
            {
                return;
            }

            var sourceVertices = source.vertices;
            var sourceTriangles = source.triangles;
            var start = vertices.Count;
            for (var i = 0; i < sourceVertices.Length; i++)
            {
                var point = sourceVertices[i];
                vertices.Add(new Vector3(
                    position.x + point.x * scale.x,
                    position.y + point.y * scale.y,
                    position.z + point.z * scale.z));
            }

            for (var i = 0; i < sourceTriangles.Length; i++)
            {
                triangles.Add(start + sourceTriangles[i]);
            }
        }

        public static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh
            {
                name = name
            };
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 center,
            Quaternion rotation,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            var start = vertices.Count;
            vertices.Add(center + rotation * a);
            vertices.Add(center + rotation * b);
            vertices.Add(center + rotation * c);
            vertices.Add(center + rotation * d);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }
    }
}
#endif
