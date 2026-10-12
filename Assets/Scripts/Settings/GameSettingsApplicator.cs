using UnityEngine;
using UnityEngine.Audio;

namespace BattleRoyale.Settings
{
    public sealed class GameSettingsApplicator : MonoBehaviour
    {
        [SerializeField] AudioMixer mixer;
        [SerializeField] string musicParameter = "MusicVolume";
        [SerializeField] string effectsParameter = "EffectsVolume";

        GameSettings current;
        IGameSettingsStore store;
        int desktopWidth;
        int desktopHeight;

        public GameSettings Current => current;
        public IGameSettingsStore Store => store;

        public void UseStore(IGameSettingsStore storeValue)
        {
            store = storeValue;
        }

        public void Load(int desktopWidthValue, int desktopHeightValue)
        {
            desktopWidth = desktopWidthValue;
            desktopHeight = desktopHeightValue;

            var activeStore = ResolveStore();
            current = GameSettings.Load(activeStore, desktopWidth, desktopHeight);
        }

        public bool HasSaved(SettingsBinding binding)
        {
            var activeStore = ResolveStore();

            switch (binding)
            {
                case SettingsBinding.CameraShake:
                    return activeStore.Has(GameSettingsKeys.CameraShake);
                case SettingsBinding.WindowMode:
                    return activeStore.Has(GameSettingsKeys.WindowMode);
                case SettingsBinding.Resolution:
                    return activeStore.Has(GameSettingsKeys.Width) && activeStore.Has(GameSettingsKeys.Height);
                case SettingsBinding.VSync:
                    return activeStore.Has(GameSettingsKeys.VSync);
                case SettingsBinding.MasterVolume:
                    return activeStore.Has(GameSettingsKeys.Master);
                case SettingsBinding.MusicVolume:
                    return activeStore.Has(GameSettingsKeys.Music);
                case SettingsBinding.EffectsVolume:
                    return activeStore.Has(GameSettingsKeys.Effects);
                default:
                    Debug.LogError($"GameSettingsApplicator: unknown binding {binding}.");
                    return false;
            }
        }

        public void ApplyCurrent()
        {
            if (current == null || !Application.isPlaying)
            {
                return;
            }

            current.Clamp(desktopWidth, desktopHeight);
            Apply();
        }

        public void Commit()
        {
            ApplyCurrent();
            if (current == null || !Application.isPlaying)
            {
                return;
            }

            current.Save(ResolveStore());
        }

        public void SetBool(SettingsBinding binding, bool value)
        {
            if (current == null)
            {
                Debug.LogError("GameSettingsApplicator: Current is null.");
                return;
            }

            switch (binding)
            {
                case SettingsBinding.CameraShake:
                    current.cameraShake = value;
                    break;
                case SettingsBinding.VSync:
                    current.vSync = value;
                    break;
                default:
                    Debug.LogError($"GameSettingsApplicator: SetBool does not support {binding}.");
                    return;
            }

            Commit();
        }

        public void SetFloat(SettingsBinding binding, float value)
        {
            if (current == null)
            {
                Debug.LogError("GameSettingsApplicator: Current is null.");
                return;
            }

            switch (binding)
            {
                case SettingsBinding.MasterVolume:
                    current.masterVolume = value;
                    break;
                case SettingsBinding.MusicVolume:
                    current.musicVolume = value;
                    break;
                case SettingsBinding.EffectsVolume:
                    current.effectsVolume = value;
                    break;
                default:
                    Debug.LogError($"GameSettingsApplicator: SetFloat does not support {binding}.");
                    return;
            }

            Commit();
        }

        public void SetWindowMode(SettingsWindowMode mode)
        {
            if (current == null)
            {
                Debug.LogError("GameSettingsApplicator: Current is null.");
                return;
            }

            current.windowMode = mode;
            Commit();
        }

        public void SetResolution(int width, int height)
        {
            if (current == null)
            {
                Debug.LogError("GameSettingsApplicator: Current is null.");
                return;
            }

            current.width = width;
            current.height = height;
            Commit();
        }

        void Apply()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Screen.SetResolution(
                current.width,
                current.height,
                GameSettings.ToFullScreenMode(current.windowMode));
            QualitySettings.vSyncCount = GameSettings.ToVSyncCount(current.vSync);
            AudioListener.volume = current.masterVolume;

            ApplyMixerVolumes();
        }

        void ApplyMixerVolumes()
        {
            if (mixer == null)
            {
                Debug.LogError("GameSettingsApplicator: assign an audio mixer.");
                return;
            }

            mixer.SetFloat(musicParameter, GameSettings.LinearToDecibels(current.musicVolume));
            mixer.SetFloat(effectsParameter, GameSettings.LinearToDecibels(current.effectsVolume));
        }

        IGameSettingsStore ResolveStore()
        {
            if (store == null)
            {
                store = new PlayerPrefsGameSettingsStore();
            }

            return store;
        }
    }
}
