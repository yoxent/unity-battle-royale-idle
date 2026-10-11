using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleProjectilePool : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int initialCapacity = 64;

        private readonly Stack<BattleRoyaleProjectile> available = new();
        private Material sharedMaterial;
        private bool prewarmed;

        public void Prewarm()
        {
            if (prewarmed)
            {
                return;
            }

            EnsureMaterial();
            for (var index = 0; index < initialCapacity; index++)
            {
                available.Push(CreateProjectile());
            }

            prewarmed = true;
        }

        public void Spawn(
            BattleRoyaleCharacter owner,
            Vector3 origin,
            Vector3 direction,
            float damage,
            float speed,
            float lifetime,
            float visualScale)
        {
            EnsureMaterial();
            var projectile = available.Count > 0
                ? available.Pop()
                : CreateProjectile();
            projectile.gameObject.SetActive(true);
            projectile.Launch(
                owner,
                origin,
                direction,
                damage,
                speed,
                lifetime,
                visualScale);
        }

        public void Release(BattleRoyaleProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            available.Push(projectile);
        }

        private BattleRoyaleProjectile CreateProjectile()
        {
            var bulletObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObject.name = "PooledBullet";
            bulletObject.transform.SetParent(transform, false);
            var collider = bulletObject.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            var renderer = bulletObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = sharedMaterial;
            }

            var projectile = bulletObject.AddComponent<BattleRoyaleProjectile>();
            projectile.Bind(this);
            bulletObject.SetActive(false);
            return projectile;
        }

        private void EnsureMaterial()
        {
            if (sharedMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            sharedMaterial = shader == null
                ? new Material(Shader.Find("Sprites/Default"))
                : new Material(shader);
            var tracerColor = new Color(1f, 0.85f, 0.15f, 1f);
            sharedMaterial.color = tracerColor;
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                sharedMaterial.SetColor("_BaseColor", tracerColor);
            }
        }

        private void OnDestroy()
        {
            if (sharedMaterial != null)
            {
                Destroy(sharedMaterial);
            }
        }
    }
}
