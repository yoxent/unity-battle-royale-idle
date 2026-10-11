using Unity.AI.Navigation;
using UnityEngine;

namespace BattleRoyale.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshSurface))]
    public sealed class MapNavigationController : MonoBehaviour
    {
        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private bool rebuildOnAwake = true;

        public NavMeshSurface Surface => surface;
        public bool HasNavigationData =>
            surface != null && surface.navMeshData != null;

        private void Reset()
        {
            surface = GetComponent<NavMeshSurface>();
        }

        private void Awake()
        {
            CacheSurface();
            if (rebuildOnAwake)
            {
                Rebuild();
            }
        }

        public void Rebuild()
        {
            if (!CacheSurface())
            {
                return;
            }

            surface.BuildNavMesh();
        }

        private bool CacheSurface()
        {
            if (surface == null)
            {
                surface = GetComponent<NavMeshSurface>();
            }

            return surface != null;
        }
    }
}
