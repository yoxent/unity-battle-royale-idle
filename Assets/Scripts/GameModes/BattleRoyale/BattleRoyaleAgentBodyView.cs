using KevinIglesias;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public sealed class BattleRoyaleAgentBodyView : MonoBehaviour
    {
        const float RunSpeedSquared = 0.04f;

        [SerializeField] private Transform visual;
        [SerializeField] private HumanSoldierController soldier;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private NavMeshAgent navigation;

        private bool living = true;
        private bool running;
        private bool posed;
        private bool missingSoldierReported;

        public Transform Visual => visual;

        public void Place(float visualScale)
        {
            if (visual == null)
            {
                Debug.LogError(
                    "BattleRoyaleAgentBodyView is missing its visual.",
                    this);
                return;
            }

            var scale = Mathf.Max(0.1f, visualScale);
            visual.localScale = Vector3.one * scale;
            var local = visual.localPosition;
            local.y = 0f;
            visual.localPosition = local;
        }

        public void ApplyColor(Color color)
        {
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogError(
                    "BattleRoyaleAgentBodyView is missing its renderers.",
                    this);
                return;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.material.color = color;
            }
        }

        public void PresentLiving()
        {
            living = true;
            posed = false;
            running = false;
            Pose(SoldierMovement.NoMovement, SoldierAction.Nothing);
        }

        public void PresentEliminated()
        {
            living = false;
            Pose(SoldierMovement.NoMovement, SoldierAction.Death01);
        }

        private void Update()
        {
            if (!living || !HasSoldier() || navigation == null)
            {
                return;
            }

            var velocity = navigation.velocity;
            velocity.y = 0f;
            var nextRunning = velocity.sqrMagnitude > RunSpeedSquared;
            if (posed && nextRunning == running)
            {
                return;
            }

            posed = true;
            running = nextRunning;
            Pose(
                nextRunning ? SoldierMovement.Run : SoldierMovement.NoMovement,
                SoldierAction.Nothing);
        }

        private void LateUpdate()
        {
            if (living || soldier == null || soldier.action != SoldierAction.Death01)
            {
                return;
            }

            if (soldier.animator == null)
            {
                return;
            }

            var current = soldier.animator.GetCurrentAnimatorStateInfo(0);
            var enteredDeath = current.IsName("Death01");
            if (!enteredDeath && soldier.animator.IsInTransition(0))
            {
                enteredDeath = soldier.animator.GetNextAnimatorStateInfo(0).IsName("Death01");
            }

            if (enteredDeath)
            {
                soldier.action = SoldierAction.Nothing;
            }
        }

        private void Pose(SoldierMovement movement, SoldierAction action)
        {
            if (!HasSoldier())
            {
                return;
            }

            soldier.equippedWeapon = SoldierWeapons.Rifle;
            soldier.position = SoldierPosition.StandUp;
            soldier.movement = movement;
            soldier.action = action;
        }

        private bool HasSoldier()
        {
            if (soldier != null)
            {
                return true;
            }

            if (!missingSoldierReported)
            {
                missingSoldierReported = true;
                Debug.LogError(
                    "BattleRoyaleAgentBodyView is missing its soldier.",
                    this);
            }

            return false;
        }
    }
}
