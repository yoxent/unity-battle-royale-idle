using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRoyale.Maps
{
    public static class AuthoringMarkerPlayMode
    {
        static bool sceneLoadHooked;

        public static void Hide(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var markers = root.GetComponentsInChildren<MapModePointMarker>(true);
            for (var i = 0; i < markers.Length; i++)
            {
                markers[i].gameObject.SetActive(false);
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            HideTransforms(transforms);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HideMarkersInLoadedScenes()
        {
            HideLoadedObjects();
            if (sceneLoadHooked)
            {
                return;
            }

            sceneLoadHooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HideLoadedObjects();
        }

        static void HideLoadedObjects()
        {
            var markers = Object.FindObjectsByType<MapModePointMarker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < markers.Length; i++)
            {
                markers[i].gameObject.SetActive(false);
            }

            var transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            HideTransforms(transforms);
        }

        static void HideTransforms(Transform[] transforms)
        {
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform.name is "ImageCornerUpperLeft" or "ImageCornerLowerRight")
                {
                    transform.gameObject.SetActive(false);
                    continue;
                }

                if (transform.name is not ("DungeonWalls" or "DungeonWallMesh" or "BoundaryWalls"))
                {
                    continue;
                }

                var renderers = transform.GetComponentsInChildren<MeshRenderer>(true);
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    renderers[rendererIndex].enabled = false;
                }
            }
        }
    }
}
