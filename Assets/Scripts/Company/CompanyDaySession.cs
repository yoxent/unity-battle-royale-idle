using System;
using BattleRoyale.Events;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.UI;
using TMPro;
using UnityEngine;

namespace BattleRoyale.Company
{
    public sealed partial class CompanyDayController
    {
        [SerializeField] CompanySaveFile saveFile;
        [SerializeField] UIPage recruitPage;
        [SerializeField] UIPage profilePage;
        [SerializeField] UIPage editorPage;
        [SerializeField] UIPage settingsPage;
        [SerializeField] CompanyIdentitySessionHost identityHost;
        [SerializeField] RecruitBoardView recruitView;
        [SerializeField] CompanyRosterView rosterView;
        [SerializeField] OfflineReportView offlineReport;
        [SerializeField] TutorialHintView tutorialHints;
        [SerializeField] TMP_Text companyNameLabel;
        [SerializeField] GameObject leaveMatchButton;

        sealed class SignedAgent
        {
            public BattleRoyaleAgentProfile Profile;
            public AgentRarity Rarity;
            public int[] Rolls;
            public int Waived;
            public bool Busy;
        }

        SignedAgent[] squad;
        RecruitBoard recruit;
        CompanyIdentity.CompanyIdentityData identity;
        TutorialStep tutorial = TutorialStep.Recruit;
        int[] fieldedIndexes;
        int pendingHireSlot = -1;

        void LoadSession()
        {
            if (saveFile == null || recruitPage == null || profilePage == null ||
                editorPage == null || settingsPage == null || identityHost == null ||
                recruitView == null || rosterView == null || offlineReport == null ||
                tutorialHints == null || companyNameLabel == null || leaveMatchButton == null)
            {
                Debug.LogError("Company day needs its save and session pages.", this);
                return;
            }

            if (!saveFile.TryLoad(out var record))
            {
                Debug.LogError("Company day needs a company file. Start from the title.", this);
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
            RefreshFielding();
            ready = true;
            SaveCompany();
            Show(dashboard);
            RefreshLabels();
            identityHost.Show(identity);
            if (salary + fansPaid > 0 && offlineReport != null)
            {
                offlineReport.Show(
                    $"While you were away\nSalary {salary}\nFans {fansPaid}\nBalance {balance}");
            }
        }

        void AdvanceRecruit(float elapsed)
        {
            if (recruit == null || !recruit.Rolled)
            {
                return;
            }

            recruit.Remaining = RecruitBoardRules.AdvanceClock(recruit.Remaining, elapsed);
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
            if (offlineReport != null)
            {
                offlineReport.Hide();
            }
        }

        public void RequestHire(int slot)
        {
            if (!ready || phase != TournamentEntryPhase.Open || recruit == null ||
                recruit.Slots == null || slot < 0 || slot >= recruit.Slots.Length)
            {
                return;
            }

            var card = recruit.Slots[slot];
            if (card == null || !card.Filled || balance < card.Contract ||
                squad.Length >= RecruitBoardRules.RosterCap)
            {
                return;
            }

            pendingHireSlot = slot;
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

            if (recruit.Remaining <= 0f)
            {
                ConfirmRefresh();
                return;
            }

            var fee = RecruitBoardRules.QuoteFee(
                tuning,
                recruit.PaidRetries,
                recruit.RetryDate,
                companyDate);
            var effective = recruit.RetryDate == companyDate ? recruit.PaidRetries : 0;
            var nextFee = RecruitBoardRules.QuoteFee(tuning, effective + 1, companyDate, companyDate);
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
            };
            squad = grown;
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
                    recruit.Remaining,
                    recruit.PaidRetries,
                    recruit.RetryDate,
                    companyDate,
                    out var nextBalance,
                    out var retries,
                    out var retryDate,
                    out _))
            {
                return;
            }

            balance = nextBalance;
            recruit.PaidRetries = retries;
            recruit.RetryDate = retryDate;
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
                    tuning.CheapEntranceFee,
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

