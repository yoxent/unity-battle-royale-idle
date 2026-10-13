using System;
using System.Collections.Generic;

namespace BattleRoyale.GameModes.Deathmatch
{
    public readonly struct DeathmatchAgentLine
    {
        public readonly int RosterIndex;
        public readonly int TeamIndex;
        public readonly string DisplayName;
        public readonly int Kills;
        public readonly int Deaths;
        public readonly int Assists;
        public readonly float DamageDealt;
        public readonly int ShotsFired;
        public readonly int Hits;

        public DeathmatchAgentLine(
            int rosterIndex,
            int teamIndex,
            string displayName,
            int kills,
            int deaths,
            int assists,
            float damageDealt,
            int shotsFired,
            int hits)
        {
            RosterIndex = rosterIndex;
            TeamIndex = teamIndex;
            DisplayName = displayName ?? string.Empty;
            Kills = kills;
            Deaths = deaths;
            Assists = assists;
            DamageDealt = damageDealt;
            ShotsFired = shotsFired;
            Hits = hits;
        }

        public float Accuracy =>
            ShotsFired <= 0 ? 0f : (float)Hits / ShotsFired;
    }

    public readonly struct DeathmatchEventLine
    {
        public readonly bool HasEvent;
        public readonly bool HasActor;
        public readonly string Phrase;
        public readonly string ActorName;
        public readonly int ActorTeam;
        public readonly string SubjectName;
        public readonly int SubjectTeam;

        public DeathmatchEventLine(
            bool hasEvent,
            bool hasActor,
            string phrase,
            string actorName,
            int actorTeam,
            string subjectName,
            int subjectTeam)
        {
            HasEvent = hasEvent;
            HasActor = hasActor;
            Phrase = phrase ?? string.Empty;
            ActorName = actorName ?? string.Empty;
            ActorTeam = actorTeam;
            SubjectName = subjectName ?? string.Empty;
            SubjectTeam = subjectTeam;
        }

        public static DeathmatchEventLine None =>
            new(false, false, string.Empty, string.Empty, -1, string.Empty, -1);
    }

    public sealed class DeathmatchCombatStats
    {
        public const float AssistWindowSeconds = 5f;

        readonly List<AgentRow> agents = new();
        readonly List<string> teamNames = new();

        public DeathmatchEventLine LatestEvent { get; private set; } =
            DeathmatchEventLine.None;

        public int AgentCount => agents.Count;

        public void Clear()
        {
            agents.Clear();
            teamNames.Clear();
            LatestEvent = DeathmatchEventLine.None;
        }

        public void RegisterAgent(
            int rosterIndex,
            int teamIndex,
            string displayName)
        {
            if (rosterIndex < 0)
            {
                return;
            }

            while (agents.Count <= rosterIndex)
            {
                agents.Add(null);
            }

            agents[rosterIndex] = new AgentRow
            {
                TeamIndex = teamIndex,
                DisplayName = CleanName(displayName)
            };
        }

        public void SetTeamName(int teamIndex, string teamName)
        {
            if (teamIndex < 0)
            {
                return;
            }

            while (teamNames.Count <= teamIndex)
            {
                teamNames.Add(null);
            }

            teamNames[teamIndex] = string.IsNullOrWhiteSpace(teamName)
                ? DefaultTeamName(teamIndex)
                : teamName.Trim();
        }

        public string TeamName(int teamIndex)
        {
            if (teamIndex < 0 ||
                teamIndex >= teamNames.Count ||
                string.IsNullOrWhiteSpace(teamNames[teamIndex]))
            {
                return DefaultTeamName(teamIndex);
            }

            return teamNames[teamIndex];
        }

        public void RecordShot(int rosterIndex)
        {
            var agent = Find(rosterIndex);
            if (agent == null)
            {
                return;
            }

            agent.Shots++;
        }

        public void RecordHit(
            int attackerIndex,
            int victimIndex,
            float healthRemoved,
            float matchTime)
        {
            if (healthRemoved <= 0f || attackerIndex == victimIndex)
            {
                return;
            }

            var attacker = Find(attackerIndex);
            var victim = Find(victimIndex);
            if (attacker == null ||
                victim == null ||
                attacker.TeamIndex == victim.TeamIndex)
            {
                return;
            }

            attacker.Hits++;
            attacker.Damage += healthRemoved;
            victim.Marks.Add(new DamageMark(attackerIndex, matchTime));
        }

