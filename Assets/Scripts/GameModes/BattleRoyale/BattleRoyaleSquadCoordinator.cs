using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public enum BattleRoyaleSquadRole
    {
        Assault,
        Scout,
        Support,
        Defender,
        Marksman
    }

    public sealed class BattleRoyaleTeamObservation
    {
        public BattleRoyaleAgent Observer { get; private set; }
        public BattleRoyaleAgent Target { get; private set; }
        public int TeamIndex { get; private set; }
        public Vector3 Position { get; private set; }
        public float ObservedAt { get; private set; }
        public float Confidence { get; private set; }

        public BattleRoyaleTeamObservation(
            BattleRoyaleAgent observer,
            BattleRoyaleAgent target,
            Vector3 position,
            float observedAt,
            float confidence)
        {
            Update(observer, target, position, observedAt, confidence);
        }

        public void Update(
            BattleRoyaleAgent observer,
            BattleRoyaleAgent target,
            Vector3 position,
            float observedAt,
            float confidence)
        {
            Observer = observer;
            Target = target;
            TeamIndex = observer == null ? -1 : observer.TeamIndex;
            Position = position;
            ObservedAt = observedAt;
            Confidence = Mathf.Clamp01(confidence);
        }
    }

    public interface ITeamTacticalPicture
    {
        void PublishObservation(
            BattleRoyaleAgent observer,
            BattleRoyaleAgent target,
            Vector3 position,
            float confidence);

        bool TryGetFreshObservation(
            int teamIndex,
            BattleRoyaleAgent requester,
            float maxAge,
            out BattleRoyaleTeamObservation observation);

        bool IsTargetClaimed(
            BattleRoyaleAgent target,
            BattleRoyaleAgent requester);

        bool TryClaimTarget(
            BattleRoyaleAgent claimer,
            BattleRoyaleAgent target,
            float duration);

        void ReleaseTargetClaim(
            BattleRoyaleAgent claimer,
            BattleRoyaleAgent target);

        int CountPlaceClaims(
            int teamIndex,
            string anchorId,
            BattleRoyaleAgent except);

        bool TryClaimPlace(
            BattleRoyaleAgent claimer,
            string anchorId,
            float duration,
            int slots);
    }

    public sealed class BattleRoyaleSquadCoordinator : ITeamTacticalPicture
    {
        private sealed class TargetClaim
        {
            public BattleRoyaleAgent claimer;
            public BattleRoyaleAgent target;
            public int teamIndex;
            public float expiresAt;
        }

        private readonly Func<float> getTime;
        private readonly List<BattleRoyaleTeamObservation> observations = new();
        private readonly List<TargetClaim> claims = new();
        private readonly List<PlaceClaim> placeClaims = new();

        private sealed class PlaceClaim
        {
            public BattleRoyaleAgent claimer;
            public int teamIndex;
            public string anchorId;
            public float expiresAt;
        }

        public BattleRoyaleSquadCoordinator(Func<float> timeProvider)
        {
            getTime = timeProvider ?? (() => 0f);
        }

        public static BattleRoyaleSquadRole GetDefaultRole(
            BattleRoyaleAIStrategy strategy,
            BattleRoyaleAgentSpecialty specialty,
            int rosterIndex)
        {
            if (specialty == BattleRoyaleAgentSpecialty.Sharpshooter)
            {
                return BattleRoyaleSquadRole.Marksman;
            }

            return strategy switch
            {
                BattleRoyaleAIStrategy.AggressiveHunter =>
                    BattleRoyaleSquadRole.Assault,
                BattleRoyaleAIStrategy.AggressiveSkirmisher =>
                    BattleRoyaleSquadRole.Assault,
                BattleRoyaleAIStrategy.NeutralScout =>
                    BattleRoyaleSquadRole.Scout,
                BattleRoyaleAIStrategy.DefensiveAnchor =>
                    BattleRoyaleSquadRole.Defender,
                BattleRoyaleAIStrategy.DefensiveSurvivor =>
                    BattleRoyaleSquadRole.Defender,
                BattleRoyaleAIStrategy.NeutralOpportunist =>
                    BattleRoyaleSquadRole.Support,
                _ => (Mathf.Max(0, rosterIndex) % 2) == 0
                    ? BattleRoyaleSquadRole.Assault
                    : BattleRoyaleSquadRole.Support
            };
        }

        public void PublishObservation(
            BattleRoyaleAgent observer,
            BattleRoyaleAgent target,
            Vector3 position,
            float confidence)
        {
            if (observer == null ||
                target == null ||
                observer.TeamIndex < 0)
            {
                return;
            }

            var now = getTime();
            foreach (var observation in observations)
            {
                if (observation.Observer == observer)
                {
                    observation.Update(
                        observer,
                        target,
                        position,
                        now,
                        confidence);
                    return;
                }
            }

            observations.Add(
                new BattleRoyaleTeamObservation(
                    observer,
                    target,
                    position,
                    now,
                    confidence));
        }

        public bool TryGetFreshObservation(
            int teamIndex,
            BattleRoyaleAgent requester,
            float maxAge,
            out BattleRoyaleTeamObservation observation)
        {
            observation = null;
            if (teamIndex < 0)
            {
                return false;
            }

            var now = getTime();
            var ageLimit = Mathf.Max(0f, maxAge);
            foreach (var candidate in observations)
            {
                if (candidate.Observer == null ||
                    candidate.Observer == requester ||
                    candidate.TeamIndex != teamIndex ||
                    candidate.Target == null ||
                    now - candidate.ObservedAt > ageLimit)
                {
                    continue;
                }

                if (observation == null ||
                    candidate.Confidence > observation.Confidence ||
                    (Mathf.Approximately(
                         candidate.Confidence,
                         observation.Confidence) &&
                     candidate.ObservedAt > observation.ObservedAt))
                {
                    observation = candidate;
                }
            }

            return observation != null;
        }

        public bool IsTargetClaimed(
            BattleRoyaleAgent target,
            BattleRoyaleAgent requester)
        {
            if (target == null || requester == null || requester.TeamIndex < 0)
            {
                return false;
            }

            PruneExpiredClaims();
            foreach (var claim in claims)
            {
                if (claim.target == target &&
                    claim.teamIndex == requester.TeamIndex &&
                    claim.claimer != requester)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryClaimTarget(
            BattleRoyaleAgent claimer,
            BattleRoyaleAgent target,
            float duration)
        {
            if (claimer == null ||
                target == null ||
                claimer.TeamIndex < 0)
            {
                return false;
            }

            PruneExpiredClaims();
            var now = getTime();
            foreach (var claim in claims)
            {
                if (claim.target != target ||
                    claim.teamIndex != claimer.TeamIndex)
                {
                    continue;
                }

                if (claim.claimer != claimer)
                {
                    return false;
                }

                claim.expiresAt = now + Mathf.Max(0f, duration);
                return true;
            }

            claims.Add(
                new TargetClaim
                {
                    claimer = claimer,
                    target = target,
                    teamIndex = claimer.TeamIndex,
                    expiresAt = now + Mathf.Max(0f, duration)
                });
            return true;
        }

        public int CountPlaceClaims(
            int teamIndex,
            string anchorId,
            BattleRoyaleAgent except)
        {
            if (teamIndex < 0 || string.IsNullOrEmpty(anchorId))
            {
                return 0;
            }

            PruneExpiredClaims();
            var count = 0;
            for (var index = 0; index < placeClaims.Count; index++)
            {
                var claim = placeClaims[index];
                if (claim.teamIndex == teamIndex &&
                    claim.anchorId == anchorId &&
                    claim.claimer != except)
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryClaimPlace(
            BattleRoyaleAgent claimer,
            string anchorId,
            float duration,
            int slots)
        {
            if (claimer == null ||
                claimer.TeamIndex < 0 ||
                string.IsNullOrEmpty(anchorId))
            {
                return false;
            }

            PruneExpiredClaims();
            var allowed = Mathf.Max(1, slots);
            var now = getTime();
            for (var index = placeClaims.Count - 1; index >= 0; index--)
            {
                if (placeClaims[index].claimer == claimer &&
                    placeClaims[index].anchorId != anchorId)
                {
                    placeClaims.RemoveAt(index);
                }
            }

            var others = 0;
            PlaceClaim own = null;
            for (var index = 0; index < placeClaims.Count; index++)
            {
                var claim = placeClaims[index];
                if (claim.teamIndex != claimer.TeamIndex ||
                    claim.anchorId != anchorId)
                {
                    continue;
                }

                if (claim.claimer == claimer)
                {
                    own = claim;
                }
                else
                {
                    others++;
                }
            }

            if (own != null)
            {
                own.expiresAt = now + Mathf.Max(0f, duration);
                return true;
            }

            if (others >= allowed)
            {
                return false;
            }

            placeClaims.Add(
                new PlaceClaim
                {
                    claimer = claimer,
                    teamIndex = claimer.TeamIndex,
                    anchorId = anchorId,
                    expiresAt = now + Mathf.Max(0f, duration)
                });
            return true;
        }

        public void ReleaseTargetClaim(
            BattleRoyaleAgent claimer,
            BattleRoyaleAgent target)
        {
            for (var index = claims.Count - 1; index >= 0; index--)
            {
                if (claims[index].claimer == claimer &&
                    claims[index].target == target)
                {
                    claims.RemoveAt(index);
                }
            }
        }

        private void PruneExpiredClaims()
        {
            var now = getTime();
            for (var index = claims.Count - 1; index >= 0; index--)
            {
                if (claims[index].expiresAt <= now)
                {
                    claims.RemoveAt(index);
                }
            }

            for (var index = placeClaims.Count - 1; index >= 0; index--)
            {
                if (placeClaims[index].expiresAt <= now)
                {
                    placeClaims.RemoveAt(index);
                }
            }
        }
    }
}
