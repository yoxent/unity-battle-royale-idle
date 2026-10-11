using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public sealed class BattleRoyaleProjectile : MonoBehaviour
    {
        private BattleRoyaleProjectilePool pool;
        private BattleRoyaleCharacter owner;
        private Vector3 direction;
        private float damage;
        private float speed;
        private float lifetime;
        private float elapsed;
        private float blockerPlaneY;
        private bool inFlight;

        public void Bind(BattleRoyaleProjectilePool projectilePool)
        {
            pool = projectilePool;
        }

        public void Launch(
            BattleRoyaleCharacter projectileOwner,
            Vector3 origin,
            Vector3 travelDirection,
            float projectileDamage,
            float projectileSpeed,
            float projectileLifetime,
            float visualScale)
        {
            owner = projectileOwner;
            direction = travelDirection.sqrMagnitude <= 0.001f
                ? Vector3.forward
                : travelDirection.normalized;
            damage = projectileDamage;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            elapsed = 0f;
            blockerPlaneY = projectileOwner == null
                ? origin.y
                : projectileOwner.transform.position.y +
                  BattleRoyaleLineOfSight.SampleHeight;
            inFlight = true;
            transform.SetParent(null, true);
            transform.position = origin;
            transform.localScale = Vector3.one * Mathf.Max(0.2f, visualScale);
        }

        private void Update()
        {
            if (!inFlight)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Release();
                return;
            }

            var previousPosition = transform.position;
            var nextPosition = previousPosition + direction * speed * Time.deltaTime;
            var travel = nextPosition - previousPosition;
            if (travel.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var wallBlocks = BattleRoyaleLineOfSight.TryGetObstacleHit(
                previousPosition,
                nextPosition,
                blockerPlaneY,
                out var wallHit);
            var wallDistance = wallBlocks
                ? HorizontalDistance(previousPosition, wallHit.point)
                : float.PositiveInfinity;
            var hits = Physics.RaycastAll(
                previousPosition,
                travel.normalized,
                travel.magnitude,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(
                hits,
                (first, second) => first.distance.CompareTo(second.distance));
            foreach (var hit in hits)
            {
                if (HorizontalDistance(previousPosition, hit.point) >= wallDistance)
                {
                    break;
                }

                var character =
                    hit.collider.GetComponentInParent<BattleRoyaleCharacter>();
                if (character == owner)
                {
                    continue;
                }

                if (character != null)
                {
                    character.ApplyDamage(damage, owner);
                }

                Release();
                return;
            }

            if (wallBlocks)
            {
                var stop = nextPosition;
                stop.x = wallHit.point.x;
                stop.z = wallHit.point.z;
                transform.position = stop;
                Release();
                return;
            }

            transform.position = nextPosition;
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private void Release()
        {
            if (!inFlight)
            {
                return;
            }

            inFlight = false;
            if (pool != null)
            {
                pool.Release(this);
            }
        }
    }
}
