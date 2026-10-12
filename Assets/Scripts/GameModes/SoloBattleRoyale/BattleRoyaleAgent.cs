using System;
using BattleRoyale.GameModes;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BattleRoyaleAgent : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private BattleRoyaleAgentProfile profile;
        [SerializeField] private BattleRoyaleAIStrategy strategy;
        [SerializeField] private BattleRoyaleAgentSpecialty specialty =
            BattleRoyaleAgentSpecialty.Balanced;
        [SerializeField] private BattleRoyaleSquadRole squadRole =
            BattleRoyaleSquadRole.Assault;
        [SerializeField] private float specialtyScore;
        [SerializeField] private float killBuffRemaining;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isBot = true;
        [SerializeField] private int teamIndex = -1;

        private static readonly Color FiringColor = new(1f, 0.45f, 0.1f);

        private Renderer agentRenderer;
        private BattleRoyaleWeapon weapon;
        private IMatch match;
        private bool hasTeamColor;
        private Color teamColor = Color.white;
        private BattleRoyaleSpecialtyModifiers specialtyModifiers =
            BattleRoyaleAgentSpecialtyRules.GetModifiers(
                BattleRoyaleAgentSpecialty.Balanced);
        private Vector3 lastPosition;
        private float stationaryDuration;

        public event Action<BattleRoyaleAgent, BattleRoyaleAgent> Eliminated;

        public BattleRoyaleAgentProfile Profile => profile;
        public BattleRoyaleAIStrategy Strategy => strategy;
        public BattleRoyaleAgentSpecialty Specialty => specialty;
        public BattleRoyaleSquadRole SquadRole => squadRole;
        public float SpecialtyScore => specialtyScore;
        public BattleRoyaleSpecialtyModifiers SpecialtyModifiers =>
            specialtyModifiers;
        public BattleRoyaleAgentStats AgentStats => profile?.stats;
        public NavMeshAgent Agent => agent;
        public BattleRoyaleWeapon Weapon => weapon;
        public IMatch Match => match;
        public int TeamIndex => teamIndex;
        public float MaxHealth =>
            AgentStats == null
                ? 0f
                : AgentStats.maxHealth *
                  specialtyModifiers.maxHealthMultiplier;
        public float MoveSpeed =>
            AgentStats == null
                ? 0f
                : AgentStats.moveSpeed *
                  Mathf.Lerp(0.85f, 1.15f, AgentStats.athleticism) *
                  specialtyModifiers.moveSpeedMultiplier *
                  (HasKillBuff
                      ? specialtyModifiers.killBuffMoveSpeedMultiplier
                      : 1f);
        public float PerceptionRange =>
            AgentStats == null
                ? 0f
                : AgentStats.perceptionRange *
                  Mathf.Lerp(0.75f, 1.25f, AgentStats.awareness) *
                  specialtyModifiers.perceptionMultiplier;
        public float WeaponRange =>
            weapon == null
                ? 0f
                : weapon.Range * specialtyModifiers.weaponRangeMultiplier;
        public float WeaponDamageMultiplier =>
            specialtyModifiers.weaponDamageMultiplier *
            (HasKillBuff
                ? specialtyModifiers.killBuffDamageMultiplier
                : 1f);
        public float WeaponFireRateMultiplier =>
            specialtyModifiers.weaponFireRateMultiplier *
            (HasKillBuff
                ? specialtyModifiers.killBuffFireRateMultiplier
                : 1f);
        public float WeaponAccuracy =>
            Mathf.Clamp01(
                (AgentStats == null ? 1f : AgentStats.accuracy) +
                specialtyModifiers.accuracyBonus +
                (IsHoldingPosition
                    ? specialtyModifiers.holdingAccuracyBonus
                    : 0f) +
                (HasKillBuff
                    ? specialtyModifiers.killBuffAccuracyBonus
                    : 0f));
        public float DamageTakenMultiplier =>
            specialtyModifiers.damageTakenMultiplier *
            (IsHoldingPosition
                ? specialtyModifiers.holdingDamageTakenMultiplier
                : 1f);
        public float SafeZoneDamageMultiplier =>
            specialtyModifiers.safeZoneDamageMultiplier;
        public bool IsHoldingPosition =>
            stationaryDuration >= HoldingPositionActivationTime;
        public bool HasKillBuff => killBuffRemaining > 0f;
        public float CurrentHealth => currentHealth;
        public float HealthPercent =>
            MaxHealth <= 0f
                ? 0f
                : currentHealth / MaxHealth;
        public bool IsBot => isBot;
        public float BodyRadius
        {
            get
            {
                var body = GetComponent<CapsuleCollider>();
                if (body == null)
                {
                    return agent == null ? 0.5f : agent.radius;
                }

                var scale = transform.lossyScale;
                return body.radius *
                       Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            }
        }
        public bool IsAlive => currentHealth > 0f;
        public BattleRoyaleAgentState State { get; private set; }

        private float HoldingPositionActivationTime =>
            profile == null || profile.ai == null
                ? 3f
                : Mathf.Max(
                    0.1f,
                    profile.ai.holdingPositionActivationTime);

        public void Initialize(
            IMatch owningMatch,
            BattleRoyaleAgentProfile agentProfile,
            BattleRoyaleAIStrategy agentStrategy,
            bool bot)
        {
            match = owningMatch;
            profile = agentProfile;
            strategy = agentStrategy;
            isBot = bot;
            teamIndex = -1;
            hasTeamColor = false;
            agent = GetComponent<NavMeshAgent>();
            if (profile == null)
            {
                Debug.LogError("A Battle Royale agent requires a profile.", this);
                return;
            }

            if (profile.stats == null)
            {
                profile.stats = new BattleRoyaleAgentStats();
            }

            if (profile.ai == null)
            {
                profile.ai = new BattleRoyaleAISettings();
            }

            var specialtyEvaluation =
                BattleRoyaleAgentSpecialtyRules.Evaluate(
                    profile.stats,
                    profile.ai);
            specialty = specialtyEvaluation.Specialty;
            specialtyScore = specialtyEvaluation.Score;
            specialtyModifiers =
                BattleRoyaleAgentSpecialtyRules.GetModifiers(specialty);
            currentHealth = Mathf.Max(1f, MaxHealth);
            lastPosition = transform.position;
            stationaryDuration = 0f;
            State = BattleRoyaleAgentState.Spawned;

            CreatePrimitiveVisual();
            ConfigureAgent();
            weapon = GetComponent<BattleRoyaleWeapon>();
            if (weapon == null)
            {
                weapon = gameObject.AddComponent<BattleRoyaleWeapon>();
            }

            weapon.Initialize(this, profile.defaultWeapon);
        }

        public void AssignTeam(int index)
        {
            teamIndex = index;
        }

        public void AssignSquadRole(BattleRoyaleSquadRole role)
        {
            squadRole = role;
        }

        public void SetTeamColor(Color color)
        {
            hasTeamColor = true;
            teamColor = color;
            ApplyStateColor();
        }

        public void Respawn(Vector3 position)
        {
            currentHealth = Mathf.Max(1f, MaxHealth);
            killBuffRemaining = 0f;
            stationaryDuration = 0f;
            State = BattleRoyaleAgentState.Spawned;
            transform.position = position;
            lastPosition = position;
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.Warp(position);
            }

            ApplyStateColor();
        }

        private void Update()
        {
            if (killBuffRemaining > 0f)
            {
                killBuffRemaining = Mathf.Max(
                    0f,
                    killBuffRemaining - Time.deltaTime);
            }

            var movement = transform.position - lastPosition;
            movement.y = 0f;
            stationaryDuration = movement.sqrMagnitude <= 0.0025f
                ? stationaryDuration + Time.deltaTime
                : 0f;
            lastPosition = transform.position;

            if (agent != null && agent.enabled)
            {
                agent.speed = MoveSpeed;
            }
        }

        private void LateUpdate()
        {
            KeepBodiesApart();
        }

        public void RegisterKill()
        {
            if (specialtyModifiers.killBuffDuration <= 0f)
            {
                return;
            }

            killBuffRemaining = specialtyModifiers.killBuffDuration;
        }

        public void SetState(BattleRoyaleAgentState nextState)
        {
            if (!IsAlive && nextState != BattleRoyaleAgentState.Eliminated)
            {
                return;
            }

            State = nextState;
            if (nextState != BattleRoyaleAgentState.Eliminated)
            {
                ApplyStateColor();
            }
        }

        public void ApplyDamage(
            float damage,
            BattleRoyaleAgent damageSource = null)
        {
            if (!IsAlive || damage <= 0f)
            {
                return;
            }

            var resilience = profile == null || profile.stats == null
                ? 0f
                : Mathf.Clamp01(profile.stats.resilience);
            var mitigatedDamage = damage *
                                  Mathf.Lerp(1f, 0.65f, resilience) *
                                  DamageTakenMultiplier;
            currentHealth = Mathf.Max(0f, currentHealth - mitigatedDamage);
            if (currentHealth > 0f)
            {
                return;
            }

            State = BattleRoyaleAgentState.Eliminated;
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (agentRenderer != null)
            {
                agentRenderer.material.color = Color.gray;
            }

            Eliminated?.Invoke(this, damageSource);
            match?.NotifyAgentEliminated(this, damageSource);
        }

        private void ApplyStateColor()
        {
            if (agentRenderer == null || profile == null)
            {
                return;
            }

            var bodyColor = hasTeamColor ? teamColor : profile.visualColor;
            agentRenderer.material.color = State == BattleRoyaleAgentState.Engaging
                ? FiringColor
                : bodyColor;
        }

        private void ConfigureAgent()
        {
            if (agent == null || profile == null || profile.stats == null)
            {
                return;
            }

            agent.speed = MoveSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 40f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.updateRotation = false;
            var body = GetComponent<CapsuleCollider>();
            if (body != null)
            {
                ApplyClearance(agent, body);
            }
        }

        public static void ApplyClearance(NavMeshAgent navigation, CapsuleCollider body)
        {
            if (navigation == null || body == null)
            {
                return;
            }

            var scale = body.transform.lossyScale;
            navigation.radius = body.radius *
                                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            navigation.height = body.height * Mathf.Abs(scale.y);
            navigation.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        public static Vector3 SeparationOffset(
            Vector3 selfPosition,
            float selfRadius,
            Vector3 otherPosition,
            float otherRadius,
            bool otherCanMove)
        {
            var delta = selfPosition - otherPosition;
            delta.y = 0f;
            var distance = delta.magnitude;
            var gap = selfRadius + otherRadius;
            if (distance >= gap - 0.05f)
            {
                return Vector3.zero;
            }

            var direction = distance <= 0.001f
                ? Vector3.forward
                : delta / distance;
            var share = otherCanMove ? 0.5f : 1f;
            return direction * (gap - distance) * share;
        }

        private void KeepBodiesApart()
        {
            if (!IsAlive || match == null)
            {
                return;
            }

            var push = Vector3.zero;
            var radius = BodyRadius;
            foreach (var other in match.Agents)
            {
                if (other == null || other == this || !other.IsAlive)
                {
                    continue;
                }

                var selfPosition = transform.position;
                var otherPosition = other.transform.position;
                if ((selfPosition - otherPosition).sqrMagnitude <= 0.0001f)
                {
                    selfPosition += GetInstanceID() < other.GetInstanceID()
                        ? Vector3.right
                        : Vector3.left;
                }

                push += SeparationOffset(
                    selfPosition,
                    radius,
                    otherPosition,
                    other.BodyRadius,
                    other.IsAlive);
            }

            if (push.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var destination = transform.position + push;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(
                        destination,
                        out var hit,
                        push.magnitude + 1f,
                        NavMesh.AllAreas) &&
                    agent.Warp(hit.position))
                {
                    return;
                }
            }

            transform.position = destination;
        }

        private void CreatePrimitiveVisual()
        {
            var existingVisual = transform.Find("AgentCapsule");
            if (existingVisual != null)
            {
                agentRenderer = existingVisual.GetComponent<Renderer>();
                return;
            }

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "AgentCapsule";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * profile.visualScale;
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            var bodyCollider = GetComponent<CapsuleCollider>();
            if (bodyCollider == null)
            {
                bodyCollider = gameObject.AddComponent<CapsuleCollider>();
            }

            var visualScale = Mathf.Max(0.1f, profile.visualScale);
            bodyCollider.center = Vector3.zero;
            bodyCollider.radius = 0.5f * visualScale;
            bodyCollider.height = 2f * visualScale;
            agentRenderer = visual.GetComponent<Renderer>();
            if (agentRenderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader != null)
            {
                agentRenderer.material = new Material(shader)
                {
                    color = profile.visualColor
                };
            }
        }
    }
}
