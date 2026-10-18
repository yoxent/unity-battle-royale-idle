#if UNITY_EDITOR
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class ImageCornerFit
    {
        public static bool IsValid(Vector2 upperLeft, Vector2 lowerRight)
        {
            return lowerRight.x > upperLeft.x + 0.001f &&
                   upperLeft.y > lowerRight.y + 0.001f;
        }

        public static Vector3 ToLocal(
            Vector2 imageUv,
            Vector2 upperLeftUv,
            Vector2 lowerRightUv,
            Vector3 floorUpperLeft,
            Vector3 floorLowerRight)
        {
            return new Vector3(
                Mathf.LerpUnclamped(
                    floorUpperLeft.x,
                    floorLowerRight.x,
                    InverseLerp(
                        upperLeftUv.x,
                        lowerRightUv.x,
                        imageUv.x)),
                0f,
                Mathf.LerpUnclamped(
                    floorUpperLeft.z,
                    floorLowerRight.z,
                    InverseLerp(
                        upperLeftUv.y,
                        lowerRightUv.y,
                        imageUv.y)));
        }

        public static Vector2 ToImageUv(
            Vector3 local,
            Vector2 upperLeftUv,
            Vector2 lowerRightUv,
            Vector3 floorUpperLeft,
            Vector3 floorLowerRight)
        {
            return new Vector2(
                Mathf.LerpUnclamped(
                    upperLeftUv.x,
                    lowerRightUv.x,
                    InverseLerp(floorUpperLeft.x, floorLowerRight.x, local.x)),
                Mathf.LerpUnclamped(
                    upperLeftUv.y,
                    lowerRightUv.y,
                    InverseLerp(floorUpperLeft.z, floorLowerRight.z, local.z)));
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
