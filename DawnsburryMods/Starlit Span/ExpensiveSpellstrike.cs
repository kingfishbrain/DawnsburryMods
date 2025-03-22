using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
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
                 QEffect qfSpellstrike = new QEffect("Expansive Spellstrike {icon:TwoActions}", "You cast a spell and deliver it through your weapon Strike.");
                 qfSpellstrike.ProvideStrikeModifierAsPossibility = delegate (Item weapon)
                 {
                     if (!weapon.HasTrait(Trait.Melee) && !creature.HasFeat(starlitSpanName))
                     {
                         return null;
                     }

                     Creature self = qfSpellstrike.Owner;
                     return CreateSpellcastingMenu("Expansive Spellstrike", new Func<CombatAction, CombatAction>(CreateSpellstrike!));
                     SubmenuPossibility CreateSpellcastingMenu(string caption, Func<CombatAction, CombatAction?> spellTransformation)
                     {
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
                                 CombatAction spellstrike = spellTransformation(spell)!;
                                 if (spellstrike != null)
                                 {
                                     string name = spellstrike.Name;
                                     spellstrike.Name = "Expansive Spellstrike (" + spellstrike?.ToString() + ")";
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

                         if (spell.Variants != null)
                             return null;
                         if (spell.SubspellVariants != null)
                             return null;


                         if (spell.ActionCost != 1 && spell.ActionCost != 2)
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
                             if (result >= CheckResult.Success)
                             {
                                 int spellActions = spell.ActionCost;
                                 spell.ActionCost = 0;
                                 var currentTile = a.Occupies;


                                 switch (spell.Target)
                                 {
                                     case CreatureTarget target:
                                         spell.Target = spellstrike.Target;
                                         spell.ChosenTargets = spellstrike.ChosenTargets;
                                         await spell.AllExecute();
                                         a.Spellcasting!.RevertExpendingOfResources(spell);
                                         break;
                                     case CloseAreaTarget target:
                                         a.Occupies = d.Occupies;
                                         await a.Battle.GameLoop.FullCast(spell);
                                         a.Spellcasting!.RevertExpendingOfResources(spell);
                                         spell.Target = spellstrike.Target;
                                         spell.ChosenTargets = spellstrike.ChosenTargets;
                                         await spell.AllExecute();
                                         a.Spellcasting!.RevertExpendingOfResources(spell);
                                         a.Occupies = currentTile;
                                         break;
                                     case BurstAreaTarget target:
                                         target.Range = 1;
                                         a.Occupies = d.Occupies;
                                         await a.Battle.GameLoop.FullCast(spell);
                                         a.Spellcasting!.RevertExpendingOfResources(spell);
                                         a.Occupies = currentTile;
                                         break;

                                 }
                                 spell.ActionCost = spellActions;

                             }
                             a.Spellcasting!.UseUpSpellcastingResources(spell);

                             a.AddQEffect(new QEffect
                             {
                                 Id = QEffectId.SpellstrikeDischarged,
                                 AfterYouTakeAction = delegate (QEffect qfDischarge, CombatAction action)
                                 {
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
                                     }, "Recharge your Spellstrike so that you can use it again." + (qfDischarge.Owner.HasEffect(QEffectId.MagussConcentration) ? " {Blue}You gain a +1 circumstance bonus to your next attack until the end of your next turn.{/Blue}" : ""), Target.Self()).WithActionCost(1).WithSoundEffect(SfxName.AuraExpansion).WithEffectOnSelf(async delegate (Creature self2)
                                     {
                                         qfDischarge.ExpiresAt = ExpirationCondition.Immediately;
                                         if (self2.HasEffect(QEffectId.MagussConcentration))
                                         {
                                             self2.AddQEffect(new QEffect("Magus's Concentration", "You have +1 to your next attack roll.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self2, (Illustration)IllustrationName.Good)
                                             {
                                                 CannotExpireThisTurn = true,
                                                 BonusToAttackRolls = (QEffect qf, CombatAction ca, Creature? df) => (!ca.HasTrait(Trait.Attack)) ? null : new Bonus(1, BonusType.Circumstance, "Magus's Concentration"),
                                                 AfterYouTakeAction = delegate (QEffect qf, CombatAction ca)
                                                 {
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
             }
                 );
            ModManager.AddFeat(spellstrike);
        }
    }
}
