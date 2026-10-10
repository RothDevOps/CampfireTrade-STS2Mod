using CampfireTrade.CampfireTradeCode.Trading;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
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
        
        Control? cardPreviewRoot = CreateRequestedCardPreview(popup, offer.RequestedCard);

        try
        {
            LocString header = new("rest_site_ui", "OPTION_TRADE.requestHeader");

            LocString body = new("rest_site_ui", "OPTION_TRADE.requestBody");
            body.Add("Initiator", GetPlayerName(offer.Initiator));
            //body.Add("Card", GetCardName(offer.RequestedCard));
            //body.Add("Cost", offer.GoldCost);

            LocString decline = new("rest_site_ui", "OPTION_TRADE.decline");
            LocString accept = new("rest_site_ui", "OPTION_TRADE.accept");

            return await popup.WaitForConfirmation(body, header, decline, accept);
        }
        finally
        {
            cardPreviewRoot?.QueueFreeSafely();
        }
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
    
    private const float TradePreviewScale = 0.36f;
    private const float TradePreviewXOffset = -30f;
    private const float TradePreviewYRatio = 0.22f;
    
    private static Control? CreateRequestedCardPreview(Control popup, CardModel card)
    {
        NCard? cardNode = NCard.Create(card);

        if (cardNode == null)
        {
            MainFile.Logger.Warn($"Trade card preview failed: NCard.Create returned null for {card.Title}.");
            return null;
        }

        Control previewRoot = new()
        {
            Name = "CampfireTradeCardPreview",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 1000
        };

        previewRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        popup.AddChildSafely(previewRoot);
        previewRoot.AddChildSafely(cardNode);

        PileType visualPileType = card.Pile?.Type ?? PileType.Deck;
        cardNode.UpdateVisuals(visualPileType, CardPreviewMode.Normal);
        
        cardNode.ZIndex = 1001;
        cardNode.MouseFilter = Control.MouseFilterEnum.Ignore;

        Vector2 popupSize = popup.GetRect().Size;

        cardNode.Scale = Vector2.One * TradePreviewScale;

        cardNode.Position = new Vector2(
            popupSize.X * 0.5f + TradePreviewXOffset,
            popupSize.Y * TradePreviewYRatio
        );

        MainFile.Logger.Info($"Showing trade card preview for {card.Title}.");

        return previewRoot;
    }

    private static string GetPlayerName(Player player)
    {
        try
        {
            if (RunManager.Instance?.NetService?.Platform != null)
            {
                return PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
            }
        }
        catch
        {
            // Fall through to debug-safe fallback.
        }

        return $"Player {player.NetId}";
    }

    private static string GetCardName(CardModel card)
    {
        return card.Title;
    }
}