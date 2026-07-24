using CampfireTrade.CampfireTradeCode.Trading;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace CampfireTrade.CampfireTradeCode.Screens;

public static class NCustomTradeRequestPopup
{
    private const string ScenePath = "res://CampfireTrade/Scenes/trade_request_popup.tscn";
    private const float CardScale = 0.55f;

    public static async Task<bool> ShowRequestToTarget(TradeOffer offer)
    {
        PackedScene? packedScene = ResourceLoader.Load<PackedScene>(ScenePath);

        if (packedScene == null)
        {
            MainFile.Logger.Error($"Could not load trade request popup scene: {ScenePath}");
            return false;
        }

        CanvasLayer scene = packedScene.Instantiate<CanvasLayer>();
        scene.Layer = 100;

        Label headerLabel = scene.GetNode<Label>("%HeaderLabel");
        RichTextLabel bodyLabel = scene.GetNode<RichTextLabel>("%BodyLabel");
        Control cardSlot = scene.GetNode<Control>("%CardSlot");
        Button noButton = scene.GetNode<Button>("%NoButton");
        Button yesButton = scene.GetNode<Button>("%YesButton");

        LocString header = new("rest_site_ui", "OPTION_TRADE.requestHeader");

        LocString body = new("rest_site_ui", "OPTION_TRADE.requestBody");
        body.Add("Initiator", GetPlayerName(offer.Initiator));

        LocString decline = new("rest_site_ui", "OPTION_TRADE.decline");
        LocString accept = new("rest_site_ui", "OPTION_TRADE.accept");

        headerLabel.Text = header.GetFormattedText();
        bodyLabel.Text = body.GetFormattedText();
        noButton.Text = decline.GetFormattedText();
        yesButton.Text = accept.GetFormattedText();

        TaskCompletionSource<bool> result = new();

        noButton.Pressed += () => Complete(scene, result, false);
        yesButton.Pressed += () => Complete(scene, result, true);

        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            scene.QueueFree();
            MainFile.Logger.Error("Cannot show trade request popup: active main loop is not a SceneTree.");
            return false;
        }
        
        Node? oldPopup = sceneTree.Root.GetNodeOrNull("CampfireTradeRequestPopup");

        if (oldPopup != null)
            oldPopup.QueueFree();

        scene.Name = "CampfireTradeRequestPopup";

        sceneTree.Root.AddChild(scene);

        await scene.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);

        AddCardPreview(cardSlot, offer.RequestedCard);

        yesButton.GrabFocus();

        return await result.Task;
    }

    private static void Complete(CanvasLayer scene, TaskCompletionSource<bool> result, bool accepted)
    {
        result.TrySetResult(accepted);

        if (GodotObject.IsInstanceValid(scene))
            scene.QueueFree();
    }

    private static void AddCardPreview(Control cardSlot, CardModel card)
    {
        NCard? cardNode = NCard.Create(card);

        if (cardNode == null)
        {
            MainFile.Logger.Warn($"Trade popup failed to create card preview for {card.Title}.");
            return;
        }

        cardSlot.AddChildSafely(cardNode);

        PileType visualPileType = card.Pile?.Type ?? PileType.Deck;
        cardNode.UpdateVisuals(visualPileType, CardPreviewMode.Normal);

        cardNode.Scale = Vector2.One * CardScale;
        cardNode.MouseFilter = Control.MouseFilterEnum.Ignore;

        Vector2 slotSize = cardSlot.GetRect().Size;

        cardNode.Position = new Vector2(
            slotSize.X * 0.5f,
            slotSize.Y * 0.5f
        );

        MainFile.Logger.Info($"Showing trade request popup card preview for {card.Title}.");
    }

    private static string GetPlayerName(Player player)
    {
        try
        {
            if (RunManager.Instance?.NetService?.Platform != null)
            {
                return PlatformUtil.GetPlayerName(
                    RunManager.Instance.NetService.Platform,
                    player.NetId);
            }
        }
        catch
        {
            // Debug/singleplayer fallback.
        }

        return $"Player {player.NetId}";
    }
}