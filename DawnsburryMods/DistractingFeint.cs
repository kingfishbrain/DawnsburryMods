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
using Dawnsbury.Core.Mechanics.Rules;
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

        //dummy QEffectId to prevent flanking from being removed
        public static QEffectId Flankee { get; } = ModManager.RegisterEnumMember<QEffectId>("FlankeeFake");

        [DawnsburyDaysModMainMethod]
        public static void loadMod()
        {
            Feat DistractingFeint = loadDistractingFeint();
            ModManager.AddFeat(DistractingFeint);
            giveFeintStep();
            addRogueMartialProficiency();
            addRuffianMartialSneakAttack();
            addRemasteredGangUp();
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
                            if (action.Item!.WeaponProperties!.DamageDieSize <= 6 
                               & action.HasTrait(Trait.Melee)
                               &  (action.HasTrait(Trait.Martial) | action.HasTrait(Trait.Advanced)) 
                               & !(action.HasTrait(Trait.Agile) | action.HasTrait(Trait.Finesse))
                               & defender.IsFlatFootedTo(action.Owner, action) 
                               & !defender.IsImmuneTo(Trait.PrecisionDamage))
                               
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

        private static void addRemasteredGangUp()
        {
            if (AllFeats.GetFeatByFeatName(FeatName.GangUp) is TrueFeat gangUpFeat)
            {
                gangUpFeat.RulesText = "Enemies are off-guard to your melee attacks due to flanking as long as they are within reach of both you and one of your allies, even if you and your ally aren't on opposite sides of the enemy. This benefits your allies as well as you, but only if they're flanking with you, not each other.";
            }

            ModManager.RegisterActionOnEachCreature(cr =>
            {
                if (!cr.HasFeat(FeatName.GangUp)) return;
                QEffect tech = new()
                {
                    StateCheck = effect =>
                    {
                        foreach (Creature friend in effect.Owner.Battle.AllCreatures.Where(friend => friend.FriendOfAndNotSelf(effect.Owner) && FlankingBuddy(effect.Owner, friend)))
                        {
                            foreach (Creature enemy in effect.Owner.Battle.AllCreatures.Where(enemy => enemy.EnemyOf(effect.Owner) && FlankingRules.IsFlanking(effect.Owner, enemy)))
                            {
                                QEffect flank = QEffect.FlankedBy(friend);
                                //dummy out the id to prevent it being removed when flanking is recalculated
                                flank.Id = Flankee;
                                enemy.AddQEffect(flank);
                            }
                        }
                    }
                };
                cr.AddQEffect(tech);
            });

        }

        //determines if an ally is flanking
        public static bool FlankingBuddy(Creature flanker, Creature buddy)
        {
            foreach (Creature flankee in flanker.Battle.AllCreatures.Where(cr => cr.EnemyOf(flanker)))
            {
                if (!FlankingRules.IsFlanking(flanker, flankee)) return false;
                int num1 = flankee.Occupies.X - flanker.Occupies.X;
                int num2 = flankee.Occupies.Y - flanker.Occupies.Y;
                int num3 = Math.Abs(num1);
                int num4 = Math.Abs(num2);
                if (num3 > 2 || num4 > 2)
                    return false;
                for (int index1 = -2; index1 <= 2; ++index1)
                {
                    for (int index2 = -2; index2 <= 2; ++index2)
                    {
                        if (!FlanksWith(flanker, Math.Abs(index1) <= 1 && Math.Abs(index2) <= 1,
                                flankee.Occupies.X + index1, flankee.Occupies.Y + index2)) continue;
                        Creature? primaryOccupant = flanker.Battle.Map.GetTile(flankee.Occupies.X + index1, flankee.Occupies.Y + index2)?.PrimaryOccupant;
                        if (primaryOccupant == null) continue;
                        if (!flanker.IsAdjacentTo(flankee) && !flanker.WieldsItem(Trait.Reach)) continue;
                        if (primaryOccupant == buddy)
                            return true;
                    }
                }
            }
            return false;
        }
        //public version of the private FlanksWith bool from FlankingRules
        public static bool FlanksWith(Creature flanker, bool adjacent, int otherX, int otherY)
        {
            Creature? primaryOccupant = flanker.Battle.Map.GetTile(otherX, otherY)?.PrimaryOccupant;
            return primaryOccupant != null && primaryOccupant.FriendOfAndNotSelf(flanker) && (adjacent || primaryOccupant.WieldsItem(Trait.Reach) || primaryOccupant.HasEffect(QEffectId.WeaponInfusion) && (primaryOccupant.HasFreeHand || primaryOccupant.HasEffect(QEffectId.SubtleShaping))) && primaryOccupant.Actions.CanTakeActions() && FlankingRules.SimplifiedCanAttack(primaryOccupant);
        }



    }
}
