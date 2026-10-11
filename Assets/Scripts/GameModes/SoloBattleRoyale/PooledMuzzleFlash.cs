using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public sealed class PooledMuzzleFlash : MonoBehaviour
    {
        private MuzzleFlashPool pool;
        private float hideAt;

        public void Bind(MuzzleFlashPool owner)
        {
            pool = owner;
        }

        public void Show(Vector3 position, float scale, float duration)
        {
            transform.position = position;
            transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            hideAt = Time.time + duration;
        }

        private void Update()
        {
            if (Time.time < hideAt || pool == null)
            {
                return;
            }

            pool.Release(this);
        }
    }
}
