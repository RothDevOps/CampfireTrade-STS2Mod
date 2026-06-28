using CampfireTrade.CampfireTradeCode.Trading;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace CampfireTrade.CampfireTradeCode.Screens;

public class NTradeRequestPopup
{
    public static async Task<bool> ShowRequestToTarget(TradeOffer offer)
    {
        NGenericPopup? popup = NGenericPopup.Create();

        if (popup == null || NModalContainer.Instance == null)
        {
            return false;
        }
        
        NModalContainer.Instance.Add(popup);

        LocString header = new("rest_site_ui", "OPTION_TRADE.requestHeader");
        
        LocString body = new("rest_site_ui", "OPTION_TRADE.requestBody");
        body.Add("Initiator", GetPlayerName(offer.Initiator));
        body.Add("Card", GetCardName(offer.RequestedCard));
        //body.Add("Cost", offer.GoldCost);

        LocString decline = new("rest_site_ui", "OPTION_TRADE.decline");
        LocString accept = new("rest_site_ui", "OPTION_TRADE.accept");
        
        return await popup.WaitForConfirmation(body, header, decline, accept);
    }

    public static async Task ShowDeclinedToInitiator(TradeOffer offer)
    {
        NGenericPopup? popup = NGenericPopup.Create();

        if (popup == null || NModalContainer.Instance == null)
        {
            return;
        }

        NModalContainer.Instance.Add(popup);

        LocString header = new("rest_site_ui", "OPTION_TRADE.declinedHeader");

        LocString body = new("rest_site_ui", "OPTION_TRADE.declinedBody");
        body.Add("Target", GetPlayerName(offer.Target));
        body.Add("Card", GetCardName(offer.RequestedCard));

        LocString ok = new("rest_site_ui", "OPTION_TRADE.ok");

        await popup.WaitForConfirmation(
            body,
            header,
            noButton: null,
            yesButton: ok);
    }

    private static string GetPlayerName(Player player)
    {
        return PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
    }

    private static string GetCardName(CardModel card)
    {
        return card.Title;
    }
}