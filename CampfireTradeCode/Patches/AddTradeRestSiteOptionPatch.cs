using CampfireTrade.CampfireTradeCode.Config;
using CampfireTrade.CampfireTradeCode.RestSites;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace CampfireTrade.CampfireTradeCode.Patches;

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
internal static class AddTradeRestSiteOptionPatch
{
    private const string TradeOptionId = "TRADE";

    private static void Postfix(Player player, List<RestSiteOption> __result)
    {
        // Optional: only show in multiplayer.
        if (!CampfireTradeConfig.AllowSoloTradePopup && player.RunState.Players.Count <= 1)
        {
            MainFile.Logger.Info($"Skipping Trade for player {player.NetId}: solo run, solo debug popup disabled.");
            return;
        }

        // Prevent duplicates if another patch/hook also adds it.
        if (__result.Any(option => option.OptionId == TradeOptionId))
        {
            MainFile.Logger.Warn($"Trade already exists for player {player.NetId}; type=" +
                                 __result.First(option => option.OptionId == TradeOptionId).GetType().FullName);
            return;
        }

        __result.Add(new TradeRestSiteOption(player));
        MainFile.Logger.Info($"Added Trade for player {player.NetId}; players={player.RunState.Players.Count}; " +
                            $"options={string.Join(", ", __result.Select(option => option.OptionId))}");
    }
}
