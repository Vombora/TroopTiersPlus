using HarmonyLib;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;

namespace TroopTiersPlus
{
    [HarmonyPatch(typeof(DefaultCharacterStatsModel), "GetTier")]
    internal class TroopTierPatch
    {
        static bool Prefix(CharacterObject character, ref int __result)
        {
            if (character.IsHero)
            {
                __result = 0;
            }
            __result = MathF.Min(MathF.Max(MathF.Ceiling(((float)character.Level - 5f) / 5f), 0), 20);

            return false;
        }
    }

    [HarmonyPatch(typeof(CampaignUIHelper), "GetCharacterTierData")]
    internal class TierIconPatch
    {
        static bool Prefix(ref StringItemWithHintVM __result, CharacterObject character, bool isBig = false)
        {
            int tier = character.Tier;
            if (tier <= 0)
            {
                __result = new StringItemWithHintVM("", TextObject.Empty);
            }
            else
            {
                string str = isBig ? (tier.ToString() + "_big") : tier.ToString();
                GameTexts.SetVariable("TIER_LEVEL", tier);
                TextObject hint = new TextObject("{=!}" + GameTexts.FindText("str_party_troop_tier", null).ToString(), null);


                if (tier <= 7) //keeping existing original icons till Tier 7
                {
                    string text = "General\\TroopTierIcons\\icon_tier_" + str;

                    __result = new StringItemWithHintVM(text, hint);
                }
                else //repeating Tier 7 icon
                {
                    string text = "General\\TroopTierIcons\\icon_tier_" + 7;
                    __result = new StringItemWithHintVM(text, hint);
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DefaultPartyWageModel), "GetCharacterWage")]
    internal class CharacterWagePatch
    {
        static bool Prefix(ref int __result, CharacterObject character)
        {
            int num;
            int tier = character.Tier;
            switch (tier)
            {
                case 0:
                    num = 1;
                    break;
                case 1:
                    num = 2;
                    break;
                case 2:
                    num = 3;
                    break;
                case 3:
                    num = 5;
                    break;
                case 4:
                    num = 8;
                    break;
                case 5:
                    num = 12;
                    break;
                case 6:
                    num = 17;
                    break;
                default:
                    num = (tier * (tier - 2) - 13);
                    break;
            }
            if (character.Occupation == Occupation.Mercenary)
            {
                num = (int)((float)num * 1.5f);
            }
            __result = num;

            return false;
        }
    }

    [HarmonyPatch(typeof(DefaultPartyWageModel), "GetTroopRecruitmentCost")]
    internal class CharacterHireCostPatch
    {
        static bool Prefix(ref int __result, CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            //int num = 10 * MathF.Round((float)troop.Level * MathF.Pow((float)troop.Level, 0.65f) * 0.2f);
            int num = troop.Level switch
            {
                <= 1 => 10,
                <= 6 => 20,
                <= 11 => 50,
                <= 16 => 100,
                <= 21 => 200,
                <= 26 => 400,
                <= 31 => 600,
                <= 36 => 1000,
                _ => (troop.Level - 26) * 100
            };

            if (troop.Equipment.Horse.Item != null && !withoutItemCost)
            {
                if (troop.Level < 26)
                {
                    num += 150;
                }
                else
                {
                    num += 500;
                }
            }
            bool flag = troop.Occupation == Occupation.Mercenary || troop.Occupation == Occupation.Gangster || troop.Occupation == Occupation.CaravanGuard;
            if (flag)
            {
                num = MathF.Round((float)num * 2f);
            }
            if (buyerHero != null)
            {
                ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
                if (troop.Tier >= 2 && buyerHero.GetPerkValue(DefaultPerks.Throwing.HeadHunter))
                {
                    explainedNumber.AddFactor(DefaultPerks.Throwing.HeadHunter.SecondaryBonus, null);
                }
                if (troop.IsInfantry)
                {
                    if (buyerHero.GetPerkValue(DefaultPerks.OneHanded.ChinkInTheArmor))
                    {
                        explainedNumber.AddFactor(DefaultPerks.OneHanded.ChinkInTheArmor.SecondaryBonus, null);
                    }
                    if (buyerHero.GetPerkValue(DefaultPerks.TwoHanded.ShowOfStrength))
                    {
                        explainedNumber.AddFactor(DefaultPerks.TwoHanded.ShowOfStrength.SecondaryBonus, null);
                    }
                    if (buyerHero.GetPerkValue(DefaultPerks.Polearm.HardyFrontline))
                    {
                        explainedNumber.AddFactor(DefaultPerks.Polearm.HardyFrontline.SecondaryBonus, null);
                    }
                    if (buyerHero.Culture.HasFeat(DefaultCulturalFeats.SturgianRecruitUpgradeFeat))
                    {
                        explainedNumber.AddFactor(DefaultCulturalFeats.SturgianRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture", null));
                    }
                }
                else if (troop.IsRanged)
                {
                    if (buyerHero.GetPerkValue(DefaultPerks.Bow.RenownedArcher))
                    {
                        explainedNumber.AddFactor(DefaultPerks.Bow.RenownedArcher.SecondaryBonus, null);
                    }
                    if (buyerHero.GetPerkValue(DefaultPerks.Crossbow.Piercer))
                    {
                        explainedNumber.AddFactor(DefaultPerks.Crossbow.Piercer.SecondaryBonus, null);
                    }
                }
                if (troop.IsMounted && buyerHero.Culture.HasFeat(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
                {
                    explainedNumber.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture", null));
                }
                if (buyerHero.IsPartyLeader && buyerHero.GetPerkValue(DefaultPerks.Steward.Frugal))
                {
                    explainedNumber.AddFactor(DefaultPerks.Steward.Frugal.SecondaryBonus, null);
                }
                if (flag)
                {
                    if (buyerHero.GetPerkValue(DefaultPerks.Trade.SwordForBarter))
                    {
                        explainedNumber.AddFactor(DefaultPerks.Trade.SwordForBarter.PrimaryBonus, null);
                    }
                    if (buyerHero.GetPerkValue(DefaultPerks.Charm.SlickNegotiator))
                    {
                        explainedNumber.AddFactor(DefaultPerks.Charm.SlickNegotiator.PrimaryBonus, null);
                    }
                }
                num = MathF.Max(1, MathF.Round((float)num * explainedNumber.ResultNumber));
            }
            __result = num;

            return false;
        }
    }
}
