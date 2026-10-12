using UnityEngine;

namespace BattleRoyale.Settings
{
    public sealed class PlayerPrefsGameSettingsStore : IGameSettingsStore
    {
        public bool Has(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public int GetInt(string key, int fallback)
        {
            return PlayerPrefs.GetInt(key, fallback);
        }

        public float GetFloat(string key, float fallback)
        {
            return PlayerPrefs.GetFloat(key, fallback);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}
