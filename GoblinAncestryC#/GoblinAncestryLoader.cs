using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Dawnsbury.Core;
using global::Dawnsbury.Core.CharacterBuilder.AbilityScores;
using global::Dawnsbury.Core.CharacterBuilder.Feats;
using global::Dawnsbury.Core.Creatures;
using global::Dawnsbury.Core.Mechanics.Enumerations;
using global::Dawnsbury.Modding;
using Microsoft.Xna.Framework.Graphics;
using System.Text;

namespace Dawnsbury.Mods.Ancestries.Goblin;

public static class GoblinAncestryLoader
{
    public static Trait GoblinTrait;

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {

        GoblinTrait = ModManager.RegisterTrait(
            "Goblin",
            new TraitProperties("Goblin", true)
            {
                IsAncestryTrait = true
            });
        GoblinWeapons.RegisterWeapons();
        AddFeats(CreateGoblinAncestryFeats());

        ModManager.AddFeat(new AncestrySelectionFeat(
                FeatName.CustomFeat,
                "Goblins are a short, scrappy, energetic people who have spent millennia maligned and feared.",
                new List<Trait> { Trait.Humanoid, GoblinTrait },
                6,
                5,
                new List<AbilityBoost>()
                {
                    new EnforcedAbilityBoost(Ability.Dexterity),
                    new EnforcedAbilityBoost(Ability.Charisma),
                    new FreeAbilityBoost()
                },
                CreateGoblinHeritages().ToList())
            .WithAbilityFlaw(Ability.Wisdom)
            .WithCustomName("Goblin")
        );
    }

