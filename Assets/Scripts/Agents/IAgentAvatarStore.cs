namespace BattleRoyale.Agents
{
    public interface IAgentAvatarStore
    {
        bool TryLoad(string agentId, out string data);

        void Save(string agentId, string data);

        void Flush();
    }
}
