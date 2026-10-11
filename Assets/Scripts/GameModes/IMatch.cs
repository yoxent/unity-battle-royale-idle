using System.Collections.Generic;
using BattleRoyale.GameModes.SoloBattleRoyale;
using UnityEngine;

namespace BattleRoyale.GameModes
{
    public interface IMatch
    {
        TeamPlaystyle TeamPlaystyle { get; }
        BattleRoyaleProjectilePool Projectiles { get; }
        IReadOnlyList<BattleRoyaleCharacter> Characters { get; }
        void NotifyCharacterEliminated(
            BattleRoyaleCharacter eliminated,
            BattleRoyaleCharacter damageSource);
    }

    public interface IMatchNavigation
    {
        bool TryGetHazardDestination(Vector3 fromPosition, out Vector3 destination);
        Vector3 GetRetreatPoint(Vector3 fromPosition, Vector3 threatPosition);
        Vector3 GetWanderPoint();
    }
}
