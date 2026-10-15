using System.Collections.Generic;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.GameModes.Elimination
{
    public sealed class EliminationStatTracker : StatSheetView
    {
        [SerializeField] EliminationGameMode match;
        [SerializeField] GameObject board;
        [SerializeField] TMP_Text teamOneCaption;
        [SerializeField] TMP_Text teamOneScore;
        [SerializeField] TMP_Text matchTime;
        [SerializeField] TMP_Text teamTwoCaption;
        [SerializeField] TMP_Text teamTwoScore;
        [SerializeField] TMP_Text eventLine;
        [SerializeField] List<TMP_Text> teamOneNames;
        [SerializeField] List<TMP_Text> teamTwoNames;
        [SerializeField] List<Image> teamOneLogos;
        [SerializeField] List<Image> teamTwoLogos;
        [SerializeField] RectTransform teamOneRows;
        [SerializeField] RectTransform teamTwoRows;
        [SerializeField] DeathmatchStatRow rowPrefab;
        [SerializeField] GameObject tableRoot;
        [SerializeField] RectTransform teamOneTable;
        [SerializeField] RectTransform teamTwoTable;
        [SerializeField] DeathmatchStatRow tableRowPrefab;
        [SerializeField] Button statsButton;
        [SerializeField] TMP_Text statsButtonLabel;

        readonly List<DeathmatchAgentLine> lines = new();
        readonly List<DeathmatchStatRow> sideOne = new();
        readonly List<DeathmatchStatRow> sideTwo = new();
        readonly List<DeathmatchStatRow> tableOne = new();
        readonly List<DeathmatchStatRow> tableTwo = new();

        bool configured;
        bool listening;
        bool reportedTeamCount;
        bool tableOpen;

        public override bool Matches(SimulationMatch candidate)
        {
            return candidate is EliminationGameMode;
        }

        public override SimulationMatch BoundMatch => match;

        public override void Bind(SimulationMatch candidate)
        {
            if (candidate is not EliminationGameMode elimination)
            {
                return;
            }

            match = elimination;
            if (isActiveAndEnabled)
            {
                Begin();
            }
        }

        void OnEnable()
        {
            Begin();
        }

        void Begin()
        {
            if (match == null)
            {
                configured = false;
                return;
            }

            configured = HasReferences();
            if (!configured || !Application.isPlaying || listening)
            {
                return;
            }

            statsButton.onClick.AddListener(ToggleTable);
            listening = true;
            tableOpen = false;
            tableRoot.SetActive(false);
            ApplyButtonLabel();
        }

        void OnDisable()
        {
            if (!listening)
            {
                return;
            }

            statsButton.onClick.RemoveListener(ToggleTable);
            listening = false;
        }

        void Update()
        {
            if (!Application.isPlaying || !configured)
            {
                return;
            }

            if (!match.IsMatchStarted)
            {
                board.SetActive(false);
                return;
            }

            if (match.RoundWins.Count != 2)
            {
                if (!reportedTeamCount)
                {
                    reportedTeamCount = true;
                    Debug.LogError(
                        "The elimination stat tracker is built for exactly two teams.",
                        this);
                }

                board.SetActive(false);
                return;
            }

            board.SetActive(true);
            Refresh();
        }

        void ToggleTable()
        {
            tableOpen = !tableOpen;
            tableRoot.SetActive(tableOpen);
            ApplyButtonLabel();
        }

        void ApplyButtonLabel()
        {
            if (statsButtonLabel != null)
            {
                statsButtonLabel.text = tableOpen ? "Close" : "Stats";
            }
        }

        void Refresh()
        {
            var stats = match.CombatStats;
            var intermission = match.IntermissionSecondsRemaining;
            var clock = intermission > 0f
                ? intermission
                : match.RoundSecondsRemaining;
            stats.NoteMatchClock(clock);
            NoteLiving(stats);
            teamOneCaption.text = stats.TeamName(0);
            teamOneCaption.color = match.TeamColorAt(0);
            teamOneScore.text = match.RoundWins[0].ToString();
            matchTime.text = intermission > 0f
                ? "INT " + DeathmatchCombatStats.FormatClock(clock)
                : DeathmatchCombatStats.FormatClock(clock);
            teamTwoCaption.text = stats.TeamName(1);
            teamTwoCaption.color = match.TeamColorAt(1);
            teamTwoScore.text = match.RoundWins[1].ToString();
            ShowTeamNames(teamOneNames, stats.TeamName(0), match.TeamColorAt(0));
            ShowTeamNames(teamTwoNames, stats.TeamName(1), match.TeamColorAt(1));
            ShowEvent(stats.LatestEvent);
            stats.CopyAgents(lines);
            var one = 0;
            var two = 0;
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (line.TeamIndex == 0)
                {
                    ShowRow(sideOne, teamOneRows, rowPrefab, one, line, false);
                    ShowRow(tableOne, teamOneTable, tableRowPrefab, one, line, true);
                    one++;
                }
                else if (line.TeamIndex == 1)
                {
                    ShowRow(sideTwo, teamTwoRows, rowPrefab, two, line, false);
                    ShowRow(tableTwo, teamTwoTable, tableRowPrefab, two, line, true);
                    two++;
                }
            }

            HideSpare(sideOne, one);
            HideSpare(sideTwo, two);
            HideSpare(tableOne, one);
            HideSpare(tableTwo, two);
        }

        void NoteLiving(DeathmatchCombatStats stats)
        {
            var agents = match.Agents;
            for (var index = 0; index < stats.AgentCount; index++)
            {
                var agent = index < agents.Count ? agents[index] : null;
                stats.NoteAlive(index, agent != null && agent.IsAlive);
            }
        }

        void ShowEvent(DeathmatchEventLine line)
        {
            var chip = eventLine.transform.parent.gameObject;
            if (!line.HasEvent)
            {
                chip.SetActive(false);
                return;
            }

            chip.SetActive(true);
            if (!line.HasActor)
            {
                eventLine.text = Paint(
                    line.SubjectName,
                    match.TeamColorAt(line.SubjectTeam)) + " died";
                return;
            }

            eventLine.text = Paint(
                    line.ActorName,
                    match.TeamColorAt(line.ActorTeam))
                + " eliminated "
                + Paint(
                    line.SubjectName,
                    match.TeamColorAt(line.SubjectTeam));
        }

        void ShowRow(
            List<DeathmatchStatRow> pool,
            RectTransform parent,
            DeathmatchStatRow prefab,
            int slot,
            DeathmatchAgentLine line,
            bool breakdown)
        {
            var row = Slot(pool, parent, prefab, slot);
            row.gameObject.SetActive(true);
            row.transform.SetSiblingIndex(slot);
            row.Show(
                line.DisplayName,
                match.TeamColorAt(line.TeamIndex),
                line.Alive,
                line.Kills,
                line.Deaths,
                line.Assists,
                DeathmatchCombatStats.RoundHalfUp(line.DamageDealt),
                DeathmatchCombatStats.AccuracyPercent(line.Hits, line.ShotsFired),
                breakdown,
                0);
        }

        static DeathmatchStatRow Slot(
            List<DeathmatchStatRow> pool,
            RectTransform parent,
            DeathmatchStatRow prefab,
            int slot)
        {
            while (pool.Count <= slot)
            {
                pool.Add(Instantiate(prefab, parent));
            }

            return pool[slot];
        }

        static void HideSpare(List<DeathmatchStatRow> pool, int used)
        {
            for (var index = used; index < pool.Count; index++)
            {
                if (pool[index] != null)
                {
                    pool[index].gameObject.SetActive(false);
                }
            }
        }

        static string Paint(string agentName, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">"
                + agentName + "</color>";
        }

        bool HasReferences()
        {
            return Require(match, "match") &&
                   Require(board, "board") &&
                   Require(teamOneCaption, "team one caption") &&
                   Require(teamOneScore, "team one score") &&
                   Require(matchTime, "match time") &&
                   Require(teamTwoCaption, "team two caption") &&
                   Require(teamTwoScore, "team two score") &&
                   Require(eventLine, "event line") &&
                   RequireEntries(teamOneNames, "team one name") &&
                   RequireEntries(teamTwoNames, "team two name") &&
                   RequireEntries(teamOneLogos, "team one logo") &&
                   RequireEntries(teamTwoLogos, "team two logo") &&
                   Require(teamOneRows, "team one rows") &&
                   Require(teamTwoRows, "team two rows") &&
                   Require(rowPrefab, "row prefab") &&
                   Require(tableRoot, "table") &&
                   Require(teamOneTable, "team one table") &&
                   Require(teamTwoTable, "team two table") &&
                   Require(tableRowPrefab, "table row prefab") &&
                   Require(statsButton, "stats button") &&
                   Require(statsButtonLabel, "stats button label");
        }

        bool Require(Object reference, string label)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                "Elimination stat tracker is missing its " + label + ".",
                this);
            return false;
        }

        static void ShowTeamNames(List<TMP_Text> labels, string teamName, Color color)
        {
            for (var index = 0; index < labels.Count; index++)
            {
                labels[index].text = teamName;
                labels[index].color = color;
            }
        }

        bool RequireEntries<T>(List<T> list, string label) where T : Object
        {
            if (list != null && list.Count > 0)
            {
                var missing = false;
                for (var index = 0; index < list.Count; index++)
                {
                    if (list[index] == null)
                    {
                        missing = true;
                    }
                }

                if (!missing)
                {
                    return true;
                }
            }

            Debug.LogError(
                "Elimination stat tracker is missing its " + label + ".",
                this);
            return false;
        }
    }
}
