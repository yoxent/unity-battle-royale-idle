using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes
{
    public static class SpawnSnapCandidates
    {
        public const int Directions = 8;

        public static void Fill(
            Vector3 anchor,
            Vector3 preferred,
            float step,
            List<Vector3> candidates)
        {
            if (candidates == null)
            {
                return;
            }

            candidates.Clear();
            if (!SamePoint(anchor, preferred))
            {
                candidates.Add(anchor);
            }

            var ringStep = Mathf.Max(0.01f, step);
            var turn = Mathf.PI * 2f / Directions;
            for (var ring = 1; ring <= SpawnPlacement.Rings; ring++)
            {
                var radius = ringStep * ring;
                for (var direction = 0; direction < Directions; direction++)
                {
                    var angle = turn * direction;
                    var point = anchor + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);
                    if (SamePoint(point, preferred))
                    {
                        continue;
                    }

                    candidates.Add(point);
                }
            }
        }

        static bool SamePoint(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude <= 0.01f;
        }
    }
}
