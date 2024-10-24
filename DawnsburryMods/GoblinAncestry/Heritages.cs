using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;


namespace GoblinAncestry.GoblinAncestry
{
    class Heritages
    {
        public static Feat TailedGoblin() { 
         return new HeritageSelectionFeat(FeatName.CustomFeat,
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
