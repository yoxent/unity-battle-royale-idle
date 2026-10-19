using BattleRoyale.Company;
using BattleRoyale.Company;
using BattleRoyale.GameModes;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class BattleRoyaleGameMode : SimulationMatch, IMatchCombatGate
    {
        [Header("Agents")]
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

        private readonly BattleRoyaleRosterOrder rosterOrder = new();
        private int[] livingCounts = System.Array.Empty<int>();
        private float elapsedMatchTime;
        private Vector3 safeZoneCenter;
        private float safeZoneStartRadius;
        private float safeZoneRadius;
        private float safeZoneFinalRadius;
        private bool winnerAnnounced;

        public override bool Runs(MapGameMode mode) => mode == MapGameMode.BattleRoyale;

        protected override string AgentRootName => "BattleRoyaleAgents";
        protected override string ProjectileRootName => "BattleRoyaleProjectiles";
        protected override string RuntimeProfileName => "Runtime Battle Royale Profile";

        public BattleRoyaleRosterOrder RosterOrder => rosterOrder;
        public BattleRoyaleAgent Winner { get; private set; }
        public bool CombatEnabled => MatchStarted && !winnerAnnounced;
        public int AliveCount
        {
            get
            {
                RefreshRoster();
                var count = 0;
                foreach (var agent in Roster)
                {
                    if (agent != null && agent.IsAlive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public Vector3 SafeZoneCenter => safeZoneCenter;
        public float SafeZoneRadius => safeZoneRadius;

        public Color TeamColorAt(int teamIndex)
        {
            return SideColorRules.Fallback(teamIndex).AsColor();
        }

        private void Update()
        {
            if (!MatchStarted || winnerAnnounced)
            {
                return;
            }

            RefreshRoster();
            elapsedMatchTime += Time.deltaTime;
            UpdateSafeZone();
            ApplySafeZoneDamage();
            CheckForWinner();
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

            rosterOrder.Clear();
            var teamSize = Mathf.Max(1, ModeConfiguration.teamSize);
            var spawnCount = botCount + (SpawnTestPlayer ? 1 : 0);
            for (var index = 0; index < spawnCount; index++)
            {
                var isPlayer = SpawnTestPlayer && index == 0;
                var strategy = isPlayer
                    ? FallbackStrategy
                    : GetStrategy(index - (SpawnTestPlayer ? 1 : 0));
                var teamIndex = BattleRoyaleRules.TeamIndexForSlot(index, teamSize);
                var spawnPosition = GetSpawnPosition(index);
                var agent = SpawnAgent(
                    $"BR_T{teamIndex + 1}_{index + 1:00}_{strategy}",
                    spawnPosition,
                    strategy,
                    !isPlayer,
                    spawned => AssignSpawnedTeam(spawned, teamIndex, teamSize));
                PresentRosterAgent(agent);
            }

            if (Roster.Count == 0)
            {
                Debug.LogError("Battle Royale could not spawn any agents.", this);
                return false;
            }

            elapsedMatchTime = 0f;
            MatchStarted = true;
            winnerAnnounced = false;
            Winner = null;
            WinningTeam = -1;
            Debug.Log(
                $"Battle Royale started with {Roster.Count} agents.",
                this);
            return true;
        }

        public override void NotifyAgentEliminated(
            BattleRoyaleAgent eliminated,
            BattleRoyaleAgent damageSource)
        {
            if (!MatchStarted)
            {
                return;
            }

            var sourceName = damageSource == null
                ? "the safe zone"
                : damageSource.name;
            damageSource?.RegisterKill();
            rosterOrder.RecordDeath(FindRosterIndex(eliminated));
            Debug.Log(
                $"{eliminated.name} was eliminated by {sourceName}. " +
                $"{AliveCount} agent(s) remain.",
                eliminated);
            CheckForWinner();
        }

        protected override void StopRunningMatch()
        {
            rosterOrder.Clear();
            base.StopRunningMatch();
        }

        private void PresentRosterAgent(BattleRoyaleAgent agent)
        {
            var rosterIndex = FindRosterIndex(agent);
            if (rosterIndex < 0)
            {
                return;
            }

            var displayName = agent.Profile == null
                ? null
                : agent.Profile.displayName;
            rosterOrder.Present(rosterIndex, agent.TeamIndex, displayName);
        }

        private int FindRosterIndex(BattleRoyaleAgent agent)
        {
            if (agent == null)
            {
                return -1;
            }

            for (var index = 0; index < Roster.Count; index++)
            {
                if (Roster[index] == agent)
                {
                    return index;
                }
            }

            return -1;
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

        private BattleRoyaleModeConfiguration Configuration =>
            ModeConfiguration as BattleRoyaleModeConfiguration;

        private bool ResolveMapAndConfiguration()
        {
            if (!TryBindActiveMap(
                    "Battle Royale requires an active map from the Active Map manager."))
            {
                return false;
            }

            if (ModeConfiguration == null ||
                ModeConfiguration.map != ActiveMap.definition ||
                ModeConfiguration is not BattleRoyaleModeConfiguration)
            {
                SetModeConfiguration(
                    FindConfiguration(configuration => configuration is BattleRoyaleModeConfiguration));
            }

            if (ModeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{ActiveMap.definition.name}' has no Battle Royale configuration.",
                    ActiveMap);
                return false;
            }

            if (Configuration.spawnPoints == null ||
                Configuration.spawnPoints.Count == 0)
            {
                Debug.LogWarning(
                    "No Battle Royale spawn points are configured. " +
                    "Agents will use random positions inside the map bounds.",
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
            safeZoneCenter = ActiveMap.WorldPoint(definition.WorldCenter);
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
            foreach (var agent in Roster)
            {
                if (agent != null &&
                    agent.IsAlive &&
                    !IsInsideSafeZone(agent.transform.position))
                {
                    agent.ApplyHazardDamage(damage);
                }
            }
        }

        private void AssignSpawnedTeam(
            BattleRoyaleAgent agent,
            int teamIndex,
            int teamSize)
        {
            agent.AssignTeam(teamIndex);
            if (teamSize <= 1)
            {
                return;
            }

            agent.SetTeamColor(SideColorRules.Fallback(teamIndex).AsColor());
        }

        private void CheckForWinner()
        {
            if (winnerAnnounced)
            {
                return;
            }

            RefreshRoster();
            CountLivingTeams();
            if (!BattleRoyaleRules.IsMatchOver(livingCounts))
            {
                return;
            }

            winnerAnnounced = true;
            WinningTeam = BattleRoyaleRules.SoleSurvivor(livingCounts);
            HoldRoster();
            Winner = null;
            foreach (var agent in Roster)
            {
                if (agent != null &&
                    agent.IsAlive &&
                    agent.TeamIndex == WinningTeam)
                {
                    Winner = agent;
                    break;
                }
            }

            if (WinningTeam < 0)
            {
                Debug.Log("Battle Royale ended with no surviving team.", this);
            }
            else
            {
                Debug.Log(
                    $"Battle Royale winner: Team {WinningTeam + 1}.",
                    Winner != null ? Winner : this);
            }
        }

        private void CountLivingTeams()
        {
            var teamCount = ModeConfiguration == null
                ? 1
                : Mathf.Max(1, ModeConfiguration.teamCount);
            if (livingCounts.Length != teamCount)
            {
                livingCounts = new int[teamCount];
            }

            for (var team = 0; team < livingCounts.Length; team++)
            {
                livingCounts[team] = 0;
            }

            foreach (var agent in Roster)
            {
                if (agent == null || !agent.IsAlive)
                {
                    continue;
                }

                var team = agent.TeamIndex;
                if (team < 0 || team >= livingCounts.Length)
                {
                    continue;
                }

                livingCounts[team]++;
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
                         BattleRoyaleAgent>())
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
            var points = Configuration == null ? null : Configuration.spawnPoints;
            if (points != null && points.Count > 0)
            {
                var point = points[index % points.Count];
                if (point != null)
                {
                    var offset = Random.insideUnitCircle * point.radius;
                    var localPosition = point.position +
                                         new Vector3(offset.x, 0f, offset.y);
                    return SnapToNavigation(ActiveMap.WorldPoint(localPosition));
                }
            }

            var random = Random.insideUnitCircle *
                         Mathf.Max(1f, safeZoneStartRadius * 0.8f);
            var fallbackPosition = safeZoneCenter +
                                   new Vector3(random.x, 0f, random.y);
            return SnapToNavigation(fallbackPosition);
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
