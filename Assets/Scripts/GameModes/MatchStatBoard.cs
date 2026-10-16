using System.Collections.Generic;

namespace BattleRoyale.GameModes
{
    public sealed class MatchStatRow
    {
        public MatchStatRow(
            string name,
            int teamIndex,
            bool alive,
            bool combat,
            int kills,
            int deaths,
            int assists,
            int damage,
            int accuracy,
            int respawnSeconds)
        {
            Name = name ?? string.Empty;
            TeamIndex = teamIndex;
            Alive = alive;
            Combat = combat;
            Kills = kills;
            Deaths = deaths;
            Assists = assists;
            Damage = damage;
            Accuracy = accuracy;
            RespawnSeconds = respawnSeconds;
        }

        public string Name { get; }
        public int TeamIndex { get; }
        public bool Alive { get; }
        public bool Combat { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public int Assists { get; }
        public int Damage { get; }
        public int Accuracy { get; }
        public int RespawnSeconds { get; }
    }

    public sealed class MatchStatBoard
    {
        readonly List<MatchStatRow> rows = new();

        public bool Visible { get; private set; }
        public bool Teams { get; private set; }
        public bool RejectedTeamCount { get; private set; }
        public string LeftName { get; private set; } = string.Empty;
        public int LeftScore { get; private set; }
        public string RightName { get; private set; } = string.Empty;
        public int RightScore { get; private set; }
        public string CenterCaption { get; private set; } = string.Empty;
        public string CenterValue { get; private set; } = string.Empty;
        public bool ShowEvent { get; private set; }
        public bool EventHasActor { get; private set; }
        public string ActorName { get; private set; } = string.Empty;
        public int ActorTeam { get; private set; } = -1;
        public string SubjectName { get; private set; } = string.Empty;
        public int SubjectTeam { get; private set; } = -1;
        public IReadOnlyList<MatchStatRow> Rows => rows;

        public void Clear()
        {
            Visible = false;
            Teams = false;
            RejectedTeamCount = false;
            LeftName = string.Empty;
            LeftScore = 0;
            RightName = string.Empty;
            RightScore = 0;
            CenterCaption = string.Empty;
            CenterValue = string.Empty;
            ShowEvent = false;
            EventHasActor = false;
            ActorName = string.Empty;
            ActorTeam = -1;
            SubjectName = string.Empty;
            SubjectTeam = -1;
            rows.Clear();
        }

        public void ShowTeams(
            string leftName,
            int leftScore,
            string rightName,
            int rightScore,
            string centerValue)
        {
            Visible = true;
            Teams = true;
            LeftName = leftName ?? string.Empty;
            LeftScore = leftScore;
            RightName = rightName ?? string.Empty;
            RightScore = rightScore;
            CenterCaption = "TIME";
            CenterValue = centerValue ?? string.Empty;
        }

        public void ShowAlive(string centerValue)
        {
            Visible = true;
            Teams = false;
            CenterCaption = string.Empty;
            CenterValue = centerValue ?? string.Empty;
        }

        public void RejectTeams()
        {
            Clear();
            RejectedTeamCount = true;
        }

        public void SetEvent(
            bool hasEvent,
            bool hasActor,
            string actorName,
            int actorTeam,
            string subjectName,
            int subjectTeam)
        {
            ShowEvent = hasEvent;
            EventHasActor = hasActor;
            ActorName = actorName ?? string.Empty;
            ActorTeam = actorTeam;
            SubjectName = subjectName ?? string.Empty;
            SubjectTeam = subjectTeam;
        }

        public void AddRow(MatchStatRow row)
        {
            if (row != null)
            {
                rows.Add(row);
            }
        }
    }
}
