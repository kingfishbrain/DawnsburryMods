using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace GoblinAncestry.GoblinAncestry
{
    public static class AncestryFeats
    {

        //Performance skill needed for Goblin Song
        public static Feat Performance = new SkillSelectionFeat(FeatName.CustomFeat, Skill.Performance, Trait.Performance).WithCustomName("Performance");
        public static Feat ExpertPerformance = new SkillIncreaseFeat(FeatName.CustomFeat, Skill.Performance, Trait.Performance).WithCustomName("Expert in Performance");



        private static ModdedIllustration GoblinNoteIllustration;

        private static SfxName GoblinSongSoundEffect;

        public static IEnumerable<Feat> CreateGoblinAncestryFeats(Trait goblinTrait)
        {
            GoblinNoteIllustration = new ModdedIllustration(@"GoblinAncestryResources\GoblinNote.png");

            GoblinSongSoundEffect = ModManager.RegisterNewSoundEffect(@"GoblinAncestryResources\GoblinSong.mp3");

            ModManager.AddFeat(Performance);
            ModManager.AddFeat(ExpertPerformance);


            yield return new AncestryFeat("Goblin Weapon Familiarity",
                    "Others might look upon them with disdain, but you know that the weapons of your people are as effective as they are sharp. ",
                    "You gain access to all uncommon weapons with the goblin trait. You have familiarity with weapons with the goblin trait—for the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.")
                .WithOnSheet(sheet =>
                {
                    sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(goblinTrait) && traits.Contains(Trait.Martial), Trait.Simple);
                    sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(goblinTrait) && traits.Contains(Trait.Advanced), Trait.Martial);
                });

            yield return new AncestryFeat("Bouncy Goblin",
                   "You have a particular elasticity that makes it easy for you to bounce and squish. ",
                   "You gain the trained proficiency rank in Acrobatics (or another skill of your choice, if you were already trained in Acrobatics). You also gain a +2 circumstance bonus to Acrobatics checks to Tumble Through a foe’s space.")
               .WithPrerequisite(values => values.AllFeats.Any(feat => feat.Name.Equals("Unbreakable Goblin")), "You must be an Unbreakable Goblin.")
               .WithOnCreature(creature =>
               {
                   creature.AddQEffect(new QEffect("Bouncy Goblin", "You have a 2+ bonus to Tumble Through.")
                   {
                       BonusToSkillChecks = (skill, combatAction, target) =>
                       {
                           if (combatAction.Name == "Tumble Through")
                           {
                               return new Bonus(2, BonusType.Circumstance, "Bouncy Goblin");
                           }
                           return null;
                       }
                   });
               })
               .WithOnSheet(sheet =>
               {

                   if (sheet.GetProficiency(Trait.Acrobatics) == Proficiency.Untrained)
                   {
                       sheet.AddFeat(AllFeats.All.Find(feat => feat.FeatName == FeatName.Acrobatics), null);
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


            yield return new AncestryFeat("Burn It!",
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
                           if (combatAction.HasTrait(Trait.Fire) && combatAction.HasTrait(Trait.Spell))
                               return new Bonus(combatAction.SpellLevel / 2, BonusType.Status, "Burn It!");
                           return null;

                       }


                   };


                   goblin.AddQEffect(burnIt);

               }).WithPermanentQEffect("Increase persistant fire damage", qfBurnIt =>
               {
                   qfBurnIt.AddGrantingOfTechnical(cr => cr.EnemyOf(qfBurnIt.Owner), qfBurnItOnAnEnemy =>
                   {
                       qfBurnItOnAnEnemy.YouAcquireQEffect = (qfBurnItOnAnEnemySelf, qfIncoming) =>
                       {
                           if (qfIncoming.Id == QEffectId.PersistentDamage && qfBurnIt.Owner.Battle.ActiveCreature == qfBurnIt.Owner
                           && qfIncoming.Key == "PersistentDamage:Fire")
                           {
                               return QEffect.PersistentDamage(qfIncoming.Name.Split(" ")[0] + "+1", DamageKind.Fire);
                           }
                           else
                           {
                               return qfIncoming;
                           }
                       };
                   });
               });

            yield return new AncestryFeat("Scuttle",
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
            yield return new AncestryFeat("Hard Tail",
                "Your tail is much stronger than most, and you can lash out with it with the strength of a whip.",
                "You gain a tail unarmed attack that deals 1d6 bludgeoning damage.")
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Hard Tail", "You have a tail attack.")
                    {
                        AdditionalUnarmedStrike = new Item(IllustrationName.Tail, "tail",
                                new[] { Trait.Unarmed, Trait.Melee, Trait.Weapon })
                            .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                    });
                }).WithPrerequisite(values => values.AllFeats.Any(feat => feat.Name.Equals("Tailed Goblin")), "You must be a Tailed Goblin.");

            yield return new AncestryFeat("Goblin Song",
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

                Target targets = Target.MultipleCreatureTargets(Enumerable.Repeat(Target.Ranged(6)
                    .WithAdditionalConditionOnTargetCreature((caster, target) =>
                    {
                        if (target.DoesNotSpeakCommon)
                        {
                            return Usability.NotUsableOnThisCreature("Target cannot understand the thoughtful lyrics");
                        }

                        if (target.QEffects.Any(effect => effect.Name == "Goblin Song Critical Failure"))
                        {
                            return Usability.NotUsableOnThisCreature("Target is immune due to a previous critically failed attempt at Goblin Song");
                        }


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
                    ProvideActionIntoPossibilitySection = (qfSelf, possibilitySection) =>
                    {
                        if (possibilitySection.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                    {
                            return null;
                        }

                        var goblin = qfSelf.Owner;

                        return new ActionPossibility(new CombatAction
                            (goblin, GoblinNoteIllustration, "Goblin Song", Array.Empty<Trait>(),
                                    "Attempt a Performance check against the Will DC of up to " + targets.ToString() + " enemy within 30 feet. This has all the usual traits and restrictions of a Performance check. " +
                                    "\r\nCritical Success The target takes a –1 status penalty to Perception checks and Will saves for 1 minute." +
                                    "\r\nSuccess The target takes a –1 status penalty to Perception checks and Will saves for 1 round." +
                                    "\r\nCritical Failure The target is temporarily immune to attempts to use Goblin Song for 1 hour.",
                                    targets)
                            .WithActionCost(1)
                            .WithSoundEffect(GoblinSongSoundEffect)
                            .WithActiveRollSpecification(new ActiveRollSpecification(Checks.SkillCheck(Skill.Performance), Checks.DefenseDC(Defense.Will)))
                            .WithEffectOnEachTarget(async (song, caster, target, result) =>
                            {

                                if (result is CheckResult.CriticalSuccess)
                                {
                                    target.QEffects.ForEach(effect =>
                                    {
                                        if (effect.Name == "Goblin Song Success" || effect.Name == "Goblin Song Critical Success")
                                        {
                                            effect.ExpiresAt = ExpirationCondition.Immediately; //remove other goblin song effects
                                        }
                                    });
                                    target.AddQEffect(new QEffect("Goblin Song Critical Success", "–1 status penalty to Perception checks and Will saves",
                                        ExpirationCondition.CountsDownAtStartOfSourcesTurn, caster, GoblinNoteIllustration)
                                    {
                                        CountsAsADebuff = true,
                                        RoundsLeft = 6,
                                        BonusToDefenses = (qfSelf, incomingEffect, targetedDefense) =>
                                        {
                                            if (targetedDefense == Defense.Will || targetedDefense == Defense.Perception)
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
                                    if (!target.QEffects.Any(effect => effect.Name == "Goblin Song Success" || effect.Name == "Goblin Song Critical Success"))
                                    {
                                        target.AddQEffect(new QEffect("Goblin Song Success", "–1 status penalty to Perception checks and Will saves",
                                        ExpirationCondition.CountsDownAtStartOfSourcesTurn, caster, GoblinNoteIllustration)
                                        {
                                            CountsAsADebuff = true,
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
                                }
                                if (result is CheckResult.CriticalFailure)
                                {
                                    QEffect immunity = new QEffect("Goblin Song Critical Failure", "Immunity to Goblin Song");
                                    target.AddQEffect(immunity);
                                }
                            }
                            ));

                    }
                }

                    );

            });

        }
    }
}