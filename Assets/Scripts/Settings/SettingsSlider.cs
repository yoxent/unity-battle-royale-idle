using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Settings
{
    public sealed class SettingsSlider : MonoBehaviour
    {
        [SerializeField] string displayName;
        [SerializeField] SettingsBinding binding = SettingsBinding.MasterVolume;
        [SerializeField] float minimum;
        [SerializeField] float maximum = 1f;
        [SerializeField] float defaultValue = 1f;
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField] Slider slider;
        [SerializeField] GameSettingsApplicator applicator;

        bool subscribed;

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

            if (slider == null)
            {
                Debug.LogError("SettingsSlider: assign a slider.", this);
                return;
            }

            slider.onValueChanged.AddListener(HandleChanged);
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(HandleChanged);
            subscribed = false;
        }

        public void WriteDefault(GameSettings record)
        {
            if (record == null)
            {
                Debug.LogError("SettingsSlider: record is null.", this);
                return;
            }

            switch (binding)
            {
                case SettingsBinding.MasterVolume:
                    record.masterVolume = defaultValue;
                    break;
                case SettingsBinding.MusicVolume:
                    record.musicVolume = defaultValue;
                    break;
                case SettingsBinding.EffectsVolume:
                    record.effectsVolume = defaultValue;
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
                Debug.LogError("SettingsSlider: record is null.", this);
                return;
            }

            if (label == null || valueLabel == null || slider == null)
            {
                Debug.LogError("SettingsSlider: assign the label, value label, and slider.", this);
                return;
            }

            var value = ReadValue(record);
            label.text = displayName;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.SetValueWithoutNotify(value);
            valueLabel.text = GameSettings.FormatPercent(value, minimum, maximum);
        }

        void HandleChanged(float value)
        {
            if (!IsSupportedBinding())
            {
                return;
            }

            if (applicator == null || valueLabel == null)
            {
                Debug.LogError("SettingsSlider: assign the applicator and value label.", this);
                return;
            }

            applicator.SetFloat(binding, value);

            var record = applicator.Current;
            if (record == null)
            {
                return;
            }

            valueLabel.text = GameSettings.FormatPercent(ReadValue(record), minimum, maximum);
        }

        float ReadValue(GameSettings record)
        {
            switch (binding)
            {
                case SettingsBinding.MusicVolume:
                    return record.musicVolume;
                case SettingsBinding.EffectsVolume:
                    return record.effectsVolume;
                default:
                    return record.masterVolume;
            }
        }

        bool IsSupportedBinding()
        {
            if (binding == SettingsBinding.MasterVolume
                || binding == SettingsBinding.MusicVolume
                || binding == SettingsBinding.EffectsVolume)
            {
                return true;
            }

            LogUnsupportedBinding();
            return false;
        }

        void LogUnsupportedBinding()
        {
            Debug.LogError($"SettingsSlider: binding {binding} is not a volume binding.", this);
        }
    }
}
