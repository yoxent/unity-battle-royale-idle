using UnityEngine;

namespace BattleRoyale.CameraSystems
{
    public static class SimulationCameraFraming
    {
        public static float OrthographicSize(
            float worldWidth,
            float worldDepth,
            float aspect,
            float paddingPercent,
            float minimum,
            float maximum)
        {
            var safeAspect = Mathf.Max(0.01f, aspect);
            var requiredVerticalSize = Mathf.Max(worldDepth, worldWidth / safeAspect);
            var paddingMultiplier = 1f + Mathf.Max(0f, paddingPercent) / 100f;
            var targetSize = requiredVerticalSize * 0.5f * paddingMultiplier;
            return Mathf.Clamp(targetSize, minimum, Mathf.Max(minimum, maximum));
        }

        public static float CameraHeight(
            float baseHeight,
            float heightScale,
            float terrainHeightOffset)
        {
            return baseHeight + Mathf.Max(0f, heightScale) +
                   Mathf.Max(0f, terrainHeightOffset);
        }

        public static Vector3 FramePosition(Vector3 worldCenter, float cameraHeight)
        {
            return new Vector3(worldCenter.x, cameraHeight, worldCenter.z);
        }
    }
}
