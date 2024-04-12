using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Auxiliary;

namespace DawnsburryMods.ClericRemastered
{
    public static class ClericLoader
    {
        [DawnsburyDaysModMainMethod]
        public static void loadMod()
        {
            AllFeats.All.RemoveAll(feat => feat.FeatName == FeatName.HealingFont);
            AllFeats.All.RemoveAll(feat => feat.FeatName == FeatName.HarmfulFont);
            LoadFonts().ForEach(feat => ModManager.AddFeat(feat)); //yooo it works
        }


        public static int Fonts(int level) =>
            level switch
            {
                <= 4 => 4,
                (>= 5) and (<= 14) => 5,
                >= 15 => 6,
            };
 

        public static IEnumerable<Feat> LoadFonts()
        {
            yield return new Feat(FeatName.HealingFont, "Through your deity's blessing, you gain additional spells that channel the life force called positive energy.", "You gain additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}heal{/i} spells in these slots, and the number of these slots is equal to 1 plus your Charisma modifier.", new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, (Action<CalculatedCharacterSheetValues>)delegate (CalculatedCharacterSheetValues values)
                {
                    int fonts = Fonts(values.CurrentLevel);
                    for (int i = 0; i < fonts; i++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Healing font", AllSpells.CreateModernSpellTemplate(SpellId.Heal, Trait.Cleric), "HealingFont:" + i));
                    }
                });
            }).WithIllustration(IllustrationName.Heal);
            yield return new Feat(FeatName.HarmfulFont, "Through your deity's blessing, you gain additional spells that channel the counterforce to life, the so-called negative energy.", "You gain additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}harm{/i} spells in these slots, and the number of these slots is equal to 1 plus your Charisma modifier.", new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, (Action<CalculatedCharacterSheetValues>)delegate (CalculatedCharacterSheetValues values)
                {
                    int fonts = Fonts(values.CurrentLevel);
                    for (int i = 0; i < fonts; i++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Harmful font", AllSpells.CreateModernSpellTemplate(SpellId.Harm, Trait.Cleric), "HarmfulFont:" + i));
                    }
                });
            }).WithIllustration(IllustrationName.Harm);
        }

    }



}
