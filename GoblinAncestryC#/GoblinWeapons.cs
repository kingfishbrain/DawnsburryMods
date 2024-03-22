﻿using Dawnsbury.Modding;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;


namespace Dawnsbury.Mods.Ancestries.Goblin;
{
    public static class GoblinWeapons
    {
        public static void RegisterWeapons()
        {
            ModManager.RegisterNewItemIntoTheShop("Boarslicer", itemName =>
            new Item(itemName, IllustrationName.Dogslicer, "Boarslicer", 0, 3, Trait.Backstabber, Trait.Agile, Trait.Goblin, Trait.Finesse, Trait.Weapon, Trait.Homebrew, Trait.Martial, Trait.Sword)
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Slashing)));
        }
    }
}