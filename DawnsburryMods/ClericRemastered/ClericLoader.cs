using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;

namespace DawnsburryMods.ClericRemastered
{
    public static class ClericLoader
    {
        [DawnsburyDaysModMainMethod]
        public static void loadMod()
        {
            AllFeats.All.RemoveAll(feat => feat.FeatName == FeatName.HealingFont);
            AllFeats.All.RemoveAll(feat => feat.FeatName == FeatName.HarmfulFont);
            LoadFonts().ForEach(feat => ModManager.AddFeat(feat)); //yooo it works
            LoadClassFeats().ForEach(feat => ModManager.AddFeat(feat)); 
        }

        //not relevant now but perhaps in the future?
        public static int Fonts(int level) =>
            level switch
            {
                <= 4 => 4,
                (>= 5) and (<= 14) => 5,
                >= 15 => 6,
            };
 

        public static IEnumerable<Feat> LoadFonts()
        {
            yield return new Feat(FeatName.HealingFont, "Through your deity's blessing, you gain additional spells that channel the life force called positive energy.", "You gain additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}heal{/i} spells in these slots, and the number of these slots is equal to 1 plus your Charisma modifier.", new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, (Action<CalculatedCharacterSheetValues>)delegate (CalculatedCharacterSheetValues values)
                {
                    int fonts = Fonts(values.CurrentLevel);
                    for (int i = 0; i < fonts; i++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Healing font", AllSpells.CreateModernSpellTemplate(SpellId.Heal, Trait.Cleric), "HealingFont:" + i));
                    }
                });
            }).WithIllustration(IllustrationName.Heal);
            yield return new Feat(FeatName.HarmfulFont, "Through your deity's blessing, you gain additional spells that channel the counterforce to life, the so-called negative energy.", "You gain additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}harm{/i} spells in these slots, and the number of these slots is equal to 1 plus your Charisma modifier.", new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, (Action<CalculatedCharacterSheetValues>)delegate (CalculatedCharacterSheetValues values)
                {
                    int fonts = Fonts(values.CurrentLevel);
                    for (int i = 0; i < fonts; i++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Harmful font", AllSpells.CreateModernSpellTemplate(SpellId.Harm, Trait.Cleric), "HarmfulFont:" + i));
                    }
                });
            }).WithIllustration(IllustrationName.Harm);
        }

        public static IEnumerable<Feat> LoadClassFeats()
        {
            var WarpriestArmor = ModManager.RegisterFeatName("Warpriest’s Armor");
            yield return new TrueFeat(WarpriestArmor, 2, "Your training has helped you adapt to ever-heavier armor.",
                "You are trained in heavy armor. Whenever you gain a class feature that grants you expert or greater proficiency in medium armor, you also gain that proficiency in heavy armor. ", 
                new Trait[1] { Trait.Cleric })
                .WithOnSheet(sheet =>
                {
                    sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.HeavyArmor), Trait.MediumArmor);
                })
                .WithPrerequisite(values => values.AllFeatNames.Contains(FeatName.Warpriest), "You must be a Warpriest.");

            var EmblazonShieldName = ModManager.RegisterFeatName("Emblazon Shield");
            var EmblazonShield = new TrueFeat(EmblazonShieldName, 2, "Carefully etching a sacred image into your shields, you steel yourself for battle. ",
                "Your shields gain a +1 status bonus to their Hardness. (This causes it to reduce more damage with the Shield Block reaction.).",
                new Trait[0] {})
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Emblazon Shield", "Your shields gain a +1 status bonus to their Hardness.") 
                    { 
                        StartOfCombat = (async (QEffect qSelf) =>
                        {
                            qSelf.Owner.HeldItems.ForEach(item =>
                            {
                                if (item.HasTrait(Trait.Shield)) item.Hardness += 1;
                            });
                            qSelf.Owner.CarriedItems.ForEach(item =>
                            {
                                if (item.HasTrait(Trait.Shield)) item.Hardness += 1;
                            });
                            
                        }
                        )
                    }    
                    );
                });
            var EmblazonWeaponName = ModManager.RegisterFeatName("Emblazon Weapon");
            var EmblazonWeapon = new TrueFeat(EmblazonWeaponName, 2, "Carefully etching a sacred image into your weapons, you steel yourself for battle. ",
                "The wielder gains a +1 status bonus to damage rolls with your weapons.",
                new Trait[0] {})
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Emblazon Weapons", "The wielder gains a +1 status bonus to damage rolls with your weapons.") 
                    { 
                        BonusToDamage = (qSelf, combatAction, target) =>
                            {
                                if (combatAction.HasTrait(Trait.Strike)) return new Bonus(1, BonusType.Status, "Emblazon Weapons", true);
                                return null;
                            }
                    }    
                    );
                });
            var EmblazonArmaments = ModManager.RegisterFeatName("Emblazon Armaments");
            yield return new TrueFeat(EmblazonArmaments, 2, "Carefully etching a sacred image into a physical object, you steel yourself for battle.", 
                "Choose if you want to emblazon your weapons or your shields.", new Trait[1] { Trait.Cleric},  new List<Feat>() {EmblazonWeapon, EmblazonShield} );

            var RaiseSymbol = ModManager.RegisterFeatName("Raise Symbol");

            var RaisingSymbol = new QEffect("Raising Symbol", "You gain a +2 circumstance bonus to saving throws until the start of your next turn." +
                                    "While it’s raised, if you roll a success at a saving throw against a positive or negative effect, you get a critical success instead.",
                                    ExpirationCondition.CountsDownAtStartOfSourcesTurn, null, null)
            {
                RoundsLeft = 1,
                CountsAsABuff = true,
                AdjustSavingThrowResult = (qSelf, combatAction, result) =>
                {
                    if ((combatAction.HasTrait(Trait.Positive) || combatAction.HasTrait(Trait.Negative)) && result == CheckResult.Success) return CheckResult.CriticalSuccess;
                    return result;
                },
                BonusToDefenses = (qSelf, combatAction, defense) =>
                {
                    if (defense == Defense.Reflex || defense == Defense.Will || defense == Defense.Fortitude) return new Bonus(2, BonusType.Circumstance, "Raise Symbol");
                    return null;
                }
            };

            yield return new TrueFeat(RaiseSymbol, 4,"You present your religious symbol emphatically.",
                "You gain a +2 circumstance bonus to saving throws until the start of your next turn. " +
                "While it’s raised, if you roll a success at a saving throw against a positive or negative effect, you get a critical success instead." +
                "If you're wielding a shield and you have the Emblazon Shield feat, you gain the effects of Raise a Shield when you use this action and the effects of this action when you Raise a Shield."
                , new Trait[1] { Trait.Cleric })
                .WithActionCost(1)
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect()
                    {
                        ProvideMainAction = (qSelf) =>
                        {
                            return new ActionPossibility(new CombatAction
                            (creature, IllustrationName.DivineLance, "Raise Symbol", Array.Empty<Trait>(),
                                "You gain a +2 circumstance bonus to saving throws until the start of your next turn. " +
                                "While it’s raised, if you roll a success at a saving throw against a positive or negative effect, you get a critical success instead." +
                                "If you're wielding a shield and you have the Emblazon Shield feat, you gain the effects of Raise a Shield when you use this action and the effects of this action when you Raise a Shield.",
                                    Target.Self())
                            .WithActionCost(1)
                            .WithEffectOnSelf(async (self) =>
                            {
                                self.AddQEffect(RaisingSymbol);
                            })
                            );

                        }

                    });
                    if (creature.HasFeat(EmblazonShieldName)) {
                        creature.AddQEffect(new QEffect()
                        {
                            YouAcquireQEffect = (qSelf, qAdded) =>
                            {
                                var owner = qSelf.Owner;
                                if (qAdded.Id == QEffectId.RaisingAShield) owner.AddQEffect(RaisingSymbol);
                                bool shieldBlock = owner.HasFeat(FeatName.ShieldBlock);
                                bool wieldsShield = owner.WieldsItem(Trait.Shield);
                                if (qAdded.Name == "Raising Symbol" && wieldsShield) owner.AddQEffect(QEffect.RaisingAShield(shieldBlock));
                                return qAdded;

                            }
                        });
                    }

                });
        }

    }



}
