using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Description;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace DawnsburryMods.Starlit_Span
{
    public static class ShootingStar
    {

        private static bool isRangedWeapon(Item? item)
        {
            if (item == null) return false;
            return item.HasTrait(Trait.Weapon) && item.HasTrait(Trait.Ranged);
        }

        public static SpellId loadShootingStar()
        {
            int arbitraryBigNumber = 300; 
            var target = Target.Ranged(arbitraryBigNumber)
                .WithAdditionalConditionOnTargetCreature((Func<Creature, Creature, Usability>)((self, enemy) =>
            {
                var usabilities = self.HeldItems.Select(item =>
                {
                    if (!isRangedWeapon(item)) return Usability.NotUsable("You must be wielding a Ranged Weapon.");
                    if (item!.EphemeralItemProperties.NeedsReload) return Usability.NotUsable("Weapon must be loaded.");
                    if (item!.WeaponProperties!.MaximumRange < self.DistanceTo(enemy)) return Usability.NotUsable("Enemy is out of your weapon's range.");
                    return Usability.Usable;
                });
                if (usabilities.Contains(Usability.Usable)) return Usability.Usable;
                if (!usabilities.Any()) return Usability.NotUsable("Must hold ranged weapon");
                String reasons = usabilities.Aggregate(String.Empty, (acc, x) => acc + x.UnusableReason + "  ");
                return Usability.NotUsable(reasons);
            }));
            return ModManager.RegisterNewSpell("Shooting Star", 0, (spellId, spellcaster, spellLevel, inCombat, spellInformation) => //SPELL CASTER IS HERE ARGH ASFJASPVVPSWEWEÜ
                Spells.CreateModern((Illustration)IllustrationName.DimensionalAssault, "Shooting Star", new Trait[4]
                {
                    Trait.Divination,
                    Trait.Magus,
                    Trait.Ranged,
                    Trait.Focus
                }
                , "You let loose a projectile that flies true and leaves the blazing trail of a meteor behind it.",
                (DescriptionDescriptor)"Make a ranged Strike, ignoring the target's concealment and reducing the target's cover by one degree for this Strike only " +
                "(greater to standard, standard to lesser, and lesser to none). If the Strike hits, the meteor trail hangs in the air. This gives the benefits of concealment negation and" +
                " cover reduction to any attacks made against the creature (by anyone) until the start of your next turn.",
                target, 1, null).WithActionCost(1).WithSoundEffect(SfxName.PhaseBolt).WithEffectOnChosenTargets((Func<Creature, ChosenTargets, Task>)(async (caster, targets) =>
                {
                    var rangedWeapon = caster.PrimaryItem;
                    if (!isRangedWeapon(rangedWeapon)) rangedWeapon = caster.SecondaryItem;
                    var strike = caster.CreateStrike(rangedWeapon!);
                    strike.ChosenTargets = targets;
                    
                    await strike.WithActionCost(0).AllExecute();
                    
                })));
        }

    }
}
