using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

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
                        shootingStar.StateCheck  =  (QEffect qSelf) =>
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
                            strike.StrikeModifiers.OnEachTarget = delegate (Creature striker, Creature target, CheckResult result) {
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

                    //replaces spell strike with ranged compatible spell strike
                    .WithOnCreature(creature =>
                    {
                        creature.QEffects.ForEach(qeffect =>
                        {
                            if (qeffect!.Name?.Equals("Spellstrike {icon:TwoActions}") == true)
                            {
                                qeffect.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        });

                        QEffect qfSpellstrike = new QEffect("Spellstrike {icon:TwoActions}", "You cast a spell and deliver it through your weapon Strike.");
                        qfSpellstrike.ProvideStrikeModifierAsPossibility = delegate (Item weapon)
                        {
                            Creature self = qfSpellstrike.Owner;
                            return CreateSpellcastingMenu("Spellstrike", new Func<CombatAction, CombatAction>(CreateSpellstrike!));
                            SubmenuPossibility CreateSpellcastingMenu(string caption, Func<CombatAction, CombatAction?> spellTransformation)
                            {
                                Func<CombatAction, CombatAction?> spellTransformation2 = spellTransformation;
                                SubmenuPossibility castASpell = new SubmenuPossibility(new SideBySideIllustration(weapon.Illustration, IllustrationName.CastASpell), caption)
                                {
                                    Subsections = new List<PossibilitySection>()
                                };
                                SpellcastingSource sourceByOrigin = self.Spellcasting!.GetSourceByOrigin(Trait.Magus)!;
                                if (self.Spellcasting!.FocusPoints > 0 && sourceByOrigin.FocusSpells.Count > 0)
                                {
                                    AddSpellSubmenu("Focus spells " + string.Join("", Enumerable.Repeat("{icon:SpontaneousSpellSlot}", self.Spellcasting!.FocusPoints)), sourceByOrigin.FocusSpells);
                                }

                                if (sourceByOrigin.Cantrips.Count > 0)
                                {
                                    AddSpellSubmenu("Cantrips", sourceByOrigin.Cantrips);
                                }

                                if ((sourceByOrigin.Kind == SpellcastingKind.Prepared || sourceByOrigin.Kind == SpellcastingKind.Innate) && sourceByOrigin.Spells.Count > 0)
                                {
                                    for (int i = 1; i <= 10; i++)
                                    {
                                        int levelJ2 = i;
                                        AddSpellSubmenu("Level " + i, sourceByOrigin.Spells.Where((CombatAction sp) => sp.SpellLevel == levelJ2));
                                    }
                                }

                                if (sourceByOrigin.Kind == SpellcastingKind.Spontaneous && sourceByOrigin.Spells.Count > 0)
                                {
                                    for (int j = 1; j <= 10; j++)
                                    {
                                        int levelJ = j;
                                        if (sourceByOrigin.SpontaneousSpellSlots[j] > 0)
                                        {
                                            AddSpellSubmenu("Level " + j + " " + string.Join("", Enumerable.Repeat("{icon:SpontaneousSpellSlot}", sourceByOrigin.SpontaneousSpellSlots[j])), sourceByOrigin.Spells.Where((CombatAction sp) => sp.SpellLevel == levelJ));
                                        }
                                    }
                                }

                                return castASpell;
                                void AddSpellSubmenu(string miniSectionCaption, IEnumerable<CombatAction> spells)
                                {
                                    PossibilitySection possibilitySection = new PossibilitySection(miniSectionCaption);
                                    foreach (CombatAction spell in spells)
                                    {
                                        CombatAction spellstrike = spellTransformation2(spell)!;
                                        if (spellstrike != null)
                                        {
                                            string name = spellstrike.Name;
                                            spellstrike.Name = "Spellstrike (" + spellstrike?.ToString() + ")";
                                            possibilitySection.Possibilities.Add(new ActionPossibility(spellstrike!, PossibilitySize.Half)
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

                            CombatAction? CreateSpellstrike(CombatAction spell)
                            {

                                if (spell.SubspellVariants != null || spell.Variants != null) //tried to include variant spells but this doesn't seem to do anything
                                {
                                    spell.ActionCost = 2;
                                    spell.SpentActions = 2;
                                }

                                if (spell.ActionCost != 1 && spell.ActionCost != 2)
                                {
                                    return null;
                                }

                                if (!spell.HasTrait(Trait.Attack))
                                {
                                    return null;
                                }

                                CombatAction spellstrike = qfSpellstrike.Owner.CreateStrike(weapon);
                                spellstrike.Name = spell.Name;
                                spellstrike.Illustration = new SideBySideIllustration(spellstrike.Illustration, spell.Illustration);
                                spellstrike.Traits.AddRange(spell.Traits.Except(new Trait[5]
                                {
                            Trait.Prepared,
                            Trait.Spontaneous,
                            Trait.Spell,
                            Trait.Melee, //stops yeeting animation from melee spells cast through a ranged weapon
                            Trait.Ranged
                                }));
                                spellstrike.Traits.Add(Trait.Spellstrike);
                                spellstrike.Traits.Add(Trait.Basic);
                                spellstrike.ActionCost = 2;
                                ((CreatureTarget)spellstrike.Target).WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => a.HasEffect(QEffectId.SpellstrikeDischarged) ? Usability.NotUsable("You must first recharge your Spellstrike by spending an action or casting a focus spell.") : Usability.Usable);
                                spellstrike.StrikeModifiers.OnEachTarget = async delegate (Creature a, Creature d, CheckResult result)
                                {
                                    a.Spellcasting!.UseUpSpellcastingResources(spell);
                                    if (result >= CheckResult.Success)
                                    {
                                        if (spell.EffectOnOneTarget != null)
                                        {
                                            await spell.EffectOnOneTarget!(spell, a, d, result);
                                        }

                                        if (spell.EffectOnChosenTargets != null)
                                        {
                                            await spell.EffectOnChosenTargets!(spell, a, new ChosenTargets
                                            {
                                                ChosenCreature = d,
                                                ChosenCreatures = { d }
                                            });
                                        }
                                    }

                                    a.AddQEffect(new QEffect
                                    {
                                        Id = QEffectId.SpellstrikeDischarged,
                                        AfterYouTakeAction = delegate (QEffect qfDischarge, CombatAction action) {
                                            if (action.HasTrait(Trait.Focus))
                                            {
                                                qfDischarge.ExpiresAt = ExpirationCondition.Immediately;
                                            }

                                            return Task.CompletedTask;
                                        },
                                        ProvideMainAction = delegate (QEffect qfDischarge)
                                        {
                                            return (ActionPossibility)new CombatAction(qfDischarge.Owner, IllustrationName.Good, "Recharge Spellstrike", new Trait[2]
                                            {
                                        Trait.Concentrate,
                                        Trait.Basic
                                            }, "Recharge your Spellstrike so that you can use it again." + (qfDischarge.Owner.HasEffect(QEffectId.MagussConcentration) ? " {Blue}You gain a +1 circumstance bonus to your next attack until the end of your next turn.{/Blue}" : ""), Target.Self()).WithActionCost(1).WithSoundEffect(SfxName.AuraExpansion).WithEffectOnSelf(async delegate (Creature self)
                                            {
                                                qfDischarge.ExpiresAt = ExpirationCondition.Immediately;
                                                if (self.HasEffect(QEffectId.MagussConcentration))
                                                {
                                                    self.AddQEffect(new QEffect("Magus's Concentration", "You have +1 to your next attack roll.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self, (Illustration)IllustrationName.Good)
                                                    {
                                                        CannotExpireThisTurn = true,
                                                        BonusToAttackRolls = (QEffect qf, CombatAction ca, Creature? df) => (!ca.HasTrait(Trait.Attack)) ? null : new Bonus(1, BonusType.Circumstance, "Magus's Concentration"),
                                                        AfterYouTakeAction = delegate (QEffect qf, CombatAction ca) {
                                                            if (ca.HasTrait(Trait.Attack))
                                                            {
                                                                qf.ExpiresAt = ExpirationCondition.Immediately;
                                                            }

                                                            return Task.CompletedTask;
                                                        }
                                                    });
                                                }
                                            });
                                        }
                                    });
                                };
                                spellstrike.Description = StrikeRules.CreateBasicStrikeDescription(spellstrike.StrikeModifiers, null, "The success effect of " + spell.Name + " is inflicted upon the target.", "Critical spell effect.", null, "You can't use Spellstrike again until you recharge it by spending an action or casting a focus spell.");
                                return spellstrike;
                            }
                        };
                        creature.AddQEffect(qfSpellstrike);
                    }));
        }

    }
}
