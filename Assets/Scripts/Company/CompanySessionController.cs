using System.Threading;
using System.Threading.Tasks;
using BattleRoyale.CameraSystems;
using BattleRoyale.Events;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.Maps;
using BattleRoyale.UI;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public sealed partial class CompanySessionController : MonoBehaviour
    {
        [SerializeField] private CompanyDaySetup setup;
        [SerializeField] private DeathmatchGameMode match;
        [SerializeField] private UIPageTransition pageTransition;
        [SerializeField] private UIPage dashboard;
        [SerializeField] private UIPage modeSelect;
        [SerializeField] private UIPage matchSetupPage;
        [SerializeField] private UIPage resultPage;
        [SerializeField] private CompanyTabBar tabBar;
        [SerializeField] private UIPage matchPreview;
        [SerializeField] private SimulationMapView mapView;
        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text rosterLabel;
        [SerializeField] private TMP_Text stanceLabel;
        [SerializeField] private TMP_Text opponentLabel;
        [SerializeField] private TMP_Text windowLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private MatchResultPresenter matchResult;
        [SerializeField] private float goldCountDuration = 0.6f;
        [SerializeField] private Button deployButton;
        [SerializeField] private Button leaveMatchControl;
        [SerializeField] private DeploymentPanel deployment;
        [SerializeField] private TournamentCatalog tournaments;
        [SerializeField] private PlayableMapCatalog playableMaps;
        [SerializeField] private PlayableMapCatalog tutorialMaps;
        [SerializeField] private BattleRoyaleGameMode battleRoyale;
        [SerializeField] private ActiveMapManager mapManager;
        [SerializeField] private MatchOfferView[] matchOffers;
        [SerializeField] private TMP_Text rankLabel;

        EconomyTuning tuning;
        int balance;
        int reserved;
        int unclaimedPrize;
        int shownGold;
        int displayedRankPoints;
        bool holdMatchPresentation;
        bool countingGold;
        MotionHandle goldMotion;
        int companyDate;
        int fans;
        int spent;
        float dayProgress;
        float windowRemaining;
        TeamPlaystyle stance;
        TournamentEntryPhase phase;
        TournamentResult result;
        int rankPoints;
        int selectedTournament;
        int viewedCard;
        TournamentOffer committedOffer;
        TournamentOffer[] listedOffers;
        MapModeConfiguration[] listedModes = System.Array.Empty<MapModeConfiguration>();
        SimulationMatch runningMatch;
        bool tutorialMapActive;
        int startedCard = -1;
        AgentAvailability[] availability;
        bool resultApplied;
        bool ready;
        float worldClockWait;
        float tickedSinceWorld;
        bool worldClockBusy;
        readonly WorldClockSample worldClockSample = new WorldClockSample();
        bool leaveMatchSubscribed;
        int presentedSpendable = int.MinValue;
        int presentedReserved = int.MinValue;
        int presentedFans = int.MinValue;
        int presentedDay = int.MinValue;
        int presentedRank = int.MinValue;
        int presentedRosterCount = int.MinValue;
        int presentedWindowSecond = int.MinValue;
        int presentedStanceId = int.MinValue;
        bool presentedLeaveMatch;
        string presentedStance;
        bool hasPresentedLiveState;

        const float WorldClockPeriodSeconds = 60f;
        const float WorldClockRetrySeconds = 20f;

        sealed class WorldClockSample
        {
            public int Ready;
            public int Cancelled;
            public long Unix;
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EventBus.Subscribe<PopupClosed>(OnPopupClosed);
            if (leaveMatchControl == null)
            {
                Debug.LogError("Company day needs its leave match button.", this);
                return;
            }

            leaveMatchControl.onClick.AddListener(AbortEntry);
            leaveMatchSubscribed = true;
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PopupClosed>(OnPopupClosed);
            if (leaveMatchSubscribed && leaveMatchControl != null)
            {
                leaveMatchControl.onClick.RemoveListener(AbortEntry);
                leaveMatchSubscribed = false;
            }

            CancelGoldCount();
        }

        void OnDestroy()
        {
            worldClockSample.Cancelled = 1;
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused)
            {
                worldClockWait = 0f;
            }
        }

        void Start()
        {
            if (setup == null || match == null)
            {
                Debug.LogError("Company day needs a setup and a Deathmatch.", this);
                return;
            }

            if (pageTransition == null || dashboard == null || modeSelect == null ||
                matchSetupPage == null || resultPage == null || matchPreview == null ||
                mapView == null)
            {
                Debug.LogError(
                    "Company day needs its page transition, pages, and match preview.",
                    this);
                return;
            }

            if (setup.playerAgents == null || setup.opponentAgents == null)
            {
                Debug.LogError("Company day needs a roster and an opponent.", this);
                return;
            }

            var saveAssigned = saveFile != null;
            var fileExists = saveAssigned && saveFile.Exists();
            if (CompanySessionBoot.Choose(saveAssigned, fileExists, true) == CompanyBoot.Authored)
            {
                BootAuthoredCompany();
                return;
            }

            LoadSession();
        }

        void BootAuthoredCompany()
        {
            tuning = EconomyTuning.Illustration();
            balance = tuning.StartingGold;
            fans = tuning.FanPayCap;
            companyDate = 1;
            stance = setup.playerStance;
            phase = TournamentEntryPhase.Open;
            var count = setup.playerAgents.Length;
            squad = new SignedAgent[count];
            availability = new AgentAvailability[count];
            for (var i = 0; i < count; i++)
            {
                var agent = setup.playerAgents[i];
                squad[i] = new SignedAgent
                {
                    Profile = agent.archetype,
                    Rarity = agent.rarity,
                    Rolls = agent.rolls,
                    Waived = agent.waivedAppearances,
                };
                availability[i] = new AgentAvailability { Id = i };
            }

            recruit = new RecruitBoard
            {
                Slots = new RecruitCard[RecruitBoardRules.BoardSize],
                Remaining = 0f,
                Rolled = false,
            };
            for (var i = 0; i < recruit.Slots.Length; i++)
            {
                recruit.Slots[i] = new RecruitCard();
            }

            ready = true;
            ApplyAuthoredIdentity();
            RefreshFielding();
            Show(dashboard);
            RefreshLabels();
        }

        void Update()
        {
            if (!ready)
            {
                return;
            }

            if (!tutorialGateReady || tutorialEnabledApplied != tutorialEnabled)
            {
                var changed = tutorialGateReady && tutorialEnabledApplied != tutorialEnabled;
                tutorialGateReady = true;
                tutorialEnabledApplied = tutorialEnabled;
                if (changed)
                {
                    ApplyTutorialEnabled();
                }
            }

            PollWorldClock();
            var elapsed = Time.unscaledDeltaTime;
            AdvanceRecruit(elapsed);
            AdvanceIdle(elapsed);
            if (phase == TournamentEntryPhase.Window)
            {
                AdvanceWindow(elapsed);
            }

            if (phase == TournamentEntryPhase.Running &&
                !resultApplied &&
                runningMatch != null &&
                runningMatch.IsMatchStarted)
            {
                if (runningMatch.WinningTeam >= 0)
                {
                    ApplyPlacement(false);
                }
                else if (runningMatch == battleRoyale && battleRoyale.AliveCount == 0)
                {
                    ApplyPlacement(true);
                }
            }

            RefreshLivePresentation();
        }

        void PollWorldClock()
        {
            if (Interlocked.Exchange(ref worldClockSample.Ready, 0) == 1)
            {
                worldClockBusy = false;
                var unix = Interlocked.Read(ref worldClockSample.Unix);
                if (unix > 0)
                {
                    ApplyWorldSample(unix);
                    worldClockWait = WorldClockPeriodSeconds;
                }
                else
                {
                    worldClockWait = WorldClockRetrySeconds;
                }
            }

            if (worldClockBusy)
            {
                return;
            }

            worldClockWait -= Time.unscaledDeltaTime;
            if (worldClockWait > 0f)
            {
                return;
            }

            worldClockBusy = true;
            worldClockWait = WorldClockPeriodSeconds;
            var sample = worldClockSample;
            Task.Run(() =>
            {
                long unix;
                if (sample.Cancelled != 0 || !WorldClock.TryQuery(out unix))
                {
                    unix = 0;
                }

                Interlocked.Exchange(ref sample.Unix, unix);
                Interlocked.Exchange(ref sample.Ready, 1);
            });
        }

        public void ShowModeSelect()
        {
            if (!ready)
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

            PresentSelectedOffer();
        }

        public void ShowDashboard()
        {
            if (!ready)
            {
                return;
            }

            Show(dashboard);
            RefreshLabels();
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

            RefreshFielding();
            if (fieldedIndexes == null)
            {
                Debug.LogError("Fill the deployment slots before this match.", this);
                return;
            }

            if (SavedPopupSkip(PopupId.EntryCommit))
            {
                CommitEntry();
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
            NoteFreed();
            startedCard = -1;
            committedOffer = null;
            ClearListedOffers();
            RestorePlayableMaps();
            SaveCompany();
            Show(modeSelect);
        }

        public void DismissResult()
        {
            if (!ready || phase != TournamentEntryPhase.Resolved)
            {
                return;
            }

            if (runningMatch == battleRoyale && battleRoyale != null)
            {
                battleRoyale.ClearCompanyMatch();
            }
            else if (match != null)
            {
                match.ClearCompanyMatch();
            }

            runningMatch = null;
            committedOffer = null;
            ClearListedOffers();
            RestorePlayableMaps();
            phase = TournamentEntryPhase.Open;
            startedCard = -1;
            result = TournamentResult.None;
            reserved = 0;
            resultApplied = false;
            spent = 0;
            RefreshFielding();
            SaveCompany();
            Show(dashboard);
        }

        void OnPopupClosed(PopupClosed closed)
        {
            if (closed.Id == PopupId.ReleaseConfirm && closed.Result != PopupResult.Confirmed)
            {
                pendingReleaseIndex = -1;
            }

            if (!ready || closed.Result != PopupResult.Confirmed)
            {
                return;
            }

            if (closed.DoNotShowAgain && closed.Id != PopupId.RefreshConfirm)
            {
                RememberPopupSkip(closed.Id);
            }

            if (closed.Id == PopupId.EntryCommit)
            {
                CommitEntry();
                return;
            }

            if (closed.Id == PopupId.HireConfirm)
            {
                ConfirmHire();
                return;
            }

            if (closed.Id == PopupId.RefreshConfirm)
            {
                if (closed.DoNotShowAgain)
                {
                    RememberRefreshConfirmDay();
                }

                ConfirmRefresh();
                return;
            }

            if (closed.Id == PopupId.ReleaseConfirm)
            {
                ConfirmRelease();
            }
        }

        const string RefreshConfirmDayKey = "recruit-refresh-confirm-day";

        static string PopupSkipKey(PopupId id)
        {
            return "popup-skip." + (int)id;
        }

        bool SavedPopupSkip(PopupId id)
        {
            return PlayerPrefs.GetInt(PopupSkipKey(id), 0) != 0;
        }

        void RememberPopupSkip(PopupId id)
        {
            PlayerPrefs.SetInt(PopupSkipKey(id), 1);
            PlayerPrefs.Save();
        }

        int SavedRefreshConfirmDay()
        {
            return PlayerPrefs.GetInt(RefreshConfirmDayKey, 0);
        }

        void RememberRefreshConfirmDay()
        {
            if (recruit == null || recruit.DayIndex <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(RefreshConfirmDayKey, recruit.DayIndex);
            PlayerPrefs.Save();
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
            if (!TryBuildPlayerSide(out var player, out var playerProfiles) ||
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

            if (committedOffer == null || !TryActivateOfferMap(committedOffer))
            {
                Debug.LogError("This tournament has no map setup.", this);
                return;
            }

            var tutorialMatch = committedOffer.Configuration.mode == MapGameMode.BattleRoyale;
            if (tutorialMatch)
            {
                if (battleRoyale == null ||
                    !battleRoyale.TryArmCompanyMatch(
                        player,
                        opponent,
                        playerProfiles,
                        opponentProfiles,
                        committedOffer.TeamCount) ||
                    !battleRoyale.BeginMatch())
                {
                    Debug.LogError("Battle Royale did not start from the snapshots.", this);
                    return;
                }

                runningMatch = battleRoyale;
            }
            else if (!match.TryArmCompanyMatch(
                    player,
                    opponent,
                    playerProfiles,
                    opponentProfiles) ||
                !match.BeginMatch())
            {
                Debug.LogError("Deathmatch did not start from the snapshots.", this);
                return;
            }
            else
            {
                runningMatch = match;
            }

            startedCard = selectedTournament;
            viewedCard = selectedTournament;
            HidePages();
        }

        void ApplyPlacement(bool draw)
        {
            var placement = !draw &&
                runningMatch != null &&
                runningMatch.WinningTeam == DeathmatchGameMode.CompanyPlayerTeam
                ? TournamentResult.First
                : TournamentResult.Last;
            var offer = SelectedOffer();
            var prize = placement == TournamentResult.First
                ? (offer != null ? offer.PrizePool : tuning.CheapFirstPrize)
                : 0;
            if (!TournamentEconomyRules.TryApplyResult(
                    CaptureEntry(),
                    placement,
                    prize,
                    out var resolved))
            {
                return;
            }

            displayedRankPoints = rankPoints;
            holdMatchPresentation = true;
            if (!draw)
            {
                rankPoints = CompanyRankRules.Apply(
                    rankPoints,
                    placement == TournamentResult.First);
            }
            ApplyEntry(resolved);
            NoteFreed();
            SaveCompany();

            resultApplied = true;
            unclaimedPrize = prize;
            shownGold = TournamentEconomyRules.Spendable(balance, unclaimedPrize);
            if (resultLabel != null)
            {
                var net = prize - spent;
                var outcome = draw ? "Draw" : placement.ToString();
                resultLabel.text =
                    $"{outcome}\nPrize {prize}\nFee and upkeep {spent}\nNet {net}\nBalance {balance}\n{CompanyRankRules.Label(rankPoints)}";
            }

            RefreshLabels();
            if (matchResult == null)
            {
                Debug.LogError("Company day needs its match result presenter.", this);
                return;
            }

            var placementLine = draw ? "Draw" : placement.ToString();
            matchResult.Begin(
                placementLine,
                "Prize " + prize,
                CompanyRankRules.Label(rankPoints),
                FinishMatchReveal);
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
            ApplyWaived(next.Agents);
        }

        FieldedAgent[] FieldedRoster()
        {
            return FieldedFromSquad();
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

        public void FinishMatchReveal()
        {
            if (matchResult != null)
            {
                matchResult.Release();
            }

            holdMatchPresentation = false;
            var from = TournamentEconomyRules.Spendable(balance, unclaimedPrize);
            var to = balance;
            DismissResult();
            if (from == to)
            {
                unclaimedPrize = 0;
                RefreshLabels();
                return;
            }

            CancelGoldCount();
            countingGold = true;
            shownGold = from;
            RefreshLabels();
            goldMotion = LMotion.Create((float)from, (float)to, goldCountDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    countingGold = false;
                    unclaimedPrize = 0;
                    shownGold = balance;
                    RefreshLabels();
                })
                .Bind(value =>
                {
                    shownGold = Mathf.RoundToInt(value);
                    RefreshLivePresentation();
                });
        }

        void CancelGoldCount()
        {
            if (goldMotion.IsActive())
            {
                goldMotion.Cancel();
            }

            goldMotion = default;
        }

        void RefreshLivePresentation()
        {
            if (!ready)
            {
                return;
            }

            var spendable = countingGold
                ? shownGold
                : TournamentEconomyRules.Spendable(balance, unclaimedPrize);
            if (!countingGold)
            {
                shownGold = spendable;
            }

            var windowSecond = phase == TournamentEntryPhase.Window
                ? Mathf.CeilToInt(windowRemaining)
                : 0;
            var rosterCount = squad == null ? 0 : squad.Length;
            var displayedRank = holdMatchPresentation ? displayedRankPoints : rankPoints;
            var leaveMatch = phase == TournamentEntryPhase.Window;
            var stanceId = (int)stance;
            var leaveButtonNeedsRefresh = leaveMatchButton != null &&
                leaveMatchButton.activeSelf != leaveMatch;
            var windowNeedsRefresh = windowLabel != null &&
                windowLabel.gameObject.activeSelf != leaveMatch;
            var changed = !hasPresentedLiveState ||
                spendable != presentedSpendable ||
                reserved != presentedReserved ||
                fans != presentedFans ||
                companyDate != presentedDay ||
                displayedRank != presentedRank ||
                rosterCount != presentedRosterCount ||
                windowSecond != presentedWindowSecond ||
                leaveMatch != presentedLeaveMatch ||
                leaveButtonNeedsRefresh ||
                windowNeedsRefresh ||
                stanceId != presentedStanceId;

            if (!changed)
            {
                return;
            }

            presentedSpendable = spendable;
            presentedReserved = reserved;
            presentedFans = fans;
            presentedDay = companyDate;
            presentedRank = displayedRank;
            presentedRosterCount = rosterCount;
            presentedWindowSecond = windowSecond;
            presentedStanceId = stanceId;
            presentedLeaveMatch = leaveMatch;
            presentedStance = stance.ToString();
            hasPresentedLiveState = true;

            if (balanceLabel != null)
            {
                balanceLabel.text = FormatTopBarResources(spendable, reserved, fans, companyDate);
            }

            if (stanceLabel != null)
            {
                stanceLabel.text = presentedStance;
            }

            if (windowLabel != null)
            {
                var showWindow = phase == TournamentEntryPhase.Window;
                windowLabel.text = showWindow
                    ? $"Match starts in {windowSecond}s"
                    : "";
                if (windowLabel.gameObject.activeSelf != showWindow)
                {
                    windowLabel.gameObject.SetActive(showWindow);
                }
            }

            if (leaveMatchButton != null)
            {
                leaveMatchButton.SetActive(leaveMatch);
            }

            if (rosterLabel != null)
            {
                rosterLabel.text = rosterCount + " / " + RecruitBoardRules.RosterCap;
            }

            if (deployButton != null)
            {
                deployButton.interactable = phase == TournamentEntryPhase.Open &&
                    CanAffordEntry();
            }

            if (rankLabel != null)
            {
                rankLabel.text = CompanyRankRules.Label(displayedRank);
            }

            if (dashboardView != null)
            {
                dashboardView.PresentLiveValues(
                    reserved > 0
                        ? $"Gold {spendable}  Held {reserved}"
                        : $"Gold {spendable}",
                    fans,
                    companyDate,
                    presentedStance);
            }
        }

        void RefreshLabels()
        {
            if (!countingGold)
            {
                shownGold = TournamentEconomyRules.Spendable(balance, unclaimedPrize);
            }

            if (balanceLabel != null)
            {
                balanceLabel.text = FormatTopBarResources(shownGold, reserved, fans, companyDate);
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
                var showWindow = phase == TournamentEntryPhase.Window;
                windowLabel.text = showWindow
                    ? $"Match starts in {Mathf.CeilToInt(windowRemaining)}s"
                    : "";
                if (windowLabel.gameObject.activeSelf != showWindow)
                {
                    windowLabel.gameObject.SetActive(showWindow);
                }
            }

            if (rosterLabel != null)
            {
                var count = squad == null ? 0 : squad.Length;
                rosterLabel.text = count + " / " + RecruitBoardRules.RosterCap;
            }

            RefreshSessionPresentation();

            if (deployButton != null)
            {
                deployButton.interactable = phase == TournamentEntryPhase.Open &&
                    CanAffordEntry();
            }

            RefreshDeployment();
            if (rankLabel != null)
            {
                var points = holdMatchPresentation ? displayedRankPoints : rankPoints;
                rankLabel.text = CompanyRankRules.Label(points);
            }
        }

        static string FormatTopBarResources(int spendable, int held, int fans, int day)
        {
            var gold = held > 0
                ? $"Gold {spendable}  Held {held}"
                : $"Gold {spendable}";
            return $"{gold}  ·  Fans {fans}  ·  Day {day}";
        }

        void RefreshMatchOffer()
        {
            if (matchOffers == null)
            {
                return;
            }

            if (TourActive)
            {
                ShowTutorialOffer();
                return;
            }

            if (tournaments == null || tournaments.Offers == null)
            {
                Debug.LogError("Mode select needs its tournament catalog.", this);
                return;
            }

            if (playableMaps == null)
            {
                Debug.LogError("Mode select needs the playable map catalog.", this);
                return;
            }

            EnsureListedOffers();
            var count = tournaments.Offers.Length;
            if (count > matchOffers.Length)
            {
                Debug.LogError("Mode select does not have a card for every tournament.", this);
            }

            for (var i = 0; i < matchOffers.Length; i++)
            {
                var card = matchOffers[i];
                if (card == null)
                {
                    Debug.LogError("Mode select is missing a tournament card.", this);
                    continue;
                }

                if (i >= count || tournaments.Offers[i] == null)
                {
                    card.Hide();
                    continue;
                }

                var offer = OfferAt(i);
                if (offer == null)
                {
                    card.Hide();
                    continue;
                }

                var map = MapForCard(i);
                card.Show(
                    i,
                    string.IsNullOrWhiteSpace(offer.Name) ? "Match" : offer.Name,
                    offer.Difficulty.ToString(),
                    offer.TeamCount,
                    offer.TeamSize,
                    offer.SuggestedRating,
                    offer.EntryFee,
                    offer.PrizePool,
                    offer.Configuration != null,
                    map == null ? "" : MapTitle.From(map),
                    map == null ? null : map.visualSprite);
            }
        }

        MapDefinition MapForCard(int index)
        {
            if (TourActive)
            {
                return index == 0 ? TutorialMap() : null;
            }

            var offer = OfferAt(index);
            return offer == null || offer.Configuration == null
                ? null
                : offer.Configuration.map;
        }

        void EnsureListedOffers()
        {
            if (TourActive || listedOffers != null || phase != TournamentEntryPhase.Open)
            {
                return;
            }

            if (tournaments == null || tournaments.Offers == null || playableMaps == null ||
                playableMaps.maps == null)
            {
                return;
            }

            var available = 0;
            for (var mapIndex = 0; mapIndex < playableMaps.maps.Count; mapIndex++)
            {
                available += CountListable(playableMaps.maps[mapIndex]);
            }

            listedModes = new MapModeConfiguration[available];
            var filled = 0;
            for (var mapIndex = 0; mapIndex < playableMaps.maps.Count; mapIndex++)
            {
                filled = CopyListable(playableMaps.maps[mapIndex], listedModes, filled);
            }

            var slots = tournaments.Offers.Length;
            listedOffers = new TournamentOffer[slots];
            var picks = new int[slots];
            if (!MatchOfferRollRules.TryRoll(listedModes.Length, picks, new System.Random()))
            {
                Debug.LogError("Mode select has no map with a playable mode.", this);
                return;
            }

            for (var i = 0; i < slots; i++)
            {
                var template = tournaments.Offers[i];
                var configuration = listedModes[picks[i]];
                listedOffers[i] = new TournamentOffer
                {
                    Name = MapGameModes.Label(configuration.mode),
                    Difficulty = template == null
                        ? TournamentDifficulty.Easy
                        : template.Difficulty,
                    TeamCount = configuration.teamCount,
                    TeamSize = configuration.teamSize,
                    EntryFee = template == null ? 0 : template.EntryFee,
                    PrizePool = template == null ? 0 : template.PrizePool,
                    SuggestedRating = template == null ? 0 : template.SuggestedRating,
                    Configuration = configuration,
                };
            }
        }

        void ClearListedOffers()
        {
            listedOffers = null;
            listedModes = System.Array.Empty<MapModeConfiguration>();
        }

        static int CountListable(MapDefinition map)
        {
            if (!CanHostOffer(map))
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < map.modeConfigurations.Count; i++)
            {
                if (IsListable(map, map.modeConfigurations[i]))
                {
                    count++;
                }
            }

            return count;
        }

        static int CopyListable(MapDefinition map, MapModeConfiguration[] destination, int filled)
        {
            if (!CanHostOffer(map))
            {
                return filled;
            }

            for (var i = 0; i < map.modeConfigurations.Count; i++)
            {
                var configuration = map.modeConfigurations[i];
                if (!IsListable(map, configuration))
                {
                    continue;
                }

                destination[filled] = configuration;
                filled++;
            }

            return filled;
        }

        static bool CanHostOffer(MapDefinition map)
        {
            return map != null && map.bakedPrefab != null && map.modeConfigurations != null;
        }

        static bool IsListable(MapDefinition map, MapModeConfiguration configuration)
        {
            return configuration != null &&
                MatchOfferRollRules.CanList(configuration.mode) &&
                (configuration.map == null || configuration.map == map) &&
                configuration.teamCount > 0 &&
                configuration.teamSize > 0;
        }

        bool TryActivateOfferMap(TournamentOffer offer)
        {
            if (offer == null || offer.Configuration == null || offer.Configuration.map == null ||
                mapManager == null || playableMaps == null)
            {
                return false;
            }

            if (!MatchOfferRollRules.CanList(offer.Configuration.mode))
            {
                return false;
            }

            if (offer.Configuration.mode == MapGameMode.BattleRoyale)
            {
                if (battleRoyale == null)
                {
                    return false;
                }
            }
            else if (match == null)
            {
                return false;
            }

            var activated = mapManager.ActivatePlayableMap(offer.Configuration.map);
            if (activated == null || activated.definition != offer.Configuration.map)
            {
                return false;
            }

            if (offer.Configuration.mode == MapGameMode.BattleRoyale)
            {
                battleRoyale.UseConfiguration(offer.Configuration);
            }
            else
            {
                match.UseConfiguration(offer.Configuration);
            }

            return true;
        }

        void ShowTutorialOffer()
        {
            for (var i = 0; i < matchOffers.Length; i++)
            {
                if (matchOffers[i] == null)
                {
                    continue;
                }

                if (i > 0)
                {
                    matchOffers[i].Hide();
                }
            }

            if (matchOffers.Length == 0 || matchOffers[0] == null)
            {
                Debug.LogError("Mode select is missing a tournament card.", this);
                return;
            }

            var offer = TutorialOffer();
            var map = TutorialMap();
            if (offer == null || map == null)
            {
                Debug.LogError("The tutorial map has no solo Battle Royale.", this);
                matchOffers[0].Hide();
                return;
            }

            matchOffers[0].Show(
                0,
                string.IsNullOrWhiteSpace(offer.Name) ? "Match" : offer.Name,
                offer.Difficulty.ToString(),
                offer.TeamCount,
                offer.TeamSize,
                offer.SuggestedRating,
                offer.EntryFee,
                offer.PrizePool,
                true,
                MapTitle.From(map),
                map.visualSprite);
        }

        MapDefinition TutorialMap()
        {
            if (tutorialMaps == null || tutorialMaps.maps == null || tutorialMaps.maps.Count == 0)
            {
                return null;
            }

            return tutorialMaps.maps[0];
        }

        TournamentOffer TutorialOffer()
        {
            var map = TutorialMap();
            if (map == null || !map.TryGetSingleConfiguration(MapGameMode.BattleRoyale, out var configuration))
            {
                return null;
            }

            var battle = configuration as BattleRoyaleModeConfiguration;
            var spawns = battle == null || battle.spawnPoints == null ? 0 : battle.spawnPoints.Count;
            var opponents = setup == null || setup.opponentAgents == null
                ? 0
                : setup.opponentAgents.Length;
            if (!TutorialMatchRules.TryOffer(configuration, spawns, opponents, out var teams, out var size))
            {
                return null;
            }

            var fees = tuning ?? EconomyTuning.Illustration();
            return new TournamentOffer
            {
                Name = MapGameModes.Label(MapGameMode.BattleRoyale),
                Difficulty = TournamentDifficulty.Easy,
                TeamCount = teams,
                TeamSize = size,
                EntryFee = fees.CheapEntranceFee,
                PrizePool = fees.CheapFirstPrize,
                Configuration = configuration,
            };
        }

        bool PrepareTutorialMap(TournamentOffer offer)
        {
            if (mapManager == null || battleRoyale == null || tutorialMaps == null || offer == null)
            {
                Debug.LogError("The tutorial match needs its map catalog and Battle Royale.", this);
                return false;
            }

            var map = TutorialMap();
            mapManager.SetPlayableMaps(tutorialMaps);
            var activated = mapManager.ActivatePlayableMap(map);
            if (activated == null || activated.definition != map)
            {
                Debug.LogError("The tutorial map did not activate.", this);
                return false;
            }

            battleRoyale.UseConfiguration(offer.Configuration);
            tutorialMapActive = true;
            return true;
        }

        void RestorePlayableMaps()
        {
            if (!tutorialMapActive)
            {
                return;
            }

            tutorialMapActive = false;
            if (mapManager == null || playableMaps == null)
            {
                Debug.LogError("Company day needs the playable map catalog.", this);
                return;
            }

            mapManager.SetPlayableMaps(playableMaps);
            mapManager.ActivateForMode(MapGameMode.Deathmatch);
        }

        bool CanAffordEntry()
        {
            return CanAffordFielding();
        }

        void PresentSelectedOffer()
        {
            var joining = !MapHasStarted(viewedCard);
            PublishViewedCard();
            if (deployment != null)
            {
                deployment.gameObject.SetActive(joining);
            }

            if (joining)
            {
                RefreshDeployment();
            }

            PresentTutorial();
        }

        void Show(UIPage page)
        {
            if (pageTransition == null)
            {
                Debug.LogError("Company day needs its page transition.", this);
                return;
            }

            visiblePage = page;
            pageTransition.Show(page);
            var matches = page == modeSelect || page == matchSetupPage || page == resultPage;
            if (matches)
            {
                var joining = !MapHasStarted(viewedCard) &&
                    (page == modeSelect || page == matchSetupPage);
                ShowMatchPreview();
                PublishViewedCard();
                if (deployment != null)
                {
                    deployment.gameObject.SetActive(joining);
                }

                if (joining)
                {
                    RefreshDeployment();
                }

                if (page == modeSelect)
                {
                    RefreshMatchOffer();
                }
            }
            else
            {
                HideMatchPreview();
            }

            PresentTutorial();
            if (tabBar == null)
            {
                return;
            }

            tabBar.SetVisible(true);
            if (page == dashboard)
            {
                tabBar.SelectCompany();
            }
            else if (page == recruitPage)
            {
                tabBar.SelectRecruit();
            }
            else if (page == modeSelect || page == matchSetupPage || page == resultPage)
            {
                tabBar.SelectMatches();
            }
        }

        void HidePages()
        {
            if (pageTransition == null)
            {
                Debug.LogError("Company day needs its page transition.", this);
                return;
            }

            pageTransition.Hide();
            ShowMatchPreview();
            PublishViewedCard();
            if (deployment != null)
            {
                deployment.gameObject.SetActive(false);
            }
            if (tabBar != null)
            {
                tabBar.SetVisible(false);
            }
        }

        void ShowMatchPreview()
        {
            if (matchPreview == null)
            {
                Debug.LogError("Company day needs its match preview.", this);
                return;
            }

            matchPreview.ShowImmediate();
        }

        void HideMatchPreview()
        {
            if (matchPreview == null)
            {
                Debug.LogError("Company day needs its match preview.", this);
                return;
            }

            SetMapShown(false);
            if (deployment != null)
            {
                deployment.gameObject.SetActive(false);
            }

            matchPreview.HideImmediate();
        }

        void SetMapShown(bool shown)
        {
            if (mapView == null)
            {
                Debug.LogError("Company day needs its simulation map view.", this);
                return;
            }

            mapView.SetShown(shown);
        }

        bool MapHasStarted(int card)
        {
            return card >= 0 && card == startedCard &&
                (phase == TournamentEntryPhase.Running ||
                 phase == TournamentEntryPhase.Resolved);
        }

        void PublishViewedCard()
        {
            var map = MapForCard(viewedCard);
            EventBus.Publish(new MatchCardViewed(
                viewedCard,
                MapHasStarted(viewedCard),
                MapTitle.From(map),
                map == null ? null : map.visualSprite));
        }
    }
}
