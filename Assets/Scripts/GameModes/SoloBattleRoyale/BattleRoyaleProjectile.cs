using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public sealed class BattleRoyaleProjectile : MonoBehaviour
    {
        private BattleRoyaleCharacter owner;
        private Vector3 direction;
        private float damage;
        private float speed;
        private float lifetime;
        private float elapsed;

        public void Initialize(
            BattleRoyaleCharacter projectileOwner,
            Vector3 travelDirection,
            float projectileDamage,
            float projectileSpeed,
            float projectileLifetime)
        {
            owner = projectileOwner;
            direction = travelDirection.normalized;
            damage = projectileDamage;
            speed = projectileSpeed;
            lifetime = projectileLifetime;

            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader != null)
            {
                renderer.material = new Material(shader)
                {
                    color = Color.yellow
                };
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var previousPosition = transform.position;
            var nextPosition = previousPosition + direction * speed * Time.deltaTime;
            var travel = nextPosition - previousPosition;
            var hits = Physics.RaycastAll(
                previousPosition,
                travel.normalized,
                travel.magnitude,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(
                hits,
                (first, second) => first.distance.CompareTo(second.distance));
            foreach (var hit in hits)
            {
                var character =
                    hit.collider.GetComponentInParent<BattleRoyaleCharacter>();
                if (character == owner)
                {
                    continue;
                }

                if (character != null)
                {
                    character.ApplyDamage(damage, owner);
                    Destroy(gameObject);
                    return;
                }

                Destroy(gameObject);
                return;
            }

            transform.position = nextPosition;
        }
    }
}
