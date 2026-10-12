using System;
using UnityEngine;

namespace BattleRoyale.CompanyIdentity
{
    public static class CompanyIdentityUiFactory
    {
        private const string PrefabResourceRoot = "CompanyIdentity/Ui/";

        public static GameObject InstantiatePrefab(string prefabName, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(PrefabResourceRoot + prefabName);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Company Identity prefab '{prefabName}' was not found at Resources/{PrefabResourceRoot}.");
            }

            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            return instance;
        }
    }
}
