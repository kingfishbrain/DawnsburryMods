using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.ThirdParty.SteamApi;

namespace DawnsburryMods.Starlit_Span
{
    public static class StarlitSpan
    {

        private static ModdedIllustration? shootingStarIllustration;

        static FeatName starlitSpanName;

        private static String shootingStarDescription = "Make a ranged Strike, ignoring the target's concealment and reducing the target's cover by one degree for this Strike only " +
            "(greater to standard, standard to lesser, and lesser to none). If the Strike hits, the meteor trail hangs in the air. " +
            "This gives the benefits of concealment negation and cover reduction to any attacks made against the creature (by anyone) until the start of your next turn.\n";

        [DawnsburyDaysModMainMethod]
        public static void loadMod()
        {
            shootingStarIllustration = new ModdedIllustration(@"StarlitSpanResources\ShootingStar.png");

            ClassSelectionFeat magus = (AllFeats.All.Find(feat => feat.FeatName == FeatName.Magus) as ClassSelectionFeat)!;
            starlitSpanName = ModManager.RegisterFeatName("Hybrid Study: Starlit Span");
            var starlitSpan = initStarlitSpan();
            ModManager.AddFeat(starlitSpan);
            magus.Subfeats!.Add(starlitSpan);
            ExpensiveSpellstrike.load(starlitSpanName);
        }

        private static CoverKind reduceCover(CoverKind cover) =>
            cover switch
            {
                CoverKind.Greater => CoverKind.Standard,
                CoverKind.Standard => CoverKind.Lesser,
                CoverKind.Lesser => CoverKind.None,
                _ => cover
            };


