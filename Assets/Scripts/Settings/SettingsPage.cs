using UnityEngine;

namespace BattleRoyale.Settings
{
    public sealed class SettingsPage : MonoBehaviour
    {
        [SerializeField] GameSettingsApplicator applicator;
        [SerializeField] GameObject gamePanel;
        [SerializeField] GameObject displayPanel;
        [SerializeField] GameObject audioPanel;
        [SerializeField] SettingsTabButton gameTab;
        [SerializeField] SettingsTabButton displayTab;
        [SerializeField] SettingsTabButton audioTab;
        [SerializeField] SettingsBoolControl cameraShakeRow;
        [SerializeField] SettingsDropdown windowModeRow;
        [SerializeField] SettingsDropdown resolutionRow;
        [SerializeField] SettingsBoolControl vSyncRow;
        [SerializeField] SettingsSlider masterRow;
        [SerializeField] SettingsSlider musicRow;
        [SerializeField] SettingsSlider effectsRow;

        void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!HasRequiredReferences())
            {
                return;
            }

            var desktop = Screen.currentResolution;
            applicator.Load(desktop.width, desktop.height);

            var record = applicator.Current;
            if (record == null)
            {
                Debug.LogError("SettingsPage: the applicator did not load settings.", this);
                return;
            }

            if (!applicator.HasSaved(SettingsBinding.CameraShake))
            {
                cameraShakeRow.WriteDefault(record);
            }

            if (!applicator.HasSaved(SettingsBinding.WindowMode))
            {
                windowModeRow.WriteDefault(record);
            }

            if (!applicator.HasSaved(SettingsBinding.Resolution))
            {
                resolutionRow.WriteResolutionDefault(record, desktop.width, desktop.height);
            }

            if (!applicator.HasSaved(SettingsBinding.VSync))
            {
                vSyncRow.WriteDefault(record);
            }

            if (!applicator.HasSaved(SettingsBinding.MasterVolume))
            {
                masterRow.WriteDefault(record);
            }

            if (!applicator.HasSaved(SettingsBinding.MusicVolume))
            {
                musicRow.WriteDefault(record);
            }

            if (!applicator.HasSaved(SettingsBinding.EffectsVolume))
            {
                effectsRow.WriteDefault(record);
            }

            applicator.ApplyCurrent();

            cameraShakeRow.Refresh(record);
            windowModeRow.Refresh(record);
            resolutionRow.Refresh(record);
            vSyncRow.Refresh(record);
            masterRow.Refresh(record);
            musicRow.Refresh(record);
            effectsRow.Refresh(record);

            Show(SettingsTab.Game);
        }

        public void Show(SettingsTab tab)
        {
            if (gamePanel == null || displayPanel == null || audioPanel == null
                || gameTab == null || displayTab == null || audioTab == null)
            {
                Debug.LogError("SettingsPage: assign the three panels and the three tab buttons.", this);
                return;
            }

            gamePanel.SetActive(tab == SettingsTab.Game);
            displayPanel.SetActive(tab == SettingsTab.Display);
            audioPanel.SetActive(tab == SettingsTab.Audio);
            gameTab.SetSelected(tab == SettingsTab.Game);
            displayTab.SetSelected(tab == SettingsTab.Display);
            audioTab.SetSelected(tab == SettingsTab.Audio);
        }

        bool HasRequiredReferences()
        {
            if (applicator == null)
            {
                Debug.LogError("SettingsPage: assign the applicator.", this);
                return false;
            }

            if (gamePanel == null || displayPanel == null || audioPanel == null
                || gameTab == null || displayTab == null || audioTab == null)
            {
                Debug.LogError("SettingsPage: assign the three panels and the three tab buttons.", this);
                return false;
            }

            if (cameraShakeRow == null || windowModeRow == null || resolutionRow == null || vSyncRow == null
                || masterRow == null || musicRow == null || effectsRow == null)
            {
                Debug.LogError("SettingsPage: assign all seven settings rows.", this);
                return false;
            }

            return true;
        }
    }
}
