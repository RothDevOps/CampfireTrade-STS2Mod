using CampfireTrade.CampfireTradeCode.RestSites;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace CampfireTrade.CampfireTradeCode.Patches;

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
internal static class AddTradeRestSiteOptionPatch
{
    private const string TradeOptionId = "TRADE";
    private const bool DebugAllowSoloTradePopup = true;

    private static void Postfix(Player player, List<RestSiteOption> __result)
    {
        // Optional: only show in multiplayer.
        if (!DebugAllowSoloTradePopup && player.RunState.Players.Count <= 1)
            return;

        // Prevent duplicates if another patch/hook also adds it.
        if (__result.Any(option => option.OptionId == TradeOptionId))
            return;

        __result.Add(new TradeRestSiteOption(player));
    }
}