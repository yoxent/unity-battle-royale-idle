using System.Collections.Generic;

namespace BattleRoyale.Settings
{
    public sealed class MemoryGameSettingsStore : IGameSettingsStore
    {
        readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        readonly Dictionary<string, float> floats = new Dictionary<string, float>();

        public bool Has(string key)
        {
            return ints.ContainsKey(key) || floats.ContainsKey(key);
        }

        public int GetInt(string key, int fallback)
        {
            return ints.TryGetValue(key, out var value) ? value : fallback;
        }

        public float GetFloat(string key, float fallback)
        {
            return floats.TryGetValue(key, out var value) ? value : fallback;
        }

        public void SetInt(string key, int value)
        {
            ints[key] = value;
        }

        public void SetFloat(string key, float value)
        {
            floats[key] = value;
        }

        public void Flush()
        {
        }
    }
}
