﻿using Dawnsbury.Modding;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;


namespace GoblinAncestry.GoblinAncestry;
public static class Weapons
{
    public static void RegisterWeapons()
    {
        ModManager.RegisterNewItemIntoTheShop("Boarslicer", itemName =>
        new Item(itemName, IllustrationName.Dogslicer, "Boarslicer", 0, 3, Trait.TwoHanded, Trait.Backstabber, Trait.Agile, GoblinAncestryLoader.GoblinTrait, Trait.Finesse, Trait.Weapon, Trait.Homebrew, Trait.Martial, Trait.Sword)
            .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Slashing))
            .WithDescription("Sometimes you need to slice something bigger than a dog."));
    }
}