        public static Feat initStarlitSpan()
        {
            return (new Feat(starlitSpanName, "With magic, the sky's the limit, and you can't be bound by the confines of physical proximity. " +
                "Your power reaches as far as your senses can perceive, transcending the space between you and your target even with spells that normally require direct physical contact.",
                "When you use Spellstrike, you can make a ranged weapon or ranged unarmed Strike, as long as the target is within the first range increment of your ranged weapon or ranged unarmed attack. " +
                "You can deliver the spell even if its range is shorter than the range increment of your ranged attack. \n\n" +
                "Conflux Spell: Shooting Star:\n" +
                shootingStarDescription, new List<Trait>(), null)
                    .WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
                    {
                        sheet.FocusPointCount += 2;
                    }
                    )
                    //adds shooting star as strike modifier
                    .WithOnCreature(creature =>
                    {
                        var shootingStar = new QEffect("Shooting Star {icon:Action}", "Focus Spell that reduces cover and removes concealment through a ranged strike.");
                        shootingStar.StateCheck = (QEffect qSelf) =>
                        {
                            qSelf.Description = "Focus Spell that reduces cover and removes concealment through a ranged strike. " + qSelf.Owner!.Spellcasting!.FocusPoints + " focus points left.";
                        }
                        ;
                        shootingStar.ProvideStrikeModifier = delegate (Item weapon)
                        {
                            if (!weapon.HasTrait(Trait.Ranged))
                            {
                                return null;
                            }
                            var owner = shootingStar.Owner;
                            if (owner.Spellcasting!.FocusPoints <= 0)
                            {
                                return null;
                            }
                            var strike = owner.CreateStrike(weapon);
                            strike.Name = "Shooting Star";
                            strike.WithActionCost(1);
                            strike.Illustration = new SideBySideIllustration(weapon.Illustration, shootingStarIllustration!);
                            strike.Traits.AddRange(new Trait[6]
                                {
                                    Trait.Divination,
                                    Trait.Ranged,
                                    Trait.Magus,
                                    Trait.Spell,
                                    Trait.UnaffectedByConcealment,
                                    Trait.Focus

                                });
                            strike.Description = shootingStarDescription + owner!.Spellcasting!.FocusPoints + " focus points left.";
                            strike.StrikeModifiers.OnEachTarget = delegate (Creature striker, Creature target, CheckResult result)
                            {
                                if (result >= CheckResult.Success)
                                {
                                    var shootingStared = new QEffect("Shooting Star", "Any concealment is negated and cover counts as one step less", ExpirationCondition.CountsDownAtStartOfSourcesTurn, striker, shootingStarIllustration);
                                    shootingStared.IncreaseCover = (qSelf, combatAction, cover) => //actually decreases cover here
                                    {
                                        combatAction.Traits.Add(Trait.UnaffectedByConcealment); // this feels janky but it works 
                                        if (combatAction.HasTrait(Trait.Attack)) return reduceCover(cover);
                                        return cover;
                                    };

                                    shootingStared.RoundsLeft = 1;

                                    target.AddQEffect(shootingStared);
                                }

                                return Task.CompletedTask;
                            };
                            return strike;

                        };
                        shootingStar.AddGrantingOfTechnical(cr => cr.EnemyOf(shootingStar.Owner), qfGettingHitByShootingStar =>
                        {
                            qfGettingHitByShootingStar.IncreaseCover = (qSelf, combatAction, cover) => //actually decreases cover here
                            {
                                if (combatAction.Name == "Shooting Star") return reduceCover(cover);
                                return cover;
                            };

                        });
                        creature.AddQEffect(shootingStar);
                    }
                    )

                    //adds ranged compatible spell strike
                    .WithOnCreature(creature =>
                    {
                        QEffect qfSpellstrike = new QEffect("Spellstrike {icon:TwoActions}", "You cast a spell and deliver it through your ranged weapon Strike.")
                        {
                            ProvideStrikeModifierAsPossibilities = (Func<QEffect, Item, IEnumerable<Possibility>>)((qfSpellstrike, weapon) =>
                            {
                                if (!weapon.HasTrait(Trait.Ranged))
                                {
                                    return Array.Empty<Possibility>();
                                }

                                Creature self = qfSpellstrike.Owner;
                                if (self.Spellcasting == null)
                                {
                                    return Array.Empty<Possibility>();
                                }

                                List<SubmenuPossibility> list = new List<SubmenuPossibility>();
                                foreach (SpellcastingSource source in self.Spellcasting.Sources)
                                {
                                    list.Add(CreateSpellstrikeMenu(source, "Spellstrike", (CombatAction action) => CreateSpellstrike(action)));
                                }

                                list.RemoveAll((SubmenuPossibility sopt) => sopt.Subsections.Count == 0 || sopt.Subsections.All((PossibilitySection sss) => sss.Possibilities.Count == 0));
                                return list;
                                CombatAction? CreateSpellstrike(CombatAction spell, string? prologue = null, string? aftertext = null)
                                {
                                    if (spell.Variants != null)
                                    {
                                        return null;
                                    }

                                    if (spell.VariantsCreator != null)
                                    {
                                        return null;
                                    }

                                    if (spell.SubspellVariants != null)
                                    {
                                        return null;
                                    }

                                    if (spell.ActionCost != 1 && spell.ActionCost != 2 && spell.ActionCost != -3 && spell.ActionCost != -1 && spell.ActionCost != -4 && spell.ActionCost != -5)
                                    {
                                        return null;
                                    }

                                    if (!spell.HasTrait(Trait.Attack))
                                    {
                                        return null;
                                    }

                                    spell.Target = Target.Ranged(weapon.WeaponProperties!.RangeIncrement);  //makes sure spell and strike have the same range and only works within first increment

                                    Target target2 = ((spell.Target is DependsOnActionsSpentTarget dependsOnActionsSpentTarget) ? dependsOnActionsSpentTarget.IfTwoActions : spell.Target);
                                    CreatureTarget creatureTarget2;
                                    if (target2 is CreatureTarget creatureTarget)
                                    {
                                        creatureTarget2 = creatureTarget;
                                    }
                                    else
                                    {
                                        if (!(target2 is MultipleCreatureTargetsTarget multipleCreatureTargetsTarget))
                                        {
                                            return null;
                                        }

                                        if (multipleCreatureTargetsTarget.MinimumTargets > 1)
                                        {
                                            return null;
                                        }

                                        creatureTarget2 = multipleCreatureTargetsTarget.Targets[0];
                                    }

                                    CombatAction combatAction = qfSpellstrike.Owner.CreateStrike(weapon);
                                    combatAction.Name = spell.Name;
                                    combatAction.Illustration = new SideBySideIllustration(combatAction.Illustration, spell.Illustration);
                                    combatAction.Traits.AddRange(spell.Traits.Except(new Trait[5]
                                    {
                        Trait.Ranged,
                        Trait.Prepared,
                        Trait.Melee, //stops yeeting animation from melee spells cast through a ranged weapon
                        Trait.Spontaneous,
                        Trait.Spell
                                    }));
                                    combatAction.Traits.Add(Trait.Spellstrike);
                                    combatAction.Traits.Add(Trait.Basic);
                                    combatAction.ActionCost = 2;
                                    CreatureTarget creatureTarget3 = (CreatureTarget)combatAction.Target;
                                    creatureTarget3.WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => a.HasEffect(QEffectId.SpellstrikeDischarged) ? Usability.NotUsable("You must first recharge your Spellstrike by spending an action or casting a focus spell.") : Usability.Usable);
                                    foreach (CreatureTargetingRequirement creatureTargetingRequirement in creatureTarget2.CreatureTargetingRequirements)
                                    {
                                        creatureTarget3.WithAdditionalConditionOnTargetCreature(creatureTargetingRequirement);
                                    }

                                    combatAction.StrikeModifiers.OnEachTarget = async delegate (Creature a, Creature d, CheckResult result)
                                    {
                                        Steam.CollectAchievement("MAGUS");
                                        spell.ChosenTargets = ChosenTargets.CreateSingleTarget(d);
                                        spell.SpentActions = 2;
                                        a.Spellcasting.UseUpSpellcastingResources(spell);
                                        if (result >= CheckResult.Success)
                                        {
                                            bool flag = false;
                                            QEffect qEffect = a.FindQEffect(QEffectId.Stupefied);
                                            if (qEffect != null)
                                            {
                                                (CheckResult, string) tuple = Checks.RollFlatCheck(5 + qEffect.Value);
                                                flag = tuple.Item1 < CheckResult.Success;
                                                if (flag)
                                                {
                                                    a.Battle.Log(spell.Name + " from Spellstrike {Red}fizzled{/} due to stupefied: " + tuple.Item2);
                                                }
                                                else
                                                {
                                                    a.Battle.Log("Spellstrike stupefied flat check {Green}passed{/}: " + tuple.Item2);
                                                }
                                            }

                                            if (!flag)
                                            {
                                                if (spell.EffectOnOneTarget != null)
                                                {
                                                    await spell.EffectOnOneTarget(spell, a, d, result);
                                                }

                                                if (spell.EffectOnChosenTargets != null)
                                                {
                                                    await spell.EffectOnChosenTargets(spell, a, ChosenTargets.CreateSingleTarget(d));
                                                }
                                            }
                                        }



                                        a.AddQEffect(new QEffect
                                        {
                                            Id = QEffectId.SpellstrikeDischarged,
                                            AfterYouTakeAction = async delegate (QEffect qfDischarge, CombatAction action)
                                            {
                                                if (action.HasTrait(Trait.Focus))
                                                {
                                                    qfDischarge.ExpiresAt = ExpirationCondition.Immediately;
                                                }
                                            },
                                            ProvideMainAction = (QEffect qfDischarge) => (ActionPossibility)new CombatAction(qfDischarge.Owner, IllustrationName.Good, "Recharge Spellstrike", new Trait[2]
                                            {
                                    Trait.Concentrate,
                                    Trait.Basic
                                            }, "Recharge your Spellstrike so that you can use it again." + (qfDischarge.Owner.HasEffect(QEffectId.MagussConcentration) ? " {Blue}You gain a +1 circumstance bonus to your next attack until the end of your next turn.{/Blue}" : ""), Target.Self()).WithActionCost(1).WithSoundEffect(SfxName.AuraExpansion).WithEffectOnSelf(async delegate (Creature self2)
                                            {
                                                RechargeSpellstrike(self2);
                                            })
                                        });
                                        if (a.HasEffect(QEffectId.EndlessSpellstrike))
                                        {
                                            RechargeSpellstrike(a);
                                        }

                                    };
                                    combatAction.Description = StrikeRules.CreateBasicStrikeDescription3(combatAction.StrikeModifiers, null, prologueText: prologue, additionalSuccessText: "The success effect of " + spell.Name + " is inflicted upon the target.", additionalCriticalSuccessText: "Critical spell effect.", additionalFailureText: null, additionalAftertext: ("You can't use Spellstrike again until you recharge it by spending an action or casting a focus spell." + ((aftertext != null) ? (" " + aftertext) : "")));
                                    return combatAction;
                                }

                                SubmenuPossibility CreateSpellstrikeMenu(SpellcastingSource source, string caption, Func<CombatAction, CombatAction?> spellTransformation)
                                {
                                    Func<CombatAction, CombatAction?> spellTransformation2 = spellTransformation;
                                    string caption2 = caption;
                                    string text = ((source.ClassOfOrigin == Trait.Magus && (source.Self.Spellcasting == null || source.Self.Spellcasting.Sources.Count == 1)) ? "" : (" (" + source.ClassOfOrigin.HumanizeTitleCase2() + ")"));
                                    SubmenuPossibility castASpell = new SubmenuPossibility(new SideBySideIllustration(weapon.Illustration, IllustrationName.CastASpell), caption2 + text)
                                    {
                                        Subsections = new List<PossibilitySection>()
                                    };
                                    if (self.Spellcasting.FocusPoints > 0 && source.FocusSpells.Count > 0)
                                    {
                                        AddSpellSubmenu("Focus spells " + string.Join("", Enumerable.Repeat("{icon:SpontaneousSpellSlot}", self.Spellcasting.FocusPoints)), source.FocusSpells);
                                    }

                                    if (source.PsiCantrips.Count > 0)
                                    {
                                        AddSpellSubmenu("Psi cantrips " + string.Join("", Enumerable.Repeat("{icon:SpontaneousSpellSlot}", self.Spellcasting.FocusPoints)), source.PsiCantrips);
                                    }

                                    if (source.Cantrips.Count > 0)
                                    {
                                        AddSpellSubmenu("Cantrips", source.Cantrips);
                                    }

                                    if ((source.Kind == SpellcastingKind.Prepared || source.Kind == SpellcastingKind.Innate) && source.Spells.Count > 0)
                                    {
                                        for (int i = 1; i <= 10; i++)
                                        {
                                            int levelJ2 = i;
                                            AddSpellSubmenu("Level " + i, source.Spells.Where((CombatAction sp) => sp.SpellLevel == levelJ2));
                                        }
                                    }

                                    if (source.Kind == SpellcastingKind.Spontaneous && source.Spells.Count > 0)
                                    {
                                        for (int j = 1; j <= 10; j++)
                                        {
                                            int levelJ = j;
                                            if (source.SpontaneousSpellSlots[j] > 0)
                                            {
                                                AddSpellSubmenu("Level " + j + " " + string.Join("", Enumerable.Repeat("{icon:SpontaneousSpellSlot}", source.SpontaneousSpellSlots[j])), source.Spells.Where((CombatAction sp) => sp.SpellLevel == levelJ));
                                            }
                                        }
                                    }

                                    return castASpell;
                                    void AddSpellSubmenu(string miniSectionCaption, IEnumerable<CombatAction> spells)
                                    {
                                        PossibilitySection possibilitySection = new PossibilitySection(miniSectionCaption);
                                        foreach (CombatAction spell3 in spells)
                                        {
                                            CombatAction combatAction = spellTransformation2(spell3);
                                            if (combatAction != null)
                                            {
                                                string name = combatAction.Name;
                                                combatAction.Name = $"{caption2} ({combatAction})";
                                                if (spell3.HasTrait(Trait.Psi))
                                                {
                                                    CombatAction combatActionSpell = AllSpells.CreateModernSpell(spell3.SpellId, spell3.Owner, spell3.SpellLevel, inCombat: true, new SpellInformation
                                                    {
                                                        PsychicAmpInformation = new PsychicAmpInformation
                                                        {
                                                            Amped = true
                                                        },
                                                        ClassOfOrigin = Trait.Psychic
                                                    }).CombatActionSpell;
                                                    combatActionSpell.SpellcastingSource = spell3.SpellcastingSource;
                                                    combatActionSpell.Name = "Amped " + combatActionSpell.Name;
                                                    CombatAction combatAction2 = spellTransformation2(combatActionSpell);
                                                    if (combatAction2 != null)
                                                    {
                                                        combatAction2.Name = caption2 + " (" + combatActionSpell.Name + ")";
                                                        possibilitySection.Possibilities.Add(new SubmenuPossibility(spell3.Illustration, spell3.Name, PossibilitySize.Half)
                                                        {
                                                            SpellIfAny = combatAction,
                                                            Subsections =
                                                {
                                                new PossibilitySection(spell3.Name)
                                                {
                                                    Possibilities =
                                                    {
                                                        (Possibility)new ActionPossibility(combatAction)
                                                        {
                                                            Caption = name
                                                        },
                                                        (Possibility)new ActionPossibility(combatAction2)
                                                        {
                                                            Caption = combatActionSpell.Name
                                                        }
                                                    }
                                                }
                                                }
                                                        });
                                                    }
                                                    else
                                                    {
                                                        possibilitySection.Possibilities.Add(new ActionPossibility(combatAction, PossibilitySize.Half)
                                                        {
                                                            Caption = name
                                                        });
                                                    }
                                                }
                                                else
                                                {
                                                    possibilitySection.Possibilities.Add(new ActionPossibility(combatAction, PossibilitySize.Half)
                                                    {
                                                        Caption = name
                                                    });
                                                }
                                            }
                                        }

                                        if (possibilitySection.Possibilities.Count > 0)
                                        {
                                            castASpell.Subsections.Add(possibilitySection);
                                        }
                                    }
                                }
                            }),
                        };
                        creature.AddQEffect(qfSpellstrike);
                    }));
        }
        public static void RechargeSpellstrike(Creature magus)
        {
            QEffect qEffect = magus.FindQEffect(QEffectId.SpellstrikeDischarged);
            if (qEffect == null)
            {
                return;
            }

            qEffect.ExpiresAt = ExpirationCondition.Immediately;
            if (!magus.HasEffect(QEffectId.MagussConcentration))
            {
                return;
            }

            magus.AddQEffect(new QEffect("Magus's Concentration", "You have +1 to your next attack roll.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, magus, IllustrationName.Good)
            {
                CannotExpireThisTurn = true,
                CountsAsBeneficialToSource = true,
                BonusToAttackRolls = (QEffect qf, CombatAction ca, Creature? df) => (!ca.HasTrait(Trait.Attack)) ? null : new Bonus(1, BonusType.Circumstance, "Magus's Concentration"),
                AfterYouTakeAction = async delegate (QEffect qf, CombatAction ca)
                {
                    if (ca.HasTrait(Trait.Attack))
                    {
                        qf.ExpiresAt = ExpirationCondition.Immediately;
                    }
                }
            });
        }
    }
}
