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
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using System.Reflection;
using AutoMapper;

namespace DawnsburryMods.Starlit_Span
{
    public static class ExpensiveSpellstrike
    {
        public static void load()
        {
            var spellstrike = new TrueFeat(ModManager.RegisterFeatName("Expensive Spellstrike"), 2, "You spellstrike for a lot of cash", "da rulez", new Trait[1]
             {
                Trait.Magus
             }).WithOnCreature(creature =>
             {
                 QEffect qfSpellstrike = new QEffect("Spellexpensivestrike {icon:TwoActions}", "You cast a spell and deliver it through your weapon Strike.");
                 qfSpellstrike.ProvideStrikeModifierAsPossibility = delegate (Item weapon)
                 {
                     Item weapon2 = weapon;

                     Creature self3 = qfSpellstrike.Owner;
                     return CreateSpellcastingMenu("Expensive Spellstrike", new Func<CombatAction, CombatAction>(CreateSpellstrike!));
                     SubmenuPossibility CreateSpellcastingMenu(string caption, Func<CombatAction, CombatAction?> spellTransformation)
                     {
                         Func<CombatAction, CombatAction?> spellTransformation2 = spellTransformation;
                         SubmenuPossibility castASpell = new SubmenuPossibility(new SideBySideIllustration(weapon2.Illustration, IllustrationName.CastASpell), caption)
                         {
                             Subsections = new List<PossibilitySection>()
                         };
                         SpellcastingSource sourceByOrigin = self3.Spellcasting!.GetSourceByOrigin(Trait.Magus)!;
                         if (self3.Spellcasting!.FocusPoints > 0 && sourceByOrigin.FocusSpells.Count > 0)
                         {
                             AddSpellSubmenu("Focus spells " + string.Join("", Enumerable.Repeat("{icon:SpontaneousSpellSlot}", self3.Spellcasting!.FocusPoints)), sourceByOrigin.FocusSpells);
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
                             foreach (CombatAction spell3 in spells)
                             {
                                 CombatAction combatAction3 = spellTransformation2(spell3)!;
                                 if (combatAction3 != null)
                                 {
                                     string name = combatAction3.Name;
                                     combatAction3.Name = "Expensive Spellstrike (" + combatAction3?.ToString() + ")";
                                     possibilitySection.Possibilities.Add(new ActionPossibility(combatAction3!, PossibilitySize.Half)
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
                         CombatAction spell2 = spell;


                         if (spell2.SubspellVariants != null || spell2.Variants != null)
                         {
                             spell2.ActionCost = 2;
                             spell2.SpentActions = 2;
                         }

                         if (spell2.ActionCost != 1 && spell2.ActionCost != 2)
                         {
                             return null;
                         }

                         if (spell2.HasTrait(Trait.Attack))
                         {
                             return null;
                         }
                         if (spell2.SavingThrow == null) //heuristic to get mostly eligeble spells
                         {
                             return null;
                         }



                         CombatAction combatAction4 = qfSpellstrike.Owner.CreateStrike(weapon2);
                         combatAction4.Name = spell2.Name;
                         combatAction4.Illustration = new SideBySideIllustration(combatAction4.Illustration, spell2.Illustration);
                         combatAction4.Traits.AddRange(spell2.Traits.Except(new Trait[5]
                         {
                         Trait.Prepared,
                         Trait.Spontaneous,
                         Trait.Spell,
                         Trait.Melee, //stops yeeting animation from melee spells cast through a ranged weapon
                         Trait.Ranged
                         }));
                         combatAction4.Traits.Add(Trait.Spellstrike);
                         combatAction4.Traits.Add(Trait.Basic);
                         combatAction4.ActionCost = 2;
                         ((CreatureTarget)combatAction4.Target).WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => a.HasEffect(QEffectId.SpellstrikeDischarged) ? Usability.NotUsable("You must first recharge your Spellstrike by spending an action or casting a focus spell.") : Usability.Usable);
                         combatAction4.StrikeModifiers.OnEachTarget = async delegate (Creature a, Creature d, CheckResult result)
                         {
                             a.Spellcasting!.UseUpSpellcastingResources(spell2);
                             if (result >= CheckResult.Success)
                             {
                                 if (combatAction4.HasTrait(Trait.Ranged))
                                 {
                                     spell2.ActionCost = 0;

                                     switch (spell2.Target)
                                     {
                                         case CreatureTarget target:
                                             spell2.Target = combatAction4.Target;
                                             spell2.ChosenTargets = combatAction4.ChosenTargets;
                                             await spell2.AllExecute();
                                             break;
                                         case CloseAreaTarget target:
                                             var currentTile = a.Occupies;
                                             a.Occupies = d.Occupies;
                                             await a.Battle.GameLoop.FullCast(spell2);
                                             await spell2.AllExecute();
                                             a.Occupies = currentTile;
                                             break;
                                         case BurstAreaTarget target:

                                             break;

                                     }


                                 }
                             }

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
                                     QEffect qfDischarge2 = qfDischarge;
                                     return (ActionPossibility)new CombatAction(qfDischarge2.Owner, IllustrationName.Good, "Recharge Spellstrike", new Trait[2]
                                     {
                                     Trait.Concentrate,
                                     Trait.Basic
                                     }, "Recharge your Spellstrike so that you can use it again." + (qfDischarge2.Owner.HasEffect(QEffectId.MagussConcentration) ? " {Blue}You gain a +1 circumstance bonus to your next attack until the end of your next turn.{/Blue}" : ""), Target.Self()).WithActionCost(1).WithSoundEffect(SfxName.AuraExpansion).WithEffectOnSelf(async delegate (Creature self2)
                                     {
                                         qfDischarge2.ExpiresAt = ExpirationCondition.Immediately;
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
                         combatAction4.Description = StrikeRules.CreateBasicStrikeDescription(combatAction4.StrikeModifiers, null, "The success effect of " + spell2.Name + " is inflicted upon the target.", "Critical spell effect.", null, "You can't use Spellstrike again until you recharge it by spending an action or casting a focus spell.");
                         return combatAction4;
                     }
                 };
                 creature.AddQEffect(qfSpellstrike);
             }
                 );
            ModManager.AddFeat(spellstrike);
        }
    }
}
