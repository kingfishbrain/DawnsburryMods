using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;


namespace DawnsburryMods
{
    public static class DistractingFeint
    {
        [DawnsburyDaysModMainMethod]
        public static void loadMod()
        {
            Feat DistractingFeint = loadDistractingFeint();
            ModManager.AddFeat(DistractingFeint);

            ModManager.RegisterActionOnEachCreature(creature =>
                {
                    if (creature.HasFeat(FeatName.ScoundrelRacket))
                    {
                        creature.AddQEffect(new QEffect("Scoundrel Racket", "If you Feint while wielding an agile or finesse melee weapon, you can Step immediately after the Feint as a free action.")
                        {
                            AfterYouTakeActionAgainstTarget = (Func<QEffect, CombatAction, Creature, CheckResult, Task>)(async(effect, action, target, checkResult) =>
                            {
                                if (action.Name == "Feint")
                                {
                                    await effect.Owner.StrideAsync("Take a step.", allowStep: true, maximumFiveFeet: true, allowCancel: true);
                                }
                            }
    )
                        }
);
                    }
                }
                );

            ModManager.RegisterNewItemIntoTheShop("Dancer's Spear", itemName =>
            new Item(itemName, IllustrationName.Glaive, "Dancer's Spear", 0, 3, Trait.TwoHanded, Trait.Backswing, Trait.Finesse, Trait.Reach, Trait.Weapon, Trait.Martial, Trait.Spear, Trait.VersatileB, Trait.Sweep)
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                .WithDescription("Originally developed for an elaborate dancing performance, it was discovered it made for an effective weapon after an unfortunate accident."));
            ModManager.RegisterNewItemIntoTheShop("Elven Branched Spear", itemName =>
            new Item(itemName, IllustrationName.Glaive, "Elven Branched Spear", 0, 3, Trait.TwoHanded, Trait.DeadlyD8, Trait.Finesse, Trait.Reach, Trait.Weapon, Trait.Martial, Trait.Spear)
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                .WithDescription("Originally developed for an elaborate dancing performance, it was discovered it made for an effective weapon after an unfortunate accident."));
            ModManager.RegisterNewItemIntoTheShop("Boarding Pike", itemName =>
                 new Item(itemName, IllustrationName.Longspear, "Boarding Pike", 0, 1, Trait.TwoHanded, Trait.Shove, Trait.Reach, Trait.Weapon, Trait.Martial, Trait.Polearm)
                .WithWeaponProperties(new WeaponProperties("1d10", DamageKind.Piercing))
                .WithDescription("Taking the form of a longspear fitted with crossbars or hooks, a boarding pike provides its wielder a sharp implement that's as adept at shoving enemies off a ship's railings as facilitating the boarding of other vessels. "));



        }

        private static Feat loadDistractingFeint()
        {
            var DistractingFeintName = ModManager.RegisterFeatName("Distracting Feint");
            var DistractingFeint = new TrueFeat(DistractingFeintName, 2, "Your Feints are far more distracting than normal.",
                "While a creature is off-guard by your Feint, it also takes a –2 circumstance penalty to Perception checks and Reflex saves.",
                new Trait[1] { Trait.Rogue })
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Distracting Feint", "Your succesful feints inflict a -2 reflex debuff on the enemy")
                    {
                        AfterYouTakeActionAgainstTarget = (Func<QEffect, CombatAction, Creature, CheckResult, Task>)(async(effect, action, target, checkResult) =>
                        {
                            if (action.Name == "Feint" && checkResult >= CheckResult.Success)
                            {
                                target.AddQEffect(new QEffect("Distracting Feint", "-2 Circumstance Bonus to Reflext and Perception saves", ExpirationCondition.Never, creature, IllustrationName.Flatfooted)
                                {
                                    BonusToPerception = (Func<QEffect, Bonus>)((bonus) => new Bonus(-2, BonusType.Circumstance, "Distracting Feint")),
                                    BonusToDefenses = (effect, action, defense) =>
                                    {
                                        if (defense == Defense.Reflex)
                                        {
                                            return new Bonus(-2, BonusType.Circumstance, "Distracting Feint");
                                        }
                                        return null;

                                    }
                                }.WithExpirationAtEndOfSourcesNextTurn(creature, true));
                            }
                        }
                        )
                    }
                    );

                }
                )
                .WithPrerequisite(values => values.AllFeatNames.Contains(FeatName.ScoundrelRacket), "You must be a Scoundrel.");
            return DistractingFeint;
        }
    }
}
