#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class MapBoundaryWalls
    {
        public static void Create(
            Transform parent,
            string meshName,
            Vector2 min,
            Vector2 max,
            float thickness,
            float height,
            float baseHeight)
        {
            if (parent == null || thickness <= 0f || height <= 0f)
            {
                return;
            }

            var walls = new List<BoundaryWall>();
            MapPlayBounds.AddBoundaryWalls(min, max, thickness, walls);
            MapPlayBounds.Subdivide(walls, MapPlayBounds.MaxColliderEdge);
            if (walls.Count == 0)
            {
                return;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var centerY = baseHeight + height * 0.5f;
            foreach (var wall in walls)
            {
                WallBoxMesh.AppendBox(
                    vertices,
                    triangles,
                    new Vector3(wall.Center.x, centerY, wall.Center.y),
                    new Vector3(wall.Size.x, height, wall.Size.y),
                    Quaternion.identity);
            }

            var wallObject = new GameObject("BoundaryWalls");
            wallObject.transform.SetParent(parent, false);
            var obstacleLayer = LayerMask.NameToLayer("MapObstacle");
            if (obstacleLayer >= 0)
            {
                wallObject.layer = obstacleLayer;
            }

            var mesh = WallBoxMesh.Build(meshName, vertices, triangles);
            wallObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var collider = wallObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            wallObject.AddComponent<MeshRenderer>().sharedMaterial = WallBoxMesh.SharedMaterial();
        }
    }
}
#endif
