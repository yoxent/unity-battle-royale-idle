using UnityEngine;

namespace BattleRoyale.GameModes
{
    public static class SpawnPlacement
    {
        public const float CapsuleRadius = 0.5f;
        public const float BakedAgentRadius = 0.5f;
        public const int Rings = 3;

        // Body radius past the baked navmesh edge. Wall thickness and the full
        // unit scale are not added. Unit scale 1 matches the bake, so the
        // clearance is zero and the authored point stays put.
        public static float Clearance(float unitScale, float visualScale)
        {
            var scale = unitScale < 0.01f ? 0.01f : unitScale;
            var visual = visualScale < 0.1f ? 0.1f : visualScale;
            return Mathf.Max(0f, CapsuleRadius * visual * scale - BakedAgentRadius);
        }

        public static float ClearanceFromBody(float bodyRadius, float bakedRadius)
        {
            return Mathf.Max(0f, bodyRadius - Mathf.Max(0f, bakedRadius));
        }

        public static float Step(float clearance)
        {
            return Mathf.Max(BakedAgentRadius, clearance / Rings);
        }

        public static Vector3 MoveInside(
            Vector3 position,
            Vector3 edgePoint,
            float edgeDistance,
            float requiredClearance)
        {
            if (edgeDistance >= requiredClearance)
            {
                return position;
            }

            var away = position - edgePoint;
            away.y = 0f;
            if (away.sqrMagnitude <= 0.0001f)
            {
                return position;
            }

            var deficit = requiredClearance - Mathf.Max(0f, edgeDistance);
            var moved = position + away.normalized * deficit;
            moved.y = position.y;
            return moved;
        }

        public static Vector3 ProbeAcross(
            Vector3 position,
            Vector3 edgePoint,
            float edgeDistance,
            float requiredClearance)
        {
            var away = position - edgePoint;
            away.y = 0f;
            if (away.sqrMagnitude <= 0.0001f)
            {
                away = Vector3.forward;
            }
            else
            {
                away.Normalize();
            }

            var probed = position + away * (Mathf.Max(0f, edgeDistance) + requiredClearance);
            probed.y = position.y;
            return probed;
        }

        public static bool LiesBetween(Vector3 position, Vector3 nearEdge, Vector3 farEdge)
        {
            var toPosition = position - nearEdge;
            var toFar = farEdge - position;
            toPosition.y = 0f;
            toFar.y = 0f;
            return Vector3.Dot(toPosition, toFar) > 0f;
        }

        public static Vector3 FitBetweenEdges(
            Vector3 nearEdge,
            Vector3 farEdge,
            float requiredClearance,
            float height)
        {
            var delta = farEdge - nearEdge;
            delta.y = 0f;
            var span = delta.magnitude;
            if (span <= 0.001f)
            {
                return new Vector3(nearEdge.x, height, nearEdge.z);
            }

            var inset = span <= requiredClearance * 2f ? span * 0.5f : requiredClearance;
            var fitted = nearEdge + delta / span * inset;
            fitted.y = height;
            return fitted;
        }
    }
}
