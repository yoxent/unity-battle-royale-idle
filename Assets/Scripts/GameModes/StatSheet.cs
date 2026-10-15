namespace BattleRoyale.GameModes
{
    public class StatSheetRow
    {
        public StatSheetRow(string displayName, int teamIndex, bool alive)
            : this(displayName, teamIndex, alive, true)
        {
        }

        protected StatSheetRow(
            string displayName,
            int teamIndex,
            bool alive,
            bool cleanBlank)
        {
            DisplayName = cleanBlank
                ? CleanName(displayName)
                : displayName ?? string.Empty;
            TeamIndex = teamIndex;
            Alive = alive;
        }

        public string DisplayName { get; }
        public int TeamIndex { get; }
        public bool Alive { get; }

        public static string CleanName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? "Unknown"
                : displayName.Trim();
        }
    }

    public readonly struct StatSheetElimination
    {
        public StatSheetElimination(
            bool hasEvent,
            bool hasActor,
            string actorName,
            int actorTeam,
            string subjectName,
            int subjectTeam)
        {
            HasEvent = hasEvent;
            HasActor = hasActor;
            ActorName = actorName ?? string.Empty;
            ActorTeam = actorTeam;
            SubjectName = subjectName ?? string.Empty;
            SubjectTeam = subjectTeam;
        }

        public bool HasEvent { get; }
        public bool HasActor { get; }
        public string ActorName { get; }
        public int ActorTeam { get; }
        public string SubjectName { get; }
        public int SubjectTeam { get; }

        public static StatSheetElimination None =>
            new(false, false, string.Empty, -1, string.Empty, -1);
    }

    public abstract class StatSheet
    {
        public StatSheetElimination LatestElimination { get; protected set; } =
            StatSheetElimination.None;

        public abstract int RowCount { get; }

        public abstract StatSheetRow RowAt(int index);

        protected void ClearElimination()
        {
            LatestElimination = StatSheetElimination.None;
        }

        protected void SetLatestElimination(StatSheetElimination elimination)
        {
            LatestElimination = elimination;
        }
    }
}
