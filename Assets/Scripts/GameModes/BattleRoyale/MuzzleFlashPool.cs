using System.Collections.Generic;
using BattleRoyale.Events;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class MuzzleFlashPool : MonoBehaviour
    {
        public const float FlashDuration = 0.08f;

        [Min(1)]
        [SerializeField] private int initialCapacity = 16;

        [SerializeField] private PooledMuzzleFlash muzzleFlashPrefab;

        private readonly Stack<PooledMuzzleFlash> available = new();
        private bool prewarmed;
        private bool reportedMissingPrefab;

        public static MuzzleFlashPool Ensure(Transform parent)
        {
            var pools = Object.FindObjectsByType<MuzzleFlashPool>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var pool in pools)
            {
                if ((pool.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    continue;
                }

                pool.Prewarm();
                return pool;
            }

            var host = new GameObject("Muzzle Flashes");
            if (parent != null)
            {
                host.transform.SetParent(parent, false);
            }

            var created = host.AddComponent<MuzzleFlashPool>();
            created.Prewarm();
            return created;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MuzzleFlashRequested>(ShowRequestedFlash);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MuzzleFlashRequested>(ShowRequestedFlash);
        }

        public void Prewarm()
        {
            if (prewarmed)
            {
                return;
            }

            if (!ResolvePrefab())
            {
                return;
            }

            for (var index = 0; index < initialCapacity; index++)
            {
                available.Push(CreateFlash());
            }

            prewarmed = true;
        }

        public void Show(Vector3 position, float scale)
        {
            if (!prewarmed)
            {
                Prewarm();
            }

            if (muzzleFlashPrefab == null)
            {
                return;
            }

            var flash = available.Count > 0
                ? available.Pop()
                : CreateFlash();
            flash.gameObject.SetActive(true);
            flash.Show(position, scale, FlashDuration);
        }

        public void Release(PooledMuzzleFlash flash)
        {
            if (flash == null)
            {
                return;
            }

            flash.transform.SetParent(transform, false);
            flash.gameObject.SetActive(false);
            available.Push(flash);
        }

        private void ShowRequestedFlash(MuzzleFlashRequested request)
        {
            Show(request.Position, request.Scale);
        }

        private bool ResolvePrefab()
        {
            if (muzzleFlashPrefab != null)
            {
                return true;
            }

            var catalog = CombatVfxCatalog.Load();
            muzzleFlashPrefab = catalog == null ? null : catalog.muzzleFlash;
            if (muzzleFlashPrefab != null)
            {
                return true;
            }

            if (reportedMissingPrefab)
            {
                return false;
            }

            reportedMissingPrefab = true;
            Debug.LogError(
                "Combat VFX is missing the muzzle flash prefab.",
                this);
            return false;
        }

        private PooledMuzzleFlash CreateFlash()
        {
            var flash = Instantiate(muzzleFlashPrefab, transform);
            flash.Bind(this);
            flash.gameObject.SetActive(false);
            return flash;
        }
    }
}
