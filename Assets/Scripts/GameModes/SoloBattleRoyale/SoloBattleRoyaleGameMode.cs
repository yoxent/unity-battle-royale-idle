using System.Collections.Generic;
using BattleRoyale.GameModes;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class SoloBattleRoyaleGameMode : SimulationMatch
    {
        [Header("Characters")]
        [Tooltip(
            "Set from the loaded mode configuration: team count times players per team, " +
            "excluding the test player when that slot is enabled.")]
        [Min(0)]
        [SerializeField] private int botCount = 8;

        [Header("Safe Zone")]
        [Min(0f)]
        [SerializeField] private float safeZoneStartDelay = 15f;
        [Min(0.1f)]
        [SerializeField] private float safeZoneShrinkDuration = 180f;
        [Range(0.05f, 1f)]
        [SerializeField] private float safeZoneStartRadiusPercent = 0.95f;
        [Range(0.01f, 1f)]
        [SerializeField] private float safeZoneFinalRadiusPercent = 0.15f;
        [Min(0f)]
        [SerializeField] private float outsideSafeZoneDamagePerSecond = 5f;

        [Header("Runtime")]
        [SerializeField] private bool drawSafeZone = true;
        [SerializeField] private bool drawLineOfSight = true;

        private float elapsedMatchTime;
        private Vector3 safeZoneCenter;
        private float safeZoneStartRadius;
        private float safeZoneRadius;
        private float safeZoneFinalRadius;
        private readonly List<LineRenderer> sightLines = new();
        private Material sightLineMaterial;
        private MaterialPropertyBlock sightLineBlock;
        private bool winnerAnnounced;

        protected override string CharacterRootName => "BattleRoyaleCharacters";
        protected override string ProjectileRootName => "BattleRoyaleProjectiles";
        protected override string RuntimeProfileName => "Runtime Battle Royale Profile";

        public BattleRoyaleCharacter Winner { get; private set; }
        public int AliveCount
        {
            get
            {
                RefreshRoster();
                var count = 0;
                foreach (var character in Roster)
                {
                    if (character != null && character.IsAlive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public Vector3 SafeZoneCenter => safeZoneCenter;
        public float SafeZoneRadius => safeZoneRadius;

        private void Update()
        {
            if (!MatchStarted)
            {
                return;
            }

            RefreshRoster();
            elapsedMatchTime += Time.deltaTime;
            UpdateSafeZone();
            ApplySafeZoneDamage();
            CheckForWinner();
        }

        private void LateUpdate()
        {
            UpdateSightLines();
        }

        public override bool BeginMatch()
        {
            if (MatchStarted)
            {
                return false;
            }

            if (!ResolveMapAndConfiguration())
            {
                return false;
            }

            ApplyLoadedModeConfiguration();
            PrepareSafeZone();
            EnsureSimulation();

            var spawnCount = botCount + (SpawnTestPlayer ? 1 : 0);
            for (var index = 0; index < spawnCount; index++)
            {
                var isPlayer = SpawnTestPlayer && index == 0;
                var strategy = isPlayer
                    ? FallbackStrategy
                    : GetStrategy(index - (SpawnTestPlayer ? 1 : 0));
                var spawnPosition = GetSpawnPosition(index);
                SpawnCharacter(
                    $"BR_{index + 1:00}_{strategy}",
                    spawnPosition,
                    strategy,
                    !isPlayer);
            }

            if (Roster.Count == 0)
            {
                Debug.LogError("The solo Battle Royale could not spawn any Roster.", this);
                return false;
            }

            elapsedMatchTime = 0f;
            MatchStarted = true;
            winnerAnnounced = false;
            Winner = null;
            Debug.Log(
                $"Solo Battle Royale started with {Roster.Count} Roster.",
                this);
            return true;
        }

        public override void NotifyCharacterEliminated(
            BattleRoyaleCharacter eliminated,
            BattleRoyaleCharacter damageSource)
        {
            if (!MatchStarted)
            {
                return;
            }

            var sourceName = damageSource == null
                ? "the safe zone"
                : damageSource.name;
            damageSource?.RegisterKill();
            Debug.Log(
                $"{eliminated.name} was eliminated by {sourceName}. " +
                $"{AliveCount} character(s) remain.",
                eliminated);
            CheckForWinner();
        }

        public bool IsInsideSafeZone(Vector3 position)
        {
            var offset = position - safeZoneCenter;
            offset.y = 0f;
            return offset.sqrMagnitude <= safeZoneRadius * safeZoneRadius;
        }

        public Vector3 GetSafeZonePoint(Vector3 fromPosition)
        {
            var offset = fromPosition - safeZoneCenter;
            offset.y = 0f;
            if (offset.sqrMagnitude <= safeZoneRadius * safeZoneRadius * 0.64f)
            {
                return fromPosition;
            }

            var direction = offset.sqrMagnitude <= 0.001f
                ? Vector3.forward
                : offset.normalized;
            var point = safeZoneCenter -
                        direction * safeZoneRadius * 0.65f;
            return SnapToNavigation(point);
        }

        public override bool TryGetHazardDestination(
            Vector3 fromPosition,
            out Vector3 destination)
        {
            if (IsInsideSafeZone(fromPosition))
            {
                destination = fromPosition;
                return false;
            }

            destination = GetSafeZonePoint(fromPosition);
            return true;
        }

        public override Vector3 GetRetreatPoint(Vector3 fromPosition, Vector3 threatPosition)
        {
            var awayFromThreat = fromPosition - threatPosition;
            awayFromThreat.y = 0f;
            if (awayFromThreat.sqrMagnitude <= 0.001f)
            {
                awayFromThreat = Random.insideUnitSphere;
                awayFromThreat.y = 0f;
            }

            awayFromThreat.Normalize();
            var towardCenter = safeZoneCenter - fromPosition;
            towardCenter.y = 0f;
            if (towardCenter.sqrMagnitude > 0.001f)
            {
                towardCenter.Normalize();
            }

            var direction = (awayFromThreat + towardCenter).normalized;
            var point = fromPosition +
                        direction * Mathf.Max(10f, safeZoneRadius * 0.2f);
            var fromCenter = point - safeZoneCenter;
            fromCenter.y = 0f;
            if (fromCenter.magnitude > safeZoneRadius * 0.75f)
            {
                point = safeZoneCenter + fromCenter.normalized *
                        safeZoneRadius * 0.75f;
            }

            point.y = safeZoneCenter.y;
            return SnapToNavigation(point);
        }

        public override Vector3 GetWanderPoint()
        {
            return GetRandomPointWithinSafeZone();
        }

        private Vector3 GetRandomPointWithinSafeZone()
        {
            var random = Random.insideUnitCircle *
                         Mathf.Max(1f, safeZoneRadius * 0.8f);
            var point = safeZoneCenter + new Vector3(random.x, 0f, random.y);
            point.y = safeZoneCenter.y;
            return SnapToNavigation(point);
        }

        private bool ResolveMapAndConfiguration()
        {
            if (!TryBindActiveMap(
                    "Solo Battle Royale requires an active map from the Active Map manager."))
            {
                return false;
            }

            if (ModeConfiguration == null ||
                ModeConfiguration.map != ActiveMap.definition ||
                !ModeConfiguration.IsBattleRoyale)
            {
                SetModeConfiguration(
                    FindConfiguration(configuration => configuration.IsBattleRoyale));
            }

            if (ModeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{ActiveMap.definition.name}' has no Battle Royale configuration.",
                    ActiveMap);
                return false;
            }

            if (ModeConfiguration.battleRoyaleSpawnPoints == null ||
                ModeConfiguration.battleRoyaleSpawnPoints.Count == 0)
            {
                Debug.LogWarning(
                    "No Battle Royale spawn points are configured. " +
                    "Characters will use random positions inside the map bounds.",
                    ModeConfiguration);
            }

            return true;
        }

        private void ApplyLoadedModeConfiguration()
        {
            if (ModeConfiguration == null)
            {
                return;
            }

            var rosterSize = Mathf.Max(1, ModeConfiguration.teamCount) *
                             Mathf.Max(1, ModeConfiguration.teamSize);
            var playerSlots = SpawnTestPlayer ? 1 : 0;
            botCount = Mathf.Max(0, rosterSize - playerSlots);
        }

        private void PrepareSafeZone()
        {
            var definition = ActiveMap.definition;
            safeZoneCenter = ActiveMap.transform.TransformPoint(
                definition.WorldCenter);
            var localWidth = definition.WorldSize.x *
                             Mathf.Abs(ActiveMap.transform.lossyScale.x);
            var localDepth = definition.WorldSize.z *
                             Mathf.Abs(ActiveMap.transform.lossyScale.z);
            safeZoneStartRadius = Mathf.Max(
                1f,
                Mathf.Min(localWidth, localDepth) *
                0.5f *
                safeZoneStartRadiusPercent);
            safeZoneFinalRadius = Mathf.Max(
                0.5f,
                safeZoneStartRadius * safeZoneFinalRadiusPercent);
            safeZoneRadius = safeZoneStartRadius;
        }

        private void UpdateSafeZone()
        {
            if (elapsedMatchTime <= safeZoneStartDelay)
            {
                safeZoneRadius = safeZoneStartRadius;
                return;
            }

            var shrinkTime = elapsedMatchTime - safeZoneStartDelay;
            var progress = Mathf.Clamp01(shrinkTime / safeZoneShrinkDuration);
            safeZoneRadius = Mathf.Lerp(
                safeZoneStartRadius,
                safeZoneFinalRadius,
                progress);
        }

        private void ApplySafeZoneDamage()
        {
            if (outsideSafeZoneDamagePerSecond <= 0f)
            {
                return;
            }

            var damage = outsideSafeZoneDamagePerSecond * Time.deltaTime;
            foreach (var character in Roster)
            {
                if (character != null &&
                    character.IsAlive &&
                    !IsInsideSafeZone(character.transform.position))
                {
                    character.ApplyDamage(
                        damage * character.SafeZoneDamageMultiplier);
                }
            }
        }

        private void CheckForWinner()
        {
            if (winnerAnnounced)
            {
                return;
            }

            RefreshRoster();
            BattleRoyaleCharacter lastAlive = null;
            var aliveCount = 0;
            foreach (var character in Roster)
            {
                if (character == null || !character.IsAlive)
                {
                    continue;
                }

                lastAlive = character;
                aliveCount++;
            }

            if (aliveCount > 1)
            {
                return;
            }

            winnerAnnounced = true;
            Winner = lastAlive;
            if (Winner == null)
            {
                Debug.Log("Solo Battle Royale ended with no survivor.", this);
            }
            else
            {
                Debug.Log($"Solo Battle Royale winner: {Winner.name}.", Winner);
            }
        }

        protected override void RefreshRoster()
        {
            if (!MatchStarted)
            {
                return;
            }

            if (Roster.Count > 0)
            {
                return;
            }

            foreach (var candidate in Resources.FindObjectsOfTypeAll<
                         BattleRoyaleCharacter>())
            {
                if (candidate == null ||
                    candidate.Match != this ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded ||
                    Roster.Contains(candidate))
                {
                    continue;
                }

                Roster.Add(candidate);
            }
        }

        private Vector3 GetSpawnPosition(int index)
        {
            var points = ModeConfiguration.battleRoyaleSpawnPoints;
            if (points != null && points.Count > 0)
            {
                var point = points[index % points.Count];
                if (point != null)
                {
                    var offset = Random.insideUnitCircle * point.radius;
                    var localPosition = point.position +
                                         new Vector3(offset.x, 0f, offset.y);
                    return SnapToNavigation(
                        ActiveMap.transform.TransformPoint(localPosition));
                }
            }

            var random = Random.insideUnitCircle *
                         Mathf.Max(1f, safeZoneStartRadius * 0.8f);
            var fallbackPosition = safeZoneCenter +
                                   new Vector3(random.x, 0f, random.y);
            return SnapToNavigation(fallbackPosition);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (sightLineMaterial != null)
            {
                Destroy(sightLineMaterial);
            }
        }

        private void UpdateSightLines()
        {
            var usedLines = 0;
            if (drawLineOfSight && MatchStarted)
            {
                RefreshRoster();
                for (var sourceIndex = 0; sourceIndex < Roster.Count; sourceIndex++)
                {
                    var source = Roster[sourceIndex];
                    if (source == null || !source.IsAlive)
                    {
                        continue;
                    }

                    for (var targetIndex = 0; targetIndex < Roster.Count; targetIndex++)
                    {
                        var target = Roster[targetIndex];
                        if (target == null || target == source || !target.IsAlive)
                        {
                            continue;
                        }

                        var sightRange = source.WeaponRange;
                        var flatOffset = target.transform.position - source.transform.position;
                        flatOffset.y = 0f;
                        if (flatOffset.sqrMagnitude > sightRange * sightRange)
                        {
                            continue;
                        }

                        var clear = BattleRoyaleLineOfSight.Evaluate(
                            source,
                            target,
                            sightRange,
                            out var origin,
                            out var end);
                        var line = GetSightLine(usedLines);
                        usedLines++;
                        line.enabled = true;
                        line.positionCount = 2;
                        line.SetPosition(0, origin);
                        line.SetPosition(1, end);
                        var color = clear
                            ? new Color(0.2f, 1f, 0.35f, 0.9f)
                            : new Color(1f, 0.2f, 0.15f, 0.9f);
                        line.startColor = color;
                        line.endColor = color;
                        sightLineBlock ??= new MaterialPropertyBlock();
                        sightLineBlock.Clear();
                        sightLineBlock.SetColor("_BaseColor", color);
                        sightLineBlock.SetColor("_Color", color);
                        line.SetPropertyBlock(sightLineBlock);
                    }
                }
            }

            for (var index = usedLines; index < sightLines.Count; index++)
            {
                sightLines[index].enabled = false;
            }
        }

        private LineRenderer GetSightLine(int index)
        {
            while (sightLines.Count <= index)
            {
                var lineObject = new GameObject($"SightLine_{sightLines.Count:00}");
                lineObject.transform.SetParent(transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.numCapVertices = 4;
                line.widthMultiplier = 0.8f;
                line.material = SightLineMaterial;
                line.enabled = false;
                sightLines.Add(line);
            }

            return sightLines[index];
        }

        private Material SightLineMaterial
        {
            get
            {
                if (sightLineMaterial != null)
                {
                    return sightLineMaterial;
                }

                var shader = Shader.Find("Sprites/Default") ??
                             Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Universal Render Pipeline/Lit");
                sightLineMaterial = new Material(shader);
                if (sightLineMaterial.HasProperty("_BaseColor"))
                {
                    sightLineMaterial.SetColor("_BaseColor", Color.white);
                }

                return sightLineMaterial;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawSafeZone || safeZoneRadius <= 0f)
            {
                return;
            }

            Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireSphere(safeZoneCenter, safeZoneRadius);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyLoadedModeConfiguration();
            botCount = Mathf.Max(SpawnTestPlayer ? 0 : 1, botCount);
            safeZoneStartDelay = Mathf.Max(0f, safeZoneStartDelay);
            safeZoneShrinkDuration = Mathf.Max(0.1f, safeZoneShrinkDuration);
            outsideSafeZoneDamagePerSecond = Mathf.Max(
                0f,
                outsideSafeZoneDamagePerSecond);
            safeZoneFinalRadiusPercent = Mathf.Min(
                safeZoneStartRadiusPercent,
                Mathf.Clamp01(safeZoneFinalRadiusPercent));
        }
    }
}
