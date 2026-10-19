using System;
using BattleRoyale.Agents;
using BattleRoyale.Events;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRoyale.Company
{
    public sealed partial class CompanySessionController
    {
        [SerializeField] CompanySaveFile saveFile;
        [SerializeField] UIPage recruitPage;
        [SerializeField] UIPage profilePage;
        [SerializeField] UIPage editorPage;
        [SerializeField] UIPage settingsPage;
        [SerializeField] CompanyIdentitySessionHost identityHost;
        [SerializeField] RecruitBoardView recruitView;
        [SerializeField] CompanyRosterView rosterView;
        [SerializeField] CompanyDashboardView dashboardView;
        [SerializeField] OfflineReportView offlineReport;
        [SerializeField] TutorialPanelView tutorialPanel;
        [Tooltip("Off hides the tour card and the tutorial match. The company file keeps its step.")]
        [SerializeField] bool tutorialEnabled = true;
        [SerializeField] RectTransform recruitTab;
        [SerializeField] RectTransform newSimulation;
        [SerializeField] string titleSceneName = "MainMenu";
        [SerializeField] TMP_Text companyNameLabel;
        [SerializeField] GameObject leaveMatchButton;
        [SerializeField] AgentAvatarCatalog avatarCatalog;

        sealed class SignedAgent
        {
            public BattleRoyaleAgentProfile Profile;
            public AgentRarity Rarity;
            public int[] Rolls;
            public int Waived;
            public AgentAvatarAppearance Avatar;
            public bool Busy;
        }

        SignedAgent[] squad;
        RecruitBoard recruit;
        CompanyIdentity.CompanyIdentityData identity;
        CompanyIdentity.CompanyIdentityData authoredIdentity;
        RosterSlotPresentation[] rosterSlots;
        TutorialStep tutorial = TutorialStep.Recruit;
        UIPage visiblePage;
        bool reportOpen;
        int tutorialHire = -1;
        bool tutorialPresented;
        bool tutorialGateReady;
        bool tutorialEnabledApplied;
        TutorialStep presentedStep;
        TutorialPlace presentedPlace;
        bool presentedReport;
        int[] fieldedIndexes;
        int[] lineup;
        int pendingHireSlot = -1;

        void LoadSession()
        {
            if (saveFile == null)
            {
                Debug.LogError("Company day needs its save.", this);
                return;
            }

            var parsed = saveFile.TryLoad(out var record);
            if (CompanySessionBoot.Choose(true, true, parsed) != CompanyBoot.Saved)
            {
                Debug.LogError("Company day could not read the company file.", this);
                return;
            }

            tuning = EconomyTuning.Illustration();
            var elapsed = ElapsedSince(record.UtcStamp);
            if (!CompanySaveRules.TrySettleOffline(
                    record,
                    elapsed,
                    tuning,
                    out var settled,
                    out var salary,
                    out var fansPaid))
            {
                Debug.LogError("Company day could not settle offline pay.", this);
                return;
            }

            if (!BuildSquad(settled) || !BuildBoard(settled) || !BuildIdentity(settled))
            {
                return;
            }

            balance = settled.Balance;
            fans = settled.Fans;
            dayProgress = settled.DayProgress;
            companyDate = settled.CompanyDate;
            rankPoints = settled.RankPoints < 0 ? 0 : settled.RankPoints;
            stance = Enum.IsDefined(typeof(TeamPlaystyle), settled.Stance)
                ? (TeamPlaystyle)settled.Stance
                : setup.playerStance;
            phase = TournamentEntryPhase.Open;
            tutorial = Enum.IsDefined(typeof(TutorialStep), settled.TutorialStep)
                ? (TutorialStep)settled.TutorialStep
                : TutorialStep.Recruit;
            availability = new AgentAvailability[squad.Length];
            if (SideSize() <= 0)
            {
                Debug.LogError("Company day needs the match side size.", this);
                return;
            }

            RebuildAvailability();
            lineup = EmptyLineup(SideSize());
            RefreshFielding();
            ready = true;
            reportOpen = salary + fansPaid > 0 && offlineReport != null;
            SaveCompany();
            Show(dashboard);
            RefreshLabels();
            if (identityHost != null)
            {
                identityHost.Show(identity);
            }
            if (reportOpen)
            {
                offlineReport.Show(
                    $"While you were away\nSalary {salary}\nFans {fansPaid}\nBalance {balance}");
            }
        }

        public void ReturnToTitle()
        {
            if (!ready)
            {
                return;
            }

            if (string.IsNullOrEmpty(titleSceneName))
            {
                Debug.LogError("Company day needs the title scene.", this);
                return;
            }

            SaveCompany();
            SceneManager.LoadScene(titleSceneName);
        }

        void AdvanceRecruit(float elapsed)
        {
            if (recruit == null || elapsed <= 0f)
            {
                return;
            }

            tickedSinceWorld += elapsed;
            CatchUpRecruit(elapsed);
            ProjectRecruitDay();
        }

        void ApplyWorldSample(long worldUnix)
        {
            if (recruit == null || worldUnix <= 0)
            {
                return;
            }

            var offline = RecruitBoardRules.OfflineSeconds(
                recruit.AcceptedUnix,
                worldUnix,
                tickedSinceWorld);
            CatchUpRecruit(offline);
            if (!RecruitBoardRules.TryAcceptWorldTime(
                    recruit.FreeUsed,
                    recruit.PaidRetries,
                    recruit.DayIndex,
                    recruit.AcceptedUnix,
                    worldUnix,
                    out var freeUsed,
                    out var paidRetries,
                    out var dayIndex,
                    out var acceptedUnix,
                    out _))
            {
                return;
            }

            recruit.FreeUsed = freeUsed;
            recruit.PaidRetries = paidRetries;
            recruit.DayIndex = dayIndex;
            recruit.AcceptedUnix = acceptedUnix;
            tickedSinceWorld = 0f;
            SaveCompany();
        }

        void CatchUpRecruit(float elapsed)
        {
            if (recruit == null || !recruit.Rolled || elapsed <= 0f || tuning == null)
            {
                return;
            }

            if (!RecruitBoardRules.TryCatchUp(
                    recruit.Remaining,
                    elapsed,
                    tuning.RecruitRefreshSeconds,
                    out var next,
                    out var due))
            {
                return;
            }

            if (!due)
            {
                recruit.Remaining = next;
                return;
            }

            if (!RollBoard(false))
            {
                recruit.Remaining = next;
                return;
            }

            recruit.Remaining = next;
            SaveCompany();
        }

        void ProjectRecruitDay()
        {
            if (recruit == null || recruit.AcceptedUnix <= 0)
            {
                return;
            }

            var projected = recruit.AcceptedUnix + (long)tickedSinceWorld;
            if (!RecruitBoardRules.TryAcceptWorldTime(
                    recruit.FreeUsed,
                    recruit.PaidRetries,
                    recruit.DayIndex,
                    recruit.AcceptedUnix,
                    projected,
                    out var freeUsed,
                    out var paidRetries,
                    out var dayIndex,
                    out var acceptedUnix,
                    out var dayReset) ||
                !dayReset)
            {
                return;
            }

            recruit.FreeUsed = freeUsed;
            recruit.PaidRetries = paidRetries;
            recruit.DayIndex = dayIndex;
            recruit.AcceptedUnix = acceptedUnix;
            tickedSinceWorld = 0f;
            SaveCompany();
        }

        void StampTrustedClock()
        {
            if (recruit == null || recruit.AcceptedUnix <= 0)
            {
                return;
            }

            var whole = (long)tickedSinceWorld;
            if (whole <= 0)
            {
                return;
            }

            recruit.AcceptedUnix += whole;
            tickedSinceWorld -= whole;
        }

        public void ShowRecruit()
        {
            if (!ready || recruit == null || recruitPage == null)
            {
                return;
            }

            if (!recruit.Rolled && !RollBoard(true))
            {
                return;
            }

            AdvanceTutorial(TutorialAction.OpenRecruit);
            Show(recruitPage);
            RefreshRecruit();
        }

        public void ShowProfile()
        {
            if (!ready || identityHost == null || profilePage == null || identity == null)
            {
                return;
            }

            identityHost.Show(identity);
            Show(profilePage);
        }

        public void ShowEditor()
        {
            if (!ready || identityHost == null || editorPage == null || identity == null)
            {
                return;
            }

            identityHost.OpenEditor();
            Show(editorPage);
        }

        public void ShowSettings()
        {
            if (!ready || settingsPage == null)
            {
                return;
            }

            Show(settingsPage);
        }

        public void DismissOfflineReport()
        {
            reportOpen = false;
            if (offlineReport != null)
            {
                offlineReport.Hide();
            }

            PresentTutorial();
        }

        public void RequestHire(int slot)
        {
            if (!ready || phase != TournamentEntryPhase.Open || recruit == null ||
                recruit.Slots == null || slot < 0 || slot >= recruit.Slots.Length)
            {
                return;
            }

            var card = recruit.Slots[slot];
            if (card == null || !card.Filled ||
                TournamentEconomyRules.Spendable(balance, unclaimedPrize) < card.Contract ||
                squad.Length >= RecruitBoardRules.RosterCap)
            {
                return;
            }

            pendingHireSlot = slot;
            if (SavedPopupSkip(PopupId.HireConfirm))
            {
                ConfirmHire();
                return;
            }

            EventBus.Publish(new PopupRequested(
                PopupId.HireConfirm,
                PopupContent.Hire(card.Contract, balance - card.Contract)));
        }

        public void RequestRefresh()
        {
            if (!ready || phase != TournamentEntryPhase.Open || recruit == null || !recruit.Rolled)
            {
                return;
            }

            var fee = RecruitBoardRules.QuoteFee(tuning, recruit.FreeUsed, recruit.PaidRetries);
            if (fee <= 0 || RecruitBoardRules.SkipPaidConfirm(SavedRefreshConfirmDay(), recruit.DayIndex))
            {
                ConfirmRefresh();
                return;
            }

            var nextFee = RecruitBoardRules.QuoteFee(tuning, recruit.FreeUsed, recruit.PaidRetries + 1);
            EventBus.Publish(new PopupRequested(
                PopupId.RefreshConfirm,
                PopupContent.Refresh(fee, nextFee)));
        }

        public void ReleaseAgent(int index)
        {
            if (!ready || phase != TournamentEntryPhase.Open || squad == null ||
                index < 0 || index >= squad.Length || squad[index].Busy)
            {
                return;
            }

            var saved = ToSaveAgents();
            if (!RecruitBoardRules.TryRelease(saved, index, false, out _))
            {
                return;
            }

            var next = new SignedAgent[squad.Length - 1];
            var write = 0;
            for (var i = 0; i < squad.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }

                next[write] = squad[i];
                write++;
            }

            LineupRules.RemapRelease(lineup, index);
            squad = next;
            RebuildAvailability();
            RefreshFielding();
            SaveCompany();
            RefreshLabels();
        }

        public void SaveIdentity(CompanyIdentity.CompanyIdentityData next)
        {
            identity = next;
            if (companyNameLabel != null && identity != null)
            {
                companyNameLabel.text = identity.companyName;
            }

            if (phase == TournamentEntryPhase.Open || phase == TournamentEntryPhase.Resolved)
            {
                SaveCompany();
                return;
            }

            PatchDisk(disk => CopyIdentity(identity, disk));
        }

        void ConfirmHire()
        {
            var slot = pendingHireSlot;
            pendingHireSlot = -1;
            if (recruit == null || recruit.Slots == null || slot < 0 || slot >= recruit.Slots.Length ||
                recruit.Slots[slot] == null)
            {
                return;
            }

            var profile = FindProfile(recruit.Slots[slot].ArchetypeId);
            if (profile == null)
            {
                Debug.LogError("The hired agent has no archetype.", this);
                return;
            }

            if (!RecruitBoardRules.TryHire(recruit, slot, balance, squad.Length, out var nextBalance, out var hired))
            {
                return;
            }

            balance = nextBalance;
            var grown = new SignedAgent[squad.Length + 1];
            for (var i = 0; i < squad.Length; i++)
            {
                grown[i] = squad[i];
            }

            grown[squad.Length] = new SignedAgent
            {
                Profile = profile,
                Rarity = hired.Rarity,
                Rolls = hired.Rolls,
                Waived = hired.Waived,
                Avatar = hired.Avatar,
            };
            squad = grown;
            tutorialHire = squad.Length - 1;
            Describe(hired);
            RebuildAvailability();
            RefreshFielding();
            AdvanceTutorial(TutorialAction.HireSucceeded);
            SaveCompany();
            RefreshLabels();
            RefreshRecruit();
        }

        void ConfirmRefresh()
        {
            if (!RecruitBoardRules.TryPayRefresh(
                    tuning,
                    balance,
                    recruit.FreeUsed,
                    recruit.PaidRetries,
                    out var nextBalance,
                    out var freeUsed,
                    out var retries,
                    out _))
            {
                return;
            }

            balance = nextBalance;
            recruit.FreeUsed = freeUsed;
            recruit.PaidRetries = retries;
            if (!RollBoard(false))
            {
                return;
            }

            SaveCompany();
            RefreshLabels();
            RefreshRecruit();
        }

        void CommitEntry()
        {
            if (phase != TournamentEntryPhase.Open)
            {
                return;
            }

            RefreshFielding();
            if (!TournamentEconomyRules.TryEnter(
                    CaptureEntry(),
                    tuning,
                    EntryFee(),
                    FieldedRoster(),
                    out var entered))
            {
                Debug.LogError("The company cannot cover this entry.", this);
                return;
            }

            ApplyEntry(entered);
            RebuildAvailability();
            var ids = FieldedIds();
            if (ids != null && AgentAvailabilityRules.TryDeploy(availability, ids, out var busy))
            {
                availability = busy;
                CopyBusy();
            }

            AdvanceTutorial(TutorialAction.Deploy);
            Show(matchSetupPage);
        }

        void NoteFreed()
        {
            RebuildAvailability();
            var ids = FieldedIds();
            if (ids == null)
            {
                return;
            }

            if (AgentAvailabilityRules.TryFinish(availability, ids, out var finished))
            {
                availability = finished;
            }

            CopyBusy();
            RefreshFielding();
        }

        bool TourActive => TutorialRules.Runs(tutorialEnabled, tutorial);

        void ApplyTutorialEnabled()
        {
            if (phase == TournamentEntryPhase.Open)
            {
                committedOffer = null;
                if (!tutorialEnabled)
                {
                    RestorePlayableMaps();
                }

                if (lineup == null || lineup.Length != SideSize())
                {
                    lineup = EmptyLineup(SideSize());
                    RefreshFielding();
                }
            }

            RefreshMatchOffer();
            PresentTutorial();
        }

        void AdvanceTutorial(TutorialAction action)
        {
            if (!tutorialEnabled)
            {
                return;
            }

            tutorial = TutorialRules.Advance(tutorial, action);
            if (phase == TournamentEntryPhase.Open || phase == TournamentEntryPhase.Resolved)
            {
                SaveCompany();
            }
            else
            {
                PatchDisk(disk => disk.TutorialStep = (int)tutorial);
            }

            PresentTutorial();
        }

        void OnApplicationQuit()
        {
            SaveCompany();
        }

        void SaveCompany()
        {
            if (!ready || saveFile == null || identity == null || squad == null ||
                phase == TournamentEntryPhase.Window ||
                phase == TournamentEntryPhase.Running)
            {
                return;
            }

            StampTrustedClock();
            var record = new CompanySaveRecord
            {
                CompanyName = identity.companyName,
                Balance = balance,
                Fans = fans,
                DayProgress = dayProgress,
                CompanyDate = companyDate,
                RankPoints = rankPoints,
                Stance = (int)stance,
                Roster = ToSaveAgents(),
                Board = ToSaveCards(),
                RefreshRemaining = recruit == null ? 0f : recruit.Remaining,
                RefreshRetries = recruit == null ? 0 : recruit.PaidRetries,
                RefreshFreeUsed = recruit == null ? 0 : recruit.FreeUsed,
                RefreshDay = recruit == null ? 0 : recruit.DayIndex,
                TrustedUnix = recruit == null ? 0 : recruit.AcceptedUnix,
                BoardRolled = recruit != null && recruit.Rolled,
                TutorialStep = (int)tutorial,
                UtcStamp = DateTime.UtcNow.Ticks.ToString(),
                HasCheckpoint = false,
            };
            CopyIdentity(identity, record);
            saveFile.TrySave(record);
        }

        void PatchDisk(Action<CompanySaveRecord> edit)
        {
            if (saveFile == null || !saveFile.TryLoad(out var disk))
            {
                return;
            }

            edit(disk);
            saveFile.TrySave(disk);
        }

        bool RollBoard(bool forceAffordable)
        {
            var ids = ArchetypeIds();
            if (ids == null)
            {
                Debug.LogError("Company day needs recruit archetypes.", this);
                return false;
            }

            var random = new System.Random();
            if (!RecruitBoardRules.TryRoll(
                    ids,
                    tuning,
                    random,
                    forceAffordable,
                    TournamentEconomyRules.Spendable(balance, unclaimedPrize),
                    out var cards))
            {
                Debug.LogError("The recruit board could not be rolled.", this);
                return false;
            }

            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null || !cards[i].Filled)
                {
                    continue;
                }

                if (avatarCatalog == null ||
                    !avatarCatalog.TryCreateAppearance(random, out var appearance))
                {
                    Debug.LogError("Company day could not roll an avatar.", this);
                    return false;
                }

                cards[i].Avatar = appearance;
            }

            recruit.Slots = cards;
            recruit.Rolled = true;
            recruit.Remaining = tuning.RecruitRefreshSeconds;
            for (var i = 0; i < cards.Length; i++)
            {
                Describe(cards[i]);
            }

            return true;
        }

        void Describe(RecruitCard card)
        {
            if (card == null || !card.Filled)
            {
                return;
            }

            var profile = FindProfile(card.ArchetypeId);
            card.DisplayName = profile == null ? card.ArchetypeId : profile.displayName;
            card.Weapon = profile == null || profile.defaultWeapon == null
                ? ""
                : profile.defaultWeapon.displayName;
            if (!FieldedAgentProfile.HasRolls(card.Rolls))
            {
                card.Specialty = "";
                return;
            }

            var stats = new BattleRoyaleAgentStats();
            FieldedAgentProfile.WriteRolls(stats, card.Rolls);
            card.Specialty = BattleRoyaleAgentSpecialtyRules.GetDisplayName(
                BattleRoyaleAgentSpecialtyRules.Evaluate(stats).Specialty);
        }

        void RefreshRecruit()
        {
            if (recruitView == null || recruit == null)
            {
                return;
            }

            var fee = RecruitBoardRules.QuoteFee(tuning, recruit.FreeUsed, recruit.PaidRetries);
            recruitView.Refresh(
                recruit,
                TournamentEconomyRules.Spendable(balance, unclaimedPrize),
                squad == null ? 0 : squad.Length,
                fee);
        }

        void RefreshSessionPresentation()
        {
            if (!ready)
            {
                return;
            }

            if (companyNameLabel != null && identity != null)
            {
                companyNameLabel.text = identity.companyName;
            }

            if (leaveMatchButton != null)
            {
                leaveMatchButton.SetActive(
                    phase == TournamentEntryPhase.Window ||
                    phase == TournamentEntryPhase.Running);
            }

            PresentTutorial();
            PresentDashboard();
            RefreshRoster();
            RefreshRecruit();
        }

        void ApplyAuthoredIdentity()
        {
            authoredIdentity = null;
            if (setup == null)
            {
                return;
            }

            var emblem = setup.playerEmblem;
            if (!System.Enum.IsDefined(typeof(CompanyIdentity.CompanyInsigniaShape), emblem.InsigniaShape) ||
                !System.Enum.IsDefined(typeof(CompanyIdentity.CompanyIcon), emblem.Icon) ||
                !System.Enum.IsDefined(typeof(CompanyIdentity.CompanyFlagPattern), emblem.FlagPattern))
            {
                return;
            }

            authoredIdentity = new CompanyIdentity.CompanyIdentityData
            {
                companyName = setup.playerName,
                insigniaShape = (CompanyIdentity.CompanyInsigniaShape)emblem.InsigniaShape,
                icon = (CompanyIdentity.CompanyIcon)emblem.Icon,
                flagPattern = (CompanyIdentity.CompanyFlagPattern)emblem.FlagPattern,
                primaryColor = emblem.Primary.AsColor(),
                secondaryColor = emblem.Secondary.AsColor(),
                accentColor = emblem.Accent.AsColor(),
            };
        }

        void PresentDashboard()
        {
            if (dashboardView == null)
            {
                return;
            }

            var source = identity != null ? identity : authoredIdentity;
            var companyName = source != null
                ? source.companyName
                : setup != null ? setup.playerName : "";
            var count = squad == null ? 0 : squad.Length;
            var points = holdMatchPresentation ? displayedRankPoints : rankPoints;
            dashboardView.Present(
                companyName,
                source,
                points,
                reserved > 0 ? "Gold " + shownGold + "  Held " + reserved : "Gold " + shownGold,
                fans,
                companyDate,
                stance.ToString(),
                count,
                RecruitBoardRules.RosterCap);
        }

        void RefreshRoster()
        {
            if (rosterView == null)
            {
                return;
            }

            if (rosterSlots == null || rosterSlots.Length != RecruitBoardRules.RosterCap)
            {
                rosterSlots = new RosterSlotPresentation[RecruitBoardRules.RosterCap];
            }

            for (var i = 0; i < rosterSlots.Length; i++)
            {
                rosterSlots[i] = default;
                if (squad == null || i >= squad.Length || squad[i] == null)
                {
                    continue;
                }

                var agent = squad[i];
                var profile = agent.Profile;
                rosterSlots[i] = new RosterSlotPresentation
                {
                    Occupied = true,
                    Name = profile == null ? "Missing" : profile.displayName,
                    Rarity = agent.Rarity,
                    Specialty = SpecialtyName(agent.Rolls),
                    Weapon = profile == null || profile.defaultWeapon == null
                        ? ""
                        : profile.defaultWeapon.displayName,
                    Upkeep = UpkeepOf(agent.Rarity),
                    Waived = agent.Waived,
                    Availability = AvailabilityOf(i, agent.Busy),
                    CanRelease = phase == TournamentEntryPhase.Open && !agent.Busy,
                    Avatar = agent.Avatar,
                };
            }

            rosterView.Show(rosterSlots);
        }

        string AvailabilityOf(int index, bool busy)
        {
            if (busy)
            {
                return "Away";
            }

            if (IsFielded(index))
            {
                return "Next match";
            }

            return "Free";
        }

        int UpkeepOf(AgentRarity rarity)
        {
            if (tuning == null || !ContractRules.TryPrice(rarity, tuning, out var price))
            {
                return 0;
            }

            return price.Upkeep;
        }

        string SpecialtyName(int[] rolls)
        {
            if (!FieldedAgentProfile.HasRolls(rolls))
            {
                return "";
            }

            var stats = new BattleRoyaleAgentStats();
            FieldedAgentProfile.WriteRolls(stats, rolls);
            return BattleRoyaleAgentSpecialtyRules.GetDisplayName(
                BattleRoyaleAgentSpecialtyRules.Evaluate(stats).Specialty);
        }

        bool IsFielded(int index)
        {
            if (fieldedIndexes == null)
            {
                return false;
            }

            for (var i = 0; i < fieldedIndexes.Length; i++)
            {
                if (fieldedIndexes[i] == index)
                {
                    return true;
                }
            }

            return false;
        }

        FieldedAgent[] FieldedFromSquad()
        {
            if (fieldedIndexes == null)
            {
                return System.Array.Empty<FieldedAgent>();
            }

            var fielded = new FieldedAgent[fieldedIndexes.Length];
            for (var i = 0; i < fieldedIndexes.Length; i++)
            {
                var agent = squad[fieldedIndexes[i]];
                fielded[i] = new FieldedAgent
                {
                    Rarity = agent.Rarity,
                    WaivedAppearances = agent.Waived,
                };
            }

            return fielded;
        }

        bool TryBuildPlayerSide(out FieldSnapshot snapshot, out BattleRoyaleAgentProfile[] profiles)
        {
            snapshot = null;
            profiles = null;
            if (fieldedIndexes == null || squad == null)
            {
                return false;
            }

            var agents = new AuthoredAgent[fieldedIndexes.Length];
            for (var i = 0; i < fieldedIndexes.Length; i++)
            {
                var agent = squad[fieldedIndexes[i]];
                agents[i] = new AuthoredAgent
                {
                    archetype = agent.Profile,
                    rarity = agent.Rarity,
                    rolls = agent.Rolls,
                };
            }

            var companyName = identity == null ? setup.playerName : identity.companyName;
            var emblem = identity == null ? setup.playerEmblem : EmblemFrom(identity);
            return TryBuildSnapshot(
                companyName,
                emblem,
                stance,
                agents,
                out snapshot,
                out profiles);
        }

        void ApplyWaived(FieldedAgent[] agents)
        {
            if (agents == null || fieldedIndexes == null || squad == null)
            {
                return;
            }

            for (var i = 0; i < agents.Length && i < fieldedIndexes.Length; i++)
            {
                var index = fieldedIndexes[i];
                if (index >= 0 && index < squad.Length)
                {
                    squad[index].Waived = agents[i].WaivedAppearances;
                }
            }
        }

        bool CanAffordFielding()
        {
            var fielded = FieldedFromSquad();
            if (fielded.Length == 0 || fielded.Length != SideSize())
            {
                return false;
            }

            var cost = EntryFee();
            for (var i = 0; i < fielded.Length; i++)
            {
                if (!ContractRules.TryChargeUpkeep(
                        fielded[i].Rarity,
                        fielded[i].WaivedAppearances,
                        tuning,
                        out var charge,
                        out _))
                {
                    return false;
                }

                cost += charge;
            }

            return TournamentEconomyRules.Spendable(balance, unclaimedPrize) >= cost;
        }

        public void ClearDeploymentSlot(int slot)
        {
            if (!ready || phase != TournamentEntryPhase.Open ||
                lineup == null || slot < 0 || slot >= lineup.Length)
            {
                return;
            }

            lineup[slot] = -1;
            RefreshFielding();
            RefreshLabels();
        }

        public void ToggleDeploymentAgent(int index)
        {
            if (!ready || phase != TournamentEntryPhase.Open ||
                squad == null || lineup == null ||
                index < 0 || index >= squad.Length)
            {
                return;
            }

            if (!LineupRules.TryToggle(
                    lineup,
                    index,
                    squad[index].Busy,
                    lineup.Length,
                    out var next))
            {
                return;
            }

            lineup = next;
            RefreshFielding();
            RefreshLabels();
        }

        public void AutoFillDeployment()
        {
            if (!ready || phase != TournamentEntryPhase.Open || squad == null)
            {
                return;
            }

            var scores = new int[squad.Length];
            var busy = new bool[squad.Length];
            for (var i = 0; i < squad.Length; i++)
            {
                scores[i] = LineupRules.Score(squad[i].Rolls);
                busy[i] = squad[i].Busy;
            }

            if (!LineupRules.TryAutoFill(scores, busy, SideSize(), out lineup))
            {
                Debug.LogError("Not enough free agents to fill the deployment.", this);
                return;
            }

            RefreshFielding();
            RefreshLabels();
        }

        void RefreshFielding()
        {
            if (squad == null || phase != TournamentEntryPhase.Open)
            {
                return;
            }

            var count = SideSize();
            if (lineup == null || lineup.Length != count)
            {
                lineup = EmptyLineup(count);
            }

            var busy = new bool[squad.Length];
            for (var i = 0; i < squad.Length; i++)
            {
                busy[i] = squad[i].Busy;
            }

            fieldedIndexes = LineupRules.IsComplete(lineup, squad.Length, busy)
                ? (int[])lineup.Clone()
                : null;
        }

        void RefreshDeployment()
        {
            if (deployment == null || squad == null || lineup == null ||
                !deployment.gameObject.activeInHierarchy)
            {
                return;
            }

            var slots = new string[lineup.Length];
            for (var i = 0; i < lineup.Length; i++)
            {
                var index = lineup[i];
                slots[i] = index >= 0 && index < squad.Length
                    ? AgentName(squad[index])
                    : "Empty";
            }

            var names = new string[squad.Length];
            var chosen = new bool[squad.Length];
            for (var i = 0; i < squad.Length; i++)
            {
                names[i] = AgentName(squad[i]);
                for (var slot = 0; slot < lineup.Length; slot++)
                {
                    if (lineup[slot] == i)
                    {
                        chosen[i] = true;
                    }
                }
            }

            deployment.ShowLineup(slots, names, chosen);
        }

        static int[] EmptyLineup(int count)
        {
            var slots = new int[Math.Max(0, count)];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = -1;
            }

            return slots;
        }

        static string AgentName(SignedAgent agent)
        {
            return agent.Profile == null ? "Missing" : agent.Profile.displayName;
        }

        public void SelectTournament(int index)
        {
            if (!ready)
            {
                return;
            }

            if (TourActive)
            {
                if (index != 0 || phase != TournamentEntryPhase.Open)
                {
                    viewedCard = index;
                    PublishViewedCard();
                    return;
                }

                var tutorialOffer = TutorialOffer();
                if (tutorialOffer == null || tutorialOffer.Configuration == null || !PrepareTutorialMap(tutorialOffer))
                {
                    Debug.LogError("This tournament has no map setup.", this);
                    PublishViewedCard();
                    return;
                }

                committedOffer = tutorialOffer;
                selectedTournament = 0;
                viewedCard = 0;
                lineup = EmptyLineup(tutorialOffer.TeamSize);
                if (tutorialHire >= 0 &&
                    tutorialHire < squad.Length &&
                    lineup.Length > 0 &&
                    !squad[tutorialHire].Busy)
                {
                    lineup[0] = tutorialHire;
                }

                RefreshFielding();
                RefreshLabels();
                Show(matchSetupPage);
                return;
            }

            var offer = OfferAt(index);
            if (offer == null)
            {
                return;
            }

            viewedCard = index;
            if (phase != TournamentEntryPhase.Open)
            {
                PublishViewedCard();
                return;
            }

            if (offer.Configuration == null || match == null)
            {
                Debug.LogError("This tournament has no map setup.", this);
                PublishViewedCard();
                return;
            }

            committedOffer = offer;
            selectedTournament = index;
            match.UseConfiguration(offer.Configuration);
            lineup = EmptyLineup(offer.TeamSize);
            RefreshFielding();
            RefreshLabels();
            Show(matchSetupPage);
        }

        TournamentOffer SelectedOffer()
        {
            if (committedOffer != null && phase != TournamentEntryPhase.Open)
            {
                return committedOffer;
            }

            if (TourActive)
            {
                return TutorialOffer();
            }

            return OfferAt(selectedTournament);
        }

        void PresentTutorial()
        {
            if (tutorialPanel == null)
            {
                return;
            }

            var place = TutorialPlace.Other;
            if (visiblePage == dashboard)
            {
                place = TutorialPlace.Company;
            }
            else if (visiblePage == modeSelect)
            {
                place = TutorialPlace.Offers;
            }
            else if (visiblePage == matchSetupPage)
            {
                place = TutorialPlace.Setup;
            }

            if (!ready || !TourActive || reportOpen)
            {
                tutorialPresented = false;
                tutorialPanel.Hide();
                return;
            }

            if (tutorialPresented &&
                presentedStep == tutorial &&
                presentedPlace == place &&
                presentedReport == reportOpen)
            {
                return;
            }

            if (!TutorialFocus.TryCopy(tutorial, out var title, out var body))
            {
                tutorialPanel.Hide();
                return;
            }

            var focus = FocusFor(TutorialFocus.TargetFor(tutorial, place));
            if (focus == null)
            {
                Debug.LogError("Tutorial step is missing its control.", this);
            }

            tutorialPresented = true;
            presentedStep = tutorial;
            presentedPlace = place;
            presentedReport = reportOpen;
            tutorialPanel.Show(title, body, focus);
        }

        RectTransform FocusFor(TutorialTarget target)
        {
            switch (target)
            {
                case TutorialTarget.RecruitTab:
                    return recruitTab;
                case TutorialTarget.Hire:
                    return recruitView == null ? null : recruitView.HireButtonAt(AffordableHireSlot());
                case TutorialTarget.NewSimulation:
                    return newSimulation;
                case TutorialTarget.Offer:
                    return matchOffers == null || matchOffers.Length == 0 || matchOffers[0] == null
                        ? null
                        : matchOffers[0].ChooseTarget;
                case TutorialTarget.Deploy:
                    return deployButton == null ? null : deployButton.transform as RectTransform;
                default:
                    return null;
            }
        }

        int AffordableHireSlot()
        {
            if (recruit == null || recruit.Slots == null || squad == null)
            {
                return -1;
            }

            for (var i = 0; i < recruit.Slots.Length; i++)
            {
                var card = recruit.Slots[i];
                if (card != null &&
                    card.Filled &&
                    TournamentEconomyRules.Spendable(balance, unclaimedPrize) >= card.Contract &&
                    squad.Length < RecruitBoardRules.RosterCap)
                {
                    return i;
                }
            }

            return -1;
        }

        TournamentOffer OfferAt(int index)
        {
            if (tournaments == null || tournaments.Offers == null ||
                index < 0 || index >= tournaments.Offers.Length)
            {
                return null;
            }

            return tournaments.Offers[index];
        }

        int EntryFee()
        {
            var offer = SelectedOffer();
            if (offer != null)
            {
                return offer.EntryFee;
            }

            return tuning == null ? 0 : tuning.CheapEntranceFee;
        }

        int SideSize()
        {
            var offer = SelectedOffer();
            if (offer != null && offer.TeamSize > 0)
            {
                return offer.TeamSize;
            }

            if (match == null || match.ModeConfiguration == null)
            {
                return 0;
            }

            return match.ModeConfiguration.teamSize;
        }

        void RebuildAvailability()
        {
            if (squad == null)
            {
                availability = System.Array.Empty<AgentAvailability>();
                return;
            }

            availability = new AgentAvailability[squad.Length];
            for (var i = 0; i < squad.Length; i++)
            {
                availability[i] = new AgentAvailability
                {
                    Id = i,
                    Busy = squad[i].Busy,
                };
            }
        }

        void CopyBusy()
        {
            if (squad == null || availability == null)
            {
                return;
            }

            for (var i = 0; i < squad.Length && i < availability.Length; i++)
            {
                squad[i].Busy = availability[i].Busy;
            }
        }

        int[] FieldedIds()
        {
            if (fieldedIndexes == null || fieldedIndexes.Length == 0)
            {
                return null;
            }

            var ids = new int[fieldedIndexes.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = fieldedIndexes[i];
            }

            return ids;
        }

        bool BuildSquad(CompanySaveRecord record)
        {
            squad = new SignedAgent[record.Roster.Length];
            for (var i = 0; i < record.Roster.Length; i++)
            {
                var saved = record.Roster[i];
                if (saved == null || !Enum.IsDefined(typeof(AgentRarity), saved.Rarity))
                {
                    Debug.LogError("Company day has a broken roster entry.", this);
                    return false;
                }

                var profile = FindProfile(saved.ArchetypeId);
                if (profile == null || !FieldedAgentProfile.HasRolls(saved.Rolls))
                {
                    Debug.LogError("Company day is missing an agent archetype.", this);
                    return false;
                }

                var rolls = new int[saved.Rolls.Length];
                for (var roll = 0; roll < rolls.Length; roll++)
                {
                    rolls[roll] = saved.Rolls[roll];
                }

                AgentAvatarAppearance appearance = null;
                if (!string.IsNullOrEmpty(saved.Avatar) &&
                    !AgentAvatarAppearanceRules.TryRead(saved.Avatar, out appearance))
                {
                    Debug.LogError("Company day has a broken avatar.", this);
                    return false;
                }

                squad[i] = new SignedAgent
                {
                    Profile = profile,
                    Rarity = (AgentRarity)saved.Rarity,
                    Rolls = rolls,
                    Waived = saved.Waived,
                    Avatar = appearance,
                };
            }

            return true;
        }

        bool BuildBoard(CompanySaveRecord record)
        {
            recruit = new RecruitBoard
            {
                Slots = new RecruitCard[RecruitBoardRules.BoardSize],
                Remaining = record.RefreshRemaining,
                PaidRetries = record.RefreshRetries,
                FreeUsed = record.RefreshFreeUsed,
                DayIndex = record.RefreshDay,
                AcceptedUnix = record.TrustedUnix,
                Rolled = record.BoardRolled,
            };
            for (var i = 0; i < recruit.Slots.Length; i++)
            {
                recruit.Slots[i] = new RecruitCard();
                if (record.Board == null || i >= record.Board.Length || record.Board[i] == null ||
                    !record.Board[i].Filled)
                {
                    continue;
                }

                var saved = record.Board[i];
                if (!Enum.IsDefined(typeof(AgentRarity), saved.Rarity) ||
                    !ContractRules.TryPrice((AgentRarity)saved.Rarity, tuning, out var price))
                {
                    Debug.LogError("Company day has a broken recruit card.", this);
                    return false;
                }

                AgentAvatarAppearance appearance = null;
                if (!string.IsNullOrEmpty(saved.Avatar) &&
                    !AgentAvatarAppearanceRules.TryRead(saved.Avatar, out appearance))
                {
                    Debug.LogError("Company day has a broken avatar.", this);
                    return false;
                }

                if (appearance == null &&
                    (avatarCatalog == null ||
                     !avatarCatalog.TryCreateAppearance(new System.Random(i + 1), out appearance)))
                {
                    Debug.LogError("Company day could not roll an avatar.", this);
                    return false;
                }

                recruit.Slots[i] = new RecruitCard
                {
                    Filled = true,
                    ArchetypeId = saved.ArchetypeId,
                    Rarity = (AgentRarity)saved.Rarity,
                    Rolls = saved.Rolls,
                    Contract = price.Contract,
                    Upkeep = price.Upkeep,
                    Waived = price.WaivedAppearances,
                    Avatar = appearance,
                };
                Describe(recruit.Slots[i]);
            }

            return true;
        }

        bool BuildIdentity(CompanySaveRecord record)
        {
            if (!Enum.IsDefined(typeof(CompanyIdentity.CompanyInsigniaShape), record.InsigniaShape) ||
                !Enum.IsDefined(typeof(CompanyIdentity.CompanyIcon), record.Icon) ||
                !Enum.IsDefined(typeof(CompanyIdentity.CompanyFlagPattern), record.FlagPattern))
            {
                Debug.LogError("Company day has a broken emblem.", this);
                return false;
            }

            identity = new CompanyIdentity.CompanyIdentityData
            {
                companyName = record.CompanyName,
                bodyIndex = record.BodyIndex,
                insigniaShape = (CompanyIdentity.CompanyInsigniaShape)record.InsigniaShape,
                icon = (CompanyIdentity.CompanyIcon)record.Icon,
                flagPattern = (CompanyIdentity.CompanyFlagPattern)record.FlagPattern,
                primaryColor = new Color(record.PrimaryR, record.PrimaryG, record.PrimaryB, 1f),
                secondaryColor = new Color(record.SecondaryR, record.SecondaryG, record.SecondaryB, 1f),
                accentColor = new Color(record.AccentR, record.AccentG, record.AccentB, 1f),
            };
            CompanyIdentity.CompanyIdentityRules.ReadEmblemPaint(
                identity,
                record.HasEmblemPaint,
                record.BodyFillR,
                record.BodyFillG,
                record.BodyFillB,
                record.BodyBorderR,
                record.BodyBorderG,
                record.BodyBorderB,
                record.SymbolIndex,
                record.SymbolR,
                record.SymbolG,
                record.SymbolB,
                record.SymbolPositionX,
                record.SymbolPositionY,
                record.SymbolRotation,
                record.SymbolScale);
            return true;
        }

        BattleRoyaleAgentProfile FindProfile(string archetypeId)
        {
            if (setup == null || string.IsNullOrWhiteSpace(archetypeId))
            {
                return null;
            }

            if (setup.recruitArchetypes != null)
            {
                for (var i = 0; i < setup.recruitArchetypes.Length; i++)
                {
                    var profile = setup.recruitArchetypes[i];
                    if (profile != null && profile.name == archetypeId)
                    {
                        return profile;
                    }
                }
            }

            if (setup.playerAgents == null)
            {
                return null;
            }

            for (var i = 0; i < setup.playerAgents.Length; i++)
            {
                var profile = setup.playerAgents[i].archetype;
                if (profile != null && profile.name == archetypeId)
                {
                    return profile;
                }
            }

            return null;
        }

        string[] ArchetypeIds()
        {
            if (setup == null || setup.recruitArchetypes == null || setup.recruitArchetypes.Length == 0)
            {
                return null;
            }

            var ids = new string[setup.recruitArchetypes.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                if (setup.recruitArchetypes[i] == null)
                {
                    return null;
                }

                ids[i] = setup.recruitArchetypes[i].name;
            }

            return ids;
        }

        CompanySaveAgent[] ToSaveAgents()
        {
            var saved = new CompanySaveAgent[squad.Length];
            for (var i = 0; i < squad.Length; i++)
            {
                saved[i] = new CompanySaveAgent
                {
                    ArchetypeId = squad[i].Profile == null ? "" : squad[i].Profile.name,
                    Rarity = (int)squad[i].Rarity,
                    Rolls = squad[i].Rolls,
                    Waived = squad[i].Waived,
                    Avatar = AgentAvatarAppearanceRules.Write(squad[i].Avatar),
                };
            }

            return saved;
        }

        CompanySaveCard[] ToSaveCards()
        {
            var saved = new CompanySaveCard[RecruitBoardRules.BoardSize];
            for (var i = 0; i < saved.Length; i++)
            {
                saved[i] = new CompanySaveCard();
                if (recruit == null || recruit.Slots == null || i >= recruit.Slots.Length ||
                    recruit.Slots[i] == null || !recruit.Slots[i].Filled)
                {
                    continue;
                }

                var card = recruit.Slots[i];
                saved[i] = new CompanySaveCard
                {
                    Filled = true,
                    ArchetypeId = card.ArchetypeId,
                    Rarity = (int)card.Rarity,
                    Rolls = card.Rolls,
                    Avatar = AgentAvatarAppearanceRules.Write(card.Avatar),
                };
            }

            return saved;
        }

        static void CopyIdentity(CompanyIdentity.CompanyIdentityData source, CompanySaveRecord record)
        {
            record.BodyIndex = source.bodyIndex;
            record.InsigniaShape = (int)source.insigniaShape;
            record.Icon = (int)source.icon;
            record.FlagPattern = (int)source.flagPattern;
            record.PrimaryR = source.primaryColor.r;
            record.PrimaryG = source.primaryColor.g;
            record.PrimaryB = source.primaryColor.b;
            record.SecondaryR = source.secondaryColor.r;
            record.SecondaryG = source.secondaryColor.g;
            record.SecondaryB = source.secondaryColor.b;
            record.AccentR = source.accentColor.r;
            record.AccentG = source.accentColor.g;
            record.AccentB = source.accentColor.b;
            CompanySaveRules.WriteEmblem(record, source);
        }

        static FieldEmblem EmblemFrom(CompanyIdentity.CompanyIdentityData source)
        {
            return new FieldEmblem
            {
                InsigniaShape = (int)source.insigniaShape,
                Icon = (int)source.icon,
                FlagPattern = (int)source.flagPattern,
                Primary = ColorOf(source.primaryColor),
                Secondary = ColorOf(source.secondaryColor),
                Accent = ColorOf(source.accentColor),
            };
        }

        static FieldColor ColorOf(Color color)
        {
            return new FieldColor
            {
                Red = color.r,
                Green = color.g,
                Blue = color.b,
            };
        }

        static float ElapsedSince(string stamp)
        {
            if (!long.TryParse(stamp, out var ticks))
            {
                return 0f;
            }

            double elapsed;
            try
            {
                elapsed = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0f;
            }
            if (elapsed < 0d)
            {
                return 0f;
            }

            if (elapsed > float.MaxValue)
            {
                return float.MaxValue;
            }

            return (float)elapsed;
        }
    }
}
