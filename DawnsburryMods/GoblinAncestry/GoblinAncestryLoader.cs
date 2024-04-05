using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace GoblinAncestry.GoblinAncestry;

public static class GoblinAncestryLoader
{


    public static Trait GoblinTrait;



    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {

        GoblinTrait = ModManager.RegisterTrait(
            "Goblin",
            new TraitProperties("Goblin", true,
            "A creature with this trait can be one of several kinds of creature, including goblins, hobgoblins, and bugbears. " +
            "Goblins tend to have darkvision. An ability with this trait can be used or chosen only by goblins. " +
            "A weapon with this trait is created and used by goblins.")
            {
                IsAncestryTrait = true
            });



        Weapons.RegisterWeapons();
        AddFeats(AncestryFeats.CreateGoblinAncestryFeats(GoblinTrait));

        ModManager.AddFeat(new AncestrySelectionFeat(
                FeatName.CustomFeat,
                "Goblins are a short, scrappy, energetic people who have spent millennia maligned and feared.",
                new List<Trait> { Trait.Humanoid, GoblinTrait },
                6,
                5,
                new List<AbilityBoost>()
                {
                    new EnforcedAbilityBoost(Ability.Dexterity),
                    new EnforcedAbilityBoost(Ability.Charisma),
                    new FreeAbilityBoost()
                },
                Heritages.CreateGoblinHeritages().ToList())
            .WithAbilityFlaw(Ability.Wisdom)
            .WithCustomName("Goblin")
        );
    }

    private static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (var feat in feats)
        {
            ModManager.AddFeat(feat);
        }
    }

}