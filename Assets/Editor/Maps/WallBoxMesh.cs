#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class WallBoxMesh
    {
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
