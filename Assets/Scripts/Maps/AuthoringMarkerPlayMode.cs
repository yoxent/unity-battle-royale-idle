using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRoyale.Maps
{
    static class AuthoringMarkerPlayMode
    {
        static bool sceneLoadHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HideMarkersInLoadedScenes()
        {
            HideMarkers();
            if (sceneLoadHooked)
            {
                return;
            }

            sceneLoadHooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HideMarkers();
        }

        static void HideMarkers()
        {
            var markers = Object.FindObjectsByType<MapModePointMarker>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (var marker in markers)
            {
                marker.gameObject.SetActive(false);
            }

            var transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (var transform in transforms)
            {
                if (transform.name is "ImageCornerUpperLeft" or "ImageCornerLowerRight")
                {
                    transform.gameObject.SetActive(false);
                    continue;
                }

                if (transform.name != "DungeonWalls")
                {
                    continue;
                }

                var renderers = transform.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }
        }
    }
}