    private static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (var feat in feats)
        {
            ModManager.AddFeat(feat);
        }
    }

    private static IEnumerable<Feat> CreateGoblinAncestryFeats()
    {

        yield return new GoblinAncestryFeat("Goblin Weapon Familiarity",
                "Others might look upon them with disdain, but you know that the weapons of your people are as effective as they are sharp. ",
                "You gain access to all uncommon weapons with the goblin trait. You have familiarity with weapons with the goblin trait—for the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.")
            .WithOnSheet(sheet =>
            {
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Goblin) && traits.Contains(Trait.Martial), Trait.Simple);
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Goblin) && traits.Contains(Trait.Advanced), Trait.Martial);
            });

        yield return new GoblinAncestryFeat("Bouncy Goblin",
               "You have a particular elasticity that makes it easy for you to bounce and squish. ",
               "You gain the trained proficiency rank in Acrobatics (or another skill of your choice, if you were already trained in Acrobatics). You also gain a +2 circumstance bonus to Acrobatics checks to Tumble Through a foe’s space.")
           .WithOnCreature(creature =>
           {
               creature.AddQEffect(new QEffect("Bouncy Goblin", "You have a 2+ bonus to Tumble Through.")
               {
                   BonusToAttackRolls = (qfSelf, combatAction, defender) =>
                   {
                       if (combatAction.ActionId == ActionId.TumbleThrough) return new Bonus(2, BonusType.Circumstance, "Bouncy Goblin");
                       return null;
                   }
               });
           }).WithPrerequisite(values => values.AllFeats.Any(feat => feat.Name.Equals("Unbreakable Goblin")), "You must be an Unbreakable Goblin.")
           .WithOnSheet(sheet =>
           {

               if (sheet.GetProficiency(Trait.Acrobatics) == Proficiency.Untrained)
               {
                   sheet.SetProficiency(Trait.Acrobatics, Proficiency.Trained);
               }
               else
               {
                   sheet.AddSelectionOption(
                       new SingleFeatSelectionOption(
                           "Bouncy Goblin Skill",
                           "Bouncy Goblin skill",
                           -1,
                           (ft) => ft is SkillSelectionFeat)

                           );
               }
           });


        yield return new GoblinAncestryFeat("Burn It!",
               "Fire fascinates you.",
               "Your spells and alchemical items that deal fire damage gain a status bonus to damage equal to half the spell's level" +
               "or one-quarter the item's level (minimum 1). " +
               "You also gain a +1 status bonus to any persistent fire damage you deal.")
           .WithOnCreature(goblin =>
           {
               var burnIt = new QEffect("Burn It!", "Adds damage to fire spells")
               {
                   BonusToDamage = (qfSelf, combatAction, defender) =>
                   {
                       if (combatAction.HasTrait(Trait.Fire) && (combatAction.HasTrait(Trait.Spell)))
                           return new Bonus(combatAction.SpellLevel / 2, BonusType.Status, "Burn It!");
                       return null;

                   }


               };

               /*    ModManager.RegisterActionOnEachCreature(creature =>
                   {
                       creature.AddQEffect(new QEffect()
                       {
                           YouAcquireQEffect = (qfSelf, qfAdded) =>
                           {
                               if (qfAdded.Id == QEffectId.PersistentDamage && qfAdded.Source.HasEffect(burnIt) && qfAdded.)
                               {

                               }
                           }
                       });

                   }); */
               goblin.AddQEffect(burnIt);

           }).WithPermanentQEffect("Increase persistant fire damage", qfBurnIt =>
           {
               qfBurnIt.AddGrantingOfTechnical(cr => cr.EnemyOf(qfBurnIt.Owner), qfBurnItOnAnEnemy =>
               {
                   qfBurnItOnAnEnemy.YouAcquireQEffect = (qfBurnItOnAnEnemySelf, qfIncoming) =>
                   {
                       if (qfIncoming.Id == QEffectId.PersistentDamage   && qfBurnIt.Owner.Battle.ActiveCreature == qfBurnIt.Owner && qfIncoming.Key == "PersistentDamage:Fire") //Need an addiotnal restriction here to check for correct source
                       {
                           IO.GeneralLog.Log("Reaches past Peristant check");
                           return QEffect.PersistentDamage(qfIncoming.Name.Split(" ")[0] + "+1", DamageKind.Fire);
                       }
                       else
                       {
                           return qfIncoming;
                       }
                   };
               });
           });
        ;
        yield return new GoblinAncestryFeat("Scuttle",
                "You take advantage of your ally’s movement to adjust your position.",
                "Trigger An ally ends a move action adjacent to you. \n You Step.")
             .WithPermanentQEffect("Trigger An ally ends a move action adjacent to you. \n You Step.", qfScuttle =>
             {
                 qfScuttle.AddGrantingOfTechnical(cr => cr.FriendOf(qfScuttle.Owner), qfScuttleTechnical =>
                 {
                     qfScuttleTechnical.AfterYouTakeAction = async (qfScuttleTechnical2, allysCombatAction) =>
                     {
                         if (allysCombatAction.HasTrait(Trait.Move) &&
                             qfScuttleTechnical2.Owner.IsAdjacentTo(qfScuttle.Owner))
                         {
                             if (await qfScuttle.Owner.Battle.AskToUseReaction(qfScuttle.Owner,
                                     "An ally ended near you. Scuttle?"))
                             {
                                 await qfScuttle.Owner.StrideAsync("Take a step.", allowStep: true, maximumFiveFeet: true, allowCancel: true);
                             }
                         }
                     };
                 });
             });
        yield return new GoblinAncestryFeat("Hard Tail",
            "Your tail is much stronger than most, and you can lash out with it with the strength of a whip." ,
            "You gain a tail unarmed attack that deals 1d6 bludgeoning damage.")
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Hard Tail", "You have a tail attack.")
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Jaws, "jaws",
                            new[] {Trait.Unarmed, Trait.Melee, Trait.Weapon})
                        .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                });
            }).WithPrerequisite(values => values.AllFeats.Any(feat => feat.Name.Equals("Tailed Goblin")), "You must be a Tailed Goblin.");

        yield return new GoblinAncestryFeat("Goblin Song",
        "You sing annoying goblin songs, distracting your foes with silly and repetitive lyrics.",
        "Attempt a Performance check against the Will DC of a single enemy within 30 feet. This has all the usual traits and restrictions of a Performance check. " +
        "You can affect up to two targets within range if you have expert proficiency in Performance, four if you have master proficiency, " +
        "and eight if you have legendary proficiency. \n " +
        "Critical Success The target takes a –1 status penalty to Perception checks and Will saves for 1 minute." +
        "\r\nSuccess The target takes a –1 status penalty to Perception checks and Will saves for 1 round." +
        "\r\nCritical Failure The target is temporarily immune to attempts to use Goblin Song for 1 hour.")
        .WithActionCost(2)
        .WithOnCreature((sheet, creature) =>
        {
            var performance = creature.PersistentCharacterSheet.Calculated.GetProficiency(Trait.Performance);
#pragma warning disable CS8524 //enum is exhaustingly matched so there is no need for a default case
            int targetCount = performance switch
            {
                Proficiency.Untrained => 1,
                Proficiency.Trained => 1,
                Proficiency.Expert => 2,
                Proficiency.Master => 4,
                Proficiency.Legendary => 8
            };

            QEffect immunity = new QEffect("Goblin Song Critical Failure", "Immunity to Goblin Song");

            Target targets = Target.MultipleCreatureTargets(Enumerable.Repeat(Target.Ranged(6)
                .WithAdditionalConditionOnTargetCreature((caster, target) =>
                {
                    if (target.DoesNotSpeakCommon) {
                        return Usability.NotUsableOnThisCreature("Target cannot understand the thoughtful lyrics"); 
    }

                    if (target.HasEffect(immunity.Id))
                    {
                        return Usability.NotUsableOnThisCreature("Target is immune due to a previous critically failed attempt at Goblin Song");
                    } // To be tested: check for Immunity effect to Goblin Song from multiple sources
                    return Usability.Usable;
                })
                 , targetCount).ToArray())
                .WithSimultaneousAnimation()
                .WithMinimumTargets(1)
                .WithMustBeDistinct()
                .WithOverriddenTargetLine("up to " + targetCount.ToString() + " enemies.", true)
                ;

            creature.AddQEffect(new QEffect("Goblin Song", "You can use Goblin Song against " + targets.ToString() + " enemies")
            {
                ProvideMainAction = (qfSelf) =>
                {
                    var goblin = qfSelf.Owner;

                    return new ActionPossibility(new CombatAction
                        (goblin, IllustrationName.Deafness, "Goblin Song", Array.Empty<Trait>(),
                                "Attempt a Performance check against the Will DC of up to " + targets.ToString() + " enemy within 30 feet. This has all the usual traits and restrictions of a Performance check. " +
                                "\r\nCritical Success The target takes a –1 status penalty to Perception checks and Will saves for 1 minute." +
                                "\r\nSuccess The target takes a –1 status penalty to Perception checks and Will saves for 1 round." +
                                "\r\nCritical Failure The target is temporarily immune to attempts to use Goblin Song for 1 hour.",
                                targets)
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.Intimidate)
                        .WithActiveRollSpecification(new ActiveRollSpecification(Checks.SkillCheck(Skill.Performance), Checks.DefenseDC(Defense.Will)))
                        .WithEffectOnEachTarget(async (song, caster, target, result) =>
                        {

                            if (result is CheckResult.CriticalSuccess)
                            {
                                target.AddQEffect(new QEffect("Goblin Song Critical Success", "–1 status penalty to Perception checks and Will saves",
                                    ExpirationCondition.CountsDownAtStartOfSourcesTurn, caster)
                                {
                                    RoundsLeft = 6,
                                    BonusToDefenses = (qfSelf, incomingEffect, targetedDefense) =>
                                    {
                                        if(targetedDefense == Defense.Will || targetedDefense == Defense.Perception)
                                        {
                                            return new Bonus(-1, BonusType.Status, "Goblin Song Critical Success");
                                        }
                                        return null;
                                    },
                                    BonusToAttackRolls = (qfSelf, combatAction, target) =>
                                    {
                                        if (combatAction.HasTrait(Trait.Perception))
                                        {
                                            return new Bonus(-1, BonusType.Status, "Goblin Song Critical Success");
                                        }
                                        return null;
                                    }
                                    

                                });
                            }
                            
                            if (result is CheckResult.Success)
                            {
                                target.AddQEffect(new QEffect("Goblin Song Success", "–1 status penalty to Perception checks and Will saves",
                                    ExpirationCondition.CountsDownAtStartOfSourcesTurn, caster)
                                {
                                    RoundsLeft = 1,
                                    BonusToDefenses = (qfSelf, incomingEffect, targetedDefense) =>
                                    {
                                        if (targetedDefense == Defense.Will || targetedDefense == Defense.Perception)
                                        {
                                            return new Bonus(-1, BonusType.Status, "Goblin Song Success");
                                        }
                                        return null;
                                    },
                                    BonusToAttackRolls = (qfSelf, combatAction, target) =>
                                    {
                                        if (combatAction.HasTrait(Trait.Perception))
                                        {
                                            return new Bonus(-1, BonusType.Status, "Goblin Song Success");
                                        }
                                        return null;
                                    }


                                });
                            }
                            if (result is CheckResult.CriticalFailure)
                            {
                                target.AddQEffect(immunity);
                            }
                        }
                        ));

                }
            }

                );

        });

            }


    private static IEnumerable<Feat> CreateGoblinHeritages()
    {
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "You're not like most other Goblins and don't share their fragile builds.",
                "You have two free ability boosts instead of a Goblin's normal ability boosts and flaw.")
            .WithCustomName("Unusual Goblin")
            .WithOnSheet(sheet =>
            {
                sheet.AbilityBoostsFabric.AbilityFlaw = null;
                sheet.AbilityBoostsFabric.AncestryBoosts =
                    new List<AbilityBoost>
                    {
                        new FreeAbilityBoost(),
                        new FreeAbilityBoost()
                    };
            });
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "You are acclimated to living in frigid lands and have skin ranging from sky blue to navy in color, as well as blue fur. .",
                "You gain cold resistance equal to half your level (minimum 1).")
            .WithCustomName("Snow Goblin")
            .WithOnCreature((sheet, creature) =>
            {
                var resistanceValue = (creature.Level + 1) / 2;
                creature.AddQEffect(new QEffect("Snow",
                        "You have cold resistance" +
                        resistanceValue + ".")
                {
                    StateCheck = (qfSelf) =>
                    {
                        var Goblin = qfSelf.Owner;
                        Goblin.WeaknessAndResistance.AddResistance(DamageKind.Cold, resistanceValue);
                    },
                });
            });

        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
               "Your ancestors have always had a connection to fire and a thicker skin, which allows you to resist burning.",
               "You gain fire resistance equal to half your level (minimum 1). You can also recover from being on fire more easily. " +
               "Your flat check to remove persistent fire damage is DC 10 instead of DC 15.")
           .WithCustomName("Charhide Goblin")
           .WithOnCreature((sheet, creature) =>
           {
               var resistanceValue = (creature.Level + 1) / 2;
               creature.AddQEffect(new QEffect("Charhide",
                       "You have fire resistance" +
                       resistanceValue + ".")
               {
                   StateCheck = (qfSelf) =>
                   {
                       var Goblin = qfSelf.Owner;
                       Goblin.WeaknessAndResistance.AddResistance(DamageKind.Fire, resistanceValue);


                   },
               });
           }).WithPermanentQEffect("Your flat check to remove persistent fire damage is DC 10 instead of DC 15", qfCharred =>
                         {
               qfCharred.YouAcquireQEffect = (qfCharredSelf, qfIncoming) =>
                             {
                   if (qfIncoming.Id == QEffectId.PersistentDamage && qfIncoming.Key == "PersistentDamage:Fire")
                   {
                       qfIncoming.EndOfYourTurn = async (qf, self) =>
                       {
                           await self.DealDirectDamage(CombatAction.CreateSimple(self.Battle.Pseudocreature, "Persistent damage"),
                               DiceFormula.FromText(qfIncoming.Name.Split(" ")[0]), self, CheckResult.Failure, DamageKind.Fire);
                           if (!self.DeathScheduledForNextStateCheck && (self.Actions.HasDelayedYieldingTo == null || self.HasTrait(Trait.AnimalCompanion)))
                           {
                               qf.RollPersistentDamageRecoveryCheck(true); // <-- HERE YOU CHANGE FROM NONASSISTED TO ASSISTED; or you could even inline the method to get further control
                             }
                       };

                         }
                                 );
                       */

                   },
                   //TO DO add peristant reduction, possibly here?

               });
           });

        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "Your family's teeth are formidable weapons.",
                "You gain a jaws unarmed attack that deals 1d6 piercing damage. Your jaws have the finesse and unarmed traits.")
            .WithCustomName("Razortooth Goblin")
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Razortooth", "You have a jaws attack.")
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Jaws, "jaws",
                            new[] { Trait.Finesse, Trait.Unarmed, Trait.Melee, Trait.Weapon })
                        .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });

        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
               "You're able to bounce back from injuries easily due to an exceptionally thick skull, cartilaginous bones, or some other mixed blessing. ",
               " You gain 10 Hit Points from your ancestry instead of 6. If the third dimension existed in this game, you would reduce the falling damage you take as though you had fallen half the distance.")
           .WithCustomName("Unbreakable Goblin")
           .WithOnCreature(creature => creature.MaxHP += 4);

        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
        "You have a powerful tail, likely because you descend from a community of monkey goblins.",
        "You reduce the number of free hands required to Trip by one.")
    .WithCustomName("Tailed Goblin")
    .WithPermanentQEffect("You reduce the number of free hands required to Trip by one.", qfTailed =>
    {
        qfTailed.ProvideActionIntoPossibilitySection = (effect, section) =>
        {
            if (section.PossibilitySectionId != PossibilitySectionId.AttackManeuvers) return null;
            if (effect.Owner.HasFreeHand)
                return null; // do nothing -- will be handled by the normal Trip action
            var customTrip = Possibilities.CreateTrip(effect.Owner);
            var customTripTarget = (customTrip.Target as CreatureTarget);
            customTripTarget.CreatureTargetingRequirements.Clear();
            customTripTarget.CreatureTargetingRequirements.Add(new AdjacencyCreatureTargetingRequirement());
            customTripTarget.CreatureTargetingRequirements.Add(new EnemyCreatureTargetingRequirement());
            customTripTarget.CreatureTargetingRequirements.Add(new LegacyCreatureTargetingRequirement((a, d) =>
                 !d.HasEffect(QEffectId.Prone)
                     ? Usability.Usable
                     : Usability.CommonReasons.TargetIsAlreadyProne));
            return new ActionPossibility(customTrip);
        };
    });


    }
}