        void AdvanceTutorial(TutorialAction action)
        {
            tutorial = TutorialRules.Advance(tutorial, action);
            if (phase == TournamentEntryPhase.Open || phase == TournamentEntryPhase.Resolved)
            {
                SaveCompany();
            }
            else
            {
                PatchDisk(disk => disk.TutorialStep = (int)tutorial);
            }

            if (tutorialHints != null)
            {
                tutorialHints.Show(tutorial);
            }
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

            var record = new CompanySaveRecord
            {
                CompanyName = identity.companyName,
                Balance = balance,
                Fans = fans,
                DayProgress = dayProgress,
                CompanyDate = companyDate,
                Stance = (int)stance,
                Roster = ToSaveAgents(),
                Board = ToSaveCards(),
                RefreshRemaining = recruit == null ? 0f : recruit.Remaining,
                RefreshRetries = recruit == null ? 0 : recruit.PaidRetries,
                RefreshRetryDate = recruit == null ? 0 : recruit.RetryDate,
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

            if (!RecruitBoardRules.TryRoll(
                    ids,
                    tuning,
                    new System.Random(),
                    forceAffordable,
                    balance,
                    out var cards))
            {
                Debug.LogError("The recruit board could not be rolled.", this);
                return false;
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

            var fee = RecruitBoardRules.QuoteFee(
                tuning,
                recruit.PaidRetries,
                recruit.RetryDate,
                companyDate);
            var effective = recruit.RetryDate == companyDate ? recruit.PaidRetries : 0;
            var nextFee = RecruitBoardRules.QuoteFee(tuning, effective + 1, companyDate, companyDate);
            recruitView.Refresh(recruit, balance, squad == null ? 0 : squad.Length, fee, nextFee);
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

            if (tutorialHints != null)
            {
                tutorialHints.Show(tutorial);
            }

            if (rosterView != null && squad != null)
            {
                var lines = new string[squad.Length];
                var canRelease = new bool[squad.Length];
                for (var i = 0; i < squad.Length; i++)
                {
                    var name = squad[i].Profile == null ? "Missing" : squad[i].Profile.displayName;
                    lines[i] = $"{name}  {squad[i].Rarity}  waived {squad[i].Waived}";
                    canRelease[i] = phase == TournamentEntryPhase.Open && !squad[i].Busy;
                }

                rosterView.Show(lines, canRelease);
            }

            RefreshRecruit();
        }

        string FieldedLines()
        {
            if (fieldedIndexes == null)
            {
                return "Need five free agents";
            }

            var lines = "";
            for (var i = 0; i < fieldedIndexes.Length; i++)
            {
                var agent = squad[fieldedIndexes[i]];
                var name = agent.Profile == null ? "Missing" : agent.Profile.displayName;
                lines += $"{name}  {agent.Rarity}  waived {agent.Waived}\n";
            }

            return lines;
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

            var cost = tuning.CheapEntranceFee;
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

            return balance >= cost;
        }

        void RefreshFielding()
        {
            if (squad == null || phase != TournamentEntryPhase.Open)
            {
                return;
            }

            var busy = new bool[squad.Length];
            for (var i = 0; i < squad.Length; i++)
            {
                busy[i] = squad[i].Busy;
            }

            if (!RosterFieldRules.TryTakeFree(busy, SideSize(), out fieldedIndexes))
            {
                fieldedIndexes = null;
            }
        }

        int SideSize()
        {
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

                squad[i] = new SignedAgent
                {
                    Profile = profile,
                    Rarity = (AgentRarity)saved.Rarity,
                    Rolls = rolls,
                    Waived = saved.Waived,
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
                RetryDate = record.RefreshRetryDate,
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

                recruit.Slots[i] = new RecruitCard
                {
                    Filled = true,
                    ArchetypeId = saved.ArchetypeId,
                    Rarity = (AgentRarity)saved.Rarity,
                    Rolls = saved.Rolls,
                    Contract = price.Contract,
                    Upkeep = price.Upkeep,
                    Waived = price.WaivedAppearances,
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
                insigniaShape = (CompanyIdentity.CompanyInsigniaShape)record.InsigniaShape,
                icon = (CompanyIdentity.CompanyIcon)record.Icon,
                flagPattern = (CompanyIdentity.CompanyFlagPattern)record.FlagPattern,
                primaryColor = new Color(record.PrimaryR, record.PrimaryG, record.PrimaryB, 1f),
                secondaryColor = new Color(record.SecondaryR, record.SecondaryG, record.SecondaryB, 1f),
                accentColor = new Color(record.AccentR, record.AccentG, record.AccentB, 1f),
            };
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
                };
            }

            return saved;
        }

        static void CopyIdentity(CompanyIdentity.CompanyIdentityData source, CompanySaveRecord record)
        {
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
