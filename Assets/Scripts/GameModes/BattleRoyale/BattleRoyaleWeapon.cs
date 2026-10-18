using BattleRoyale.Events;
using BattleRoyale.GameModes;
using BattleRoyale.Weapons;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleWeapon : MonoBehaviour
    {
        // Authored speeds take about a second to cross weapon range. This
        // scale makes the tracer cross that distance before a return shot.
        // The round itself is not stopped at that distance.
        private const float ProjectileSpeedScale = 40f;

        private BattleRoyaleAgent owner;
        private WeaponProfile profile;
        private WeaponProfile runtimeProfile;
        private BattleRoyaleSightLines sightLines;
        private float nextFireTime;
        private float reloadReadyTime;
        private int roundsRemaining;

        public WeaponProfile Profile => profile;
        public BattleRoyaleSightLines SightLines => sightLines;
        public Vector3 MuzzlePosition => GetMuzzlePosition();
        public int RoundsRemaining => roundsRemaining;
        public bool IsReloading => Time.time < reloadReadyTime;
        public float Range => profile == null || profile.stats == null
            ? 0f
            : profile.stats.range;

        public void Initialize(
            BattleRoyaleAgent agent,
            WeaponProfile weaponProfile)
        {
            owner = agent;
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
            BindSightLines();
        }

        public bool TryFire(BattleRoyaleAgent target)
        {
            return TryFire(target, false);
        }

        public bool TryFire(
            BattleRoyaleAgent target,
            bool lineOfSightAlreadyConfirmed)
        {
            if (owner == null ||
                target == null ||
                !owner.IsAlive ||
                !target.IsAlive ||
                (owner.Match is IMatchCombatGate combatGate &&
                 !combatGate.CombatEnabled) ||
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
                                 Vector3.up * BattleRoyaleAgent.SeatedHeight(targetScale, 0.5f);
            var direction = targetPosition - origin;
            var distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return false;
            }

            direction /= distance;
            if (!lineOfSightAlreadyConfirmed &&
                !BattleRoyaleLineOfSight.IsClear(owner, target))
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
                if (SpawnProjectile(
                        origin,
                        ApplyAccuracy(direction, stats.spreadDegrees),
                        stats))
                {
                    owner.Match?.RecordFiredShot(owner);
                }
            }

            roundsRemaining = Mathf.Max(0, roundsRemaining - 1);
            if (roundsRemaining == 0)
            {
                reloadReadyTime = Time.time + ReloadDuration;
            }

            var ownerScale = owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            EventBus.Publish(new MuzzleFlashRequested(
                origin,
                0.45f * ownerScale));
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

        private bool SpawnProjectile(
            Vector3 origin,
            Vector3 direction,
            WeaponStats stats)
        {
            var pool = owner.Match == null
                ? null
                : owner.Match.Projectiles;
            if (pool == null)
            {
                return false;
            }

            var ownerScale = owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
            return pool.Spawn(
                owner,
                origin,
                direction,
                stats.damage * owner.WeaponDamageMultiplier,
                stats.projectileSpeed * ProjectileSpeedScale,
                float.PositiveInfinity,
                Mathf.Max(0.05f, stats.projectileScale) * ownerScale,
                owner.WeaponRange);
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

        private void LateUpdate()
        {
            if (sightLines == null)
            {
                return;
            }

            if (owner == null || !owner.IsAlive)
            {
                sightLines.HideFrom(0);
                return;
            }

            BattleRoyaleLineOfSight.TryGetDisplayedEnd(
                GetMuzzlePosition(),
                owner.transform.forward,
                owner.WeaponRange,
                owner.transform.position.y + BattleRoyaleLineOfSight.SampleHeight,
                out var end);
            sightLines.Show(0, GetMuzzlePosition(), end);
            sightLines.HideFrom(1);
        }

        private void BindSightLines()
        {
            var gun = transform.Find("GunCube");
            if (gun == null)
            {
                sightLines = null;
                return;
            }

            EnsureSightLine(gun);
            sightLines = gun.GetComponentInChildren<BattleRoyaleSightLines>(true);
        }

        private static void EnsureSightLine(Transform gun)
        {
            var existingLine = FindSightLine(gun);
            if (existingLine != null)
            {
                if (existingLine.GetComponent<BattleRoyaleSightLines>() == null)
                {
                    var view = existingLine.gameObject
                        .AddComponent<BattleRoyaleSightLines>();
                    view.Bind(existingLine);
                }

                BattleRoyaleSightLines.ApplyVisibleDefaults(existingLine);
                return;
            }

            var sightObject = new GameObject("SightLine");
            PlaceOnGun(sightObject.transform, gun);
            var line = sightObject.AddComponent<LineRenderer>();
            BattleRoyaleSightLines.ApplyVisibleDefaults(line);
            var created = sightObject.AddComponent<BattleRoyaleSightLines>();
            created.Bind(line);
        }

        private static LineRenderer FindSightLine(Transform gun)
        {
            var lines = gun.GetComponentsInChildren<LineRenderer>(true);
            foreach (var candidate in lines)
            {
                if (candidate.transform != gun)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void PlaceOnGun(Transform sight, Transform gun)
        {
            sight.SetParent(gun, false);
            sight.localPosition = Vector3.zero;
            sight.localRotation = Quaternion.identity;
            var scale = gun.localScale;
            sight.localScale = new Vector3(
                Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
                Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
                Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
        }

        private Vector3 GetMuzzlePosition()
        {
            var gun = transform.Find("GunCube");
            if (gun != null)
            {
                return gun.position + gun.forward * (gun.lossyScale.z * 0.5f);
            }

            return transform.position +
                   Vector3.up * BattleRoyaleAgent.SeatedHeight(BodyScale(), 0.75f);
        }

        private float BodyScale()
        {
            var body = transform.Find("AgentCapsule");
            if (body != null)
            {
                return Mathf.Max(0.1f, Mathf.Abs(body.localScale.y));
            }

            return owner == null || owner.Profile == null
                ? 1f
                : Mathf.Max(0.1f, owner.Profile.visualScale);
        }

        private void CreatePrimitiveVisual()
        {
            var scale = BodyScale();
            var existingGun = transform.Find("GunCube");
            if (existingGun != null)
            {
                var local = existingGun.localPosition;
                local.y = BattleRoyaleAgent.SeatedHeight(scale, 0.5f);
                existingGun.localPosition = local;
                return;
            }

            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "GunCube";
            gun.transform.SetParent(transform, false);
            var proportions = GetVisualProportions();
            var length = 1.6f * proportions.x * scale;
            var thickness = 0.25f * proportions.y * scale;
            gun.transform.localPosition = new Vector3(
                0.85f * scale,
                BattleRoyaleAgent.SeatedHeight(scale, 0.5f),
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
        }
    }
}
