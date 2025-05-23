using Dawnsbury;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.ThirdParty.SteamApi;
using Microsoft.Xna.Framework;


namespace DawnsburryMods
{
    public static class DistractingFeint
    {
        [DawnsburyDaysModMainMethod]
        public static void loadMod()
        {
            Feat DistractingFeint = loadDistractingFeint();
            ModManager.AddFeat(DistractingFeint);
            giveFeintStep();
            addRogueMartialProficiency();
            addRuffianMartialSneakAttack();
        // addWeapons();

        }

        private static void addWeapons()
        {
            ModManager.RegisterNewItemIntoTheShop("Dancer's Spear", itemName =>
            new Item(itemName, IllustrationName.Glaive, "Dancer's Spear", 0, 3, Trait.TwoHanded, Trait.Backswing, Trait.Finesse, Trait.Reach, Trait.Weapon, Trait.Martial, Trait.Spear, Trait.VersatileB, Trait.Sweep)
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                .WithDescription("Originally developed for an elaborate dancing performance, it was discovered it made for an effective weapon after an unfortunate accident."));
            ModManager.RegisterNewItemIntoTheShop("Elven Branched Spear", itemName =>
            new Item(itemName, IllustrationName.Glaive, "Elven Branched Spear", 0, 3, Trait.TwoHanded, Trait.DeadlyD8, Trait.Finesse, Trait.Reach, Trait.Weapon, Trait.Martial, Trait.Spear)
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                .WithDescription("Several short branches project from this delicate spear's shaft, each angled forward and tipped with a leaflike blade."));
            ModManager.RegisterNewItemIntoTheShop("Boarding Pike", itemName =>
                 new Item(itemName, IllustrationName.Longspear, "Boarding Pike", 0, 1, Trait.TwoHanded, Trait.Shove, Trait.Reach, Trait.Weapon, Trait.Martial, Trait.Polearm)
                .WithWeaponProperties(new WeaponProperties("1d10", DamageKind.Piercing))
                .WithDescription("Taking the form of a longspear fitted with crossbars or hooks, a boarding pike provides its wielder a sharp implement that's as adept at shoving enemies off a ship's railings as facilitating the boarding of other vessels. "));
        }

        private static void addRuffianMartialSneakAttack()
        {
            Feat ruffian = (AllFeats.All.Find(feat => feat.FeatName == FeatName.RuffianRacket))!;
            ruffian.RulesText = ruffian.RulesText.Replace("You can deal sneak attack damage with any simple weapon", "You can deal sneak attack damage with any simple weapon and martial or advanced weapons with damage dice up to d6");
            ruffian.WithOnCreature((creature =>
            {
                String damage = damageFormula(creature.Level);
                DiceFormula extraDamage = DiceFormula.FromText(damage, "Sneak attack");
                creature.AddQEffect(
                    new QEffect("Sneak Attack", "You deal an extra " + extraDamage.ToString() + " precision damage to flat-footed creatures.", ExpirationCondition.Never, (Creature)null, (Illustration)IllustrationName.None)
                    {
                        Id = QEffectId.SneakAttack,
                        Innate = true,
                        YouDealDamageWithStrike = (Delegates.YouDealDamageWithStrike)((qf, action, diceFormula, defender) =>
                        {
                            if (action.Item!.WeaponProperties!.DamageDieSize <= 6 & (action.HasTrait(Trait.Martial) | action.HasTrait(Trait.Advanced)) & defender.IsFlatFootedTo(action.Owner, action) & !defender.IsImmuneTo(Trait.PrecisionDamage))
                            {
                                if (action.Owner.OwningFaction.IsPlayer) Steam.CollectAchievement("ROGUE");
                                action.UsedSneakAttack = true;
                                defender.Occupies.Overhead("sneak attack!", Color.Gainsboro);
                                return (DiceFormula)diceFormula.Add(extraDamage);
                            }
                            return diceFormula;
                        })
                    });
            }));
        }

        private static void addRogueMartialProficiency()
        {
            ClassSelectionFeat rogue = (AllFeats.All.Find(feat => feat.FeatName == FeatName.Rogue) as ClassSelectionFeat)!;
            rogue.RulesText = rogue.RulesText.Replace("You're trained in all simple weapons, as well as the rapier, shortbow and shortsword.", "You're trained in all simple and martial weapons.");
            rogue.WithOnSheet((Action<CalculatedCharacterSheetValues>)(sheet =>
            {
                sheet.SetProficiency(Trait.Martial, Proficiency.Trained);
                sheet.AddAtLevel(5, (Action<CalculatedCharacterSheetValues>)(values =>
                {
                    values.SetProficiency(Trait.Martial, Proficiency.Expert);
                }));
                sheet.AddAtLevel(13, (Action<CalculatedCharacterSheetValues>)(values =>
                {
                    values.SetProficiency(Trait.Martial, Proficiency.Master);
                }));
            }));
        }

        private static void giveFeintStep()
        {
            Feat scoundrel = (AllFeats.All.Find(feat => feat.FeatName == FeatName.ScoundrelRacket))!;
            scoundrel.RulesText += "\nIf you Feint while wielding an agile or finesse melee weapon, you can Step immediately after the Feint as a free action.\r\n";

            scoundrel.WithOnCreature(creature =>
            {
                    creature.AddQEffect(new QEffect("Scoundrel Racket", "If you Feint while wielding an agile or finesse melee weapon, you can Step immediately after the Feint as a free action.")
                    {
                        AfterYouTakeActionAgainstTarget = (Func<QEffect, CombatAction, Creature, CheckResult, Task>)(async (effect, action, target, checkResult) =>
                        {
                            if (action.Name == "Feint" &
                                action.Owner.HeldItems.Exists(item => item.HasTrait(Trait.Agile) | item.HasTrait(Trait.Finesse)))
                            {
                                await effect.Owner.StrideAsync("Take a step.", allowStep: true, maximumFiveFeet: true, allowCancel: true);
                            }
                        }
)
                    }
);
                
            }
                            );
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
                        AfterYouTakeActionAgainstTarget = (Func<QEffect, CombatAction, Creature, CheckResult, Task>)(async (effect, action, target, checkResult) =>
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
        public static String damageFormula(int level) =>
        level switch
        {
            <= 4 => "1d6",
            (>= 5) and (<= 10) => "2d6",
            (>=11) and (<= 16) => "3d6",
            >= 17 => "4d6"
        };

    }
}
