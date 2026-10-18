#if UNITY_EDITOR
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class TreePointPlacement
    {
        public static Vector2 ToSource(
            Vector2 point,
            Vector2 sourceMin,
            Vector2 sourceMax,
            Vector2 upperLeft,
            Vector2 lowerRight)
        {
            return new Vector2(
                Mathf.LerpUnclamped(
                    upperLeft.x,
                    lowerRight.x,
                    InverseLerp(sourceMin.x, sourceMax.x, point.x)),
                Mathf.LerpUnclamped(
                    upperLeft.y,
                    lowerRight.y,
                    InverseLerp(sourceMax.y, sourceMin.y, point.y)));
        }

        static float InverseLerp(float from, float to, float value)
        {
            var span = to - from;
            if (Mathf.Abs(span) <= 0.0001f)
            {
                return 0f;
            }

            return (value - from) / span;
        }
    }
}
#endif
