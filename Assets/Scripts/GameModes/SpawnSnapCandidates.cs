using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes
{
    public static class SpawnSnapCandidates
    {
        public const int Directions = 8;
        public const int Rings = 3;

        public static void Fill(
            Vector3 anchor,
            Vector3 preferred,
            float snapDistance,
            List<Vector3> candidates)
        {
            if (candidates == null)
            {
                return;
            }

            candidates.Clear();
            if ((anchor - preferred).sqrMagnitude > 0.01f)
            {
                candidates.Add(anchor);
            }

            var step = Mathf.Max(0.1f, snapDistance) * 0.5f;
            var turn = Mathf.PI * 2f / Directions;
            for (var ring = 1; ring <= Rings; ring++)
            {
                var radius = step * ring;
                for (var direction = 0; direction < Directions; direction++)
                {
                    var angle = turn * direction;
                    candidates.Add(anchor + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius));
                }
            }
        }
    }
}
