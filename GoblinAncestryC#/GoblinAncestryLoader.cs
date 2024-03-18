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

namespace Dawnsbury.Mods.Ancestries.Goblin;

public static class GoblinAncestryLoader
{
    public static Trait GoblinTrait;

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        GoblinTrait = ModManager.RegisterTrait(
            "Kobold",
            new TraitProperties("Goblin", true)
            {
                IsAncestryTrait = true
            });
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
                CreateKoboldHeritages().ToList())
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
        yield return new GoblinAncestryFeat(
                "Draconic Sycophant",
                "You have an affect that dragonkind find unusually pleasing—and when that fails, you know when to duck.",
                "You gain a +2 circumstance bonus to saving throws against dragons.")
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Draconic Sycophant", "You have +2 to saves against dragons.")
                {
                    BonusToDefenses = (qfSelf, incomingAttack, targetedDefense) =>
                    {
                        if (targetedDefense == Defense.Fortitude || targetedDefense == Defense.Reflex || targetedDefense == Defense.Will)
                        {
                            if (incomingAttack?.Owner.HasTrait(Trait.Dragon) ?? false)
                            {
                                return new Bonus(2, BonusType.Circumstance, "Draconic Sycophant");
                            }
                        }

                        return null;
                    }
                });
            });
        yield return new KoboldAncestryFeat("Dragon's Presence",
                "As a member of dragonkind, you project unflappable confidence.",
                "When you attempt to Demoralize a foe of your level or lower, you gain a +1 circumstance bonus to the Intimidation check.")
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Dragon's Presence", "You have a +1 circumstance bonus to Demoralize.")
                {
                    BonusToAttackRolls = (qfSelf, combatAction, defender) =>
                    {
                        if (combatAction.ActionId == ActionId.Demoralize) return new Bonus(1, BonusType.Circumstance, "Dragon's Presence");
                        return null;
                    }
                });
            });
        yield return new GoblinAncestryFeat("Goblin Weapon Familiarity",
                "Others might look upon them with disdain, but you know that the weapons of your people are as effective as they are sharp. ",
                "You gain access to all uncommon weapons with the goblin trait. You have familiarity with weapons with the goblin trait—for the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.")
            .WithOnSheet(sheet =>
            {
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Goblin) && traits.Contains(Trait.Martial), Trait.Simple);
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Goblin) && traits.Contains(Trait.Advanced), Trait.Martial);
            });
        yield return new KoboldAncestryFeat("Kobold Breath",
                "You channel your draconic exemplar's power into a gout of energy.",
                "You gain a breath weapon attack that manifests as a 30-foot line or a 15-foot cone, dealing 1d4 damage. Each creature in the area must attempt a basic Reflex saving throw against the higher of your class DC. You can't use this ability again for 1d4 rounds.\n\nAt 3rd level, the damage increases by 1d4. The shape of the breath and the damage type match those of your draconic exemplar.")
            .WithActionCost(2)
            .WithOnCreature((sheet, creature) =>
            {
                var exemplarFeat = sheet.AllFeats.FirstOrDefault(ft => ft.Name.StartsWith("Draconic exemplar:"));
                if (exemplarFeat != null)
                {
                    var draconicExemplar = DraconicExemplarDescription.DraconicExemplarDescriptions[exemplarFeat.Name];
                    creature.AddQEffect(new QEffect("Breath Weapon", "You have a breath weapon.")
                    {
                        ProvideMainAction = (qfSelf) =>
                        {
                            var kobold = qfSelf.Owner;
                            return new ActionPossibility(new CombatAction(kobold, IllustrationName.BreathWeapon, "Breath Weapon", Array.Empty<Trait>(),
                                    "You manifest as a 30-foot line or a 15-foot cone, dealing 1d4 damage. Each creature in the area must attempt a basic Reflex saving throw against your class DC. You can't use this ability again for 1d4 rounds.\n\nAt 3rd level, the damage increases by 1d4. The shape of the breath and the damage type match those of your draconic exemplar.",
                                    draconicExemplar.IsCone ? Target.Cone(3) : Target.Line(6))
                                .WithActionCost(2)
                                .WithProjectileCone(IllustrationName.BreathWeapon, 15, ProjectileKind.Cone)
                                .WithSoundEffect(SfxName.FireRay)
                                .WithSavingThrow(new SavingThrow(draconicExemplar.SavingThrow, (breathOwner) => 13 + breathOwner.Level + GetBestAbility(breathOwner)))
                                .WithEffectOnEachTarget((async (spell, caster, target, result) => { await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, (caster.Level + 1) / 2 + "d4", draconicExemplar.DamageKind); }))
                                .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                                {
                                    caster.AddQEffect(QEffect.CannotUseForXRound("Breath Weapon", caster, R.Next(2, 5)));
                                }))
                            ).WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ADDITIONAL_NATURAL_STRIKE);
                        }
                    });
                }
            });
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
            .WithOnSheet((sheet) =>
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
               "Your flat check to remove persistent fire damage is DC 10 instead of DC 15." + "(the flatcheck part isn't added yet)")
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
                      // Fiture out how to reduce Flatcheck...
                   },

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
    }
}