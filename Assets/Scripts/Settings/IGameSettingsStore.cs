namespace BattleRoyale.Settings
{
    public interface IGameSettingsStore
    {
        bool Has(string key);
        int GetInt(string key, int fallback);
        float GetFloat(string key, float fallback);
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void Flush();
    }
}
