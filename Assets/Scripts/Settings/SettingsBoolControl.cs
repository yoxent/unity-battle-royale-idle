using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Settings
{
    public sealed class SettingsBoolControl : MonoBehaviour
    {
        [SerializeField] string displayName;
        [SerializeField] SettingsBinding binding = SettingsBinding.CameraShake;
        [SerializeField] bool defaultValue = true;
        [SerializeField] TMP_Text label;
        [SerializeField] Toggle toggle;
        [SerializeField] ToggleSwitch toggleSwitch;
        [SerializeField] GameSettingsApplicator applicator;

        bool subscribed;

        void OnEnable()
        {
            if (!IsSupportedBinding())
            {
                return;
            }

            if (toggle == null)
            {
                Debug.LogError("SettingsBoolControl: assign a toggle.", this);
                return;
            }

            toggle.onValueChanged.AddListener(HandleChanged);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            toggle.onValueChanged.RemoveListener(HandleChanged);
            subscribed = false;
        }

        public void WriteDefault(GameSettings record)
        {
            if (record == null)
            {
                Debug.LogError("SettingsBoolControl: record is null.", this);
                return;
            }

            switch (binding)
            {
                case SettingsBinding.CameraShake:
                    record.cameraShake = defaultValue;
                    break;
                case SettingsBinding.VSync:
                    record.vSync = defaultValue;
                    break;
                default:
                    LogUnsupportedBinding();
                    return;
            }
        }

        public void Refresh(GameSettings record)
        {
            if (!IsSupportedBinding())
            {
                return;
            }

            if (record == null)
            {
                Debug.LogError("SettingsBoolControl: record is null.", this);
                return;
            }

            if (label == null || toggle == null)
            {
                Debug.LogError("SettingsBoolControl: assign the label and toggle.", this);
                return;
            }

            label.text = displayName;
            bool value = binding == SettingsBinding.CameraShake ? record.cameraShake : record.vSync;
            if (toggleSwitch != null)
            {
                toggleSwitch.SetOn(value, false);
                return;
            }

            toggle.SetIsOnWithoutNotify(value);
        }

        void HandleChanged(bool value)
        {
            if (!IsSupportedBinding())
            {
                return;
            }

            if (applicator == null)
            {
                Debug.LogError("SettingsBoolControl: assign the applicator.", this);
                return;
            }

            applicator.SetBool(binding, value);
        }

        bool IsSupportedBinding()
        {
            if (binding == SettingsBinding.CameraShake || binding == SettingsBinding.VSync)
            {
                return true;
            }

            LogUnsupportedBinding();
            return false;
        }

        void LogUnsupportedBinding()
        {
            Debug.LogError($"SettingsBoolControl: binding {binding} is not a toggle binding.", this);
        }
    }
}
