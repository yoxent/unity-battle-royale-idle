using System.Collections.Generic;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.GameModes.Elimination;
using BattleRoyale.Maps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.GameModes
{
    public sealed class MatchPreviewStatView : MonoBehaviour
    {
        [SerializeField] private global::BattleRoyale.Maps.SimulationStage stage;
        [SerializeField] private GameObject scoresRoot;
        [SerializeField] private GameObject teamOne;
        [SerializeField] private TMP_Text teamOneCaption;
        [SerializeField] private TMP_Text teamOneScore;
        [SerializeField] private TMP_Text centerCaption;
        [SerializeField] private TMP_Text centerValue;
        [SerializeField] private GameObject teamTwo;
        [SerializeField] private TMP_Text teamTwoCaption;
        [SerializeField] private TMP_Text teamTwoScore;
        [SerializeField] private GameObject eventChip;
        [SerializeField] private TMP_Text eventLine;
        [SerializeField] private RectTransform roster;
        [SerializeField] private TMP_Text modeLabel;
        [SerializeField] private TMP_Text teamOneHeader;
        [SerializeField] private TMP_Text teamTwoHeader;
        [SerializeField] private RectTransform rows;
        [SerializeField] private DeathmatchStatRow rowPrefab;
        [SerializeField] private DeathmatchStatRow tableRowPrefab;
        [SerializeField] private BattleRoyaleStatRow aliveRowPrefab;
        [SerializeField] private Button statsButton;
        [SerializeField] private TMP_Text statsButtonLabel;

        readonly MatchStatBoard board = new();
        readonly List<bool> alive = new();
        readonly List<int> respawnSeconds = new();
        readonly List<DeathmatchStatRow> compactRows = new();
        readonly List<DeathmatchStatRow> tableRows = new();
        readonly List<BattleRoyaleStatRow> aliveRows = new();

        bool configured;
        bool listening;
        bool tableOpen;
        bool holdFullTable;
        bool reportedTeamCount;

        void OnEnable()
        {
            configured = HasReferences();
            if (!configured || !Application.isPlaying || listening)
            {
                return;
            }

            statsButton.onClick.AddListener(ToggleTable);
            listening = true;
            tableOpen = false;
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

            var match = stage.WatchedMatch;
            if (match is DeathmatchGameMode deathmatch)
            {
                FillPresence(deathmatch, deathmatch.CombatStats, true);
                DeathmatchStatTracker.Write(
                    board,
                    deathmatch.CombatStats,
                    deathmatch.IsMatchStarted,
                    deathmatch.TeamScores,
                    deathmatch.ElapsedMatchTime,
                    alive,
                    respawnSeconds);
            }
            else if (match is EliminationGameMode elimination)
            {
                FillPresence(elimination, elimination.CombatStats, false);
                var intermission = elimination.IntermissionSecondsRemaining;
                var clock = intermission > 0f
                    ? intermission
                    : elimination.RoundSecondsRemaining;
                EliminationStatTracker.Write(
                    board,
                    elimination.CombatStats,
                    elimination.IsMatchStarted,
                    elimination.RoundWins,
                    clock,
                    intermission > 0f,
                    alive);
            }
            else if (match is BattleRoyaleGameMode battleRoyale)
            {
                BattleRoyaleStatTracker.Write(
                    board,
                    battleRoyale.IsMatchStarted,
                    battleRoyale.AliveCount,
                    battleRoyale.RosterOrder);
            }
            else
            {
                board.Clear();
            }

            Paint(match);
        }

        public void HoldFullTable()
        {
            holdFullTable = true;
            tableOpen = true;
            if (statsButton != null)
            {
                statsButton.gameObject.SetActive(false);
            }
        }

        public void ReleaseFullTable()
        {
            holdFullTable = false;
            tableOpen = false;
            ApplyButtonLabel();
        }

        void ToggleTable()
        {
            if (holdFullTable)
            {
                return;
            }

            tableOpen = !tableOpen;
            ApplyButtonLabel();
        }

        void ApplyButtonLabel()
        {
            statsButtonLabel.text = tableOpen ? "Close" : "Stats";
        }

        void FillPresence(SimulationMatch match, DeathmatchCombatStats stats, bool respawn)
        {
            alive.Clear();
            respawnSeconds.Clear();
            if (stats == null)
            {
                return;
            }

            var agents = match.Agents;
            for (var index = 0; index < stats.AgentCount; index++)
            {
                var agent = agents != null && index < agents.Count ? agents[index] : null;
                var living = agent != null && agent.IsAlive;
                alive.Add(living);
                var seconds = 0;
                if (respawn && !living && agent != null && match is DeathmatchGameMode deathmatch)
                {
                    seconds = Mathf.CeilToInt(deathmatch.RespawnSecondsRemaining(agent));
                }

                respawnSeconds.Add(seconds);
            }
        }

        void Paint(SimulationMatch match)
        {
            PaintMode(match);
            if (board.RejectedTeamCount)
            {
                if (!reportedTeamCount)
                {
                    reportedTeamCount = true;
                    Debug.LogError(
                        "The match preview stat board is built for exactly two teams.",
                        this);
                }

                Hide();
                return;
            }

            reportedTeamCount = false;
            if (!board.Visible)
            {
                Hide();
                return;
            }

            scoresRoot.SetActive(true);
            teamOne.SetActive(board.Teams);
            teamTwo.SetActive(board.Teams);
            if (board.Teams)
            {
                teamOneCaption.text = board.LeftName;
                teamOneCaption.color = TeamColor(match, 0);
                teamOneScore.text = board.LeftScore.ToString();
                teamTwoCaption.text = board.RightName;
                teamTwoCaption.color = TeamColor(match, 1);
                teamTwoScore.text = board.RightScore.ToString();
            }

            var showCaption = !string.IsNullOrEmpty(board.CenterCaption);
            centerCaption.gameObject.SetActive(showCaption);
            if (showCaption)
            {
                centerCaption.text = board.CenterCaption;
            }

            centerValue.text = board.CenterValue;
            PaintEvent(match);
            PaintRows(match);
        }

        void Hide()
        {
            scoresRoot.SetActive(false);
            eventChip.SetActive(false);
            statsButton.gameObject.SetActive(false);
            tableOpen = false;
            ApplyButtonLabel();
            HideTeamHeaders();
            HideSpare(compactRows, 0);
            HideSpare(tableRows, 0);
            HideSpare(aliveRows, 0);
        }

        void PaintEvent(SimulationMatch match)
        {
            if (!board.ShowEvent)
            {
                eventChip.SetActive(false);
                return;
            }

            eventChip.SetActive(true);
            if (!board.EventHasActor)
            {
                eventLine.text = PaintName(
                    board.SubjectName,
                    TeamColor(match, board.SubjectTeam)) + " died";
                return;
            }

            eventLine.text = PaintName(board.ActorName, TeamColor(match, board.ActorTeam))
                + " eliminated "
                + PaintName(board.SubjectName, TeamColor(match, board.SubjectTeam));
        }

        void PaintRows(SimulationMatch match)
        {
            var count = board.Rows.Count;
            if (count == 0)
            {
                statsButton.gameObject.SetActive(false);
                HideTeamHeaders();
                HideSpare(compactRows, 0);
                HideSpare(tableRows, 0);
                HideSpare(aliveRows, 0);
                return;
            }

            var combat = board.Teams;
            if (holdFullTable && combat)
            {
                tableOpen = true;
            }

            statsButton.gameObject.SetActive(combat && !holdFullTable);
            if (!combat)
            {
                tableOpen = false;
                ApplyButtonLabel();
            }

            HideTeamHeaders();
            var sibling = 0;
            var shownTeam = -1;
            for (var index = 0; index < count; index++)
            {
                var row = board.Rows[index];
                if (combat && row.TeamIndex != shownTeam)
                {
                    shownTeam = row.TeamIndex;
                    sibling = PlaceTeamHeader(match, shownTeam, sibling);
                }

                if (combat)
                {
                    var compact = Slot(compactRows, rowPrefab, index);
                    compact.gameObject.SetActive(!tableOpen);
                    compact.transform.SetSiblingIndex(sibling);
                    sibling++;
                    ShowCombat(compact, match, row, false);
                    var table = Slot(tableRows, tableRowPrefab, index);
                    table.gameObject.SetActive(tableOpen);
                    table.transform.SetSiblingIndex(sibling);
                    sibling++;
                    ShowCombat(table, match, row, true);
                }
                else
                {
                    var aliveRow = Slot(aliveRows, aliveRowPrefab, index);
                    aliveRow.gameObject.SetActive(true);
                    aliveRow.transform.SetSiblingIndex(sibling);
                    sibling++;
                    aliveRow.Show(
                        row.Name,
                        TeamColor(match, row.TeamIndex),
                        row.Alive);
                }
            }

            if (combat)
            {
                HideSpare(compactRows, count);
                HideSpare(tableRows, count);
                HideSpare(aliveRows, 0);
            }
            else
            {
                HideSpare(compactRows, 0);
                HideSpare(tableRows, 0);
                HideSpare(aliveRows, count);
            }
        }

        void PaintMode(SimulationMatch match)
        {
            if (match is DeathmatchGameMode)
            {
                modeLabel.text = MapGameModes.Label(MapGameMode.Deathmatch);
                return;
            }

            if (match is EliminationGameMode)
            {
                modeLabel.text = MapGameModes.Label(MapGameMode.Elimination);
                return;
            }

            if (match is BattleRoyaleGameMode)
            {
                modeLabel.text = MapGameModes.Label(MapGameMode.BattleRoyale);
                return;
            }

            modeLabel.text = string.Empty;
        }

        int PlaceTeamHeader(SimulationMatch match, int teamIndex, int sibling)
        {
            var header = teamIndex == 0 ? teamOneHeader : teamTwoHeader;
            var teamName = teamIndex == 0 ? board.LeftName : board.RightName;
            if (header == null || teamIndex > 1)
            {
                return sibling;
            }

            header.gameObject.SetActive(true);
            header.text = teamName;
            header.color = TeamColor(match, teamIndex);
            header.rectTransform.SetSiblingIndex(sibling);
            return sibling + 1;
        }

        void HideTeamHeaders()
        {
            teamOneHeader.gameObject.SetActive(false);
            teamTwoHeader.gameObject.SetActive(false);
        }

        void ShowCombat(
            DeathmatchStatRow row,
            SimulationMatch match,
            MatchStatRow line,
            bool breakdown)
        {
            row.Show(
                line.Name,
                TeamColor(match, line.TeamIndex),
                line.Alive,
                line.Kills,
                line.Deaths,
                line.Assists,
                line.Damage,
                line.Accuracy,
                breakdown,
                line.RespawnSeconds);
        }

        DeathmatchStatRow Slot(List<DeathmatchStatRow> pool, DeathmatchStatRow prefab, int index)
        {
            while (pool.Count <= index)
            {
                pool.Add(Instantiate(prefab, rows));
            }

            return pool[index];
        }

        BattleRoyaleStatRow Slot(List<BattleRoyaleStatRow> pool, BattleRoyaleStatRow prefab, int index)
        {
            while (pool.Count <= index)
            {
                pool.Add(Instantiate(prefab, rows));
            }

            return pool[index];
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

        static void HideSpare(List<BattleRoyaleStatRow> pool, int used)
        {
            for (var index = used; index < pool.Count; index++)
            {
                if (pool[index] != null)
                {
                    pool[index].gameObject.SetActive(false);
                }
            }
        }

        static Color TeamColor(SimulationMatch match, int teamIndex)
        {
            if (teamIndex < 0)
            {
                return Color.white;
            }

            if (match is DeathmatchGameMode deathmatch)
            {
                return deathmatch.TeamColorAt(teamIndex);
            }

            if (match is EliminationGameMode elimination)
            {
                return elimination.TeamColorAt(teamIndex);
            }

            if (match is BattleRoyaleGameMode battleRoyale)
            {
                return battleRoyale.TeamColorAt(teamIndex);
            }

            return Color.white;
        }

        static string PaintName(string agentName, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">"
                + agentName + "</color>";
        }

        bool HasReferences()
        {
            return Require(stage, "simulation stage") &&
                   Require(scoresRoot, "scores") &&
                   Require(teamOne, "team one") &&
                   Require(teamOneCaption, "team one caption") &&
                   Require(teamOneScore, "team one score") &&
                   Require(centerCaption, "time caption") &&
                   Require(centerValue, "match time") &&
                   Require(teamTwo, "team two") &&
                   Require(teamTwoCaption, "team two caption") &&
                   Require(teamTwoScore, "team two score") &&
                   Require(eventChip, "event line") &&
                   Require(eventLine, "event label") &&
                   Require(roster, "roster") &&
                   Require(modeLabel, "mode label") &&
                   Require(teamOneHeader, "team one header") &&
                   Require(teamTwoHeader, "team two header") &&
                   Require(rows, "rows") &&
                   Require(rowPrefab, "row prefab") &&
                   Require(tableRowPrefab, "table row prefab") &&
                   Require(aliveRowPrefab, "alive row prefab") &&
                   Require(statsButton, "stats button") &&
                   Require(statsButtonLabel, "stats button label");
        }

        bool Require(Object reference, string label)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError("Match preview stats are missing the " + label + ".", this);
            return false;
        }
    }
}