        public void RecordElimination(
            int victimIndex,
            int killerIndex,
            float matchTime)
        {
            var victim = Find(victimIndex);
            if (victim == null)
            {
                return;
            }

            victim.Deaths++;
            var killer = Find(killerIndex);
            if (killer == null || killer.TeamIndex == victim.TeamIndex)
            {
                LatestEvent = new DeathmatchEventLine(
                    true,
                    false,
                    victim.DisplayName + " died",
                    string.Empty,
                    -1,
                    victim.DisplayName,
                    victim.TeamIndex);
                return;
            }

            killer.Kills++;
            AwardAssists(victim, killerIndex, killer.TeamIndex, matchTime);
            LatestEvent = new DeathmatchEventLine(
                true,
                true,
                killer.DisplayName + " eliminated " + victim.DisplayName,
                killer.DisplayName,
                killer.TeamIndex,
                victim.DisplayName,
                victim.TeamIndex);
        }

        public DeathmatchAgentLine AgentAt(int rosterIndex)
        {
            var agent = Find(rosterIndex);
            if (agent == null)
            {
                return new DeathmatchAgentLine(
                    -1,
                    -1,
                    string.Empty,
                    0,
                    0,
                    0,
                    0f,
                    0,
                    0);
            }

            return ToLine(rosterIndex, agent);
        }

        public void CopyAgents(List<DeathmatchAgentLine> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            for (var index = 0; index < agents.Count; index++)
            {
                if (agents[index] == null)
                {
                    continue;
                }

                destination.Add(ToLine(index, agents[index]));
            }
        }

        public static float HealthRemoved(float currentHealth, float mitigatedDamage)
        {
            if (currentHealth <= 0f || mitigatedDamage <= 0f)
            {
                return 0f;
            }

            return Math.Min(currentHealth, mitigatedDamage);
        }

        public static string FormatClock(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f)
            {
                seconds = 0f;
            }

            var whole = (int)Math.Floor(seconds);
            var minutes = whole / 60;
            var remainder = whole % 60;
            return minutes.ToString("00") + ":" + remainder.ToString("00");
        }

        public static int RoundHalfUp(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0;
            }

            return (int)Math.Floor(value + 0.5f);
        }

        public static int AccuracyPercent(int hits, int shots)
        {
            if (shots <= 0 || hits <= 0)
            {
                return 0;
            }

            return RoundHalfUp(hits * 100f / shots);
        }

        static string DefaultTeamName(int teamIndex)
        {
            return "Team " + (teamIndex + 1);
        }

        static string CleanName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? "Unknown"
                : displayName.Trim();
        }

        AgentRow Find(int rosterIndex)
        {
            if (rosterIndex < 0 || rosterIndex >= agents.Count)
            {
                return null;
            }

            return agents[rosterIndex];
        }

        void AwardAssists(
            AgentRow victim,
            int killerIndex,
            int killerTeam,
            float matchTime)
        {
            var credited = new List<int>();
            for (var index = 0; index < victim.Marks.Count; index++)
            {
                var mark = victim.Marks[index];
                var age = matchTime - mark.Time;
                if (age < 0f || age > AssistWindowSeconds)
                {
                    continue;
                }

                if (mark.Attacker == killerIndex || Contains(credited, mark.Attacker))
                {
                    continue;
                }

                var attacker = Find(mark.Attacker);
                if (attacker == null || attacker.TeamIndex != killerTeam)
                {
                    continue;
                }

                attacker.Assists++;
                credited.Add(mark.Attacker);
            }
        }

        static bool Contains(List<int> values, int candidate)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        static DeathmatchAgentLine ToLine(int rosterIndex, AgentRow agent)
        {
            return new DeathmatchAgentLine(
                rosterIndex,
                agent.TeamIndex,
                agent.DisplayName,
                agent.Kills,
                agent.Deaths,
                agent.Assists,
                agent.Damage,
                agent.Shots,
                agent.Hits);
        }

        sealed class AgentRow
        {
            public int TeamIndex;
            public string DisplayName;
            public int Kills;
            public int Deaths;
            public int Assists;
            public int Shots;
            public int Hits;
            public float Damage;
            public readonly List<DamageMark> Marks = new();
        }

        readonly struct DamageMark
        {
            public readonly int Attacker;
            public readonly float Time;

            public DamageMark(int attacker, float time)
            {
                Attacker = attacker;
                Time = time;
            }
        }
    }
}
