using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;


namespace GoblinAncestry.GoblinAncestry
{
    class Heritages
    {
        public static IEnumerable<Feat> CreateGoblinHeritages()
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
                    creature.AddQEffect(new QEffect("Snow Goblin",
                            "You have cold resistance " +
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
                   creature.AddQEffect(new QEffect("Charhide Goblin",
                           "You have fire resistance " +
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
                       return qfIncoming;
                   };
               }
        );

            yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                    "Your family's teeth are formidable weapons.",
                    "You gain a jaws unarmed attack that deals 1d6 piercing damage. Your jaws have the finesse and unarmed traits.")
                .WithCustomName("Razortooth Goblin {icon:Action}")
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

            yield return new HeritageSelectionFeat(FeatName.CustomFeat,
            "You have a powerful tail, likely because you descend from a community of monkey goblins.",
            "You reduce the number of free hands required to Trip by one.")
        .WithCustomName("Tailed Goblin")
        .WithPermanentQEffect("You reduce the number of free hands required to Trip by one.", qfTailed =>
        {
            qfTailed.ProvideActionIntoPossibilitySection = (effect, section) =>
            {
                if (section.PossibilitySectionId != PossibilitySectionId.AttackManeuvers) return null;
                if (effect.Owner.HasFreeHand)
                    return null; // do nothing -- will be handled by the normal Trip action
                var customTrip = Possibilities.CreateTrip(effect.Owner);
                var customTripTarget = customTrip.Target as CreatureTarget;
                customTripTarget.CreatureTargetingRequirements.Clear();
                customTripTarget.CreatureTargetingRequirements.Add(new AdjacencyCreatureTargetingRequirement());
                customTripTarget.CreatureTargetingRequirements.Add(new EnemyCreatureTargetingRequirement());
                customTripTarget.CreatureTargetingRequirements.Add(new LegacyCreatureTargetingRequirement((a, d) =>
                     !d.HasEffect(QEffectId.Prone)
                         ? Usability.Usable
                         : Usability.CommonReasons.TargetIsAlreadyProne));
                return new ActionPossibility(customTrip);
            };
        });


        }

    }
}
