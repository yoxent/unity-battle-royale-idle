using BattleRoyale.Weapons;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleWeapon : MonoBehaviour
    {
        private BattleRoyaleCharacter owner;
        private WeaponProfile profile;
        private WeaponProfile runtimeProfile;
        private float nextFireTime;

        public WeaponProfile Profile => profile;
        public float Range => profile == null || profile.stats == null
            ? 0f
            : profile.stats.range;

        public void Initialize(
            BattleRoyaleCharacter character,
            WeaponProfile weaponProfile)
        {
            owner = character;
            profile = weaponProfile;
            if (profile == null)
            {
                runtimeProfile =
                    ScriptableObject.CreateInstance<WeaponProfile>();
                runtimeProfile.name = "Runtime Prototype Rifle";
                runtimeProfile.hideFlags = HideFlags.DontSave;
                profile = runtimeProfile;
            }

            CreatePrimitiveVisual();
        }

        public bool TryFire(BattleRoyaleCharacter target)
        {
            if (owner == null ||
                target == null ||
                !owner.IsAlive ||
                !target.IsAlive ||
                profile == null ||
                profile.stats == null ||
                Time.time < nextFireTime)
            {
                return false;
            }

            var stats = profile.stats;
            var origin = GetMuzzlePosition();
            var targetScale = target.Profile == null
                ? 1f
                : Mathf.Max(0.1f, target.Profile.visualScale);
            var targetPosition = target.transform.position +
                                 Vector3.up * (targetScale * 0.5f);
            var direction = targetPosition - origin;
            var distance = direction.magnitude;
            if (distance <= 0.01f ||
                distance > owner.WeaponRange)
            {
                return false;
            }

            direction /= distance;
            var visibilityHits = Physics.RaycastAll(
                origin,
                direction,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(
                visibilityHits,
                (first, second) => first.distance.CompareTo(second.distance));
            foreach (var visibilityHit in visibilityHits)
            {
                var hitCharacter =
                    visibilityHit.collider.GetComponentInParent<
                        BattleRoyaleCharacter>();
                if (hitCharacter == owner)
                {
                    continue;
                }

                if (hitCharacter != target)
                {
                    return false;
                }

                break;
            }

            nextFireTime = Time.time + 1f /
                           Mathf.Max(
                               0.01f,
                               stats.fireRate *
                               owner.WeaponFireRateMultiplier);
            var pelletCount = Mathf.Clamp(stats.pelletCount, 1, 12);
            for (var pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
                SpawnProjectile(
                    origin,
                    ApplyAccuracy(direction, stats.spreadDegrees),
                    stats);
            }

            return true;
        }

        private void SpawnProjectile(
            Vector3 origin,
            Vector3 direction,
            WeaponStats stats)
        {
            var bulletObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObject.name = $"Bullet_{owner.name}";
            bulletObject.transform.position = origin;
            bulletObject.transform.localScale = Vector3.one *
                                                Mathf.Max(
                                                    0.05f,
                                                    stats.projectileScale);
            var bulletCollider = bulletObject.GetComponent<Collider>();
            if (bulletCollider != null)
            {
                Object.Destroy(bulletCollider);
            }

            var projectile = bulletObject.AddComponent<BattleRoyaleProjectile>();
            projectile.Initialize(
                owner,
                direction,
                stats.damage * owner.WeaponDamageMultiplier,
                stats.projectileSpeed,
                stats.projectileLifetime);
        }

        private Vector3 ApplyAccuracy(
            Vector3 direction,
            float weaponSpreadDegrees)
        {
            var accuracy = owner == null
                ? 1f
                : owner.WeaponAccuracy;
            var spreadDegrees = weaponSpreadDegrees *
                                Mathf.Lerp(1.5f, 0.25f, accuracy);
            if (spreadDegrees <= 0f)
            {
                return direction;
            }

            var spread = Random.insideUnitCircle * spreadDegrees;
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            if (right.sqrMagnitude <= 0.001f)
            {
                right = Vector3.right;
            }

            var spreadDirection = Quaternion.AngleAxis(
                spread.x,
                Vector3.up) * direction;
            spreadDirection = Quaternion.AngleAxis(
                spread.y,
                right) * spreadDirection;
            return spreadDirection.normalized;
        }

        private Vector3 GetMuzzlePosition()
        {
            var scale = owner == null || owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            return transform.position + Vector3.up * (0.75f * scale);
        }

        private void CreatePrimitiveVisual()
        {
            if (transform.Find("GunCube") != null)
            {
                return;
            }

            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "GunCube";
            gun.transform.SetParent(transform, false);
            var scale = owner == null || owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            gun.transform.localPosition = new Vector3(
                0.85f * scale,
                0.5f * scale,
                0f);
            var proportions = GetVisualProportions();
            gun.transform.localScale = new Vector3(
                1.6f * proportions.x * scale,
                0.25f * proportions.y * scale,
                0.25f * proportions.y * scale);
            var collider = gun.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = gun.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader != null)
            {
                renderer.material = new Material(shader)
                {
                    color = profile == null
                        ? new Color(0.12f, 0.12f, 0.12f)
                        : profile.visualColor
                };
            }
        }

        private Vector2 GetVisualProportions()
        {
            var category = profile == null
                ? WeaponCategory.AssaultRifle
                : profile.category;
            return category switch
            {
                WeaponCategory.Handgun => new Vector2(0.55f, 0.7f),
                WeaponCategory.SubmachineGun =>
                    new Vector2(0.8f, 0.75f),
                WeaponCategory.Shotgun => new Vector2(1.15f, 1.25f),
                WeaponCategory.LightMachineGun =>
                    new Vector2(1.45f, 1.35f),
                WeaponCategory.MarksmanRifle =>
                    new Vector2(1.35f, 0.85f),
                WeaponCategory.SniperRifle =>
                    new Vector2(1.7f, 0.8f),
                _ => Vector2.one
            };
        }

        private void OnDestroy()
        {
            if (runtimeProfile != null)
            {
                Destroy(runtimeProfile);
            }
        }
    }
}
