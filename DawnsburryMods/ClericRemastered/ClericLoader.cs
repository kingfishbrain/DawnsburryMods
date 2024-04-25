using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Mechanics.Rules;

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
            yield return new Feat(FeatName.HealingFont, "Through your deity's blessing, you gain additional spells that channel the life force called positive energy.", 
                "You gain 4 additional spell slots each day at your highest rank of cleric spell slots. You can prepare only {i}heal{/i} spells in these slots. "
                , new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
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
            yield return new Feat(FeatName.HarmfulFont, "Through your deity's blessing, you gain additional spells that channel the counterforce to life, the so-called negative energy.",
                "You gain 4 additional spell slots each day at your highest rank of cleric spell slots. You can prepare only {i}harm{/i} spells in these slots. "
               , new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
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



            var EmblazonTrait = ModManager.RegisterTrait(
                "Emblazoned",
                new TraitProperties("Emblazoned", true,
                    "A weapon or a shield that has a religious symbol etched onto it. " +
                    "This weapon or shield has been emblazoned by the Emblazoned Armaments feat.")
                {
                    RelevantForShortBlock = true
                });

            var EmblazonShieldName = ModManager.RegisterFeatName("Emblazon Shield");
            var EmblazonShield = new TrueFeat(EmblazonShieldName, 2, "Carefully etching a sacred image into your shield, you steel yourself for battle. ",
                "The symbol gets etched into the shield you hold in your right hand at the start of any combat (Make sure to always have a shield there!)." +
                "The symbol does not persist between combat encounters (Meaning you won't be able to have multiple shields with the symbol)." +
                "This shield gains a +1 status bonus to its Hardness. (This causes it to reduce more damage with the Shield Block reaction.).",
                new Trait[0] { })
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Emblazon Shield", "Your emblazoned shield gains a +1 status bonus to its Hardness.")
                    {
                        StartOfCombat = (async (QEffect qSelf) =>
                        {
                            var shield = qSelf.Owner.SecondaryItem;
                            if (shield?.HasTrait(Trait.Shield) == true)
                            {
                                shield.Traits.Add(EmblazonTrait);
                                shield.Hardness += 1;
                            }
                        }
                        )
                    }
                    );
                });
            var EmblazonWeaponName = ModManager.RegisterFeatName("Emblazon Weapon");
            var EmblazonWeapon = new TrueFeat(EmblazonWeaponName, 2, "Carefully etching a sacred image into your weapon, you steel yourself for battle. ",
                "The symbol gets etched into the weapon you hold in your left hand at the start of any combat (Make sure to always have a weapon there!). " +
                "The symbol does not persist between combat encounters (Meaning you won't be able to have multiple weapons with the symbol). The wielder gains a +1 status bonus to damage rolls with this Weapon.",
                new Trait[0] { })
                .WithOnCreature(creature =>
                {
                    creature.AddQEffect(new QEffect("Emblazon Weapon", "The wielder gains a +1 status bonus to damage rolls with the emblazoned weapon.")
                    {
                        StartOfCombat = (async (QEffect qSelf) =>
                        {
                            var weapon = qSelf.Owner.PrimaryItem;
                            if (weapon?.HasTrait(Trait.Weapon) == true)
                            {
                                weapon.Traits.Add(EmblazonTrait);
                            }
                        }),

                        BonusToDamage = (qSelf, combatAction, target) =>
                        {
                            if (combatAction.HasTrait(Trait.Strike) && combatAction.Item!.HasTrait(EmblazonTrait)) return new Bonus(1, BonusType.Status, "Emblazon Weapons", true);
                            return null;
                        }
                    }
                    );
                });
            var EmblazonArmaments = ModManager.RegisterFeatName("Emblazon Armaments");
            yield return new TrueFeat(EmblazonArmaments, 2, "Carefully etching a sacred image into a physical object, you steel yourself for battle.",
                "Choose if you want to emblazon your weapons or your shields.", new Trait[1] { Trait.Cleric }, new List<Feat>() { EmblazonWeapon, EmblazonShield });




            var RaiseSymbol = ModManager.RegisterFeatName("Raise Symbol");
            yield return new TrueFeat(RaiseSymbol, 4, "You present your religious symbol emphatically.",
                "You gain a +2 circumstance bonus to saving throws until the start of your next turn. " +
                "While it’s raised, if you roll a success at a saving throw against a positive or negative effect, you get a critical success instead." +
                "If you're wielding a shield and you have the Emblazon Shield feat, you gain the effects of Raise a Shield when you use this action and the effects of this action when you Raise a Shield."
                , new Trait[1] { Trait.Cleric })
                .WithActionCost(1)
                .WithOnCreature(creature =>
                {
                    var RaisingSymbol = new QEffect("Raising Symbol", "You gain a +2 circumstance bonus to saving throws until the start of your next turn." +
                        "While it’s raised, if you roll a success at a saving throw against a positive or negative effect, you get a critical success instead.",
                        ExpirationCondition.CountsDownAtStartOfSourcesTurn, creature, IllustrationName.Shield)
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

                    creature.AddQEffect(new QEffect()
                    {
                        ProvideMainAction = (qSelf) =>
                        {
                            if (qSelf.Owner.QEffects.Contains(RaisingSymbol)) return null;
                            return new ActionPossibility(new CombatAction
                            (creature, IllustrationName.DivineLance, "Raise Symbol", Array.Empty<Trait>(),
                                "You gain a +2 circumstance bonus to saving throws until the start of your next turn. " +
                                "While it’s raised, if you roll a success at a saving throw against a positive or negative effect, you get a critical success instead." +
                                "If you're wielding an emblazoned shield and you have the Emblazon Shield feat, you gain the effects of Raise a Shield when you use this action and the effects of this action when you Raise a Shield.",
                                    Target.Self())
                            .WithActionCost(1)
                            .WithEffectOnSelf(async (self) =>
                            {
                                self.AddQEffect(RaisingSymbol);
                            })
                            );

                        }

                    });
                    if (creature.HasFeat(EmblazonShieldName))
                    {
                        creature.AddQEffect(new QEffect()
                        {
                            AfterYouAcquireEffect = async (qSelf, qAdded) =>
                            {
                                var owner = qSelf.Owner;
                                bool wieldsEmblazonedShield = owner.HeldItems.Any(item => item.HasTrait(Trait.Shield) && item.HasTrait(EmblazonTrait));
                                if (qAdded.Id == QEffectId.RaisingAShield && !owner.QEffects.Contains(RaisingSymbol) && wieldsEmblazonedShield) owner.AddQEffect(RaisingSymbol);
                                bool shieldBlock = owner.HasFeat(FeatName.ShieldBlock);
                                //  GeneralLog.Log("Gets here and shield is: " + wieldsEmblazonedShield.ToString());
                                if (qAdded.Name == "Raising Symbol" && wieldsEmblazonedShield && !owner.QEffects.Contains(QEffect.RaisingAShield(shieldBlock))) owner.AddQEffect(QEffect.RaisingAShield(shieldBlock));
                            }
                        });
                    }

                });
            var ChannelSmite = ModManager.RegisterFeatName("Channel Smite");
            yield return new TrueFeat(ChannelSmite, 4, "You siphon the energies of life and death through a melee attack and into your foe.",
                "Cost Expend a harm or heal spell\r\n" +
                "Make a melee Strike. On a hit, you cast the 1-action version of the expended spell to damage the target, in addition to the normal damage from your Strike. " +
                "The target automatically gets a failure on its save (or a critical failure if your Strike was a critical hit). The spell doesn’t have the manipulate trait when cast this way.",
                new Trait[1] { Trait.Cleric })
                .WithOnCreature(creature =>
                {
                    QEffect channelSmite = new QEffect("Channel Smite {icon:TwoActions}", "You cast Harm or Heal and deliver it through your weapon.");
                    channelSmite.ProvideStrikeModifierAsPossibility = delegate (Item weapon)
                    {
                        if (!weapon.HasTrait(Trait.Melee))
                        {
                            return null;
                        }
                        Creature owner = channelSmite.Owner;

                        CombatAction? createChannelSmite(CombatAction spell)
                        {
                            if (!(spell.Name == "Harm" || spell.Name == "Heal"))
                            {
                                return null;
                            }
                            spell.SpentActions = 1;
                            CombatAction strike = owner.CreateStrike(weapon);
                            strike.Name = spell.Name;
                            strike.Illustration = new SideBySideIllustration(strike.Illustration, spell.Illustration);
                            strike.Traits.AddRange(spell.Traits.Except(new Trait[5]
                            {
                                Trait.Ranged,
                                Trait.Prepared,
                                Trait.Spontaneous,
                                Trait.Spell,
                                Trait.Manipulate
                            }));
                            strike.Traits.Add(Trait.Basic);
                            strike.ActionCost = 2;
                            // add restrictions like holy castegation and undead and stuff
                            ((CreatureTarget)strike.Target).WithAdditionalConditionOnTargetCreature((Creature attacker, Creature target) =>
                            {
                                bool holyCastigation = attacker?.HasEffect(QEffectId.HolyCastigation) ?? false;
                                bool isHeal = (spell.Name == "Heal");
                                if (isHeal && target.IsLivingCreature && (!holyCastigation || !target.HasTrait(Trait.Fiend))) return Usability.NotUsableOnThisCreature("Target would be healed by strike.");
                                return (!isHeal && target.HasTrait(Trait.Undead)) ? Usability.NotUsableOnThisCreature("Target would be healed by strike.") : Usability.Usable;
                            });
                            strike.StrikeModifiers.OnEachTarget = async delegate (Creature striker, Creature target, CheckResult result)
                            {
                                striker.Spellcasting!.UseUpSpellcastingResources(spell);
                                if (result >= CheckResult.Success)
                                {
                                    result = (result == CheckResult.Success) ? CheckResult.Failure : CheckResult.CriticalFailure; //harm is a saving throw so an attack success means a failed saving throw
                                    await spell.EffectOnOneTarget!(spell, striker, target, result);
                                    
                                }

                            };
                            strike.Description = StrikeRules.CreateBasicStrikeDescription(strike.StrikeModifiers, null, "The success effect of " + spell.Name + "(" + spell.SpellLevel + ")" + " is inflicted upon the target.", "Critical spell effect.", null);
                            return strike;
                        }
                        SubmenuPossibility CreateSpellcastingMenu(string caption, Func<CombatAction, CombatAction?> spellTransformation)
                        {
                            Func<CombatAction, CombatAction?> spellTransformation2 = spellTransformation;
                            SubmenuPossibility castASpell = new SubmenuPossibility(new SideBySideIllustration(weapon.Illustration, IllustrationName.CastASpell), caption)
                            {
                                Subsections = new List<PossibilitySection>()
                            };
                            SpellcastingSource sourceByOrigin = owner.Spellcasting!.GetSourceByOrigin(Trait.Cleric)!;
                            if ((sourceByOrigin.Kind == SpellcastingKind.Prepared || sourceByOrigin.Kind == SpellcastingKind.Innate) && sourceByOrigin.Spells.Count > 0)
                            {
                                for (int i = 1; i <= 10; i++)
                                {
                                    int levelJ2 = i;
                                    AddSpellSubmenu("Level " + i, sourceByOrigin.Spells.Where((CombatAction sp) => sp.SpellLevel == levelJ2));
                                }
                            }
                            return castASpell;
                            void AddSpellSubmenu(string miniSectionCaption, IEnumerable<CombatAction> spells)
                            {
                                PossibilitySection possibilitySection = new PossibilitySection(miniSectionCaption);
                                foreach (CombatAction spell in spells)
                                {
                                    CombatAction? strike = spellTransformation2(spell);
                                    if (strike != null)
                                    {
                                        string name = strike.Name;
                                        strike.Name = "Channel Smite (" + strike.ToString() + ")";
                                        possibilitySection.Possibilities.Add(new ActionPossibility(strike, PossibilitySize.Half)
                                        {
                                            Caption = name
                                        });
                                    }
                                }

                                if (possibilitySection.Possibilities.Count > 0)
                                {
                                    castASpell.Subsections.Add(possibilitySection);
                                }
                            }
                        }
                        return CreateSpellcastingMenu("Channel Smite", createChannelSmite);

                    };
                    creature.AddQEffect(channelSmite);
                });

            var RestorativeStrike = ModManager.RegisterFeatName("Restorative Strike");
            yield return new TrueFeat(RestorativeStrike, 4, "You balance both sides of the scales, restoring yourself while striking a foe.",
                "Requirements: You have a heal spell you can cast\r\n" +
                "Cast a 1-action heal spell to heal yourself, expending the spell normally. It loses the manipulate trait when cast this way. Then make a melee Strike. " +
                "If you make this Strike with your deity’s favored weapon, you gain a +1 status bonus to the attack roll.\n" +
                "If the Strike hits, you can target a second willing creature to heal the same amount from the spell. This creature can be outside of the spell’s range, " +
                "provided it’s adjacent to the enemy you hit. ",
                new Trait[1] { Trait.Cleric })
                .WithOnCreature(creature =>
                {
                    QEffect restorativeStrike = new QEffect("Restorative Strike {icon:TwoActions}", "You cast Heal and strike. On a hit you heal an ally too.");
                    restorativeStrike.ProvideStrikeModifierAsPossibility = delegate (Item weapon)
                    {
                        if (!weapon.HasTrait(Trait.Melee))
                        {
                            return null;
                        }
                        Creature owner = restorativeStrike.Owner;

                        CombatAction? createRestorativeStrike(CombatAction spell)
                        {
                            //if there are ever undead PCs consider checking for harm here
                            if (spell.Name != "Heal")
                            {
                                return null;
                            }
                            spell.SpentActions = 1;
                            CombatAction strike = owner.CreateStrike(weapon);
                            strike.Name = spell.Name;
                            strike.Illustration = new SideBySideIllustration(strike.Illustration, spell.Illustration);
                            strike.Traits.AddRange(spell.Traits.Except(new Trait[5]
                            {
                                Trait.Ranged,
                                Trait.Prepared,
                                Trait.Spontaneous,
                                Trait.Spell,
                                Trait.Manipulate
                            }));
                            strike.Traits.Add(Trait.Basic);
                            strike.ActionCost = 2;
                            strike.StrikeModifiers.OnEachTarget = async delegate (Creature striker, Creature target, CheckResult result)
                            {
                                striker.Spellcasting!.UseUpSpellcastingResources(spell);

                                int lastHp = striker.HP;
                                await spell.EffectOnOneTarget!(spell, striker, striker, result);
                                int healed = striker.HP - lastHp;

                                if (result >= CheckResult.Success)
                                {

                                    var healExtra = new CombatAction(striker, IllustrationName.None, "Restorative Strike (on hit heal)", new Trait[1] { Trait.Positive },
                                                  "",
                                                  Target.RangedFriend(5).WithAdditionalConditionOnTargetCreature((caster, friend) =>
                                                  {
                                                      if(friend == caster) return Usability.NotUsable("Second heal can't be used on yourself.");
                                                      if (friend.IsAdjacentTo(target) || friend.IsAdjacentTo(caster)) return Usability.Usable;
                                                      return Usability.NotUsable("Not in range.");
                                                  }))
                                                  .WithActionCost(0);

                                    healExtra = healExtra.WithEffectOnEachTarget(async (heal, caster, target, result) =>
                                    {
                                        target.Heal(healed.ToString(), spell);
                                    });
                                    await striker.Battle.GameLoop.FullCast(healExtra);
                                }

                            };
                            strike.Description = StrikeRules.CreateBasicStrikeDescription(strike.StrikeModifiers, null, "You heal yourself and on hit an ally for the same amount.", null, null);
                            return strike;
                        }
                        SubmenuPossibility CreateSpellcastingMenu(string caption, Func<CombatAction, CombatAction?> spellTransformation)
                        {
                            Func<CombatAction, CombatAction?> spellTransformation2 = spellTransformation;
                            SubmenuPossibility castASpell = new SubmenuPossibility(new SideBySideIllustration(weapon.Illustration, IllustrationName.CastASpell), caption)
                            {
                                Subsections = new List<PossibilitySection>()
                            };
                            SpellcastingSource sourceByOrigin = owner.Spellcasting!.GetSourceByOrigin(Trait.Cleric)!;
                            if ((sourceByOrigin.Kind == SpellcastingKind.Prepared || sourceByOrigin.Kind == SpellcastingKind.Innate) && sourceByOrigin.Spells.Count > 0)
                            {
                                for (int i = 1; i <= 10; i++)
                                {
                                    int levelJ2 = i;
                                    AddSpellSubmenu("Level " + i, sourceByOrigin.Spells.Where((CombatAction sp) => sp.SpellLevel == levelJ2));
                                }
                            }
                            return castASpell;
                            void AddSpellSubmenu(string miniSectionCaption, IEnumerable<CombatAction> spells)
                            {
                                PossibilitySection possibilitySection = new PossibilitySection(miniSectionCaption);
                                foreach (CombatAction spell in spells)
                                {
                                    CombatAction? strike = spellTransformation2(spell);
                                    if (strike != null)
                                    {
                                        string name = strike.Name;
                                        strike.Name = "Restorative Strike (" + strike.ToString() + ")";
                                        possibilitySection.Possibilities.Add(new ActionPossibility(strike, PossibilitySize.Half)
                                        {
                                            Caption = name
                                        });
                                    }
                                }

                                if (possibilitySection.Possibilities.Count > 0)
                                {
                                    castASpell.Subsections.Add(possibilitySection);
                                }
                            }
                        }
                        return CreateSpellcastingMenu("Restorative Strike", createRestorativeStrike);

                    };
                    restorativeStrike.BonusToAttackRolls = (qSelf, combatAction, target) =>
                    {
                        if (combatAction == null) return null;
                        var deity = qSelf.Owner.PersistentCharacterSheet?.Calculated.Deity;
                        var weapon = deity?.FavoredWeapon;
                        bool isFavored = (weapon == combatAction.Item?.BaseItemName) && (weapon != null);
                        if (combatAction.Name.Contains("Restorative Strike") && isFavored) return new Bonus(1, BonusType.Status, combatAction.Name);
                        return null;
                    };
                    creature.AddQEffect(restorativeStrike);
                });




        }

    }



}
