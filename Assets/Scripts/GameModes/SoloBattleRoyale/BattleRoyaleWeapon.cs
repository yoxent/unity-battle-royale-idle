using BattleRoyale.Weapons;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleWeapon : MonoBehaviour
    {
        // Authored speeds take about a second to cross weapon range, so both
        // fighters can shoot before either round arrives. This scale makes
        // the shot connect before a return shot. Lifetime shrinks by the
        // same amount, so the round still stops at its authored distance.
        private const float ProjectileSpeedScale = 40f;

        private BattleRoyaleCharacter owner;
        private WeaponProfile profile;
        private WeaponProfile runtimeProfile;
        private Renderer muzzleFlash;
        private Material muzzleFlashMaterial;
        private float nextFireTime;
        private float muzzleFlashUntil;
        private float reloadReadyTime;
        private int roundsRemaining;

        public WeaponProfile Profile => profile;
        public int RoundsRemaining => roundsRemaining;
        public bool IsReloading => Time.time < reloadReadyTime;
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

            roundsRemaining = MagazineCapacity;
            reloadReadyTime = 0f;
            CreatePrimitiveVisual();
            EnsureMuzzleFlash();
        }

        private void Update()
        {
            if (muzzleFlash == null || !muzzleFlash.enabled)
            {
                return;
            }

            if (Time.time >= muzzleFlashUntil)
            {
                muzzleFlash.enabled = false;
            }
        }

        public bool TryFire(BattleRoyaleCharacter target)
        {
            if (owner == null ||
                target == null ||
                !owner.IsAlive ||
                !target.IsAlive ||
                profile == null ||
                profile.stats == null)
            {
                return false;
            }

            if (Time.time < reloadReadyTime)
            {
                return false;
            }

            if (roundsRemaining <= 0)
            {
                roundsRemaining = MagazineCapacity;
            }

            if (Time.time < nextFireTime)
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
            if (!BattleRoyaleLineOfSight.IsClear(owner, target))
            {
                return false;
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

            roundsRemaining = Mathf.Max(0, roundsRemaining - 1);
            if (roundsRemaining == 0)
            {
                reloadReadyTime = Time.time + ReloadDuration;
            }

            ShowMuzzleFlash(origin);
            return true;
        }

        private int MagazineCapacity =>
            profile == null || profile.stats == null
                ? 1
                : Mathf.Max(1, profile.stats.magazineSize);

        private float ReloadDuration =>
            profile == null || profile.stats == null
                ? 2.5f
                : Mathf.Max(0.05f, profile.stats.reloadDuration);

        private void SpawnProjectile(
            Vector3 origin,
            Vector3 direction,
            WeaponStats stats)
        {
            var pool = owner.Match == null
                ? null
                : owner.Match.Projectiles;
            if (pool == null)
            {
                return;
            }

            var ownerScale = owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            pool.Spawn(
                owner,
                origin,
                direction,
                stats.damage * owner.WeaponDamageMultiplier,
                stats.projectileSpeed * ProjectileSpeedScale,
                stats.projectileLifetime / ProjectileSpeedScale,
                Mathf.Max(0.05f, stats.projectileScale) * ownerScale);
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
            var gun = transform.Find("GunCube");
            if (gun != null)
            {
                return gun.position + gun.forward * (gun.lossyScale.z * 0.5f);
            }

            var scale = owner == null || owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            return transform.position + Vector3.up * (0.75f * scale);
        }

        private void EnsureMuzzleFlash()
        {
            if (muzzleFlash != null)
            {
                return;
            }

            var flashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashObject.name = "MuzzleFlash";
            flashObject.transform.SetParent(transform, false);
            var collider = flashObject.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            muzzleFlash = flashObject.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader != null && muzzleFlash != null)
            {
                muzzleFlashMaterial = new Material(shader);
                var flashColor = new Color(1f, 0.95f, 0.6f, 1f);
                muzzleFlashMaterial.color = flashColor;
                if (muzzleFlashMaterial.HasProperty("_BaseColor"))
                {
                    muzzleFlashMaterial.SetColor("_BaseColor", flashColor);
                }

                muzzleFlash.material = muzzleFlashMaterial;
            }

            var scale = owner == null || owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            flashObject.transform.localScale = Vector3.one * (0.45f * scale);
            muzzleFlash.enabled = false;
        }

        private void ShowMuzzleFlash(Vector3 position)
        {
            EnsureMuzzleFlash();
            if (muzzleFlash == null)
            {
                return;
            }

            muzzleFlash.transform.position = position;
            muzzleFlash.enabled = true;
            muzzleFlashUntil = Time.time + 0.08f;
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
            var proportions = GetVisualProportions();
            var length = 1.6f * proportions.x * scale;
            var thickness = 0.25f * proportions.y * scale;
            gun.transform.localPosition = new Vector3(
                0.85f * scale,
                0.5f * scale,
                length * 0.5f);
            gun.transform.localScale = new Vector3(
                thickness,
                thickness,
                length);
            var collider = gun.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
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

            if (muzzleFlashMaterial != null)
            {
                Destroy(muzzleFlashMaterial);
            }
        }
    }
}
