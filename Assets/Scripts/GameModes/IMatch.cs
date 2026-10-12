using System.Collections.Generic;
using BattleRoyale.GameModes.SoloBattleRoyale;
using UnityEngine;

namespace BattleRoyale.GameModes
{
    public interface IMatch
    {
        TeamPlaystyle TeamPlaystyle { get; }
        BattleRoyaleProjectilePool Projectiles { get; }
        IReadOnlyList<BattleRoyaleAgent> Agents { get; }
        void NotifyAgentEliminated(
            BattleRoyaleAgent eliminated,
            BattleRoyaleAgent damageSource);
    }

    public interface IMatchNavigation
    {
        bool TryGetHazardDestination(Vector3 fromPosition, out Vector3 destination);
        Vector3 GetRetreatPoint(Vector3 fromPosition, Vector3 threatPosition);
        Vector3 GetWanderPoint();
    }

    public interface ITacticalAnchorProvider
    {
        bool TryGetTacticalAnchor(
            Vector3 fromPosition,
            Vector3 threatPosition,
            float preferredRange,
            out Vector3 destination);
    }

    public interface IAgentDirectiveProvider
    {
        BattleRoyaleAgentDirective CurrentDirective { get; }
    }
}
