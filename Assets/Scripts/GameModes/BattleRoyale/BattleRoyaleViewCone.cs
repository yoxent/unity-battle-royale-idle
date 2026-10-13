using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleViewCone : MonoBehaviour
    {
        [SerializeField] private BattleRoyaleAgent agent;
        [SerializeField] private MeshRenderer coneRenderer;
        [SerializeField] private float viewAngle = 60f;
        [SerializeField] private float openViewLength = 400f;

        private bool loggedMissingAgent;
        private bool loggedMissingRenderer;

        private void LateUpdate()
        {
            if (agent == null)
            {
                if (!loggedMissingAgent)
                {
                    loggedMissingAgent = true;
                    Debug.LogError(
                        $"{nameof(BattleRoyaleViewCone)} is missing its agent.",
                        this);
                }

                return;
            }

            if (coneRenderer == null)
            {
                if (!loggedMissingRenderer)
                {
                    loggedMissingRenderer = true;
                    Debug.LogError(
                        $"{nameof(BattleRoyaleViewCone)} is missing its renderer.",
                        this);
                }

                return;
            }

            coneRenderer.enabled = agent.IsAlive;
            if (!agent.IsAlive)
            {
                return;
            }

            var length = openViewLength;
            var origin = agent.transform.position;
            origin.y += BattleRoyaleLineOfSight.SampleHeight;
            var forward = agent.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f &&
                BattleRoyaleLineOfSight.TryGetObstacleHit(
                    origin,
                    origin + forward.normalized * openViewLength,
                    origin.y,
                    out var hit))
            {
                var hitPoint = hit.point;
                hitPoint.y = origin.y;
                length = Vector3.Distance(origin, hitPoint);
            }

            length = Mathf.Max(0.5f, length);
            var radius = Mathf.Tan(viewAngle * 0.5f * Mathf.Deg2Rad);
            transform.localScale = new Vector3(
                length * radius,
                length * radius,
                length);
        }
    }
}
