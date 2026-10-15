using BattleRoyale.Events;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed class CompanyDayController : MonoBehaviour
    {
        [SerializeField] private CompanyDaySetup setup;
        [SerializeField] private DeathmatchGameMode match;
        [SerializeField] private UIPageTransition pageTransition;
        [SerializeField] private UIPage dashboard;
        [SerializeField] private UIPage modeSelect;
        [SerializeField] private UIPage matchSetupPage;
        [SerializeField] private UIPage resultPage;
        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text rosterLabel;
        [SerializeField] private TMP_Text stanceLabel;
        [SerializeField] private TMP_Text opponentLabel;
        [SerializeField] private TMP_Text windowLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button deployButton;

        EconomyTuning tuning;
        int balance;
        int reserved;
        int companyDate;
        int fans;
        int spent;
        float dayProgress;
        float windowRemaining;
        TeamPlaystyle stance;
        TournamentEntryPhase phase;
        TournamentResult result;
        int[] waived;
        AgentAvailability[] availability;
        bool resultApplied;
        bool ready;

        void OnEnable()
        {
            EventBus.Subscribe<PopupClosed>(OnPopupClosed);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PopupClosed>(OnPopupClosed);
        }

        void Start()
        {
            if (setup == null || match == null)
            {
                Debug.LogError("Company day needs a setup and a Deathmatch.", this);
                return;
            }

            if (pageTransition == null || dashboard == null || modeSelect == null ||
                matchSetupPage == null || resultPage == null)
            {
                Debug.LogError("Company day needs its page transition and pages.", this);
                return;
            }

            if (setup.playerAgents == null || setup.opponentAgents == null)
            {
                Debug.LogError("Company day needs a roster and an opponent.", this);
                return;
            }

            tuning = EconomyTuning.Illustration();
            balance = tuning.StartingGold;
            fans = tuning.FanPayCap;
            companyDate = 1;
            stance = setup.playerStance;
            phase = TournamentEntryPhase.Open;
            waived = new int[setup.playerAgents.Length];
            availability = new AgentAvailability[setup.playerAgents.Length];
            for (var i = 0; i < setup.playerAgents.Length; i++)
            {
                waived[i] = setup.playerAgents[i].waivedAppearances;
                availability[i] = new AgentAvailability { Id = i };
            }

            ready = true;
            Show(dashboard);
            RefreshLabels();
        }

        void Update()
        {
            if (!ready)
            {
                return;
            }

            var elapsed = Time.unscaledDeltaTime;
            AdvanceIdle(elapsed);
            if (phase == TournamentEntryPhase.Window)
            {
                AdvanceWindow(elapsed);
            }

            if (phase == TournamentEntryPhase.Running &&
                !resultApplied &&
                match.IsMatchStarted &&
                match.WinningTeam >= 0)
            {
                ApplyPlacement();
            }

            RefreshLabels();
        }

        public void ShowModeSelect()
        {
            if (!ready || phase == TournamentEntryPhase.Running)
            {
                return;
            }

            Show(modeSelect);
        }

        public void ShowMatchSetup()
        {
            if (!ready || phase == TournamentEntryPhase.Running)
            {
                return;
            }

            Show(matchSetupPage);
        }

        public void ShowDashboard()
        {
            if (!ready || phase == TournamentEntryPhase.Window ||
                phase == TournamentEntryPhase.Running)
            {
                return;
            }

            Show(dashboard);
        }

        public void SetAggressive()
        {
            SetStance(TeamPlaystyle.Aggressive);
        }

        public void SetNeutral()
        {
            SetStance(TeamPlaystyle.Neutral);
        }

        public void SetDefensive()
        {
            SetStance(TeamPlaystyle.Defensive);
        }

        public void RequestDeploy()
        {
            if (!ready || phase != TournamentEntryPhase.Open)
            {
                return;
            }

            EventBus.Publish(new PopupRequested(
                PopupId.EntryCommit,
                PopupContent.EntryCommit()));
        }

        public void AbortEntry()
        {
            if (!ready || phase != TournamentEntryPhase.Window)
            {
                return;
            }

            if (!TournamentEconomyRules.TryAbort(CaptureEntry(), out var aborted))
            {
                return;
            }

            ApplyEntry(aborted);
            var ids = AgentIds();
            if (AgentAvailabilityRules.TryAbort(availability, ids, out var freed))
            {
                availability = freed;
            }

            Show(matchSetupPage);
        }

        public void DismissResult()
        {
            if (!ready || phase != TournamentEntryPhase.Resolved)
            {
                return;
            }

            match.ClearCompanyMatch();
            phase = TournamentEntryPhase.Open;
            result = TournamentResult.None;
            reserved = 0;
            resultApplied = false;
            spent = 0;
            Show(dashboard);
        }

        void OnPopupClosed(PopupClosed closed)
        {
            if (!ready || closed.Id != PopupId.EntryCommit ||
                closed.Result != PopupResult.Confirmed ||
                phase != TournamentEntryPhase.Open)
            {
                return;
            }

            if (!TournamentEconomyRules.TryEnter(
                    CaptureEntry(),
                    tuning,
                    tuning.CheapEntranceFee,
                    FieldedRoster(),
                    out var entered))
            {
                Debug.LogError("The company cannot cover this entry.", this);
                return;
            }

            ApplyEntry(entered);
            var ids = AgentIds();
            if (AgentAvailabilityRules.TryDeploy(availability, ids, out var busy))
            {
                availability = busy;
            }

            Show(matchSetupPage);
        }

        void AdvanceIdle(float elapsed)
        {
            var income = new IdleIncomeState
            {
                Balance = balance,
                Fans = fans,
                DayProgress = dayProgress,
            };
            if (!IdleIncomeRules.TryAdvance(income, tuning, elapsed, false, out var next))
            {
                return;
            }

            balance = next.Balance;
            fans = next.Fans;
            dayProgress = next.DayProgress;
        }

        void AdvanceWindow(float elapsed)
        {
            var before = balance;
            if (!TournamentEconomyRules.TryAdvance(CaptureEntry(), elapsed, out var next))
            {
                return;
            }

            ApplyEntry(next);
            if (next.Phase != TournamentEntryPhase.Running)
            {
                return;
            }

            spent = before - balance;
            StartCompanyMatch();
        }

        void StartCompanyMatch()
        {
            if (!TryBuildSnapshot(
                    setup.playerName,
                    setup.playerEmblem,
                    stance,
                    setup.playerAgents,
                    out var player,
                    out var playerProfiles) ||
                !TryBuildSnapshot(
                    setup.opponentName,
                    setup.opponentEmblem,
                    setup.opponentStance,
                    setup.opponentAgents,
                    out var opponent,
                    out var opponentProfiles))
            {
                Debug.LogError("The company match is missing a side.", this);
                return;
            }

            if (!match.TryArmCompanyMatch(
                    player,
                    opponent,
                    playerProfiles,
                    opponentProfiles) ||
                !match.BeginMatch())
            {
                Debug.LogError("Deathmatch did not start from the snapshots.", this);
                return;
            }

            HidePages();
        }

        void ApplyPlacement()
        {
            var placement = match.WinningTeam == DeathmatchGameMode.CompanyPlayerTeam
                ? TournamentResult.First
                : TournamentResult.Last;
            var prize = placement == TournamentResult.First
                ? tuning.CheapFirstPrize
                : tuning.CheapLastPrize;
            if (!TournamentEconomyRules.TryApplyResult(
                    CaptureEntry(),
                    placement,
                    prize,
                    out var resolved))
            {
                return;
            }

            ApplyEntry(resolved);
            var ids = AgentIds();
            if (AgentAvailabilityRules.TryFinish(availability, ids, out var freed))
            {
                availability = freed;
            }

            resultApplied = true;
            if (resultLabel != null)
            {
                var net = prize - spent;
                resultLabel.text =
                    $"{placement}\nPrize {prize}\nFee and upkeep {spent}\nNet {net}\nBalance {balance}";
            }

            Show(resultPage);
        }

        void SetStance(TeamPlaystyle next)
        {
            if (!ready || phase == TournamentEntryPhase.Running)
            {
                return;
            }

            stance = next;
        }

        TournamentEntryState CaptureEntry()
        {
            return new TournamentEntryState
            {
                Balance = balance,
                Reserved = reserved,
                CompanyDate = companyDate,
                Phase = phase,
                Result = result,
                WindowRemaining = windowRemaining,
                Agents = FieldedRoster(),
            };
        }

        void ApplyEntry(TournamentEntryState next)
        {
            balance = next.Balance;
            reserved = next.Reserved;
            companyDate = next.CompanyDate;
            phase = next.Phase;
            result = next.Result;
            windowRemaining = next.WindowRemaining;
            if (next.Agents == null)
            {
                return;
            }

            for (var i = 0; i < next.Agents.Length && i < waived.Length; i++)
            {
                waived[i] = next.Agents[i].WaivedAppearances;
            }
        }

        FieldedAgent[] FieldedRoster()
        {
            var fielded = new FieldedAgent[waived.Length];
            for (var i = 0; i < waived.Length; i++)
            {
                fielded[i] = new FieldedAgent
                {
                    Rarity = setup.playerAgents[i].rarity,
                    WaivedAppearances = waived[i],
                };
            }

            return fielded;
        }

        int[] AgentIds()
        {
            var ids = new int[availability.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = availability[i].Id;
            }

            return ids;
        }

        static bool TryBuildSnapshot(
            string companyName,
            FieldEmblem emblem,
            TeamPlaystyle sideStance,
            AuthoredAgent[] agents,
            out FieldSnapshot snapshot,
            out BattleRoyaleAgentProfile[] profiles)
        {
            snapshot = null;
            profiles = null;
            if (agents == null)
            {
                return false;
            }

            var fielded = new FieldAgent[agents.Length];
            profiles = new BattleRoyaleAgentProfile[agents.Length];
            for (var i = 0; i < agents.Length; i++)
            {
                if (agents[i].archetype == null)
                {
                    return false;
                }

                profiles[i] = agents[i].archetype;
                fielded[i] = new FieldAgent
                {
                    ArchetypeId = agents[i].archetype.name,
                    Rarity = agents[i].rarity,
                    Rolls = agents[i].rolls,
                };
            }

            return FieldSnapshotRules.TryCapture(
                new FieldSnapshotSource
                {
                    CompanyName = companyName,
                    Emblem = emblem,
                    Stance = sideStance,
                    Agents = fielded,
                },
                out snapshot);
        }

        void RefreshLabels()
        {
            if (balanceLabel != null)
            {
                balanceLabel.text = reserved > 0
                    ? $"Gold {balance}  Held {reserved}"
                    : $"Gold {balance}";
            }

            if (stanceLabel != null)
            {
                stanceLabel.text = stance.ToString();
            }

            if (opponentLabel != null && setup != null)
            {
                opponentLabel.text = setup.opponentName;
            }

            if (windowLabel != null)
            {
                windowLabel.text = phase == TournamentEntryPhase.Window
                    ? $"Match starts in {Mathf.CeilToInt(windowRemaining)}s"
                    : "";
            }

            if (rosterLabel != null && setup != null && waived != null)
            {
                var lines = "";
                for (var i = 0; i < setup.playerAgents.Length; i++)
                {
                    var archetype = setup.playerAgents[i].archetype;
                    var name = archetype == null ? "Missing" : archetype.displayName;
                    lines += $"{name}  {setup.playerAgents[i].rarity}  waived {waived[i]}\n";
                }

                rosterLabel.text = lines;
            }

            if (deployButton != null)
            {
                deployButton.interactable = phase == TournamentEntryPhase.Open &&
                    CanAffordEntry();
            }
        }

        bool CanAffordEntry()
        {
            var cost = tuning.CheapEntranceFee;
            for (var i = 0; i < waived.Length; i++)
            {
                if (!ContractRules.TryChargeUpkeep(
                        setup.playerAgents[i].rarity,
                        waived[i],
                        tuning,
                        out var charge,
                        out _))
                {
                    return false;
                }

                cost += charge;
            }

            return balance >= cost;
        }

        void Show(UIPage page)
        {
            if (pageTransition == null)
            {
                Debug.LogError("Company day needs its page transition.", this);
                return;
            }

            pageTransition.Show(page);
        }

        void HidePages()
        {
            if (pageTransition == null)
            {
                Debug.LogError("Company day needs its page transition.", this);
                return;
            }

            pageTransition.Hide();
        }
    }
}
