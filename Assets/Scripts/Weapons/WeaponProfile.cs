using UnityEngine;

namespace BattleRoyale.Weapons
{
    public enum WeaponCategory
    {
        AssaultRifle = 0,
        Handgun = 1,
        SubmachineGun = 2,
        Shotgun = 3,
        LightMachineGun = 4,
        MarksmanRifle = 5,
        SniperRifle = 6
    }

    [CreateAssetMenu(
        fileName = "WeaponProfile",
        menuName = "Battle Royale/Weapons/Weapon Profile")]
    public sealed class WeaponProfile : ScriptableObject
    {
        public string displayName = "Service Rifle";
        public WeaponCategory category = WeaponCategory.AssaultRifle;

        [Min(0)]
        [Tooltip(
            "Purchase price for modes that sell guns, such as deathmatch. " +
            "Battle royale ignores this and spawns guns in the map.")]
        public int cost;

        public WeaponStats stats = new();
        public Color visualColor = new(0.12f, 0.12f, 0.12f);
    }
}
