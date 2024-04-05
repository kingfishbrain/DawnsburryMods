using Dawnsbury.Core.CharacterBuilder.Feats;

namespace GoblinAncestryC_.GoblinAncestry;

public class GoblinAncestryFeat : TrueFeat
{
    public GoblinAncestryFeat(string name, string flavorText, string rulesText)
        : base(FeatName.CustomFeat, 1, flavorText, rulesText, new[]
        {
            GoblinAncestryLoader.GoblinTrait,
            // The following line is not needed -- because we registered the Kobold trait as an ancestry trait, the Ancestry trait is added automatically.
            // Trait.Ancestry
        })
    {
        WithCustomName(name);
        // The following line is not needed -- because we registered the Kobold trait as an ancestry trait, the prerequisite is added automatically.
        // this.WithPrerequisite(sheet => sheet.Ancestries.Contains(KoboldAncestryLoader.KoboldTrait), "You must be a Kobold.");
    }
}