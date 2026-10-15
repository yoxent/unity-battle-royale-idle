using UnityEngine;

namespace BattleRoyale.GameModes
{
    public abstract class StatSheetView : MonoBehaviour
    {
        public void SetWatched(bool watched)
        {
            if (!Application.isPlaying)
            {
                if (!watched)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            gameObject.SetActive(watched);
        }

        public abstract bool Matches(SimulationMatch match);

        public abstract void Bind(SimulationMatch match);

        public abstract SimulationMatch BoundMatch { get; }
    }
}
