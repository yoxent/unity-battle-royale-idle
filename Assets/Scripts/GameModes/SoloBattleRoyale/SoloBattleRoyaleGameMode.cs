using System.Collections.Generic;
using BattleRoyale.GameModes;
using BattleRoyale.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class SoloBattleRoyaleGameMode : MonoBehaviour
    {
        private static readonly BattleRoyaleAIStrategy[] StrategyRotation =
        {
            BattleRoyaleAIStrategy.AggressiveHunter,
            BattleRoyaleAIStrategy.AggressiveSkirmisher,
            BattleRoyaleAIStrategy.NeutralOpportunist,
            BattleRoyaleAIStrategy.NeutralScout,
            BattleRoyaleAIStrategy.DefensiveAnchor,
            BattleRoyaleAIStrategy.DefensiveSurvivor
        };

        [Header("Map")]
        [SerializeField] private MapInstance activeMap;
        [SerializeField] private MapModeConfiguration modeConfiguration;
        [SerializeField] private bool findMapOnStart = true;

        [Header("Team Management")]
        [Tooltip(
            "Player-selected behavior applied to every managed AI agent. " +
            "Individual bot strategies still provide variation.")]
        [SerializeField] private TeamPlaystyle teamPlaystyle =
            TeamPlaystyle.Neutral;

        [Header("Characters")]
        [SerializeField] private BattleRoyaleCharacter characterPrefab;
        [SerializeField] private BattleRoyaleCharacterProfile defaultProfile;
        [Tooltip(
            "Set from the loaded mode configuration: team count times players per team, " +
            "excluding the test player when that slot is enabled.")]
        [Min(0)]
        [SerializeField] private int botCount = 8;
        [SerializeField] private bool spawnTestPlayer;
        [SerializeField] private bool distributeStrategies = true;
        [SerializeField] private BattleRoyaleAIStrategy fallbackStrategy =
            BattleRoyaleAIStrategy.NeutralOpportunist;
        [SerializeField] private Transform characterParent;
        [Min(0.1f)]
        [SerializeField] private float navMeshSnapDistance = 20f;

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
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool drawSafeZone = true;
        [SerializeField] private bool drawLineOfSight = true;

        private readonly List<BattleRoyaleCharacter> characters = new();
        private BattleRoyaleProjectilePool projectilePool;
        private BattleRoyaleCharacterProfile runtimeDefaultProfile;
        private float elapsedMatchTime;
        private Vector3 safeZoneCenter;
        private float safeZoneStartRadius;
        private float safeZoneRadius;
        private float safeZoneFinalRadius;
        private readonly List<LineRenderer> sightLines = new();
        private Material sightLineMaterial;
        private MaterialPropertyBlock sightLineBlock;
        private bool matchStarted;
        private bool winnerAnnounced;

        public BattleRoyaleProjectilePool Projectiles => projectilePool;
        public MapInstance ActiveMap => activeMap;
        public MapModeConfiguration ModeConfiguration => modeConfiguration;
        public TeamPlaystyle TeamPlaystyle => teamPlaystyle;
        public IReadOnlyList<BattleRoyaleCharacter> Characters
        {
            get
            {
                RefreshRoster();
                return characters;
            }
        }
        public BattleRoyaleCharacter Winner { get; private set; }
        public bool IsMatchStarted => matchStarted;
        public int AliveCount
        {
            get
            {
                RefreshRoster();
                var count = 0;
                foreach (var character in characters)
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

        public void SetTeamPlaystyle(TeamPlaystyle playstyle)
        {
            teamPlaystyle = playstyle;
        }

        private void Start()
        {
            if (autoStart)
            {
                BeginMatch();
            }
        }

        private void Update()
        {
            if (!matchStarted)
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

        public bool BeginMatch()
        {
            if (matchStarted)
            {
                return false;
            }

            if (!ResolveMapAndConfiguration())
            {
                return false;
            }

            ApplyLoadedModeConfiguration();
            PrepareSafeZone();
            EnsureCharacterParent();
            EnsureDefaultProfile();
            EnsureProjectilePool();

            var spawnCount = botCount + (spawnTestPlayer ? 1 : 0);
            for (var index = 0; index < spawnCount; index++)
            {
                var isPlayer = spawnTestPlayer && index == 0;
                var strategy = isPlayer
                    ? fallbackStrategy
                    : GetStrategy(index - (spawnTestPlayer ? 1 : 0));
                var spawnPosition = GetSpawnPosition(index);
                SpawnCharacter(index, spawnPosition, strategy, !isPlayer);
            }

            if (characters.Count == 0)
            {
                Debug.LogError("The solo Battle Royale could not spawn any characters.", this);
                return false;
            }

            elapsedMatchTime = 0f;
            matchStarted = true;
            winnerAnnounced = false;
            Winner = null;
            Debug.Log(
                $"Solo Battle Royale started with {characters.Count} characters.",
                this);
            return true;
        }

        public void NotifyCharacterEliminated(
            BattleRoyaleCharacter eliminated,
            BattleRoyaleCharacter damageSource)
        {
            if (!matchStarted)
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

        public Vector3 GetRetreatPoint(Vector3 fromPosition, Vector3 threatPosition)
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

        public Vector3 GetRandomPointWithinSafeZone()
        {
            var random = Random.insideUnitCircle *
                         Mathf.Max(1f, safeZoneRadius * 0.8f);
            var point = safeZoneCenter + new Vector3(random.x, 0f, random.y);
            point.y = safeZoneCenter.y;
            return SnapToNavigation(point);
        }

        private bool ResolveMapAndConfiguration()
        {
            if (activeMap == null && findMapOnStart)
            {
                activeMap = Object.FindFirstObjectByType<MapInstance>();
            }

            if (activeMap == null || activeMap.definition == null)
            {
                Debug.LogError(
                    "Solo Battle Royale requires an active MapInstance with a definition.",
                    this);
                return false;
            }

            if (modeConfiguration == null ||
                modeConfiguration.map != activeMap.definition ||
                !modeConfiguration.IsBattleRoyale)
            {
                modeConfiguration = FindBattleRoyaleConfiguration(
                    activeMap.definition);
            }

            if (modeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{activeMap.definition.name}' has no Battle Royale configuration.",
                    activeMap);
                return false;
            }

            if (modeConfiguration.battleRoyaleSpawnPoints == null ||
                modeConfiguration.battleRoyaleSpawnPoints.Count == 0)
            {
                Debug.LogWarning(
                    "No Battle Royale spawn points are configured. " +
                    "Characters will use random positions inside the map bounds.",
                    modeConfiguration);
            }

            return true;
        }

        private void ApplyLoadedModeConfiguration()
        {
            if (modeConfiguration == null)
            {
                return;
            }

            var rosterSize = Mathf.Max(1, modeConfiguration.teamCount) *
                             Mathf.Max(1, modeConfiguration.teamSize);
            var playerSlots = spawnTestPlayer ? 1 : 0;
            botCount = Mathf.Max(0, rosterSize - playerSlots);
        }

        private void PrepareSafeZone()
        {
            var definition = activeMap.definition;
            safeZoneCenter = activeMap.transform.TransformPoint(
                definition.WorldCenter);
            var localWidth = definition.WorldSize.x *
                             Mathf.Abs(activeMap.transform.lossyScale.x);
            var localDepth = definition.WorldSize.z *
                             Mathf.Abs(activeMap.transform.lossyScale.z);
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
            foreach (var character in characters)
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
            foreach (var character in characters)
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

        private void RefreshRoster()
        {
            if (!matchStarted)
            {
                return;
            }

            if (characters.Count > 0)
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
                    characters.Contains(candidate))
                {
                    continue;
                }

                characters.Add(candidate);
            }
        }

        private void SpawnCharacter(
            int index,
            Vector3 position,
            BattleRoyaleAIStrategy strategy,
            bool isBot)
        {
            var characterObject = CreateCharacterObject(index, strategy);
            characterObject.transform.SetParent(characterParent, true);
            characterObject.transform.position = position;

            var agent = characterObject.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = characterObject.AddComponent<NavMeshAgent>();
            }

            var character = characterObject.GetComponent<BattleRoyaleCharacter>();
            if (character == null)
            {
                character = characterObject.AddComponent<BattleRoyaleCharacter>();
            }
            character.Initialize(
                this,
                defaultProfile != null ? defaultProfile : runtimeDefaultProfile,
                strategy,
                isBot);

            var snappedPosition = SnapToNavigation(position);
            if (agent.enabled && agent.Warp(snappedPosition))
            {
                characterObject.transform.position = snappedPosition;
            }
            else
            {
                characterObject.transform.position = snappedPosition;
            }

            if (isBot)
            {
                var ai = characterObject.AddComponent<SoloBattleRoyaleAgent>();
                ai.Initialize(this, character);
            }

            characters.Add(character);
        }

        private GameObject CreateCharacterObject(
            int index,
            BattleRoyaleAIStrategy strategy)
        {
            var objectName = $"BR_{index + 1:00}_{strategy}";
            if (characterPrefab == null)
            {
                return new GameObject(objectName);
            }

            var characterObject = Instantiate(characterPrefab).gameObject;
            characterObject.name = objectName;
            return characterObject;
        }

        private Vector3 GetSpawnPosition(int index)
        {
            var points = modeConfiguration.battleRoyaleSpawnPoints;
            if (points != null && points.Count > 0)
            {
                var point = points[index % points.Count];
                if (point != null)
                {
                    var offset = Random.insideUnitCircle * point.radius;
                    var localPosition = point.position +
                                         new Vector3(offset.x, 0f, offset.y);
                    return SnapToNavigation(
                        activeMap.transform.TransformPoint(localPosition));
                }
            }

            var random = Random.insideUnitCircle *
                         Mathf.Max(1f, safeZoneStartRadius * 0.8f);
            var fallbackPosition = safeZoneCenter +
                                   new Vector3(random.x, 0f, random.y);
            return SnapToNavigation(fallbackPosition);
        }

        private Vector3 SnapToNavigation(Vector3 position)
        {
            if (NavMesh.SamplePosition(
                    position,
                    out var hit,
                    navMeshSnapDistance,
                    NavMesh.AllAreas))
            {
                return hit.position;
            }

            return position;
        }

        private BattleRoyaleAIStrategy GetStrategy(int index)
        {
            if (!distributeStrategies)
            {
                return fallbackStrategy;
            }

            return StrategyRotation[index % StrategyRotation.Length];
        }

        private void EnsureProjectilePool()
        {
            if (projectilePool != null)
            {
                projectilePool.Prewarm();
                return;
            }

            var poolObject = new GameObject("BattleRoyaleProjectiles");
            poolObject.transform.SetParent(transform, false);
            projectilePool = poolObject.AddComponent<BattleRoyaleProjectilePool>();
            projectilePool.Prewarm();
        }

        private void EnsureCharacterParent()
        {
            if (characterParent != null)
            {
                return;
            }

            var parentObject = new GameObject("BattleRoyaleCharacters");
            parentObject.transform.SetParent(transform, false);
            characterParent = parentObject.transform;
        }

        private void EnsureDefaultProfile()
        {
            if (defaultProfile != null)
            {
                return;
            }

            runtimeDefaultProfile =
                ScriptableObject.CreateInstance<BattleRoyaleCharacterProfile>();
            runtimeDefaultProfile.name = "Runtime Battle Royale Profile";
            runtimeDefaultProfile.displayName = "Prototype Bot";
            runtimeDefaultProfile.defaultStrategy = fallbackStrategy;
            runtimeDefaultProfile.hideFlags = HideFlags.DontSave;
        }

        private static MapModeConfiguration FindBattleRoyaleConfiguration(
            MapDefinition definition)
        {
            if (definition.modeConfigurations == null)
            {
                return null;
            }

            foreach (var configuration in definition.modeConfigurations)
            {
                if (configuration != null &&
                    configuration.map == definition &&
                    configuration.IsBattleRoyale)
                {
                    return configuration;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            if (runtimeDefaultProfile != null)
            {
                Destroy(runtimeDefaultProfile);
            }

            if (sightLineMaterial != null)
            {
                Destroy(sightLineMaterial);
            }
        }

        private void UpdateSightLines()
        {
            var usedLines = 0;
            if (drawLineOfSight && matchStarted)
            {
                RefreshRoster();
                for (var sourceIndex = 0; sourceIndex < characters.Count; sourceIndex++)
                {
                    var source = characters[sourceIndex];
                    if (source == null || !source.IsAlive)
                    {
                        continue;
                    }

                    for (var targetIndex = 0; targetIndex < characters.Count; targetIndex++)
                    {
                        var target = characters[targetIndex];
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

        private void OnValidate()
        {
            ApplyLoadedModeConfiguration();
            botCount = Mathf.Max(spawnTestPlayer ? 0 : 1, botCount);
            navMeshSnapDistance = Mathf.Max(0.1f, navMeshSnapDistance);
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
