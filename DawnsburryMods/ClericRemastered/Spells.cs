using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Description;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace DawnsburryMods.ClericRemastered
{
    public static class adjustSpells
    {
        public static void addSpells()
        {
            bool isBless = true;
            IllustrationName illustration = IllustrationName.Bless;
            ModManager.ReplaceExistingSpell(SpellId.Bless, 1, (spellcaster, level, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(illustration, isBless ? "Bless" : "Bane", new Trait[4]
{
    Trait.Enchantment,
    Trait.Mental,
    Trait.Divine,
    Trait.Occult
}, isBless ? "Blessings from beyond help your companions strike true." : "You fill the minds of your enemies with doubt.", isBless ? "For the rest of the encounter, you and your allies gain a +1 status bonus to attack rolls while within a 5-foot-radius emanation around you.\n\nOnce per turn, starting the turn after you cast bless, you can use a single action to increase the emanation's radius by 5 feet." : "You create a 5-foot-radius emanation around you. Enemies within it must succeed at a Will save or take a –1 status penalty to attack rolls as long as they are in the area.\n\nAn enemy who failed and then leaves the area and reenters later is automatically affected again. An enemy who enters the area for the first time rolls a Will save as they enter.\n\nOnce per turn, starting the turn after you cast bane, you can use a single action to increase the emanation's radius by 5 feet and force all not yet afflicted enemies in the area to attempt another saving throw.", Target.Self(), level, null).WithSoundEffect(isBless ? SfxName.Bless : SfxName.Fear).WithEffectOnSelf(async delegate (CombatAction action, Creature self)
{
    CombatAction action2 = action;
    AuraAnimation auraAnimation = self.AnimationData.AddAuraAnimation(isBless ? IllustrationName.BlessCircle : IllustrationName.BaneCircle, 3f);
    QEffect qEffect = new QEffect(isBless ? "Bless" : "Bane", "[this condition has no description]", ExpirationCondition.Never, self, (Illustration)IllustrationName.None)
    {
        WhenExpires = delegate
        {
            auraAnimation.MoveTo(0f);
        },
        Tag = (3, true),
        StartOfYourTurn = async delegate (QEffect qfBless, Creature _)
        {
            qfBless.Tag = ((((int, bool))qfBless.Tag).Item1, false);
        },
        ProvideContextualAction = delegate (QEffect qfBless)
        {
            QEffect qfBless3 = qfBless;
            (int, bool) tag = ((int, bool))qfBless3.Tag;
            return (!tag.Item2) ? new ActionPossibility(new CombatAction(qfBless3.Owner, illustration, isBless ? "Increase Bless radius" : "Increase Bane radius", new Trait[1] { Trait.Concentrate }, "Increase the radius of the " + (isBless ? "bless" : "bane") + " emanation by 5 feet.", Target.Self()).WithEffectOnSelf(delegate
            {
                int newEmanationSize = tag.Item1 + 2;
                qfBless3.Tag = (newEmanationSize, true);
                auraAnimation.MoveTo(newEmanationSize);
                if (!isBless)
                {
                    foreach (Creature item in qfBless3.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless3.Owner) <= newEmanationSize && cr.EnemyOf(qfBless3.Owner)))
                    {
                        item.RemoveAllQEffects((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBless3);
                    }
                }
            })).WithPossibilityGroup("Maintain an activity") : null;
        }
    };
    if (isBless)
    {
        auraAnimation.Color = Color.Yellow;
        qEffect.StateCheck = delegate (QEffect qfBless)
        {
            QEffect qfBless2 = qfBless;
            int emanationSize2 = (((int, bool))qfBless2.Tag).Item1;
            foreach (Creature item2 in qfBless2.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless2.Owner) <= emanationSize2 && cr.FriendOf(qfBless2.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
            {
                item2.AddQEffect(new QEffect("Bless", "You gain a +1 status bonus to attack rolls.", ExpirationCondition.Ephemeral, qfBless2.Owner, (Illustration)IllustrationName.Bless)
                {
                    CountsAsABuff = true,
                    BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(1, BonusType.Status, "bless") : null
                });
            }
        };
    }
    else
    {
        qEffect.StateCheckWithVisibleChanges = async delegate (QEffect qfBane)
        {
            QEffect qfBane2 = qfBane;
            int emanationSize = (((int, bool))qfBane2.Tag).Item1;
            foreach (Creature item3 in qfBane2.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBane2.Owner) <= emanationSize && cr.EnemyOf(qfBane2.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
            {
                if (!item3.QEffects.Any((QEffect qf) => qf.ImmuneToTrait == Trait.Mental))
                {
                    if (item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.FailedAgainstBane && qf.Tag == qfBane2))
                    {
                        item3.AddQEffect(new QEffect("Bane", "You take a -1 status penalty to attack rolls.", ExpirationCondition.Ephemeral, qfBane2.Owner, (Illustration)IllustrationName.Bane)
                        {
                            Key = "BanePenalty",
                            BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(-1, BonusType.Status, "bane") : null
                        });
                    }
                    else if (!item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBane2))
                    {
                        CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(item3, action2, Defense.Will);
                        item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                        {
                            Id = QEffectId.RolledAgainstBane,
                            Tag = qfBane2
                        });
                        if (checkResult <= CheckResult.Failure)
                        {
                            item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                            {
                                Id = QEffectId.FailedAgainstBane,
                                Tag = qfBane2
                            });
                        }
                    }
                }
            }
        };
    }

    self.AddQEffect(qEffect);
});
            });




            isBless = false;
            illustration = IllustrationName.Bane;
            ModManager.ReplaceExistingSpell(SpellId.Bane, 1, (spellcaster, level, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(illustration, isBless ? "Bless" : "Bane", new Trait[4]
{
    Trait.Enchantment,
    Trait.Mental,
    Trait.Divine,
    Trait.Occult
}, isBless ? "Blessings from beyond help your companions strike true." : "You fill the minds of your enemies with doubt.", isBless ? "For the rest of the encounter, you and your allies gain a +1 status bonus to attack rolls while within a 5-foot-radius emanation around you.\n\nOnce per turn, starting the turn after you cast bless, you can use a single action to increase the emanation's radius by 5 feet." : "You create a 5-foot-radius emanation around you. Enemies within it must succeed at a Will save or take a –1 status penalty to attack rolls as long as they are in the area.\n\nAn enemy who failed and then leaves the area and reenters later is automatically affected again. An enemy who enters the area for the first time rolls a Will save as they enter.\n\nOnce per turn, starting the turn after you cast bane, you can use a single action to increase the emanation's radius by 5 feet and force all not yet afflicted enemies in the area to attempt another saving throw.", Target.Self(), level, null).WithSoundEffect(isBless ? SfxName.Bless : SfxName.Fear).WithEffectOnSelf(async delegate (CombatAction action, Creature self)
{
    CombatAction action2 = action;
    AuraAnimation auraAnimation = self.AnimationData.AddAuraAnimation(isBless ? IllustrationName.BlessCircle : IllustrationName.BaneCircle, 3f);
    QEffect qEffect = new QEffect(isBless ? "Bless" : "Bane", "[this condition has no description]", ExpirationCondition.Never, self, (Illustration)IllustrationName.None)
    {
        WhenExpires = delegate
        {
            auraAnimation.MoveTo(0f);
        },
        Tag = (3, true),
        StartOfYourTurn = async delegate (QEffect qfBless, Creature _)
        {
            qfBless.Tag = ((((int, bool))qfBless.Tag).Item1, false);
        },
        ProvideContextualAction = delegate (QEffect qfBless)
        {
            QEffect qfBless3 = qfBless;
            (int, bool) tag = ((int, bool))qfBless3.Tag;
            return (!tag.Item2) ? new ActionPossibility(new CombatAction(qfBless3.Owner, illustration, isBless ? "Increase Bless radius" : "Increase Bane radius", new Trait[1] { Trait.Concentrate }, "Increase the radius of the " + (isBless ? "bless" : "bane") + " emanation by 5 feet.", Target.Self()).WithEffectOnSelf(delegate
            {
                int newEmanationSize = tag.Item1 + 2;
                qfBless3.Tag = (newEmanationSize, true);
                auraAnimation.MoveTo(newEmanationSize);
                if (!isBless)
                {
                    foreach (Creature item in qfBless3.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless3.Owner) <= newEmanationSize && cr.EnemyOf(qfBless3.Owner)))
                    {
                        item.RemoveAllQEffects((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBless3);
                    }
                }
            })).WithPossibilityGroup("Maintain an activity") : null;
        }
    };
    if (isBless)
    {
        auraAnimation.Color = Color.Yellow;
        qEffect.StateCheck = delegate (QEffect qfBless)
        {
            QEffect qfBless2 = qfBless;
            int emanationSize2 = (((int, bool))qfBless2.Tag).Item1;
            foreach (Creature item2 in qfBless2.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless2.Owner) <= emanationSize2 && cr.FriendOf(qfBless2.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
            {
                item2.AddQEffect(new QEffect("Bless", "You gain a +1 status bonus to attack rolls.", ExpirationCondition.Ephemeral, qfBless2.Owner, (Illustration)IllustrationName.Bless)
                {
                    CountsAsABuff = true,
                    BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(1, BonusType.Status, "bless") : null
                });
            }
        };
    }
    else
    {
        qEffect.StateCheckWithVisibleChanges = async delegate (QEffect qfBane)
        {
            QEffect qfBane2 = qfBane;
            int emanationSize = (((int, bool))qfBane2.Tag).Item1;
            foreach (Creature item3 in qfBane2.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBane2.Owner) <= emanationSize && cr.EnemyOf(qfBane2.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
            {
                if (!item3.QEffects.Any((QEffect qf) => qf.ImmuneToTrait == Trait.Mental))
                {
                    if (item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.FailedAgainstBane && qf.Tag == qfBane2))
                    {
                        item3.AddQEffect(new QEffect("Bane", "You take a -1 status penalty to attack rolls.", ExpirationCondition.Ephemeral, qfBane2.Owner, (Illustration)IllustrationName.Bane)
                        {
                            Key = "BanePenalty",
                            BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(-1, BonusType.Status, "bane") : null
                        });
                    }
                    else if (!item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBane2))
                    {
                        CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(item3, action2, Defense.Will);
                        item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                        {
                            Id = QEffectId.RolledAgainstBane,
                            Tag = qfBane2
                        });
                        if (checkResult <= CheckResult.Failure)
                        {
                            item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                            {
                                Id = QEffectId.FailedAgainstBane,
                                Tag = qfBane2
                            });
                        }
                    }
                }
            }
        };
    }

    self.AddQEffect(qEffect);
});
            });







        }
    }
}

