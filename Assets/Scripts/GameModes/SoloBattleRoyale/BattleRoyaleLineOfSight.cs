using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public static class BattleRoyaleLineOfSight
    {
        public const float SampleHeight = 1.5f;

        private static int sightMask = -1;
        private static int obstacleMask = -1;

        public static bool IsClear(
            BattleRoyaleAgent from,
            BattleRoyaleAgent to)
        {
            return Evaluate(from, to, float.PositiveInfinity, out _, out _);
        }

        public static bool Evaluate(
            BattleRoyaleAgent from,
            BattleRoyaleAgent to,
            float maxDistance,
            out Vector3 origin,
            out Vector3 end)
        {
            origin = Vector3.zero;
            end = Vector3.zero;
            if (from == null || to == null)
            {
                return false;
            }

            origin = from.transform.position;
            origin.y += SampleHeight;
            end = to.transform.position;
            end.y = origin.y;
            var offset = end - origin;
            var targetDistance = offset.magnitude;
            var reach = Mathf.Max(0f, maxDistance);
            var withinReach = targetDistance <= reach;
            if (targetDistance <= 0.05f)
            {
                return withinReach;
            }

            var distance = withinReach ? targetDistance : reach;
            if (!withinReach)
            {
                end = origin + offset / targetDistance * distance;
            }

            if (distance <= 0.05f)
            {
                return false;
            }

            var hits = Physics.RaycastAll(
                origin,
                offset / distance,
                distance,
                SightMask,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(
                hits,
                (first, second) => first.distance.CompareTo(second.distance));
            foreach (var hit in hits)
            {
                var agent =
                    hit.collider.GetComponentInParent<BattleRoyaleAgent>();
                if (agent == from)
                {
                    continue;
                }

                end = hit.point;
                return agent == to;
            }

            return withinReach;
        }

        public static Vector3 EyePosition(BattleRoyaleAgent from)
        {
            if (from != null && from.Weapon != null)
            {
                return from.Weapon.MuzzlePosition;
            }

            var origin = from == null ? Vector3.zero : from.transform.position;
            origin.y += SampleHeight;
            return origin;
        }

        public static float ClipDistance(
            float range,
            bool blocked,
            float blockDistance)
        {
            var reach = Mathf.Max(0f, range);
            if (!blocked)
            {
                return reach;
            }

            return Mathf.Clamp(blockDistance, 0f, reach);
        }

        public static bool TryGetDisplayedEnd(
            Vector3 start,
            Vector3 forward,
            float range,
            float planeY,
            out Vector3 end)
        {
            end = start;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            forward.Normalize();
            var reach = Mathf.Max(0f, range);
            var probeStart = start;
            probeStart.y = planeY;
            var blocked = TryGetObstacleHit(
                probeStart,
                probeStart + forward * reach,
                planeY,
                out var hit);
            var blockDistance = reach;
            if (blocked)
            {
                var hitPoint = hit.point;
                hitPoint.y = probeStart.y;
                blockDistance = Vector3.Distance(probeStart, hitPoint);
            }

            var distance = ClipDistance(reach, blocked, blockDistance);
            end = start + forward * distance;
            return blocked;
        }

        public static bool TryGetObstacleHit(
            Vector3 from,
            Vector3 to,
            float planeY,
            out RaycastHit hit)
        {
            var origin = from;
            origin.y = planeY;
            var destination = to;
            destination.y = planeY;
            var offset = destination - origin;
            var distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                hit = default;
                return false;
            }

            return Physics.Raycast(
                origin,
                offset / distance,
                out hit,
                distance,
                ObstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private static int SightMask
        {
            get
            {
                if (sightMask >= 0)
                {
                    return sightMask;
                }

                sightMask = Physics.DefaultRaycastLayers;
                var ground = LayerMask.NameToLayer("MapGround");
                if (ground >= 0)
                {
                    sightMask &= ~(1 << ground);
                }

                return sightMask;
            }
        }

        private static int ObstacleMask
        {
            get
            {
                if (obstacleMask >= 0)
                {
                    return obstacleMask;
                }

                var obstacle = LayerMask.NameToLayer("MapObstacle");
                obstacleMask = obstacle < 0
                    ? Physics.DefaultRaycastLayers
                    : 1 << obstacle;
                return obstacleMask;
            }
        }
    }
}
