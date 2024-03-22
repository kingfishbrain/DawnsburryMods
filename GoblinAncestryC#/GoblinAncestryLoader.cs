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
               "You also gain a +1 status bonus to any persistent fire damage you deal. (Persistant fire bonus damage not yet added)") //TO DO
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


    }

    private static int GetBestAbility(Creature creature)
    {
        int bestAbility = 0;
        if (creature.Abilities.Strength > bestAbility) bestAbility = creature.Abilities.Strength;
        if (creature.Abilities.Dexterity > bestAbility) bestAbility = creature.Abilities.Dexterity;
        if (creature.Abilities.Constitution > bestAbility) bestAbility = creature.Abilities.Constitution;
        if (creature.Abilities.Intelligence > bestAbility) bestAbility = creature.Abilities.Intelligence;
        if (creature.Abilities.Wisdom > bestAbility) bestAbility = creature.Abilities.Wisdom;
        if (creature.Abilities.Charisma > bestAbility) bestAbility = creature.Abilities.Charisma;
        return bestAbility;
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
    }
}