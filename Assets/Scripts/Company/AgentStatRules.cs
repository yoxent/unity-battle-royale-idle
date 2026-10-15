namespace BattleRoyale.Company
{
    public static class AgentStatRules
    {
        public const float SharedHealth = 100f;
        public const float MinimumMoveSpeed = 5f;
        public const float MaximumMoveSpeed = 12f;
        public const float MinimumPerception = 50f;
        public const float MaximumPerception = 120f;
        public const float MaximumBulletMitigation = 0.8f;
        public const float MaximumHazardMitigation = 0.8f;
        public const float MaximumHoldSettleReduction = 0.5f;

        public static float MoveSpeed(int athleticism)
        {
            return Lerp(MinimumMoveSpeed, MaximumMoveSpeed, athleticism);
        }

        public static float Perception(int awareness)
        {
            return Lerp(MinimumPerception, MaximumPerception, awareness);
        }

        public static float Rating(int roll)
        {
            return ClampRoll(roll) / 100f;
        }

        public static float BulletDamage(float incoming, int resilience)
        {
            return Mitigate(incoming, resilience, MaximumBulletMitigation);
        }

        public static float HazardDamage(float incoming, int endurance)
        {
            return Mitigate(incoming, endurance, MaximumHazardMitigation);
        }

        public static float HoldSettleSeconds(float baseSeconds, int endurance)
        {
            return Mitigate(baseSeconds, endurance, MaximumHoldSettleReduction);
        }

        public static float StanceWeight(int discipline)
        {
            return Rating(discipline);
        }

        static float Lerp(float from, float to, int roll)
        {
            return from + (to - from) * Rating(roll);
        }

        static float Mitigate(float incoming, int roll, float maximumReduction)
        {
            if (incoming <= 0f)
            {
                return 0f;
            }

            var kept = 1f - maximumReduction * Rating(roll);
            return incoming * kept;
        }

        static int ClampRoll(int roll)
        {
            if (roll < 0)
            {
                return 0;
            }

            if (roll > 100)
            {
                return 100;
            }

            return roll;
        }
    }
}
