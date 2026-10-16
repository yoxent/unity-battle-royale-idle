using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleRoyale.Company
{
    public sealed class TitleStartInput : MonoBehaviour
    {
        [SerializeField] MainMenuController menu;
        [SerializeField] UI.UIPage titlePage;

        void Update()
        {
            if (menu == null || titlePage == null || !titlePage.IsFullyShown)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                menu.StartGame();
            }
        }
    }

    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] CompanySaveFile saveFile;
        [SerializeField] CompanyDaySetup setup;
        [SerializeField] UI.UIPageTransition pages;
        [SerializeField] UI.UIPage titlePage;
        [SerializeField] UI.UIPage creationPage;
        [SerializeField] UI.UIPage settingsPage;
        [SerializeField] CompanyIdentity.CompanyIdentityCreationViewBindings creationBindings;
        [SerializeField] string gameSceneName = "SampleScene";

        CompanyIdentity.CompanyIdentityCreationView creation;
        bool ready;

        void Start()
        {
            if (saveFile == null || setup == null || pages == null ||
                titlePage == null || creationPage == null || settingsPage == null)
            {
                Debug.LogError("Main menu needs its save, setup, and pages.", this);
                return;
            }

            if (creationBindings == null)
            {
                Debug.LogError("Main menu needs the company creation view.", this);
                return;
            }

            creation = new CompanyIdentity.CompanyIdentityCreationView();
            creation.Initialize(creationBindings, CreateCompany);
            ready = true;
            pages.Show(titlePage);
        }

        public void StartGame()
        {
            if (!ready)
            {
                return;
            }

            if (saveFile.TryLoad(out _))
            {
                LoadGame();
                return;
            }

            pages.Show(creationPage);
        }

        public void ShowSettings()
        {
            if (!ready)
            {
                return;
            }

            pages.Show(settingsPage);
        }

        public void ShowTitle()
        {
            if (!ready)
            {
                return;
            }

            pages.Show(titlePage);
        }

        void CreateCompany(string companyName)
        {
            if (setup.playerAgents == null || setup.playerAgents.Length == 0)
            {
                Debug.LogError("Main menu needs the opening roster.", this);
                return;
            }

            var random = new System.Random();
            if (!CompanyIdentity.CompanyIdentityRandomizer.TryCreate(
                    companyName,
                    random,
                    out var identity))
            {
                Debug.LogError("The company name was rejected.", this);
                return;
            }

            var tuning = EconomyTuning.Illustration();
            var roster = new CompanySaveAgent[setup.playerAgents.Length];
            for (var i = 0; i < roster.Length; i++)
            {
                var agent = setup.playerAgents[i];
                if (agent.archetype == null)
                {
                    Debug.LogError("Main menu is missing an opening agent.", this);
                    return;
                }

                roster[i] = new CompanySaveAgent
                {
                    ArchetypeId = agent.archetype.name,
                    Rarity = (int)agent.rarity,
                    Rolls = agent.rolls,
                    Waived = agent.waivedAppearances,
                };
            }

            var record = new CompanySaveRecord
            {
                CompanyName = identity.companyName,
                InsigniaShape = (int)identity.insigniaShape,
                Icon = (int)identity.icon,
                FlagPattern = (int)identity.flagPattern,
                PrimaryR = identity.primaryColor.r,
                PrimaryG = identity.primaryColor.g,
                PrimaryB = identity.primaryColor.b,
                SecondaryR = identity.secondaryColor.r,
                SecondaryG = identity.secondaryColor.g,
                SecondaryB = identity.secondaryColor.b,
                AccentR = identity.accentColor.r,
                AccentG = identity.accentColor.g,
                AccentB = identity.accentColor.b,
                Balance = tuning.StartingGold,
                Fans = tuning.FanPayCap,
                CompanyDate = 1,
                Stance = (int)setup.playerStance,
                Roster = roster,
                Board = new CompanySaveCard[RecruitBoardRules.BoardSize],
                TutorialStep = (int)TutorialStep.Recruit,
                UtcStamp = System.DateTime.UtcNow.Ticks.ToString(),
            };
            for (var i = 0; i < record.Board.Length; i++)
            {
                record.Board[i] = new CompanySaveCard();
            }

            if (!saveFile.TrySave(record))
            {
                return;
            }

            LoadGame();
        }

        void LoadGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogError("Main menu needs the game scene name.", this);
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
    }
}
