using BattleRoyale.Agents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Company
{
    public struct RosterSlotPresentation
    {
        public bool Occupied;
        public string Name;
        public AgentRarity Rarity;
        public string Specialty;
        public string Weapon;
        public int Upkeep;
        public int Waived;
        public string Availability;
        public bool CanRelease;
        public AgentAvatarAppearance Avatar;
    }

    public sealed class CompanyRosterCardView : MonoBehaviour
    {
        [SerializeField] GameObject occupiedRoot;
        [SerializeField] GameObject openRoot;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text rarityLabel;
        [SerializeField] TMP_Text specialtyLabel;
        [SerializeField] TMP_Text weaponLabel;
        [SerializeField] TMP_Text upkeepLabel;
        [SerializeField] TMP_Text waivedLabel;
        [SerializeField] TMP_Text availabilityLabel;
        [SerializeField] Image rarityStripe;
        [SerializeField] AgentRarityColors rarityColors;
        [SerializeField] AgentAvatarView avatar;
        [SerializeField] Button releaseButton;

        public Button ReleaseButton => releaseButton;

        AgentAvatarAppearance shownAvatar;
        bool avatarShown;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (occupiedRoot == null || openRoot == null || nameLabel == null ||
                rarityLabel == null || specialtyLabel == null || weaponLabel == null ||
                upkeepLabel == null || waivedLabel == null || availabilityLabel == null ||
                rarityStripe == null || rarityColors == null || avatar == null ||
                releaseButton == null)
            {
                Debug.LogError("Roster card is missing a binding.", this);
            }
        }

        public void ShowOpen()
        {
            gameObject.SetActive(true);
            SetRoot(occupiedRoot, false);
            SetRoot(openRoot, true);
            if (releaseButton != null)
            {
                SetActiveIfChanged(releaseButton.gameObject, false);
            }

            shownAvatar = null;
            avatarShown = false;
        }

        public void Show(RosterSlotPresentation slot)
        {
            gameObject.SetActive(true);
            SetRoot(occupiedRoot, true);
            SetRoot(openRoot, false);
            SetText(nameLabel, slot.Name);
            SetText(rarityLabel, slot.Rarity.ToString());
            SetText(specialtyLabel, slot.Specialty);
            SetText(weaponLabel, slot.Weapon);
            SetText(upkeepLabel, "Upkeep " + slot.Upkeep);
            SetText(waivedLabel, slot.Waived > 0 ? "Waived " + slot.Waived : "");
            if (waivedLabel != null)
            {
                SetActiveIfChanged(waivedLabel.gameObject, slot.Waived > 0);
            }

            SetText(availabilityLabel, slot.Availability);
            if (releaseButton != null)
            {
                SetActiveIfChanged(releaseButton.gameObject, true);
                if (releaseButton.interactable != slot.CanRelease)
                {
                    releaseButton.interactable = slot.CanRelease;
                }
            }

            ApplyRarity(slot.Rarity);
            ShowAvatar(slot.Avatar);
        }

        void ApplyRarity(AgentRarity rarity)
        {
            if (rarityColors == null || !rarityColors.TryGet(rarity, out var color))
            {
                return;
            }

            if (rarityLabel != null)
            {
                if (rarityLabel.color != color)
                {
                    rarityLabel.color = color;
                }
            }

            if (rarityStripe != null)
            {
                if (rarityStripe.color != color)
                {
                    rarityStripe.color = color;
                }
            }

            if (avatar != null)
            {
                avatar.SetBorder(color);
            }
        }

        void ShowAvatar(AgentAvatarAppearance appearance)
        {
            if (avatar == null || (avatarShown && appearance == shownAvatar))
            {
                return;
            }

            avatarShown = true;
            shownAvatar = appearance;
            if (appearance == null)
            {
                avatar.gameObject.SetActive(false);
                return;
            }

            avatar.gameObject.SetActive(true);
            avatar.Show(appearance);
        }

        static void SetRoot(GameObject root, bool active)
        {
            SetActiveIfChanged(root, active);
        }

        static void SetActiveIfChanged(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        static void SetText(TMP_Text label, string value)
        {
            var text = value ?? "";
            if (label != null && label.text != text)
            {
                label.text = text;
            }
        }
    }
}
