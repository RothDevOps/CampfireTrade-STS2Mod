using CampfireTrade.CampfireTradeCode.Screens;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace CampfireTrade.CampfireTradeCode.Trading;

public static class TradeFlow
{
    public static async Task<bool> Start(RestSiteOption sourceOption, Player initiator)
    {
        MainFile.Logger.Info($"Trade started by player {initiator.NetId}");
        
        Player? target = await SelectTargetPlayer(sourceOption, initiator);
        MainFile.Logger.Info($"Selected target: {target?.NetId}");
        if (target == null)
        {
            return false;
        }

        CardModel? requestedCard = await SelectCardFromTargetDeck(initiator, target);
        MainFile.Logger.Info($"Selected card: {requestedCard?.Id.Entry}");
        if (requestedCard == null)
        {
            return false;
        }

        TradeOffer offer = new()
        {
            Initiator = initiator,
            Target = target,
            RequestedCard = requestedCard,
            // GoldCost = 0
        };

        // if (!TradeExecutor.CanPayCost(offer))
        // {
        //     MainFile.Logger.Info("Trade canceled: initiator cannot pay cost");
        //     return false;
        // }
        
        bool accepted = await AskTargetToAcceptTrade(offer);
        MainFile.Logger.Info($"Target accepted trade: {accepted}");
        if (!accepted)
        {
            await NotifyInitiatorTradeDeclined(offer);
            return false;
        }

        return await TradeExecutor.Execute(offer);
    }

    private static async Task<Player?> SelectTargetPlayer(RestSiteOption sourceOption, Player initiator)
    {
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(initiator);
        Player? target = null;

        if (LocalContext.IsMe(initiator))
        {
            NRestSiteRoom? restSiteRoom = NRestSiteRoom.Instance;
            restSiteRoom?.AnimateDescriptionDown();
            
            Vector2 startPosition = Vector2.Zero;

            NRestSiteButton? button = restSiteRoom?.GetButtonForOption(sourceOption);
            if (button != null)
            {
                startPosition = button.GlobalPosition + button.Size / 2f;
            }

            NTargetManager targetManager = NTargetManager.Instance;
            targetManager.StartTargeting(
                TargetType.AnyPlayer,
                startPosition,
                TargetMode.ClickMouseToTarget,
                ShouldCancelTargeting,
                node => AllowHoveringNode(initiator, node));

            try
            {
                Node? selectedNode = await targetManager.SelectionFinished();
                target = NodeToPlayer(selectedNode);
                
                RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                    initiator,
                    choiceId,
                    PlayerChoiceResult.FromPlayerId(target?.NetId));
            }
            finally
            {
                restSiteRoom?.AnimateDescriptionUp();
            }
        }
        else
        {
            ulong? targetId =
                (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(initiator, choiceId))
                .AsPlayerId();

            if (targetId.HasValue)
            {
                target = initiator.RunState.GetPlayer(targetId.Value);
            }
        }
        
        return target;
    }

    private static async Task<CardModel?> SelectCardFromTargetDeck(Player initiator, Player target)
    {
        List<CardModel> cards = PileType.Deck
            .GetPile(target)
            .Cards
            .Where(CanTradeCard)
            .ToList();

        if (cards.Count == 0)
        {
            return null;
        }

        CardSelectorPrefs prefs = new(
            new LocString("rest_site_ui", "OPTION_TRADE.selectCard"),
            0,
            1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        List<CardModel> selectedCards;
        
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(initiator);

        if (LocalContext.IsMe(initiator))
        {
            NDeckCardSelectScreen screen = NDeckCardSelectScreen.Create(cards, prefs);
            NOverlayStack.Instance!.Push(screen);
            
            selectedCards = (await screen.CardsSelected()).ToList();

            List<int> selectedIndexes = selectedCards
                .Select(card => cards.IndexOf(card))
                .Where(index => index >= 0)
                .ToList();
            
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                initiator,
                choiceId,
                PlayerChoiceResult.FromIndexes(selectedIndexes));
        }
        else
        {
            selectedCards = 
                (
                    from index in (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(initiator, choiceId)).AsIndexes()
                    where index >= 0 && index < cards.Count
                    select cards[index]
                ).ToList();
        }
        
        return selectedCards.FirstOrDefault();
    }

    private static async Task<bool> AskTargetToAcceptTrade(TradeOffer offer)
    {
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(offer.Target);

        if (LocalContext.IsMe(offer.Target))
        {
            bool accepted = await NTradeRequestPopup.ShowRequestToTarget(offer);
            
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                offer.Target,
                choiceId,
                PlayerChoiceResult.FromIndex(accepted ? 1 : 0));
            
            return accepted;
        }

        int remoteResult = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(offer.Target, choiceId))
            .AsIndex();

        return remoteResult == 1;
    }

    private static bool CanTradeCard(CardModel card)
    {
        // TODO: Maybe expose to config, which card types are allowed
        return card.Type != CardType.Quest;
    }

    private static Player? NodeToPlayer(Node? node)
    {
        if (node == null)
        {
            return null;
        }

        if (node is NMultiplayerPlayerState multiplayerState)
        {
            return multiplayerState.Player;
        }

        if (node is NRestSiteCharacter restSiteCharacter)
        {
            return restSiteCharacter.Player;
        }
        
        return null;
    }

    private static bool AllowHoveringNode(Player initiator, Node node)
    {
        Player? hoveredPlayer = NodeToPlayer(node);
        return hoveredPlayer != null && hoveredPlayer != initiator;
    }

    private static bool ShouldCancelTargeting()
    {
        if (NOverlayStack.Instance != null && NOverlayStack.Instance.ScreenCount > 0)
        {
            return true;
        }

        return NCapstoneContainer.Instance != null && NCapstoneContainer.Instance.InUse;
    }

    private static async Task NotifyInitiatorTradeDeclined(TradeOffer offer)
    {
        if (!LocalContext.IsMe(offer.Initiator))
        {
            return;
        }
        
        await NTradeRequestPopup.ShowDeclinedToInitiator(offer);
    }
}