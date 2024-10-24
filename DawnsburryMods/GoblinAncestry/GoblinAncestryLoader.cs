using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace GoblinAncestry.GoblinAncestry;

public static class GoblinAncestryLoader
{




    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        
        Weapons.RegisterWeapons();
        AddFeats(AncestryFeats.CreateGoblinAncestryFeats());
        Feat goblin = (AllFeats.All.Find(feat => feat.FeatName == FeatName.Goblin))!;
        var tailedGoblin = Heritages.TailedGoblin();
        ModManager.AddFeat(tailedGoblin);
        goblin.Subfeats!.Add(tailedGoblin);
    }

    private static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (var feat in feats)
        {
            ModManager.AddFeat(feat);
        }
    }

}