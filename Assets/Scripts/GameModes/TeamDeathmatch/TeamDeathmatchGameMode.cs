using System.Collections.Generic;
using BattleRoyale.GameModes.SoloBattleRoyale;
using UnityEngine;

namespace BattleRoyale.GameModes.TeamDeathmatch
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class TeamDeathmatchGameMode : SimulationMatch
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
        private int[] teamScores = System.Array.Empty<int>();
        private int scoreLimit = TeamDeathmatchRules.DefaultScoreLimit;
        private float respawnSeconds = TeamDeathmatchRules.DefaultRespawnSeconds;

        protected override string AgentRootName => "TeamDeathmatchAgents";
        protected override string ProjectileRootName => "TeamDeathmatchProjectiles";
        protected override string RuntimeProfileName => "Runtime Team Deathmatch Profile";

        public IReadOnlyList<int> TeamScores => teamScores;
        public int WinningTeam { get; private set; } = -1;

        private void Update()
        {
            if (!MatchStarted || WinningTeam >= 0)
            {
                return;
            }

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
                    $"TDM_T{teamIndex + 1}_{index + 1:00}_{strategy}",
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
                Debug.LogError("Team Deathmatch could not spawn any agents.", this);
                return false;
            }

            MatchStarted = true;
            Debug.Log(
                $"Team Deathmatch started with {teamCount} teams and {Roster.Count} agents.",
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
                !TeamDeathmatchRules.SharesTeam(
                    damageSource.TeamIndex,
                    eliminated.TeamIndex))
            {
                damageSource.RegisterKill();
                TeamDeathmatchRules.AddKill(teamScores, damageSource.TeamIndex);
                Debug.Log(
                    $"{eliminated.name} was eliminated by {damageSource.name}. " +
                    $"Team {damageSource.TeamIndex + 1} score: " +
                    $"{teamScores[damageSource.TeamIndex]}.",
                    eliminated);
            }

            var winner = TeamDeathmatchRules.FindWinner(teamScores, scoreLimit);
            if (winner >= 0)
            {
                WinningTeam = winner;
                pendingRespawns.Clear();
                Debug.Log($"Team Deathmatch winner: Team {winner + 1}.", this);
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
                    "Team Deathmatch requires an active map from the Active Map manager."))
            {
                return false;
            }

            if (ModeConfiguration == null ||
                ModeConfiguration.map != ActiveMap.definition ||
                !ModeConfiguration.IsTeamDeathmatch)
            {
                SetModeConfiguration(
                    FindConfiguration(configuration => configuration.IsTeamDeathmatch));
            }

            if (ModeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{ActiveMap.definition.name}' has no Team Deathmatch configuration.",
                    ActiveMap);
                return false;
            }

            if (ModeConfiguration.teamDeathmatchTeams == null ||
                ModeConfiguration.teamDeathmatchTeams.Count <
                TeamDeathmatchRules.RequiredTeamCount(ModeConfiguration.teamCount))
            {
                Debug.LogWarning(
                    "Team Deathmatch is missing a spawn for one or more teams. " +
                    "Those teams respawn at the map center.",
                    ModeConfiguration);
            }

            return true;
        }

        private void ApplyLoadedModeConfiguration()
        {
            var teamCount = TeamDeathmatchRules.RequiredTeamCount(
                ModeConfiguration.teamCount);
            scoreLimit = Mathf.Max(
                1,
                ModeConfiguration.scoreLimit <= 0
                    ? TeamDeathmatchRules.DefaultScoreLimit
                    : ModeConfiguration.scoreLimit);
            respawnSeconds = Mathf.Max(0f, ModeConfiguration.respawnSeconds);
            teamScores = new int[teamCount];
        }

        private Vector3 GetSpawnPosition(int teamIndex)
        {
            var teams = ModeConfiguration == null
                ? null
                : ModeConfiguration.teamDeathmatchTeams;
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
