using BattleRoyale.GameModes.TeamDeathmatch;
using BattleRoyale.Weapons;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public sealed class BattleRoyaleProjectile : MonoBehaviour
    {
        private BattleRoyaleProjectilePool pool;
        private BattleRoyaleAgent owner;
        private Vector3 direction;
        private const float MissDistance = 4000f;

        private float damage;
        private float speed;
        private float lifetime;
        private float weaponRange;
        private float elapsed;
        private float blockerPlaneY;
        private Vector3 impactPoint;
        private bool inFlight;

        public void Bind(BattleRoyaleProjectilePool projectilePool)
        {
            pool = projectilePool;
        }

        public void Launch(
            BattleRoyaleAgent projectileOwner,
            Vector3 origin,
            Vector3 travelDirection,
            float projectileDamage,
            float projectileSpeed,
            float projectileLifetime,
            float visualScale,
            float effectiveRange)
        {
            owner = projectileOwner;
            direction = travelDirection.sqrMagnitude <= 0.001f
                ? Vector3.forward
                : travelDirection.normalized;
            damage = projectileDamage;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            weaponRange = Mathf.Max(0f, effectiveRange);
            elapsed = 0f;
            blockerPlaneY = origin.y;
            inFlight = true;
            transform.SetParent(null, true);
            transform.position = origin;
            transform.localScale = Vector3.one * Mathf.Max(0.2f, visualScale);
            ResolveImpact();
        }

        private void Update()
        {
            if (!inFlight)
            {
                return;
            }

            var step = Mathf.Min(Time.deltaTime, Mathf.Max(0f, lifetime - elapsed));
            elapsed += step;
            var toImpact = impactPoint - transform.position;
            var remaining = toImpact.magnitude;
            var travel = speed * step;
            if (step <= 0f || remaining <= travel)
            {
                transform.position = impactPoint;
                Release();
                return;
            }

            transform.position += toImpact / remaining * travel;
        }

        private void ResolveImpact()
        {
            var origin = transform.position;
            var travel = direction * MissDistance;
            impactPoint = origin + travel;
            if (travel.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var wallBlocks = BattleRoyaleLineOfSight.TryGetObstacleHit(
                origin,
                impactPoint,
                blockerPlaneY,
                out var wallHit);
            var wallDistance = wallBlocks
                ? HorizontalDistance(origin, wallHit.point)
                : float.PositiveInfinity;
            var hits = Physics.RaycastAll(
                origin,
                direction,
                Mathf.Infinity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(
                hits,
                (first, second) => first.distance.CompareTo(second.distance));
            foreach (var hit in hits)
            {
                if (HorizontalDistance(origin, hit.point) >= wallDistance)
                {
                    break;
                }

                var agent =
                    hit.collider.GetComponentInParent<BattleRoyaleAgent>();
                if (agent == owner ||
                    (agent != null &&
                     TeamDeathmatchRules.SharesTeam(
                         owner == null ? -1 : owner.TeamIndex,
                         agent.TeamIndex)))
                {
                    continue;
                }

                if (agent != null)
                {
                    agent.ApplyDamage(
                        WeaponRangeFalloff.Scale(
                            damage,
                            hit.distance,
                            weaponRange),
                        owner);
                }

                impactPoint = hit.point;
                return;
            }

            if (!wallBlocks)
            {
                return;
            }

            impactPoint.x = wallHit.point.x;
            impactPoint.z = wallHit.point.z;
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
