using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes
{
    public static class ExploreAnchorRules
    {
        public readonly struct Anchor
        {
            public Anchor(string id, Vector3 position, float weight)
            {
                Id = id;
                Position = position;
                Weight = weight;
            }

            public string Id { get; }
            public Vector3 Position { get; }
            public float Weight { get; }
        }

        public static bool TrySelect(
            IReadOnlyList<Anchor> anchors,
            bool restrictToZone,
            Vector3 zoneCenter,
            float zoneRadius,
            out Anchor selected)
        {
            selected = default;
            if (anchors == null || anchors.Count == 0)
            {
                return false;
            }

            var found = false;
            var bestWeight = 0f;
            var best = default(Anchor);
            var radiusSquared = zoneRadius * zoneRadius;
            for (var index = 0; index < anchors.Count; index++)
            {
                var anchor = anchors[index];
                if (anchor.Weight <= 0f)
                {
                    continue;
                }

                if (restrictToZone &&
                    !IsInsideZone(anchor.Position, zoneCenter, radiusSquared))
                {
                    continue;
                }

                if (found && anchor.Weight <= bestWeight)
                {
                    continue;
                }

                best = anchor;
                bestWeight = anchor.Weight;
                found = true;
            }

            if (!found)
            {
                return false;
            }

            selected = best;
            return true;
        }

        public static bool TrySelectFlank(
            IReadOnlyList<Anchor> anchors,
            Vector3 fromPosition,
            string hotAnchorId,
            Vector3 hotAnchorPosition,
            bool restrictToZone,
            Vector3 zoneCenter,
            float zoneRadius,
            out Anchor selected)
        {
            selected = default;
            if (anchors == null)
            {
                return false;
            }

            var line = hotAnchorPosition - fromPosition;
            line.y = 0f;
            if (line.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            line.Normalize();
            var radiusSquared = zoneRadius * zoneRadius;
            var found = false;
            var bestLateral = 1f;
            var best = default(Anchor);
            for (var index = 0; index < anchors.Count; index++)
            {
                var anchor = anchors[index];
                if (anchor.Weight <= 0f || anchor.Id == hotAnchorId)
                {
                    continue;
                }

                if (restrictToZone &&
                    !IsInsideZone(anchor.Position, zoneCenter, radiusSquared))
                {
                    continue;
                }

                var offset = anchor.Position - fromPosition;
                offset.y = 0f;
                if (Vector3.Dot(offset, line) <= 0f)
                {
                    continue;
                }

                var lateral = Mathf.Abs(offset.x * line.z - offset.z * line.x);
                if (lateral <= 1f || (found && lateral <= bestLateral))
                {
                    continue;
                }

                best = anchor;
                bestLateral = lateral;
                found = true;
            }

            if (!found)
            {
                return false;
            }

            selected = best;
            return true;
        }

        public static bool TryChoose(
            IReadOnlyList<Anchor> anchors,
            Vector3 fromPosition,
            bool preferFlank,
            int hotClaimCount,
            bool restrictToZone,
            Vector3 zoneCenter,
            float zoneRadius,
            out Anchor chosen,
            out int slots)
        {
            chosen = default;
            slots = 2;
            if (!TrySelect(
                    anchors,
                    restrictToZone,
                    zoneCenter,
                    zoneRadius,
                    out var hot))
            {
                return false;
            }

            if (preferFlank &&
                TrySelectFlank(
                    anchors,
                    fromPosition,
                    hot.Id,
                    hot.Position,
                    restrictToZone,
                    zoneCenter,
                    zoneRadius,
                    out var flank))
            {
                chosen = flank;
                slots = 1;
                return true;
            }

            if (hotClaimCount >= 2)
            {
                return false;
            }

            chosen = hot;
            slots = 2;
            return true;
        }

        private static bool IsInsideZone(
            Vector3 position,
            Vector3 center,
            float radiusSquared)
        {
            var offset = position - center;
            offset.y = 0f;
            return offset.sqrMagnitude <= radiusSquared;
        }
    }
}
