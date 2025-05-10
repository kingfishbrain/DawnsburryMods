using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
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
    public static class ExpensiveSpellstrike
    {
        public static void load(FeatName starlitSpanName)
        {
            var spellstrike = new TrueFeat(ModManager.RegisterFeatName("Expansive Spellstrike {icon:TwoActions}"), 2, "You've adapted a wider array of spells to work with your attacks. ", "Rather than needing to use a spell that has a spell attack roll for a Spellstrike, you can use a spell with a saving throw that can target a creature or that has an area of a burst, cone, or line (abiding by any other restrictions of Spellstrike). When you Cast a Spell that doesn't have a spell attack roll as part of a Spellstrike, it works in the following ways. \n" +
                "-If your Strike critically fails, the spell is lost with no effect.\n" +
                "-Creatures use their normal defenses against the spell, such as saving throws.\n" +
                "-If the spell lets you select a number of targets, it instead targets only the creature you attacked with your Strike.\n" +
                "-If the spell has an area, the area emits from the target. The spell affects the target and all creatures in the area as normal, but the Strike still targets only one creature.", new Trait[1]
             {
                Trait.Magus
             }).WithOnCreature(creature =>
             {
                 QEffect qfSpellstrike = new QEffect("Expansive Spellstrike {icon:TwoActions}", "You cast a saving-throw spell and deliver it through your weapon Strike.")
                 {
                     ProvideStrikeModifierAsPossibilities = (Func<QEffect, Item, IEnumerable<Possibility>>)((qfSpellstrike, weapon) =>
                     {
                         Creature self = qfSpellstrike.Owner;
                         if (self.Spellcasting == null)
                         {
                             return Array.Empty<Possibility>();
                         }

                         if (!weapon.HasTrait(Trait.Melee) && !creature.HasFeat(starlitSpanName))
                         {
                             return Array.Empty<Possibility>();
                         }
                         List<SubmenuPossibility> list = new List<SubmenuPossibility>();
                         foreach (SpellcastingSource source in self.Spellcasting.Sources)
                         {
                             list.Add(CreateSpellstrikeMenu(source, "Expansive Spellstrike", (CombatAction action) => CreateSpellstrike(action)));
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
                             //not sure why the de-compiled code is checking for all these negative action costs but i'm not gonna question its wisdom
                             if (spell.ActionCost != 1 && spell.ActionCost != 2 && spell.ActionCost != -3 && spell.ActionCost != -1 && spell.ActionCost != -4 && spell.ActionCost != -5)
                             {
                                 return null;
                             }

                             if (spell.HasTrait(Trait.Attack))
                             {
                                 return null;
                             }
                             if (spell.SavingThrow == null) //heuristic to get mostly eligeble spells
                             {
                                 return null;
                             }


                             Target target = ((spell.Target is DependsOnActionsSpentTarget dependsOnActionsSpentTarget) ? dependsOnActionsSpentTarget.IfTwoActions : spell.Target);
                   
                             CombatAction spellstrike = qfSpellstrike.Owner.CreateStrike(weapon);
                             spellstrike.Name = spell.Name;
                             spellstrike.Illustration = new SideBySideIllustration(spellstrike.Illustration, spell.Illustration);
                             spellstrike.Traits.AddRange(spell.Traits.Except(new Trait[5]
                             {
                        Trait.Ranged,
                        Trait.Prepared,
                        Trait.Melee, //stops yeeting animation from melee spells cast through a ranged weapon
                        Trait.Spontaneous,
                        Trait.Spell
                             }));
                             spellstrike.Traits.Add(Trait.Spellstrike);
                             spellstrike.Traits.Add(Trait.Basic);
                             spellstrike.ActionCost = 2;
                             CreatureTarget creatureTarget3 = (CreatureTarget)spellstrike.Target;
                             creatureTarget3.WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => a.HasEffect(QEffectId.SpellstrikeDischarged) ? Usability.NotUsable("You must first recharge your Spellstrike by spending an action or casting a focus spell.") : Usability.Usable);

                             spellstrike.StrikeModifiers.OnEachTarget = async delegate (Creature a, Creature d, CheckResult result)
                             {
                                 Steam.CollectAchievement("MAGUS");
                                 spell.ChosenTargets = ChosenTargets.CreateSingleTarget(d);
                                 spell.SpentActions = 2;
                                 if (result >= CheckResult.Success)
                                 {
                                     bool fizzled = false;
                                     QEffect qEffect = a.FindQEffect(QEffectId.Stupefied);
                                     if (qEffect != null)
                                     {
                                         (CheckResult, string) tuple = Checks.RollFlatCheck(5 + qEffect.Value);
                                         fizzled = tuple.Item1 < CheckResult.Success;
                                         if (fizzled)
                                         {
                                             a.Battle.Log(spell.Name + " from Spellstrike {Red}fizzled{/} due to stupefied: " + tuple.Item2);
                                         }
                                         else
                                         {
                                             a.Battle.Log("Spellstrike stupefied flat check {Green}passed{/}: " + tuple.Item2);
                                         }
                                     }

                                     if (!fizzled)
                                     {
                                         //temporarily save the spell information
                                         int spellActions = spell.ActionCost;
                                         spell.ActionCost = 0;
                                         var currentTile = a.Occupies;
                                         Target spellTarget = spell.Target;
                                         ChosenTargets spellChosen = spell.ChosenTargets;
                                         a.Occupies = d.Occupies; //the general idea is that the pc temporarily assumes the position of the spellstriked enemy and casts from there

                                         switch (spell.Target)
                                         {
                                            
                                             case CloseAreaTarget target:
                                                 await a.Battle.GameLoop.FullCast(spell);
                                                 a.Spellcasting!.RevertExpendingOfResources(spell);
                                                 spell.Target = CreatureTarget.Ranged(10); //need this so the spell can hit the creature while the PC is sharing its space
                                                 spell.ChosenTargets = spellstrike.ChosenTargets;
                                                 await spell.AllExecute();
                                                 a.Spellcasting!.RevertExpendingOfResources(spell);
                                                 break;
                                             case BurstAreaTarget target:
                                                 target.Range = 1;
                                                 await a.Battle.GameLoop.FullCast(spell);
                                                 a.Spellcasting!.RevertExpendingOfResources(spell);
                                                 break;
                                             case CreatureTarget target:
                                             case MultipleCreatureTargetsTarget targets:
                                                 spell.Target = CreatureTarget.Ranged(10); //need this so the spell can hit the creature while the PC is sharing its space
                                                 spell.ChosenTargets = spellstrike.ChosenTargets;
                                                 await spell.AllExecute();
                                                 a.Spellcasting!.RevertExpendingOfResources(spell);
                                                 break;
                                         }
                                         a.Occupies = currentTile; //return caster to original position
                                         //re-assign saved spell information
                                         spell.ActionCost = spellActions;
                                         spell.Target = spellTarget;
                                         spell.ChosenTargets = spellChosen;

                                     }
                                 }
                                 a.Spellcasting!.UseUpSpellcastingResources(spell);



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
                             spellstrike.Description = StrikeRules.CreateBasicStrikeDescription3(spellstrike.StrikeModifiers, null, prologueText: prologue, additionalSuccessText: "The success effect of " + spell.Name + " is inflicted upon the target.", additionalCriticalSuccessText: "Critical spell effect.", additionalFailureText: null, additionalAftertext: ("You can't use Spellstrike again until you recharge it by spending an action or casting a focus spell." + ((aftertext != null) ? (" " + aftertext) : "")));
                             return spellstrike;
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
             }
                 );
            ModManager.AddFeat(spellstrike);
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
