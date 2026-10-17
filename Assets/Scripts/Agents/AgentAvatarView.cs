using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRoyale.Agents
{
    public sealed class AgentAvatarView : MonoBehaviour
    {
        [SerializeField] AgentAvatarCatalog catalog;
        [SerializeField] Image fillPrefab;
        [SerializeField] Part body;
        [SerializeField] Part kit;
        [SerializeField] Image face;
        [SerializeField] Part hair;
        [SerializeField] Image border;

        readonly List<Image> bodyFills = new();
        readonly List<Image> kitFills = new();
        readonly List<Image> hairFills = new();

        public bool TryShowRandom(System.Random random)
        {
            if (catalog == null)
            {
                Debug.LogError(name + " needs an avatar catalog.", this);
                return false;
            }

            if (random == null)
            {
                Debug.LogError(name + " needs a random source.", this);
                return false;
            }

            if (!catalog.TryCreateAppearance(random, out var appearance))
            {
                Debug.LogError(name + " could not roll an avatar.", this);
                return false;
            }

            Show(appearance);
            return true;
        }

        public void SetBorder(Color color)
        {
            if (border == null)
            {
                Debug.LogError(name + " is missing its border.", this);
                return;
            }

            border.color = color;
        }

        public void Show(AgentAvatarAppearance appearance)
        {
            if (catalog == null)
            {
                Debug.LogError(name + " needs an avatar catalog.", this);
                return;
            }

            if (appearance == null)
            {
                Debug.LogError(name + " needs an avatar appearance.", this);
                return;
            }

            if (fillPrefab == null)
            {
                Debug.LogError(name + " needs an avatar fill prefab.", this);
                return;
            }

            if (face == null)
            {
                Debug.LogError(name + " is missing its face image.", this);
                return;
            }

            if (!Ready(body, "body") ||
                !Ready(kit, "kit") ||
                !Ready(hair, "hair"))
            {
                return;
            }

            ShowPart(
                body,
                bodyFills,
                catalog.TryGetBody(appearance.bodyIndex, out var bodyPart),
                bodyPart,
                appearance.bodyColors,
                appearance.bodyIndex,
                "body");
            ShowPart(
                kit,
                kitFills,
                catalog.TryGetKit(appearance.kitIndex, out var kitPart),
                kitPart,
                appearance.kitColors,
                appearance.kitIndex,
                "kit");
            ShowSprite(
                face,
                catalog.TryGetFace(appearance.faceIndex, out var faceSprite),
                faceSprite,
                Color.white,
                appearance.faceIndex,
                "face");
            ShowPart(
                hair,
                hairFills,
                catalog.TryGetHair(appearance.hairIndex, out var hairPart),
                hairPart,
                appearance.hairColors,
                appearance.hairIndex,
                "hair");
        }

        bool Ready(Part part, string label)
        {
            if (part != null && part.Fills != null && part.Outline != null)
            {
                return true;
            }

            Debug.LogError(name + " is missing its " + label + " images.", this);
            return false;
        }

        void ShowSprite(
            Image image,
            bool found,
            Sprite sprite,
            Color color,
            int index,
            string label)
        {
            if (!found)
            {
                image.sprite = null;
                image.color = Color.white;
                image.enabled = false;
                Debug.LogError(
                    name + " has no " + label + " sprite at index " + index + ".",
                    this);
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.enabled = sprite != null;
        }

        void ShowPart(
            Part part,
            List<Image> pool,
            bool found,
            AgentAvatarSprites sprites,
            Color[] colors,
            int index,
            string label)
        {
            if (!found)
            {
                Hide(part, pool);
                Debug.LogError(
                    name + " has no " + label + " sprite at index " + index + ".",
                    this);
                return;
            }

            part.Outline.sprite = sprites.Outline;
            part.Outline.color = Color.white;
            part.Outline.enabled = sprites.Outline != null;
            var fills = sprites.Sprites;
            for (var slot = 0; slot < fills.Count; slot++)
            {
                var image = Slot(pool, part.Fills, slot);
                var sprite = fills[slot];
                image.sprite = sprite;
                image.color = ColorAt(colors, slot);
                image.enabled = sprite != null;
                image.gameObject.SetActive(true);
                image.transform.SetSiblingIndex(slot);
            }

            HideSpare(pool, fills.Count);
        }

        Image Slot(List<Image> pool, RectTransform parent, int slot)
        {
            while (pool.Count <= slot)
            {
                pool.Add(Instantiate(fillPrefab, parent));
            }

            return pool[slot];
        }

        static Color ColorAt(Color[] colors, int index)
        {
            if (colors == null || index < 0 || index >= colors.Length)
            {
                return Color.white;
            }

            return colors[index];
        }

        static void Hide(Part part, List<Image> pool)
        {
            part.Outline.sprite = null;
            part.Outline.color = Color.white;
            part.Outline.enabled = false;
            HideSpare(pool, 0);
        }

        static void HideSpare(List<Image> pool, int used)
        {
            for (var index = used; index < pool.Count; index++)
            {
                if (pool[index] != null)
                {
                    pool[index].color = Color.white;
                    pool[index].gameObject.SetActive(false);
                }
            }
        }

        [Serializable]
        sealed class Part
        {
            [SerializeField] RectTransform fills;
            [SerializeField] Image outline;

            public RectTransform Fills => fills;

            public Image Outline => outline;
        }
    }
}
