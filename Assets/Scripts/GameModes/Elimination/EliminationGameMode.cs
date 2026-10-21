using System.Collections.Generic;
using BattleRoyale.Company;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.GameModes.Elimination
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class EliminationGameMode : SimulationMatch, IMatchCombatGate
    {
        private readonly DeathmatchCombatStats combatStats = new();
        private int[] roundWins = System.Array.Empty<int>();
        private int[] livingCounts = System.Array.Empty<int>();
        private int roundWinLimit = EliminationRules.DefaultRoundWinLimit;
        private float roundSeconds = EliminationRules.DefaultRoundSeconds;
        private float intermissionSeconds = EliminationRules.DefaultIntermissionSeconds;
        private float roundElapsed;
        private float intermissionRemaining;
        private bool roundLive;

        public override bool Runs(MapGameMode mode) => mode == MapGameMode.Elimination;

        protected override string AgentRootName => "EliminationAgents";
        protected override string ProjectileRootName => "EliminationProjectiles";
        protected override string RuntimeProfileName => "Runtime Elimination Profile";

        public IReadOnlyList<int> RoundWins => roundWins;
        public int RoundNumber { get; private set; }
        public float RoundSecondsRemaining =>
            roundLive ? Mathf.Max(0f, roundSeconds - roundElapsed) : 0f;
        public float IntermissionSecondsRemaining =>
            roundLive ? 0f : Mathf.Max(0f, intermissionRemaining);
        public bool CombatEnabled => MatchStarted && WinningTeam < 0 && roundLive;
        public DeathmatchCombatStats CombatStats => combatStats;

        public Color TeamColorAt(int teamIndex)
        {
            if (teamIndex < 0)
            {
                return Color.white;
            }

            return SideColorRules.Fallback(teamIndex).AsColor();
        }

        private void Update()
        {
            if (!MatchStarted || WinningTeam >= 0)
            {
                return;
            }

            if (!roundLive)
            {
                intermissionRemaining -= Time.deltaTime;
                if (intermissionRemaining <= 0f)
                {
                    StartNextRound();
                }

                return;
            }

            roundElapsed += Time.deltaTime;
            CloseRoundIfNeeded(roundElapsed >= roundSeconds);
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
            WinningTeam = -1;
            roundElapsed = 0f;
            intermissionRemaining = 0f;
            RoundNumber = 1;
            roundLive = true;
            combatStats.Clear();

            var teamCount = roundWins.Length;
            var rosterSize = teamCount * Mathf.Max(1, ModeConfiguration.teamSize);
            for (var index = 0; index < rosterSize; index++)
            {
                var teamIndex = index % teamCount;
                var isPlayer = SpawnTestPlayer && index == 0;
                var strategy = isPlayer
                    ? FallbackStrategy
                    : GetStrategy(index);
                SpawnAgent(
                    $"ELIM_T{teamIndex + 1}_{index + 1:00}_{strategy}",
                    GetSpawnPosition(teamIndex),
                    strategy,
                    !isPlayer,
                    agent =>
                    {
                        agent.AssignTeam(teamIndex);
                        agent.SetTeamColor(TeamColorAt(teamIndex));
                    });
            }

            if (Roster.Count == 0)
            {
                roundLive = false;
                Debug.LogError("Elimination could not spawn any agents.", this);
                return false;
            }

            RegisterCombatRows();
            MatchStarted = true;
            Debug.Log(
                $"Elimination started. First team to {roundWinLimit} rounds. " +
                $"{teamCount} teams, {Roster.Count} agents.",
                this);
            return true;
        }

        public override void NotifyAgentEliminated(
            BattleRoyaleAgent eliminated,
            BattleRoyaleAgent damageSource)
        {
            if (!CombatEnabled || eliminated == null)
            {
                return;
            }

            if (damageSource != null &&
                !EliminationRules.SharesTeam(
                    damageSource.TeamIndex,
                    eliminated.TeamIndex))
            {
                damageSource.RegisterKill();
            }

            RecordElimination(eliminated, damageSource);
            CloseRoundIfNeeded(false);
        }

        public override void RecordFiredShot(BattleRoyaleAgent shooter)
        {
            if (!CombatEnabled)
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
            if (!CombatEnabled)
            {
                return;
            }

            combatStats.RecordHit(
                RosterIndex(attacker),
                RosterIndex(victim),
                healthRemoved,
                roundElapsed);
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

        private void CloseRoundIfNeeded(bool timeExpired)
        {
            if (!roundLive || WinningTeam >= 0)
            {
                return;
            }

            CountLiving();
            int winner;
            if (!EliminationRules.IsRoundOver(livingCounts))
            {
                if (!timeExpired)
                {
                    return;
                }

                winner = EliminationRules.TimeExpiryWinner(livingCounts);
            }
            else
            {
                winner = EliminationRules.SoleSurvivor(livingCounts);
            }

            roundLive = false;
            HoldRoster();
            if (winner >= 0)
            {
                EliminationRules.AddRoundWin(roundWins, winner);
                Debug.Log(
                    $"Elimination round {RoundNumber} won by Team {winner + 1}.",
                    this);
            }
            else
            {
                Debug.Log($"Elimination round {RoundNumber} was a draw.", this);
            }

            var matchWinner = EliminationRules.FindMatchWinner(roundWins, roundWinLimit);
            if (matchWinner >= 0)
            {
                WinningTeam = matchWinner;
                Debug.Log($"Elimination winner: Team {matchWinner + 1}.", this);
                return;
            }

            intermissionRemaining = intermissionSeconds;
        }

        private void StartNextRound()
        {
            for (var index = 0; index < Roster.Count; index++)
            {
                var agent = Roster[index];
                if (agent == null)
                {
                    continue;
                }

                agent.Respawn(GetSpawnPosition(agent.TeamIndex));
            }

            RoundNumber++;
            roundElapsed = 0f;
            intermissionRemaining = 0f;
            roundLive = true;
            Debug.Log($"Elimination round {RoundNumber} started.", this);
        }

        private void CountLiving()
        {
            for (var team = 0; team < livingCounts.Length; team++)
            {
                livingCounts[team] = 0;
            }

            for (var index = 0; index < Roster.Count; index++)
            {
                var agent = Roster[index];
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

            var teams = Configuration == null
                ? null
                : Configuration.teams;
            for (var team = 0; team < roundWins.Length; team++)
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
                !EliminationRules.SharesTeam(
                    damageSource.TeamIndex,
                    eliminated.TeamIndex))
            {
                killerIndex = RosterIndex(damageSource);
            }

            combatStats.RecordElimination(
                RosterIndex(eliminated),
                killerIndex,
                roundElapsed);
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

        private EliminationModeConfiguration Configuration =>
            ModeConfiguration as EliminationModeConfiguration;

        private bool ResolveMapAndConfiguration()
        {
            if (!TryBindActiveMap(
                    "Elimination requires an active map from the Active Map manager."))
            {
                return false;
            }

            if (ModeConfiguration == null ||
                ModeConfiguration.map != ActiveMap.definition ||
                ModeConfiguration is not EliminationModeConfiguration)
            {
                SetModeConfiguration(
                    FindConfiguration(configuration => configuration is EliminationModeConfiguration));
            }

            if (ModeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{ActiveMap.definition.name}' has no Elimination configuration.",
                    ActiveMap);
                return false;
            }

            if (Configuration.teams == null ||
                Configuration.teams.Count <
                EliminationRules.RequiredTeamCount(ModeConfiguration.teamCount))
            {
                Debug.LogWarning(
                    "Elimination is missing a spawn for one or more teams. " +
                    "Those teams spawn at the map center.",
                    ModeConfiguration);
            }

            return true;
        }

        private void ApplyLoadedModeConfiguration()
        {
            var teamCount = EliminationRules.RequiredTeamCount(
                ModeConfiguration.teamCount);
            roundWinLimit = Mathf.Max(
                1,
                Configuration.roundWinLimit <= 0
                    ? EliminationRules.DefaultRoundWinLimit
                    : Configuration.roundWinLimit);
            roundSeconds = Mathf.Max(
                1f,
                Configuration.roundSeconds <= 0f
                    ? EliminationRules.DefaultRoundSeconds
                    : Configuration.roundSeconds);
            intermissionSeconds = Mathf.Max(0f, Configuration.intermissionSeconds);
            roundWins = new int[teamCount];
            livingCounts = new int[teamCount];
        }

        private Vector3 GetSpawnPosition(int teamIndex)
        {
            var teams = Configuration == null
                ? null
                : Configuration.teams;
            if (teams != null &&
                teamIndex >= 0 &&
                teamIndex < teams.Count &&
                teams[teamIndex] != null)
            {
                var point = teams[teamIndex];
                var offset = Random.insideUnitCircle * Mathf.Max(0f, point.radius);
                var localPosition = point.spawnPosition +
                                    new Vector3(offset.x, 0f, offset.y);
                return ResolveSpawn(
                    ActiveMap.WorldPoint(localPosition),
                    ActiveMap.WorldPoint(point.spawnPosition));
            }

            var center = ActiveMap.definition.WorldCenter;
            var worldCenter = ActiveMap.WorldPoint(center);
            return ResolveSpawn(worldCenter, worldCenter);
        }
    }
}
