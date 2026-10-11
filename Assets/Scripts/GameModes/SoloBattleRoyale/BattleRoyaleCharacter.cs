using System;
using BattleRoyale.GameModes;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BattleRoyaleCharacter : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private BattleRoyaleCharacterProfile profile;
        [SerializeField] private BattleRoyaleAIStrategy strategy;
        [SerializeField] private BattleRoyaleAgentSpecialty specialty =
            BattleRoyaleAgentSpecialty.Balanced;
        [SerializeField] private float specialtyScore;
        [SerializeField] private float killBuffRemaining;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isBot = true;
        [SerializeField] private int teamIndex = -1;

        private static readonly Color FiringColor = new(1f, 0.45f, 0.1f);

        private Renderer characterRenderer;
        private BattleRoyaleWeapon weapon;
        private IMatch match;
        private bool hasTeamColor;
        private Color teamColor = Color.white;
        private BattleRoyaleSpecialtyModifiers specialtyModifiers =
            BattleRoyaleAgentSpecialtyRules.GetModifiers(
                BattleRoyaleAgentSpecialty.Balanced);
        private Vector3 lastPosition;
        private float stationaryDuration;

        public event Action<BattleRoyaleCharacter, BattleRoyaleCharacter> Eliminated;

        public BattleRoyaleCharacterProfile Profile => profile;
        public BattleRoyaleAIStrategy Strategy => strategy;
        public BattleRoyaleAgentSpecialty Specialty => specialty;
        public float SpecialtyScore => specialtyScore;
        public BattleRoyaleSpecialtyModifiers SpecialtyModifiers =>
            specialtyModifiers;
        public BattleRoyaleCharacterStats AgentStats => profile?.stats;
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
        public bool IsAlive => currentHealth > 0f;
        public BattleRoyaleCharacterState State { get; private set; }

        private float HoldingPositionActivationTime =>
            profile == null || profile.ai == null
                ? 3f
                : Mathf.Max(
                    0.1f,
                    profile.ai.holdingPositionActivationTime);

        public void Initialize(
            IMatch owningMatch,
            BattleRoyaleCharacterProfile characterProfile,
            BattleRoyaleAIStrategy characterStrategy,
            bool bot)
        {
            match = owningMatch;
            profile = characterProfile;
            strategy = characterStrategy;
            isBot = bot;
            teamIndex = -1;
            hasTeamColor = false;
            agent = GetComponent<NavMeshAgent>();
            if (profile == null)
            {
                Debug.LogError("A Battle Royale character requires a profile.", this);
                return;
            }

            if (profile.stats == null)
            {
                profile.stats = new BattleRoyaleCharacterStats();
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
            State = BattleRoyaleCharacterState.Spawned;

            ConfigureAgent();
            CreatePrimitiveVisual();
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
            State = BattleRoyaleCharacterState.Spawned;
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

        public void RegisterKill()
        {
            if (specialtyModifiers.killBuffDuration <= 0f)
            {
                return;
            }

            killBuffRemaining = specialtyModifiers.killBuffDuration;
        }

        public void SetState(BattleRoyaleCharacterState nextState)
        {
            if (!IsAlive && nextState != BattleRoyaleCharacterState.Eliminated)
            {
                return;
            }

            State = nextState;
            if (nextState != BattleRoyaleCharacterState.Eliminated)
            {
                ApplyStateColor();
            }
        }

        public void ApplyDamage(
            float damage,
            BattleRoyaleCharacter damageSource = null)
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

            State = BattleRoyaleCharacterState.Eliminated;
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (characterRenderer != null)
            {
                characterRenderer.material.color = Color.gray;
            }

            Eliminated?.Invoke(this, damageSource);
            match?.NotifyCharacterEliminated(this, damageSource);
        }

        private void ApplyStateColor()
        {
            if (characterRenderer == null || profile == null)
            {
                return;
            }

            var bodyColor = hasTeamColor ? teamColor : profile.visualColor;
            characterRenderer.material.color = State == BattleRoyaleCharacterState.Engaging
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
            agent.radius = 0.5f;
            agent.height = 2f;
        }

        private void CreatePrimitiveVisual()
        {
            var existingVisual = transform.Find("CharacterCapsule");
            if (existingVisual != null)
            {
                characterRenderer = existingVisual.GetComponent<Renderer>();
                return;
            }

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "CharacterCapsule";
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
            characterRenderer = visual.GetComponent<Renderer>();
            if (characterRenderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader != null)
            {
                characterRenderer.material = new Material(shader)
                {
                    color = profile.visualColor
                };
            }
        }
    }
}
