using System.Collections.Generic;
using BattleRoyale.GameModes.BattleRoyale;
using UnityEngine;

namespace BattleRoyale.GameModes.Deathmatch
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class DeathmatchGameMode : SimulationMatch
    {
        private static readonly Color[] TeamColors =
        {
            new(0.2f, 0.45f, 1f),
            new(1f, 0.45f, 0.15f),
            new(0.2f, 0.8f, 0.35f),
            new(0.75f, 0.3f, 0.9f)
        };

        private struct PendingRespawn
        {
            public BattleRoyaleAgent Agent;
            public float ReadyTime;
        }

        private readonly List<PendingRespawn> pendingRespawns = new();
        private readonly DeathmatchCombatStats combatStats = new();
        private int[] teamScores = System.Array.Empty<int>();
        private int scoreLimit = DeathmatchRules.DefaultScoreLimit;
        private float respawnSeconds = DeathmatchRules.DefaultRespawnSeconds;
        private float elapsedMatchTime;

        protected override string AgentRootName => "DeathmatchAgents";
        protected override string ProjectileRootName => "DeathmatchProjectiles";
        protected override string RuntimeProfileName => "Runtime Deathmatch Profile";

        public IReadOnlyList<int> TeamScores => teamScores;
        public int WinningTeam { get; private set; } = -1;
        public float ElapsedMatchTime => elapsedMatchTime;
        public DeathmatchCombatStats CombatStats => combatStats;

        public float RespawnSecondsRemaining(BattleRoyaleAgent agent)
        {
            if (agent == null)
            {
                return 0f;
            }

            for (var index = 0; index < pendingRespawns.Count; index++)
            {
                if (pendingRespawns[index].Agent != agent)
                {
                    continue;
                }

                return Mathf.Max(0f, pendingRespawns[index].ReadyTime - Time.time);
            }

            return 0f;
        }

        public Color TeamColorAt(int teamIndex)
        {
            if (teamIndex < 0)
            {
                return Color.white;
            }

            return TeamColors[teamIndex % TeamColors.Length];
        }

        private void Update()
        {
            if (!MatchStarted || WinningTeam >= 0)
            {
                return;
            }

            elapsedMatchTime += Time.deltaTime;
            RespawnReadyAgents();
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
            EnsureSimulation();
            Roster.Clear();
            pendingRespawns.Clear();
            WinningTeam = -1;
            elapsedMatchTime = 0f;
            combatStats.Clear();

            var teamCount = teamScores.Length;
            var rosterSize = teamCount * Mathf.Max(1, ModeConfiguration.teamSize);
            for (var index = 0; index < rosterSize; index++)
            {
                var teamIndex = index % teamCount;
                var isPlayer = SpawnTestPlayer && index == 0;
                var strategy = isPlayer
                    ? FallbackStrategy
                    : GetStrategy(index);
                SpawnAgent(
                    $"DM_T{teamIndex + 1}_{index + 1:00}_{strategy}",
                    GetSpawnPosition(teamIndex),
                    strategy,
                    !isPlayer,
                    agent =>
                    {
                        agent.AssignTeam(teamIndex);
                        agent.SetTeamColor(TeamColors[teamIndex % TeamColors.Length]);
                    });
            }

            if (Roster.Count == 0)
            {
                Debug.LogError("Deathmatch could not spawn any agents.", this);
                return false;
            }

            RegisterCombatRows();
            MatchStarted = true;
            Debug.Log(
                $"Deathmatch started with {teamCount} teams and {Roster.Count} agents.",
                this);
            return true;
        }

        public override void NotifyAgentEliminated(
            BattleRoyaleAgent eliminated,
            BattleRoyaleAgent damageSource)
        {
            if (!MatchStarted || WinningTeam >= 0 || eliminated == null)
            {
                return;
            }

            if (damageSource != null &&
                damageSource.TeamIndex >= 0 &&
                damageSource.TeamIndex < teamScores.Length &&
                !DeathmatchRules.SharesTeam(
                    damageSource.TeamIndex,
                    eliminated.TeamIndex))
            {
                damageSource.RegisterKill();
                DeathmatchRules.AddKill(teamScores, damageSource.TeamIndex);
            }

            RecordElimination(eliminated, damageSource);

            var winner = DeathmatchRules.FindWinner(teamScores, scoreLimit);
            if (winner >= 0)
            {
                WinningTeam = winner;
                pendingRespawns.Clear();
                Debug.Log($"Deathmatch winner: Team {winner + 1}.", this);
                return;
            }

            if (!IsAwaitingRespawn(eliminated))
            {
                pendingRespawns.Add(
                    new PendingRespawn
                    {
                        Agent = eliminated,
                        ReadyTime = Time.time + respawnSeconds
                    });
            }
        }

        public override void RecordFiredShot(BattleRoyaleAgent shooter)
        {
            if (!CanRecordStats())
            {
                return;
            }

            combatStats.RecordShot(RosterIndex(shooter));
        }

        public override void RecordDealtDamage(
            BattleRoyaleAgent attacker,
            BattleRoyaleAgent victim,
            float healthRemoved)
        {
            if (!CanRecordStats())
            {
                return;
            }

            combatStats.RecordHit(
                RosterIndex(attacker),
                RosterIndex(victim),
                healthRemoved,
                elapsedMatchTime);
        }

        private void RegisterCombatRows()
        {
            for (var index = 0; index < Roster.Count; index++)
            {
                var agent = Roster[index];
                if (agent == null)
                {
                    continue;
                }

                var displayName = agent.Profile == null
                    ? null
                    : agent.Profile.displayName;
                combatStats.RegisterAgent(index, agent.TeamIndex, displayName);
            }

            var teams = ModeConfiguration == null
                ? null
                : ModeConfiguration.deathmatchTeams;
            for (var team = 0; team < teamScores.Length; team++)
            {
                var configured = teams != null &&
                                 team < teams.Count &&
                                 teams[team] != null
                    ? teams[team].teamId
                    : null;
                combatStats.SetTeamName(team, configured);
            }
        }

        private void RecordElimination(
            BattleRoyaleAgent eliminated,
            BattleRoyaleAgent damageSource)
        {
            var killerIndex = -1;
            if (damageSource != null &&
                !DeathmatchRules.SharesTeam(
                    damageSource.TeamIndex,
                    eliminated.TeamIndex))
            {
                killerIndex = RosterIndex(damageSource);
            }

            combatStats.RecordElimination(
                RosterIndex(eliminated),
                killerIndex,
                elapsedMatchTime);
        }

        private bool CanRecordStats()
        {
            return MatchStarted && WinningTeam < 0;
        }

        private int RosterIndex(BattleRoyaleAgent agent)
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

        public override bool TryGetHazardDestination(
            Vector3 fromPosition,
            out Vector3 destination)
        {
            destination = fromPosition;
            return false;
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
            return SnapToNavigation(fromPosition + awayFromThreat * 12f);
        }

        public override Vector3 GetWanderPoint()
        {
            if (ActiveMap == null || ActiveMap.definition == null)
            {
                return SnapToNavigation(transform.position);
            }

            var definition = ActiveMap.definition;
            var half = definition.WorldSize * 0.4f;
            var localPoint = definition.WorldCenter + new Vector3(
                Random.Range(-half.x, half.x),
                0f,
                Random.Range(-half.z, half.z));
            return SnapToNavigation(ActiveMap.transform.TransformPoint(localPoint));
        }

        private void RespawnReadyAgents()
        {
            var now = Time.time;
            for (var index = pendingRespawns.Count - 1; index >= 0; index--)
            {
                var pending = pendingRespawns[index];
                if (pending.Agent == null || now < pending.ReadyTime)
                {
                    continue;
                }

                pendingRespawns.RemoveAt(index);
                pending.Agent.Respawn(GetSpawnPosition(pending.Agent.TeamIndex));
            }
        }

        private bool IsAwaitingRespawn(BattleRoyaleAgent agent)
        {
            foreach (var pending in pendingRespawns)
            {
                if (pending.Agent == agent)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ResolveMapAndConfiguration()
        {
            if (!TryBindActiveMap(
                    "Deathmatch requires an active map from the Active Map manager."))
            {
                return false;
            }

            if (ModeConfiguration == null ||
                ModeConfiguration.map != ActiveMap.definition ||
                !ModeConfiguration.IsDeathmatch)
            {
                SetModeConfiguration(
                    FindConfiguration(configuration => configuration.IsDeathmatch));
            }

            if (ModeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{ActiveMap.definition.name}' has no Deathmatch configuration.",
                    ActiveMap);
                return false;
            }

            if (ModeConfiguration.deathmatchTeams == null ||
                ModeConfiguration.deathmatchTeams.Count <
                DeathmatchRules.RequiredTeamCount(ModeConfiguration.teamCount))
            {
                Debug.LogWarning(
                    "Deathmatch is missing a spawn for one or more teams. " +
                    "Those teams respawn at the map center.",
                    ModeConfiguration);
            }

            return true;
        }

        private void ApplyLoadedModeConfiguration()
        {
            var teamCount = DeathmatchRules.RequiredTeamCount(
                ModeConfiguration.teamCount);
            scoreLimit = Mathf.Max(
                1,
                ModeConfiguration.scoreLimit <= 0
                    ? DeathmatchRules.DefaultScoreLimit
                    : ModeConfiguration.scoreLimit);
            respawnSeconds = Mathf.Max(0f, ModeConfiguration.respawnSeconds);
            teamScores = new int[teamCount];
        }

        private Vector3 GetSpawnPosition(int teamIndex)
        {
            var teams = ModeConfiguration == null
                ? null
                : ModeConfiguration.deathmatchTeams;
            if (teams != null &&
                teamIndex >= 0 &&
                teamIndex < teams.Count &&
                teams[teamIndex] != null)
            {
                var point = teams[teamIndex];
                var offset = Random.insideUnitCircle * Mathf.Max(0f, point.radius);
                var localPosition = point.spawnPosition +
                                    new Vector3(offset.x, 0f, offset.y);
                return SnapToNavigation(
                    ActiveMap.transform.TransformPoint(localPosition));
            }

            var center = ActiveMap.definition.WorldCenter;
            return SnapToNavigation(ActiveMap.transform.TransformPoint(center));
        }
    }
}
