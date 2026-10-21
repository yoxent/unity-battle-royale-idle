using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BattleRoyale.Settings
{
    public sealed class SettingsDropdown : MonoBehaviour
    {
        const int WindowModeOptionCount = 3;

        [SerializeField] string displayName;
        [SerializeField] SettingsBinding binding = SettingsBinding.WindowMode;
        [SerializeField] List<string> options = new List<string>();
        [SerializeField] int defaultIndex = 1;
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Dropdown dropdown;
        [SerializeField] GameSettingsApplicator applicator;

        readonly List<ResolutionSize> rawResolutions = new List<ResolutionSize>();
        readonly List<string> resolutionLabels = new List<string>();
        List<ResolutionSize> resolutionSizes = new List<ResolutionSize>();
        bool subscribed;

        public static bool HasValidWindowModeOptions(int count)
        {
            return count == WindowModeOptionCount;
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!IsSupportedBinding())
            {
                return;
            }

            if (dropdown == null)
            {
                Debug.LogError("SettingsDropdown: assign a dropdown.", this);
                return;
            }

            dropdown.onValueChanged.AddListener(HandleChanged);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            dropdown.onValueChanged.RemoveListener(HandleChanged);
            subscribed = false;
        }

        public void WriteDefault(GameSettings record)
        {
            if (binding != SettingsBinding.WindowMode)
            {
                Debug.LogError($"SettingsDropdown: WriteDefault needs the window mode binding, not {binding}.", this);
                return;
            }

            if (record == null)
            {
                Debug.LogError("SettingsDropdown: record is null.", this);
                return;
            }

            record.windowMode = (SettingsWindowMode)Mathf.Clamp(defaultIndex, 0, WindowModeOptionCount - 1);
        }

        public void WriteResolutionDefault(GameSettings record, int desktopWidth, int desktopHeight)
        {
            if (binding != SettingsBinding.Resolution)
            {
                Debug.LogError($"SettingsDropdown: WriteResolutionDefault needs the resolution binding, not {binding}.", this);
                return;
            }

            if (record == null)
            {
                Debug.LogError("SettingsDropdown: record is null.", this);
                return;
            }

            record.width = desktopWidth;
            record.height = desktopHeight;
        }

        public void Refresh(GameSettings record)
        {
            if (!IsSupportedBinding())
            {
                return;
            }

            if (record == null)
            {
                Debug.LogError("SettingsDropdown: record is null.", this);
                return;
            }

            if (label == null || dropdown == null)
            {
                Debug.LogError("SettingsDropdown: assign the label and dropdown.", this);
                return;
            }

            if (binding == SettingsBinding.WindowMode)
            {
                RefreshWindowMode(record);
                return;
            }

            RefreshResolution(record);
        }

        void RefreshWindowMode(GameSettings record)
        {
            if (!HasValidWindowModeOptions(options.Count))
            {
                Debug.LogError($"SettingsDropdown: window mode needs {WindowModeOptionCount} options, found {options.Count}.", this);
                return;
            }

            label.text = displayName;
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify((int)record.windowMode);
        }

        void RefreshResolution(GameSettings record)
        {
            rawResolutions.Clear();
            var screenResolutions = Screen.resolutions;
            for (var i = 0; i < screenResolutions.Length; i++)
            {
                rawResolutions.Add(new ResolutionSize(screenResolutions[i].width, screenResolutions[i].height));
            }

            resolutionSizes = GameSettings.CollectResolutions(rawResolutions);
            if (resolutionSizes.Count == 0)
            {
                Debug.LogError("SettingsDropdown: the display reported no resolutions.", this);
                return;
            }

            resolutionLabels.Clear();
            for (var i = 0; i < resolutionSizes.Count; i++)
            {
                resolutionLabels.Add(resolutionSizes[i].Width + " x " + resolutionSizes[i].Height);
            }

            var desktop = Screen.currentResolution;
            var selected = GameSettings.SelectResolution(
                resolutionSizes,
                record.width,
                record.height,
                desktop.width,
                desktop.height);

            var selectedIndex = 0;
            for (var i = 0; i < resolutionSizes.Count; i++)
            {
                if (resolutionSizes[i].Equals(selected))
                {
                    selectedIndex = i;
                    break;
                }
            }

            label.text = displayName;
            dropdown.ClearOptions();
            dropdown.AddOptions(resolutionLabels);
            dropdown.SetValueWithoutNotify(selectedIndex);
        }

        void HandleChanged(int index)
        {
            if (!IsSupportedBinding())
            {
                return;
            }

            if (applicator == null)
            {
                Debug.LogError("SettingsDropdown: assign the applicator.", this);
                return;
            }

            if (binding == SettingsBinding.WindowMode)
            {
                if (!HasValidWindowModeOptions(options.Count))
                {
                    Debug.LogError($"SettingsDropdown: window mode needs {WindowModeOptionCount} options, found {options.Count}.", this);
                    return;
                }

                applicator.SetWindowMode((SettingsWindowMode)Mathf.Clamp(index, 0, WindowModeOptionCount - 1));
                return;
            }

            if (index < 0 || index >= resolutionSizes.Count)
            {
                return;
            }

            var size = resolutionSizes[index];
            applicator.SetResolution(size.Width, size.Height);
        }

        bool IsSupportedBinding()
        {
            if (binding == SettingsBinding.WindowMode || binding == SettingsBinding.Resolution)
            {
                return true;
            }

            Debug.LogError($"SettingsDropdown: binding {binding} is not a dropdown binding.", this);
            return false;
        }
    }
}
