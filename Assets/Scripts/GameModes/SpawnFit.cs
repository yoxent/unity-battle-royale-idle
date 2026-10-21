using UnityEngine;

namespace BattleRoyale.GameModes
{
    public static class SpawnFit
    {
        public const float DefaultBakedRadius = 0.5f;

        // The navmesh edge is already the baked agent radius away from the wall face.
        // Clearance is the rest of the body radius. Wall thickness and the full unit
        // scale are not added; adding both pushes a spawn through a narrow floor
        // into the opposite wall.
        public static float EdgeClearance(float bodyRadius, float bakedAgentRadius)
        {
            return Mathf.Max(0f, bodyRadius - Mathf.Max(0f, bakedAgentRadius));
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

        public static bool LiesBetween(Vector3 position, Vector3 nearEdge, Vector3 farEdge)
        {
            var toPosition = position - nearEdge;
            var toFar = farEdge - position;
            toPosition.y = 0f;
            toFar.y = 0f;
            return Vector3.Dot(toPosition, toFar) > 0f;
        }
    }
}
