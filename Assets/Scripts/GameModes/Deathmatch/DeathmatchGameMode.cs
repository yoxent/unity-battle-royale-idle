using System.Collections.Generic;
using BattleRoyale.Company;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.Maps;
using UnityEngine;

namespace BattleRoyale.GameModes.Deathmatch
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class DeathmatchGameMode : SimulationMatch
    {
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
        private FieldSnapshot companyPlayer;
        private FieldSnapshot companyOpponent;
        private BattleRoyaleAgentProfile[] companyPlayerProfiles;
        private BattleRoyaleAgentProfile[] companyOpponentProfiles;
        private readonly List<BattleRoyaleAgentProfile> runtimeProfiles = new();

        public const int CompanyPlayerTeam = 0;
        private float elapsedMatchTime;

        protected override string AgentRootName => "DeathmatchAgents";
        protected override string ProjectileRootName => "DeathmatchProjectiles";
        protected override string RuntimeProfileName => "Runtime Deathmatch Profile";

        public override bool Runs(MapGameMode mode) => mode == MapGameMode.Deathmatch;

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

            var emblem = default(FieldEmblem);
            if (teamIndex == CompanyPlayerTeam && companyPlayer != null)
            {
                emblem = companyPlayer.Emblem;
            }
            else if (teamIndex == 1 && companyOpponent != null)
            {
                emblem = companyOpponent.Emblem;
            }

            return SideColorRules.ForSide(emblem, teamIndex).AsColor();
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

        public bool TryArmCompanyMatch(
            FieldSnapshot player,
            FieldSnapshot opponent,
            BattleRoyaleAgentProfile[] playerProfiles,
            BattleRoyaleAgentProfile[] opponentProfiles)
        {
            if (player == null ||
                opponent == null ||
                player.Agents == null ||
                opponent.Agents == null ||
                playerProfiles == null ||
                opponentProfiles == null ||
                player.Agents.Length == 0 ||
                player.Agents.Length != opponent.Agents.Length ||
                playerProfiles.Length != player.Agents.Length ||
                opponentProfiles.Length != opponent.Agents.Length)
            {
                return false;
            }

            companyPlayer = player;
            companyOpponent = opponent;
            companyPlayerProfiles = playerProfiles;
            companyOpponentProfiles = opponentProfiles;
            return true;
        }

        protected override void StopRunningMatch()
        {
            ClearCompanyMatch();
        }

        public void ClearCompanyMatch()
        {
            for (var i = Roster.Count - 1; i >= 0; i--)
            {
                if (Roster[i] != null)
                {
                    Destroy(Roster[i].gameObject);
                }
            }

            Roster.Clear();
            pendingRespawns.Clear();
            combatStats.Clear();
            WinningTeam = -1;
            MatchStarted = false;
            companyPlayer = null;
            companyOpponent = null;
            companyPlayerProfiles = null;
            companyOpponentProfiles = null;
            DestroyRuntimeProfiles();
        }

        protected override void OnDestroy()
        {
            DestroyRuntimeProfiles();
            base.OnDestroy();
        }

        bool SpawnCompanySides(int teamCount)
        {
            var teamSize = Mathf.Max(1, ModeConfiguration.teamSize);
            if (teamCount < 2 || companyPlayer.Agents.Length != teamSize)
            {
                Debug.LogError(
                    "The company match does not match this Deathmatch's side size.",
                    this);
                return false;
            }

            SetTeamPlaystyle(companyPlayer.Stance);
            for (var teamIndex = 0; teamIndex < 2; teamIndex++)
            {
                var snapshot = teamIndex == CompanyPlayerTeam
                    ? companyPlayer
                    : companyOpponent;
                var profiles = teamIndex == CompanyPlayerTeam
                    ? companyPlayerProfiles
                    : companyOpponentProfiles;
                for (var index = 0; index < teamSize; index++)
                {
                    var runtime = FieldedAgentProfile.Create(
                        profiles[index],
                        snapshot.Agents[index].Rolls);
                    if (runtime == null)
                    {
                        Debug.LogError(
                            "A company agent is missing an archetype or a six-stat roll.",
                            this);
                        return false;
                    }

                    runtimeProfiles.Add(runtime);
                    var strategy = runtime.defaultStrategy;
                    var sideStance = snapshot.Stance;
                    var color = TeamColorAt(teamIndex);
                    SpawnAgent(
                        $"DM_T{teamIndex + 1}_{index + 1:00}_{strategy}",
                        GetSpawnPosition(teamIndex),
                        strategy,
                        true,
                        agent =>
                        {
                            agent.AssignTeam(teamIndex);
                            agent.SetTeamColor(color);
                            agent.AssignSidePlaystyle(sideStance);
                        },
                        runtime);
                }
            }

            return true;
        }

        void DestroyRuntimeProfiles()
        {
            for (var i = 0; i < runtimeProfiles.Count; i++)
            {
                if (runtimeProfiles[i] != null)
                {
                    Destroy(runtimeProfiles[i]);
                }
            }

            runtimeProfiles.Clear();
        }

        void DestroyRosterObjects()
        {
            for (var i = 0; i < Roster.Count; i++)
            {
                if (Roster[i] != null)
                {
                    Destroy(Roster[i].gameObject);
                }
            }
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
            if (companyPlayer != null)
            {
                DestroyRosterObjects();
            }

            Roster.Clear();
            pendingRespawns.Clear();
            WinningTeam = -1;
            elapsedMatchTime = 0f;
            combatStats.Clear();

            var teamCount = teamScores.Length;
            if (companyPlayer != null)
            {
                if (!SpawnCompanySides(teamCount))
                {
                    DestroyRosterObjects();
                    Roster.Clear();
                    DestroyRuntimeProfiles();
                    return false;
                }
            }
            else
            {
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
                            agent.SetTeamColor(TeamColorAt(teamIndex));
                        });
                }
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

            var teams = Configuration == null
                ? null
                : Configuration.teams;
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

        private DeathmatchModeConfiguration Configuration =>
            ModeConfiguration as DeathmatchModeConfiguration;

        private bool ResolveMapAndConfiguration()
        {
            if (!TryBindActiveMap(
                    "Deathmatch requires an active map from the Active Map manager."))
            {
                return false;
            }

            if (ModeConfiguration == null ||
                ModeConfiguration.map != ActiveMap.definition ||
                ModeConfiguration is not DeathmatchModeConfiguration)
            {
                SetModeConfiguration(
                    FindConfiguration(configuration => configuration is DeathmatchModeConfiguration));
            }

            if (ModeConfiguration == null)
            {
                Debug.LogError(
                    $"Map '{ActiveMap.definition.name}' has no Deathmatch configuration.",
                    ActiveMap);
                return false;
            }

            if (Configuration.teams == null ||
                Configuration.teams.Count <
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
                Configuration.scoreLimit <= 0
                    ? DeathmatchRules.DefaultScoreLimit
                    : Configuration.scoreLimit);
            respawnSeconds = Mathf.Max(0f, Configuration.respawnSeconds);
            teamScores = new int[teamCount];
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
                return SnapToNavigation(ActiveMap.WorldPoint(localPosition));
            }

            var center = ActiveMap.definition.WorldCenter;
            return SnapToNavigation(ActiveMap.WorldPoint(center));
        }
    }
}
