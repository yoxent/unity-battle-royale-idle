using UnityEngine;

namespace BattleRoyale.Agents
{
    public sealed class PlayerPrefsAgentAvatarStore : IAgentAvatarStore
    {
        const string Prefix = "agent-avatar.";

        public bool TryLoad(string agentId, out string data)
        {
            data = null;
            if (!TryKey(agentId, out var key) || !PlayerPrefs.HasKey(key))
            {
                return false;
            }

            data = PlayerPrefs.GetString(key, string.Empty);
            return !string.IsNullOrEmpty(data);
        }

        public void Save(string agentId, string data)
        {
            if (!TryKey(agentId, out var key))
            {
                return;
            }

            PlayerPrefs.SetString(key, data ?? string.Empty);
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }

        static bool TryKey(string agentId, out string key)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                key = null;
                return false;
            }

            key = Prefix + agentId.Trim();
            return true;
        }
    }
}
